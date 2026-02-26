using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

public class HexUShortConverter : JsonConverter<ushort>
{
    public override ushort Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            string? hexString = reader.GetString();
            if (hexString != null && hexString.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                return ushort.Parse(hexString.Substring(2), NumberStyles.HexNumber);
            }
            else
            {
                return ushort.Parse(hexString ?? "0"); // fallback
            }
        }
        else if (reader.TokenType == JsonTokenType.Number)
        {
            return reader.GetUInt16();
        }
        else
        {
            throw new JsonException("Invalid token type for ushort hex conversion.");
        }
    }
    public override void Write(Utf8JsonWriter writer, ushort value, JsonSerializerOptions options)
    {
        writer.WriteStringValue($"0x{value:X}");
    }
}
