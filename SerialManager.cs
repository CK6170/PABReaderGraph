using PABReaderGraph;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Timers;

public class SerialManager
{
    enum PabOpCode : ushort
    {
        GetData = 0x07,
        GetVersion = 0x15,
        GetFactors = 0x16,
        KeepAlive = 0x1C
    }
    enum PabPort : ushort
    {
        None = 0x00,
        All = 0xFF
    }
    public event Action CalibrationCompleted;
    public string SessionFolder { get; private set; }
    
    private static SerialManager? instance;
    public static SerialManager Instance => instance ??= new SerialManager();
    public bool IsCalibrationComplete => calibrationComplete;
    public DateTime CalibrationStartTime => calibrationStartTime;
    public int CalibrationSeconds => calibrationSeconds;

    private SerialPort? serialPort;
    private System.Timers.Timer? keepAliveTimer;
    public delegate void DataReceivedHandler(int id, double time, double[] values, int recordNumber, uint[] adcValues);
    public event DataReceivedHandler OnDataReceived;
    private Settings settings;
    private Thread readThread;
    private bool running = false;
    private Dictionary<ushort, (double totalFactor, double[] factors)> portFactors = new();
    private Dictionary<ushort, uint[]> zeroOffsets = new();
    private Dictionary<ushort, List<uint[]>> zeroCalibrationData = new();
    private Dictionary<ushort, uint[]> lastReadings = new Dictionary<ushort, uint[]>();
    private Dictionary<ushort, int> recordCounters = new Dictionary<ushort, int>();
    private bool calibrationComplete = false;
    private DateTime calibrationStartTime;
    private int calibrationSeconds;
    private DateTime calibrationEndTime;
    private Dictionary<ushort, List<uint[]>> manualZeroCalibrationData = new();
    private Dictionary<ushort, DateTime> manualZeroStartTimes = new();
    private Dictionary<ushort, GraphForm> graphForms = new();    
    private SerialManager()
    {
    }
    public void Initialize(Settings settings)
    {
        this.settings = settings;
        serialPort = TryConnectToPAB();        

        if (serialPort == null)
            throw new Exception("Could not find PAB device!");

        calibrationComplete = false;
        calibrationEndTime = DateTime.MinValue;

        zeroOffsets.Clear();
        zeroCalibrationData.Clear();
        recordCounters.Clear();

        keepAliveTimer = new System.Timers.Timer(45000); // 45 seconds
        keepAliveTimer.Elapsed += KeepAliveTimer_Elapsed;
        KeepAliveTimer_Elapsed(null, null);
        keepAliveTimer.Start();
        
        SendCommand(PabPort.None, PabOpCode.GetData);
        ReadFactors();
        this.calibrationSeconds = settings.CalibrationSeconds;
        this.calibrationStartTime = DateTime.Now;
        SendCommand(PabPort.All, PabOpCode.GetData);
        
        running = true;
        readThread = new Thread(ReadLoop);
        readThread.Start();
    }
    public List<int> GetAvailableIDs()
    {
        return portFactors.Keys
            .Where(id => (settings.PabPortSerialNumber & (1 << id)) != 0)
            .Select(id => (int)id)
            .ToList();
    }
    public void StartSessionFolder()
    {
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        SessionFolder = Path.Combine(settings.BaseFolder, $"results_{timestamp}");
        Directory.CreateDirectory(SessionFolder);
    }
    public void Close()
    {
        running = false;

        keepAliveTimer?.Stop();
        keepAliveTimer?.Dispose();
        keepAliveTimer = null;

        if (serialPort != null)
        {
            try
            {
                if (serialPort.IsOpen)
                {
                    SendCommand(PabPort.None, PabOpCode.GetData);
                    serialPort.Close();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error closing serial port: " + ex.Message);
            }
            finally
            {
                serialPort.Dispose();
                serialPort = null;
            }
        }
    }
    public void StartManualZeroCalibration(ushort id)
    {
        manualZeroCalibrationData[id] = new List<uint[]>();
        manualZeroStartTimes[id] = DateTime.Now;
    }
    public void RegisterGraphForm(ushort id, GraphForm form)
    {
        graphForms[id] = form;
    }

    public (double totalFactor, double[] factors) GetFactors(ushort id)
    {
        return portFactors.ContainsKey(id) ? portFactors[id] : (1.0, new double[4] { 1.0, 1.0, 1.0, 1.0 });
    }

    public uint[] GetZeroOffsets(ushort id)
    {
        return zeroOffsets.ContainsKey(id) ? zeroOffsets[id] : new uint[4];
    }
    private void KeepAliveTimer_Elapsed(object? sender, ElapsedEventArgs? e)
    {
        SendCommand(PabPort.None, PabOpCode.KeepAlive);
    }
    private SerialPort? TryConnectToPAB()
    {
        var probeCommand = BuildOpcodeCommand((ushort)PabPort.None, (ushort)PabOpCode.GetVersion);
        // Try LastPort first if exists
        if (!string.IsNullOrEmpty(settings.LastPort))
        {
            try
            {
                SerialPort port = new SerialPort(settings.LastPort, 115200)
                {
                    ReadTimeout = 1000,
                    WriteTimeout = 1000
                };
                port.Open();
                port.Write(probeCommand, 0, probeCommand.Length);

                Thread.Sleep(100);
                string response = port.ReadExisting();
                if (response.Contains(settings.PabVersion))
                {
                    Debug.WriteLine($"Connected to PAB on {settings.LastPort} (last known port).");
                    return port;
                }
                port.Close();
            }
            catch
            {
                // LastPort failed — fallback to scanning
            }
        }
        // Full scan if LastPort failed
        foreach (string portName in SerialPort.GetPortNames())
        {
            try
            {
                SerialPort port = new SerialPort(portName, 115200)
                {
                    ReadTimeout = 1000,
                    WriteTimeout = 1000
                };
                port.Open();
                port.Write(probeCommand, 0, probeCommand.Length);

                Thread.Sleep(100);
                string response = port.ReadExisting();
                if (response.Contains(settings.PabVersion))
                {
                    Debug.WriteLine($"Connected to PAB on {portName} (scanned).");
                    // ✅ Update LastPort in settings
                    settings.LastPort = portName;
                    SaveSettings();  
                    return port;
                }
                port.Close();
            }
            catch { }
        }
        return null;
    }
    public void SaveSettings()
    {
        try
        {
            var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
            System.IO.File.WriteAllText("settings.json", System.Text.Json.JsonSerializer.Serialize(settings, options));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to save settings: {ex.Message}");
        }
    }
    private void ReadFactors()
    {
        const int maxRetries = 3;
        int attempt = 0;
        bool factorsValid = false;

        while (attempt < maxRetries && !factorsValid)
        {
            attempt++;
            Debug.WriteLine($"Reading factors - Attempt {attempt}/{maxRetries}");

            serialPort?.DiscardInBuffer();
            SendCommand(PabPort.All, PabOpCode.GetFactors);

            Thread.Sleep(500);

            var tempPortFactors = new Dictionary<ushort, (double totalFactor, double[] factors)>();
            bool readingSuccess = false;

            while (serialPort?.BytesToRead >= 48)
            {
                try
                {
                    byte[] buffer = new byte[48];
                    int read = serialPort.Read(buffer, 0, 48);
                    if (read != 48)
                        throw new Exception("Wrong Record Length."); 

                    if (buffer[0] != 0x53 || buffer[1] != 0x4B)
                        throw new Exception("Wrong Initial");

                    ushort id = BitConverter.ToUInt16(buffer, 12);

                    if ((settings.PabPortSerialNumber & (1 << id)) == 0)
                        continue;

                    Span<byte> factorSpan = buffer.AsSpan(24, 24);
                    List<float> decoded = new();
                    byte[] floatBytes = new byte[4];
                    for (int i = 0; i < factorSpan.Length; i += 4)
                    {
                        factorSpan.Slice(i, 4).CopyTo(floatBytes);
                        Array.Reverse(floatBytes);
                        decoded.Add(BitConverter.ToSingle(floatBytes));
                    }
                    double totalFactor = decoded[0];
                    double[] factors = decoded.Skip(1).Take(4).Select(x => (double)x).ToArray();

                    tempPortFactors[id] = (totalFactor, factors);
                    readingSuccess = true;
                }
                catch
                {
                    serialPort?.DiscardInBuffer();
                    Debug.WriteLine("Error reading factors. Retrying...");
                    break; // Exit the while loop and retry from the beginning
                }
            }

            // Validate factors only if reading was successful
            if (readingSuccess && tempPortFactors.Count > 0)
            {
                bool allFactorsValid = true;

                foreach (var kvp in tempPortFactors)
                {
                    ushort id = kvp.Key;
                    var (totalFactor, factors) = kvp.Value;

                    // Check if totalFactor or any individual factor is greater than 1
                    if (totalFactor > 1.0)
                    {
                        Debug.WriteLine($"Invalid totalFactor for Port {id + 1}: {totalFactor} (should be <= 1)");
                        allFactorsValid = false;
                        break;
                    }

                    for (int i = 0; i < factors.Length; i++)
                    {
                        if (factors[i] > 1.0)
                        {
                            Debug.WriteLine($"Invalid factor[{i}] for Port {id + 1}: {factors[i]} (should be <= 1)");
                            allFactorsValid = false;
                            break;
                        }
                    }

                    if (!allFactorsValid) break;
                }

                if (allFactorsValid)
                {
                    // All factors are valid, store them
                    foreach (var kvp in tempPortFactors)
                    {
                        portFactors[kvp.Key] = kvp.Value;
                    }
                    factorsValid = true;
                    Debug.WriteLine("All factors validated successfully.");
                }
                else
                {
                    Debug.WriteLine($"Factor validation failed on attempt {attempt}");
                    if (attempt < maxRetries)
                    {
                        Thread.Sleep(1000); // Wait before retry
                    }
                }
            }
            else
            {
                Debug.WriteLine($"Failed to read factors on attempt {attempt}");
                if (attempt < maxRetries)
                {
                    Thread.Sleep(1000); // Wait before retry
                }
            }
        }

        if (!factorsValid)
        {
            string errorMessage = $"Factor validation failed after {maxRetries} attempts. One or more factors are greater than 1. Please check the PAB device configuration.";
            Debug.WriteLine(errorMessage);

            // Show error message to user
            AutoClosingMessage.Show(
                errorMessage,
                1000);

            // Throw exception to prevent streaming data
            throw new Exception("Invalid factors detected - streaming data aborted for safety.");
        }
    }
    private void ReadLoop()
    {
        while (running)
        {
            try
            {
                if (serialPort?.BytesToRead >= 68)
                {
                    byte[] buffer = new byte[68];
                    int read = serialPort.Read(buffer, 0, 68);
                    if (read != 68)
                        throw new Exception("Wrong Record Length.");

                    if (buffer[0] != 0x53 || buffer[1] != 0x4B)
                        throw new Exception("Wrong Initial");


                    ushort id = BitConverter.ToUInt16(buffer, 12);

                    if ((settings.PabPortSerialNumber & (1 << id)) == 0)
                        continue;

                    // Find the end of data marked by 0x0D 0x0A sequence
                    int endIndex = -1;
                    for (int i = 22; i < buffer.Length - 1; i++)
                    {
                        if (buffer[i] == 0x0D && buffer[i + 1] == 0x0A)
                        {
                            endIndex = i;
                            break;
                        }
                    }

                    if (endIndex == -1)
                        throw new Exception("Could not find end marker (0x0D 0x0A) in buffer.");

                    Span<byte> inputSpan = buffer.AsSpan(22, endIndex - 22);
                    ushort crc = CalcCRC16(inputSpan.ToArray());
                    if (crc != 0)
                        throw new Exception("Wrong CRC.");

                    string line = Encoding.ASCII.GetString(inputSpan);
                    var parts = line.Split('|');
                    if (parts.Length < 5)
                        throw new Exception("Wrong Data."); 

                    uint[] adcValues = new uint[4];
                    for (int i = 1; i <= 4; i++)
                    {
                        Match match = Regex.Match(parts[i], @"\d{1,9}");
                        if (match.Success)

                        {
                            adcValues[i - 1] = uint.Parse(match.Value); 
                        }
                        else
                        {
                            throw new Exception("Parsing Error.");
                        }
                    }
                    lastReadings[id] = adcValues.Select(value => (uint)value).ToArray(); ; // Store latest reading per port

                    // Calibration Phase: Collect data for zeroing
                    if (!calibrationComplete)
                    {
                        if (!zeroCalibrationData.ContainsKey(id))
                            zeroCalibrationData[id] = new List<uint[]>();

                        zeroCalibrationData[id].Add(adcValues);

                        if ((DateTime.Now - calibrationStartTime).TotalSeconds >= calibrationSeconds)
                        {
                            foreach (var kvp in zeroCalibrationData)
                            {
                                var avg = new ulong[4];
                                foreach (var row in kvp.Value)
                                {
                                    for (int i = 0; i < 4; i++)
                                        avg[i] += row[i];
                                }
                                zeroOffsets[kvp.Key] = new uint[4];
                                for (int i = 0; i < 4; i++)
                                {
                                    avg[i] /= (ulong)kvp.Value.Count;
                                    zeroOffsets[kvp.Key][i] = (uint)avg[i];
                                }
                            }
                            calibrationComplete = true;
                            calibrationEndTime = DateTime.Now;
                            StartSessionFolder();
                            CalibrationCompleted?.Invoke();

                            Debug.WriteLine("Zero Calibration Completed.");
                        }
                    }
                    else
                    {
                        if (manualZeroCalibrationData.ContainsKey(id))
                        {
                            manualZeroCalibrationData[id].Add(adcValues);

                            if ((DateTime.Now - manualZeroStartTimes[id]).TotalSeconds >= calibrationSeconds)
                            {
                                CompleteManualZeroCalibration(id);
                            }
                        }
                        uint[] zeros = zeroOffsets.ContainsKey(id) ? zeroOffsets[id] : new uint[4];
                        var (totalFactor, factors) = portFactors.ContainsKey(id) ? portFactors[id] : (1.0, new double[4] { 1.0, 1.0, 1.0, 1.0 });

                        double[] weights = new double[4];
                        for (int i = 0; i < 4; i++)
                        {
                            weights[i] = (int)(adcValues[i] - zeros[i]) * factors[i] * totalFactor;
                        }
                        double now = (DateTime.Now - calibrationEndTime).TotalSeconds;

                        // Increment record counter for this port
                        if (!recordCounters.ContainsKey(id))
                            recordCounters[id] = 0;
                        recordCounters[id]++;

                        //Debug.WriteLine($"| {id + 1} | {(int)weights[0],9} | {(int)weights[1],9} | {(int)weights[2],9} | {(int)weights[3],9} |");
                        OnDataReceived?.Invoke(id, now, weights, recordCounters[id], adcValues);
                    }
                }
                else
                {
                    Thread.Sleep(5);
                }
            }
            catch
            {
                serialPort?.DiscardInBuffer();
            }
        }
    }
    private void CompleteManualZeroCalibration(ushort id)
    {
        if (!manualZeroCalibrationData.ContainsKey(id) || manualZeroCalibrationData[id].Count == 0)
            return;

        int count = manualZeroCalibrationData[id].Count;
        int numCells = manualZeroCalibrationData[id][0].Length;

        ulong[] avg = new ulong[numCells];
        foreach (var row in manualZeroCalibrationData[id])
        {
            for (int i = 0; i < numCells; i++)
            {
                avg[i] += row[i];
            }
        }

        uint[] newZero = new uint[numCells];
        for (int i = 0; i < numCells; i++)
        {
            avg[i] /= (ulong)count;
            newZero[i] = (uint)avg[i];
        }
        zeroOffsets[id] = newZero;
        // Clear calibration data
        manualZeroCalibrationData.Remove(id);
        manualZeroStartTimes.Remove(id);

        Debug.WriteLine($"Manual zero calibration complete for Port {id + 1}.");
        double now = (DateTime.Now - calibrationEndTime).TotalSeconds;
        if (graphForms.ContainsKey(id))
        {
            graphForms[id].MarkZero(now);
        }
    }
    private void SendCommand(PabPort pabPortSerialNumber, PabOpCode opCode)
    {
        var command = BuildOpcodeCommand((ushort)pabPortSerialNumber, (ushort)opCode);
        serialPort?.Write(command, 0, command.Length);
    }
    private byte[] BuildOpcodeCommand(ushort pabPortSerialNumber, ushort opCode,
       ushort sequenceNumber = 0, uint timestamp = 0, uint leopardSequenceNumber = 0)
    {
        byte[] command = [(byte)'S', (byte)'K', 0, 0, 0, 0, 0, 0,
            ..BitConverter.GetBytes(sequenceNumber),
            ..BitConverter.GetBytes(opCode),
            ..BitConverter.GetBytes(pabPortSerialNumber),
            ..BitConverter.GetBytes(timestamp),
            ..BitConverter.GetBytes(leopardSequenceNumber)];

        ushort length = (ushort)command.Length;
        command[2] = (byte)(length & 0xFF);
        command[3] = (byte)((length >> 8) & 0xFF);

        ushort checksum = CalculateChecksum(command);
        command[6] = (byte)(checksum & 0xFF);
        command[7] = (byte)((checksum >> 8) & 0xFF);

        return command;
    }
    private ushort CalcCRC16(byte[] data)
    {
        ushort cs = 0;
        foreach (byte b in data)
        {
            cs ^= (ushort)(b << 8);
            for (int i = 0; i < 8; i++)
            {
                ushort carry = (ushort)(cs & 0x8000);
                if (carry != 0)
                {
                    carry = 1;
                    cs ^= 0x8810;
                }
                cs = (ushort)((cs << 1) + carry);
            }
        }
        return cs;
    }
    private ushort CalculateChecksum(byte[] data)
    {
        int sum = 0;
        for (int i = 0; i < data.Length; i++)
        {
            sum += data[i];
        }
        return (ushort)(~sum);
    }
}
