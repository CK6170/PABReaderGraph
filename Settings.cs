using System.Text.Json.Serialization;

public class Settings
{
    public string LastPort { get; set; } = "";
    public int CalibrationSeconds { get; set; } = 10;
    public string PabVersion { get; set; } = "PABG3-12324.02.27.01.25";
    public string BaseFolder { get; set; } = "Data";

    [JsonConverter(typeof(HexUShortConverter))]
    public ushort PabPortSerialNumber { get; set; } = 0xFF;
    public bool AutoArrangeGraphs { get; set; } = true;
    public Dictionary<int, WindowSettings> WindowSettingsPerId { get; set; } = new();
    public List<string> KnownPabVersions { get; set; } = new List<string>
{
    "PABG1-12266.02.17.05.23",
    "PABG3-12324.02.27.01.25"
};

}
public class WindowSettings
{
    public int Width { get; set; } = 800;
    public int Height { get; set; } = 600;
    public int Left { get; set; } = 100;
    public int Top { get; set; } = 100;
    public bool Maximized { get; set; } = false;
}

