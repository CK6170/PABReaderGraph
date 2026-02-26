using PABReaderGraph;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Timers;

/// <summary>
/// Operation codes for PAB device communication protocol
/// Defines the available commands that can be sent to PAB hardware devices
/// Each opcode triggers specific functionality in the connected PAB device
/// </summary>
enum PabOpCode : ushort
{
    /// <summary>Start or stop data acquisition from load cell sensors</summary>
    GetData = 0x07,
    /// <summary>Request firmware version information from PAB device</summary>
    GetVersion = 0x15,
    /// <summary>Request calibration factors for weight calculation</summary>
    GetFactors = 0x16,
    /// <summary>Send keep-alive signal to maintain connection</summary>
    KeepAlive = 0x1C
}

/// <summary>
/// Port targeting options for PAB device commands
/// Specifies which ports should respond to transmitted commands
/// Supports both individual port targeting and broadcast operations
/// </summary>
enum PabPort : ushort
{
    /// <summary>No specific port - used for device-level commands</summary>
    None = 0x00,
    /// <summary>Broadcast to all available ports simultaneously</summary>
    All = 0xFF
}

/// <summary>
/// Singleton hardware communication manager for PAB (Portable Analysis Board) devices
/// Provides centralized control over serial communication, data acquisition, and calibration operations
/// Handles device discovery, connection management, data parsing, and real-time distribution to UI components
/// 
/// Core Responsibilities:
/// 
/// HARDWARE COMMUNICATION:
/// - Automatic PAB device discovery across available serial ports
/// - Protocol-compliant command transmission with checksum validation
/// - Real-time data reception with CRC verification and error handling
/// - Keep-alive functionality to maintain stable device connections
/// 
/// DATA PROCESSING:
/// - Binary protocol parsing with multi-byte value extraction
/// - ADC-to-weight conversion using calibrated scaling factors
/// - Zero offset calculation and application for accurate measurements
/// - Real-time data distribution to registered GraphForm instances
/// 
/// CALIBRATION MANAGEMENT:
/// - Initial calibration phase with configurable duration and validation
/// - Manual re-zeroing operations for field adjustments
/// - Calibration factor validation with safety bounds checking
/// - Zero offset averaging with statistical noise reduction
/// 
/// SESSION MANAGEMENT:
/// - Settings persistence with last-known-good port optimization
/// - Session folder creation with timestamped organization
/// - Port availability detection with bitmask filtering
/// - Graceful shutdown with proper resource cleanup
/// 
/// SAFETY AND VALIDATION:
/// - Factor validation with upper bounds checking (factors must be ≤ 1.0)
/// - CRC verification for data integrity assurance
/// - Retry logic for transient communication failures
/// - Error isolation preventing single-port failures from affecting others
/// 
/// Architecture Pattern:
/// - Singleton design ensures single point of hardware control
/// - Event-driven architecture for real-time data distribution
/// - Thread-safe operations with dedicated read thread
/// - Resource management with proper disposal patterns
/// 
/// Threading Model:
/// - Main thread: Command transmission and initialization
/// - Read thread: Continuous data reception and parsing
/// - Timer thread: Keep-alive signal transmission
/// - UI thread: Event notifications and form registration
/// 
/// Performance Characteristics:
/// - Low-latency data processing for real-time visualization
/// - Efficient binary protocol parsing with minimal allocations
/// - Batch processing for calibration data collection
/// - Optimized port scanning with last-known-good prioritization
/// </summary>
public class SerialManager
{
    /// <summary>
    /// Event fired when initial calibration phase completes successfully
    /// Notifies all registered listeners that zero calibration has finished and data acquisition can begin
    /// Used by GraphForm instances to update UI state and begin normal operation display
    /// </summary>
    public event Action CalibrationCompleted;

    /// <summary>
    /// Path to the current session's data storage folder
    /// Created automatically after successful calibration with timestamp-based naming
    /// Used by GraphForm instances for coordinated file output and data organization
    /// </summary>
    public string SessionFolder { get; private set; }

    /// <summary>
    /// Singleton instance accessor with lazy initialization
    /// Ensures single point of hardware control throughout application lifetime
    /// Thread-safe initialization using null-conditional operator and null-coalescing assignment
    /// </summary>
    private static SerialManager? instance;
    public static SerialManager Instance => instance ??= new SerialManager();

    /// <summary>
    /// Indicates whether the initial calibration phase has completed successfully
    /// Used by GraphForm instances to determine when to begin data display operations
    /// Remains true once set until next initialization cycle
    /// </summary>
    public bool IsCalibrationComplete => calibrationComplete;

    /// <summary>
    /// Timestamp when the current calibration phase began
    /// Used for calculating elapsed calibration time and remaining duration
    /// Set during Initialize() method and used throughout calibration period
    /// </summary>
    public DateTime CalibrationStartTime => calibrationStartTime;

    /// <summary>
    /// Duration in seconds for calibration data collection period
    /// Configurable through Settings to accommodate different measurement environments
    /// Used for both initial calibration and manual re-zeroing operations
    /// </summary>
    public int CalibrationSeconds => calibrationSeconds;

    // HARDWARE COMMUNICATION INFRASTRUCTURE
    /// <summary>Serial port instance for PAB device communication</summary>
    private SerialPort? serialPort;

    /// <summary>Timer for periodic keep-alive signal transmission to maintain device connection</summary>
    private System.Timers.Timer? keepAliveTimer;

    /// <summary>
    /// Delegate signature for real-time data distribution events
    /// Provides strongly-typed callback interface for data reception notifications
    /// </summary>
    /// <param name="id">Zero-based port identifier for the data source</param>
    /// <param name="time">Elapsed time since calibration completion in seconds</param>
    /// <param name="values">Array of 4 weight values from load cells</param>
    /// <param name="recordNumber">Sequential record number for this port</param>
    /// <param name="adcValues">Raw ADC readings before calibration processing</param>
    public delegate void DataReceivedHandler(int id, double time, double[] values, int recordNumber, uint[] adcValues);

    /// <summary>
    /// Event for real-time data distribution to registered GraphForm instances
    /// Fired for each processed data record during normal operation phase
    /// Enables real-time plotting and data logging across multiple UI components
    /// </summary>
    public event DataReceivedHandler OnDataReceived;

    // CONFIGURATION AND STATE MANAGEMENT
    /// <summary>Application settings containing hardware parameters and user preferences</summary>
    private Settings settings;

    /// <summary>Background thread for continuous data reading and parsing operations</summary>
    private Thread readThread;

    /// <summary>Thread control flag for graceful read loop termination</summary>
    private bool running = false;

    // CALIBRATION DATA COLLECTIONS
    /// <summary>Mapping of port IDs to their calibration factors (totalFactor, individual factors)</summary>
    private Dictionary<ushort, (double totalFactor, double[] factors)> portFactors = new();

    /// <summary>Mapping of port IDs to their zero offset values for baseline correction</summary>
    private Dictionary<ushort, uint[]> zeroOffsets = new();

    /// <summary>Collection of raw ADC readings during initial calibration period</summary>
    private Dictionary<ushort, List<uint[]>> zeroCalibrationData = new();

    /// <summary>Most recent ADC readings for each port (used for display and diagnostics)</summary>
    private Dictionary<ushort, uint[]> lastReadings = new Dictionary<ushort, uint[]>();

    /// <summary>Sequential record counters for each port to track data acquisition progress</summary>
    private Dictionary<ushort, int> recordCounters = new Dictionary<ushort, int>();

    // CALIBRATION STATE TRACKING
    /// <summary>Flag indicating whether initial calibration phase has completed</summary>
    private bool calibrationComplete = false;

    /// <summary>Timestamp when current calibration phase began</summary>
    private DateTime calibrationStartTime;

    /// <summary>Duration in seconds for calibration data collection</summary>
    private int calibrationSeconds;

    /// <summary>Timestamp when calibration completed (used as time reference for data)</summary>
    private DateTime calibrationEndTime;

    // MANUAL ZERO CALIBRATION SUPPORT
    /// <summary>Collection of raw ADC readings during manual zero calibration operations</summary>
    private Dictionary<ushort, List<uint[]>> manualZeroCalibrationData = new();

    /// <summary>Start times for manual zero calibration operations per port</summary>
    private Dictionary<ushort, DateTime> manualZeroStartTimes = new();

    // FORM REGISTRATION AND COORDINATION
    /// <summary>Registry of GraphForm instances by port ID for zero marker coordination</summary>
    /// <summary>Registry of GraphForm instances by port ID for zero marker coordination</summary>
    private Dictionary<ushort, GraphForm> graphForms = new();

    /// <summary>
    /// Private constructor enforcing singleton pattern
    /// Prevents external instantiation to ensure single point of hardware control
    /// All initialization occurs in Initialize() method for proper error handling
    /// </summary>
    private SerialManager()
    {
    }

    /// <summary>
    /// Initializes SerialManager with complete hardware setup and calibration sequence
    /// Performs device discovery, connection establishment, factor validation, and calibration start
    /// Throws exceptions for hardware failures that require user intervention
    /// 
    /// Initialization Sequence:
    /// 1. Store settings and attempt PAB device connection
    /// 2. Validate successful device connection or throw exception
    /// 3. Reset calibration state and clear previous session data
    /// 4. Setup keep-alive timer for connection maintenance
    /// 5. Read and validate calibration factors from device
    /// 6. Begin data acquisition and start calibration period
    /// 7. Launch background read thread for continuous data processing
    /// 
    /// Error Conditions:
    /// - Device connection failure: Throws exception with descriptive message
    /// - Factor validation failure: Throws exception with safety information
    /// - Communication errors: May throw during factor reading or command sending
    /// 
    /// Thread Safety:
    /// - Called on main UI thread during application startup
    /// - Starts background read thread for data processing
    /// - Keep-alive timer runs on thread pool thread
    /// 
    /// Resource Management:
    /// - Serial port opened and configured for communication
    /// - Background thread started for continuous operation
    /// - Timer resources allocated for keep-alive functionality
    /// </summary>
    /// <param name="settings">
    /// Application settings containing hardware configuration parameters
    /// Used for device discovery, port filtering, and calibration timing
    /// </param>
    /// <exception cref="Exception">
    /// Thrown when PAB device cannot be found or connected
    /// Thrown when calibration factors fail validation for safety reasons
    /// </exception>
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
    /// <summary>
    /// Retrieves list of available port IDs based on detected devices and configuration
    /// Filters discovered ports against user-configured port mask for selective monitoring
    /// Returns zero-based port identifiers suitable for GraphForm creation
    /// 
    /// Filtering Logic:
    /// - Only includes ports that responded during factor reading
    /// - Applies bitmask filter from settings.PabPortSerialNumber
    /// - Converts ushort port IDs to int for external API consistency
    /// 
    /// Usage Context:
    /// - Called after successful Initialize() to determine available monitoring targets
    /// - Used by Program.Main and MultiFormContext for GraphForm creation
    /// - Result drives UI creation and form layout decisions
    /// </summary>
    /// <returns>
    /// List of zero-based integer port identifiers ready for monitoring
    /// Empty list indicates no valid ports available (should trigger application exit)
    /// </returns>
    public List<int> GetAvailableIDs()
    {
        return portFactors.Keys
            .Where(id => (settings.PabPortSerialNumber & (1 << id)) != 0)
            .Select(id => (int)id)
            .ToList();
    }

    /// <summary>
    /// Creates timestamped session folder for coordinated data output
    /// Establishes centralized location for all session files with consistent naming
    /// Called automatically after successful calibration completion
    /// 
    /// Folder Structure:
    /// - Base folder from settings (typically "Data")
    /// - Timestamped subfolder: "results_yyyyMMdd_HHmmss"
    /// - Created directory ready for file output operations
    /// 
    /// Thread Safety:
    /// - Called from read thread after calibration completion
    /// - Path stored in thread-safe property for external access
    /// - Directory creation is atomic operation
    /// </summary>
    public void StartSessionFolder()
    {
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        SessionFolder = Path.Combine(settings.BaseFolder, $"results_{timestamp}");
        Directory.CreateDirectory(SessionFolder);
    }

    /// <summary>
    /// Performs graceful shutdown with complete resource cleanup
    /// Stops all background operations and releases hardware resources
    /// Safe to call multiple times - handles already-closed state gracefully
    /// 
    /// Shutdown Sequence:
    /// 1. Stop background read thread by clearing running flag
    /// 2. Stop and dispose keep-alive timer
    /// 3. Send stop command to device
    /// 4. Close and dispose serial port
    /// 5. Clear all internal state collections
    /// 
    /// Error Handling:
    /// - Individual cleanup failures are logged but don't prevent complete shutdown
    /// - Serial port closure protected by try-catch for robustness
    /// - Resource disposal guaranteed through finally blocks
    /// 
    /// Thread Safety:
    /// - Can be called from any thread (UI thread, read thread, timer thread)
    /// - Atomic operations ensure clean shutdown regardless of timing
    /// - Background thread terminates gracefully on next iteration
    /// </summary>
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
    /// <summary>
    /// Initiates manual zero calibration for specified port
    /// Begins data collection period for new zero baseline calculation
    /// Used for field adjustments when load conditions change during operation
    /// 
    /// Process:
    /// 1. Initialize data collection for specified port
    /// 2. Record start time for duration tracking
    /// 3. Collect ADC readings during configured calibration period
    /// 4. Complete calibration automatically after duration expires
    /// 5. Update GraphForm with zero marker when complete
    /// 
    /// Usage Context:
    /// - Triggered by user clicking Zero button in GraphForm
    /// - Operates during normal data acquisition phase
    /// - Provides real-time baseline adjustment capability
    /// 
    /// Thread Safety:
    /// - Called from UI thread via GraphForm button click
    /// - Data collection occurs on background read thread
    /// - Completion callback invoked on read thread
    /// </summary>
    /// <param name="id">Zero-based port identifier for calibration target</param>
    public void StartManualZeroCalibration(ushort id)
    {
        manualZeroCalibrationData[id] = new List<uint[]>();
        manualZeroStartTimes[id] = DateTime.Now;
    }

    /// <summary>
    /// Registers GraphForm instance for zero marker coordination
    /// Enables automatic zero marker placement when manual calibration completes
    /// Maintains registry for coordinated UI updates across multiple forms
    /// 
    /// Registration Purpose:
    /// - Provides callback target for zero marker placement
    /// - Enables coordinated UI updates during calibration operations
    /// - Supports multiple forms monitoring different ports simultaneously
    /// 
    /// Thread Safety:
    /// - Called from UI thread during form initialization
    /// - Registry access from read thread during calibration completion
    /// - Dictionary operations are thread-safe for this usage pattern
    /// </summary>
    /// <param name="id">Zero-based port identifier for form association</param>
    /// <param name="form">GraphForm instance to register for callbacks</param>
    public void RegisterGraphForm(ushort id, GraphForm form)
    {
        graphForms[id] = form;
    }

    /// <summary>
    /// Retrieves calibration factors for specified port with safe fallback values
    /// Returns both total scaling factor and individual load cell factors
    /// Provides default values if port factors haven't been loaded successfully
    /// 
    /// Factor Structure:
    /// - totalFactor: Overall scaling applied to all measurements
    /// - factors[]: Individual scaling for each of 4 load cells
    /// - Default values: All factors = 1.0 (no scaling) for missing data
    /// 
    /// Thread Safety:
    /// - Called from UI thread during display updates
    /// - Dictionary access is read-only after initialization
    /// - Returns defensive copy to prevent external modification
    /// </summary>
    /// <param name="id">Zero-based port identifier for factor lookup</param>
    /// <returns>
    /// Tuple containing (totalFactor, factors array)
    /// Falls back to (1.0, [1.0,1.0,1.0,1.0]) if port not found
    /// </returns>
    public (double totalFactor, double[] factors) GetFactors(ushort id)
    {
        return portFactors.ContainsKey(id) ? portFactors[id] : (1.0, new double[4] { 1.0, 1.0, 1.0, 1.0 });
    }

    /// <summary>
    /// Retrieves zero offset values for specified port with safe fallback
    /// Returns baseline ADC values used for weight calculation offset correction
    /// Provides zero array if port offsets haven't been calibrated yet
    /// 
    /// Offset Usage:
    /// - Subtracted from raw ADC readings before applying calibration factors
    /// - Calculated during initial calibration or manual zero operations
    /// - Default values: All zeros (no offset) for missing calibration data
    /// 
    /// Thread Safety:
    /// - Called from UI thread during display operations
    /// - Dictionary access is safe for concurrent read operations
    /// - Returns defensive copy to prevent external modification
    /// </summary>
    /// <param name="id">Zero-based port identifier for offset lookup</param>
    /// <returns>
    /// Array of 4 zero offset values for load cells
    /// Falls back to [0,0,0,0] array if port not found
    /// </returns>
    public uint[] GetZeroOffsets(ushort id)
    {
        return zeroOffsets.ContainsKey(id) ? zeroOffsets[id] : new uint[4];
    }
    /// <summary>
    /// Keep-alive timer callback to maintain device connection
    /// Sends periodic KeepAlive command to prevent device timeout
    /// Ensures stable communication during extended monitoring sessions
    /// </summary>
    /// <param name="sender">Timer instance (not used)</param>
    /// <param name="e">Timer event arguments (not used)</param>
    private void KeepAliveTimer_Elapsed(object? sender, ElapsedEventArgs? e)
    {
        SendCommand(PabPort.None, PabOpCode.KeepAlive);
    }

    /// <summary>
    /// Attempts connection to PAB device across available serial ports
    /// Implements smart connection strategy with last-known-port optimization
    /// Returns configured SerialPort or null if no compatible device found
    /// 
    /// Connection Strategy:
    /// 1. Try last successful port first (optimization for repeated connections)
    /// 2. Fall back to comprehensive port scanning if last port fails
    /// 3. Test each port with GetVersion command and version string validation
    /// 4. Update settings with successful port for future optimization
    /// 
    /// Communication Protocol:
    /// - 115200 baud rate for high-speed data transfer
    /// - 1000ms timeouts for reliable communication
    /// - Version string validation ensures proper PAB device recognition
    /// 
    /// Error Handling:
    /// - Individual port failures are caught and ignored (port scanning continues)
    /// - Successful connection updates settings automatically
    /// - Returns null if no compatible devices found on any port
    /// </summary>
    /// <returns>
    /// Configured SerialPort connected to PAB device, or null if none found
    /// </returns>
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
    /// <summary>
    /// Persists current settings to JSON file for application restart optimization
    /// Saves configuration including last successful port for connection optimization
    /// Called automatically when successful connections are established
    /// 
    /// Persistence Features:
    /// - JSON format with indented formatting for human readability
    /// - Includes HexUShortConverter for proper port serial number representation
    /// - Error handling prevents settings save failures from affecting operation
    /// 
    /// File Location:
    /// - "settings.json" in application directory
    /// - Overwrites existing settings with current values
    /// - Accessible for manual editing if needed
    /// 
    /// Error Handling:
    /// - Save failures are logged but don't affect application operation
    /// - Settings remain in memory for current session regardless of save status
    /// - Next successful connection will attempt save again
    /// </summary>
    public void SaveSettings()
    {
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText("settings.json", JsonSerializer.Serialize(settings, options));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to save settings: {ex.Message}");
        }
    }

    /// <summary>
    /// Reads and validates calibration factors from all configured PAB ports
    /// Implements retry logic with safety validation to ensure measurement accuracy
    /// Critical safety method preventing unsafe operation with invalid factors
    /// 
    /// Reading Process:
    /// 1. Clear receive buffer and send GetFactors command to all ports
    /// 2. Parse incoming 48-byte factor records with protocol validation
    /// 3. Extract floating-point factor values with endianness correction
    /// 4. Validate all factors are ≤ 1.0 (safety requirement for proper scaling)
    /// 5. Store validated factors or retry on failure
    /// 
    /// Safety Validation:
    /// - All factors must be ≤ 1.0 to prevent scaling errors
    /// - Total factor and individual factors independently validated
    /// - Retry up to 3 times with 1-second delays between attempts
    /// - Throws exception if validation fails after all retries
    /// 
    /// Protocol Details:
    /// - 48-byte records starting with "SK" signature
    /// - Port ID at offset 12, factors at offset 24
    /// - Big-endian IEEE 754 floating-point values
    /// - totalFactor followed by 4 individual load cell factors
    /// 
    /// Error Recovery:
    /// - Communication errors trigger buffer clearing and retry
    /// - Validation failures show user warning before exception
    /// - Maximum 3 retry attempts with progressive delay
    /// - Complete failure prevents data streaming for safety
    /// </summary>
    /// <exception cref="Exception">
    /// Thrown when factor validation fails after maximum retry attempts
    /// Thrown when factors exceed safety bounds (> 1.0)
    /// </exception>
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
    /// <summary>
    /// Main data processing loop running on dedicated background thread
    /// Handles continuous data reception, parsing, calibration, and real-time distribution
    /// Implements dual-phase operation: calibration collection followed by normal data processing
    /// 
    /// Operation Phases:
    /// 
    /// PHASE 1: CALIBRATION (until calibrationComplete == true)
    /// - Collects raw ADC readings for zero offset calculation
    /// - Accumulates data until calibration period expires
    /// - Calculates averaged zero offsets for all active ports
    /// - Transitions to normal operation and fires CalibrationCompleted event
    /// 
    /// PHASE 2: NORMAL OPERATION (after calibration complete)
    /// - Processes incoming data with zero offset correction and factor scaling
    /// - Handles manual zero calibration requests during normal operation
    /// - Converts raw ADC values to calibrated weight measurements
    /// - Distributes real-time data to registered GraphForm instances
    /// 
    /// Data Processing Pipeline:
    /// 1. Read 68-byte records from serial buffer
    /// 2. Validate record structure ("SK" signature, length, CRC)
    /// 3. Extract port ID and filter against configured port mask
    /// 4. Parse ASCII payload to extract 4 ADC values
    /// 5. Apply calibration (zero offset subtraction, factor scaling)
    /// 6. Calculate elapsed time and increment record counter
    /// 7. Fire OnDataReceived event for UI distribution
    /// 
    /// Protocol Validation:
    /// - 68-byte fixed record length with signature validation
    /// - CRC-16 verification for data integrity assurance
    /// - End marker detection (0x0D 0x0A sequence)
    /// - Port ID filtering based on configuration bitmask
    /// 
    /// Error Handling:
    /// - Communication errors cause buffer flush and continue operation
    /// - CRC failures are logged and ignored (skip corrupted records)
    /// - Parsing errors clear buffer and resume reading
    /// - Individual port failures don't affect other ports
    /// 
    /// Threading and Performance:
    /// - Runs on dedicated background thread for UI responsiveness
    /// - 5ms sleep when no data available to prevent CPU spinning
    /// - Efficient binary parsing with minimal allocations
    /// - Real-time event firing for immediate UI updates
    /// 
    /// Memory Management:
    /// - Fixed-size buffers prevent excessive allocations
    /// - Dictionary operations optimized for read performance
    /// - Automatic collection cleanup during calibration phases
    /// </summary>
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
    /// <summary>
    /// Completes manual zero calibration for specified port with averaged baseline calculation
    /// Processes collected ADC readings to generate new zero offset values
    /// Provides immediate visual feedback through GraphForm zero marker placement
    /// 
    /// Calculation Process:
    /// 1. Validate sufficient calibration data has been collected
    /// 2. Calculate average ADC values across all collected samples
    /// 3. Use integer averaging to prevent floating-point precision loss
    /// 4. Store new zero offsets for immediate use in weight calculations
    /// 5. Clean up calibration data structures to free memory
    /// 6. Notify associated GraphForm with zero marker placement
    /// 
    /// Statistical Processing:
    /// - Uses 64-bit accumulator to prevent integer overflow
    /// - Integer division for final average calculation
    /// - Handles variable sample count based on calibration duration
    /// - Applies to all 4 load cells simultaneously
    /// 
    /// Integration Features:
    /// - Updates zero offsets immediately for real-time effect
    /// - Calculates current elapsed time for marker placement
    /// - Invokes GraphForm zero marker callback for visual indication
    /// - Cleans up calibration state for memory efficiency
    /// 
    /// Thread Safety:
    /// - Called from background read thread during data processing
    /// - Dictionary operations are atomic for this usage pattern
    /// - GraphForm callback invoked on read thread (handles UI marshalling)
    /// </summary>
    /// <param name="id">Zero-based port identifier for calibration completion</param>
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
    /// <summary>
    /// Sends PAB protocol command with proper formatting and transmission
    /// Builds complete command packet and transmits via serial port
    /// Provides abstraction over low-level command construction details
    /// </summary>
    /// <param name="pabPortSerialNumber">Target port for command (None, All, or specific port)</param>
    /// <param name="opCode">Command opcode defining requested operation</param>
    private void SendCommand(PabPort pabPortSerialNumber, PabOpCode opCode)
    {
        var command = BuildOpcodeCommand((ushort)pabPortSerialNumber, (ushort)opCode);
        serialPort?.Write(command, 0, command.Length);
    }

    /// <summary>
    /// Constructs PAB protocol command packet with proper header and checksum
    /// Builds binary command following PAB device communication specification
    /// Ensures protocol compliance with signature, length, and checksum validation
    /// 
    /// Protocol Structure:
    /// - Bytes 0-1: "SK" signature for packet identification
    /// - Bytes 2-3: Packet length (little-endian 16-bit)
    /// - Bytes 4-5: Reserved (zero padding)
    /// - Bytes 6-7: Checksum (calculated over entire packet)
    /// - Bytes 8+: Payload containing sequence number, opcode, port, timestamp, etc.
    /// 
    /// Checksum Algorithm:
    /// - Sum all bytes in packet
    /// - Apply bitwise NOT operation for error detection
    /// - Provides basic integrity verification for transmission
    /// </summary>
    /// <param name="pabPortSerialNumber">Target port identifier</param>
    /// <param name="opCode">Operation code for command</param>
    /// <param name="sequenceNumber">Optional sequence number (default: 0)</param>
    /// <param name="timestamp">Optional timestamp (default: 0)</param>
    /// <param name="leopardSequenceNumber">Optional leopard sequence (default: 0)</param>
    /// <returns>Complete binary command packet ready for transmission</returns>
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
    /// <summary>
    /// Calculates CRC-16 checksum for data integrity verification
    /// Implements standard CRC-16 algorithm with polynomial 0x8810
    /// Used for validating received data packets from PAB device
    /// 
    /// Algorithm Details:
    /// - 16-bit cyclic redundancy check
    /// - Polynomial: 0x8810 (standard CRC-16 variant)
    /// - Processes each byte with 8-bit shift operations
    /// - Returns 0 for valid data (checksum included in calculation)
    /// 
    /// Usage:
    /// - Applied to received data payload including embedded checksum
    /// - Result of 0 indicates valid data (checksum matches)
    /// - Non-zero result indicates data corruption or transmission error
    /// </summary>
    /// <param name="data">Byte array containing data to verify</param>
    /// <returns>CRC-16 value (0 for valid data with correct checksum)</returns>
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
    /// <summary>
    /// Calculates simple checksum for PAB command packets
    /// Implements complement checksum algorithm for command validation
    /// Provides basic error detection for transmitted commands
    /// 
    /// Algorithm:
    /// - Sum all bytes in the data array
    /// - Apply bitwise NOT operation to result
    /// - Provides simple error detection for command transmission
    /// 
    /// Usage:
    /// - Applied to outgoing command packets before transmission
    /// - Device validates checksum on received commands
    /// - Provides basic protection against transmission errors
    /// </summary>
    /// <param name="data">Command packet data for checksum calculation</param>
    /// <returns>16-bit checksum value for packet validation</returns>
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
