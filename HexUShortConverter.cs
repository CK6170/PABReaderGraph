using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Custom JSON converter for ushort values with hexadecimal string representation
/// Enables serialization of unsigned 16-bit integers as human-readable hex strings
/// Provides bidirectional conversion between numeric ushort values and "0x" prefixed hex strings
/// 
/// Purpose and Context:
/// - Designed for hardware-related values that are conceptually hexadecimal (port numbers, device IDs)
/// - Improves JSON readability by showing hardware values in their natural hex format
/// - Maintains numeric precision while enhancing human understanding in configuration files
/// - Used specifically for PabPortSerialNumber serialization in application settings
/// 
/// Serialization Behavior:
/// - Output Format: "0x" prefix followed by uppercase hexadecimal digits
/// - Example: ushort value 255 serializes as "0xFF"
/// - Always uses minimal hex representation (no zero padding)
/// - Consistent uppercase formatting for professional appearance
/// 
/// Deserialization Behavior:
/// - Accepts multiple input formats for maximum compatibility:
///   1. Hex strings with "0x" prefix: "0xFF", "0x1A2B"
///   2. Plain decimal strings: "255", "6699"
///   3. Numeric JSON values: 255, 6699
/// - Case-insensitive hex parsing ("0xff" and "0xFF" both work)
/// - Robust error handling with informative exception messages
/// 
/// Design Philosophy:
/// - Human-readable: Config files show hex values in natural format
/// - Flexible input: Accepts various formats users might provide
/// - Consistent output: Always produces standardized hex format
/// - Error-resilient: Graceful handling of invalid input with clear messages
/// 
/// Performance Characteristics:
/// - Minimal overhead: Simple string parsing and formatting operations
/// - Memory efficient: No intermediate object allocations
/// - Parse validation: Ensures only valid ushort ranges are accepted
/// </summary>
public class HexUShortConverter : JsonConverter<ushort>
{
    /// <summary>
    /// Deserializes JSON data to ushort values with support for multiple input formats
    /// Handles both hexadecimal string representations and numeric JSON values
    /// Provides robust parsing with fallback mechanisms for maximum compatibility
    /// 
    /// Supported Input Formats:
    /// 1. Hexadecimal Strings with "0x" prefix:
    ///    - "0xFF" → 255
    ///    - "0x1A2B" → 6699
    ///    - "0x0" → 0
    ///    - Case insensitive: "0xff" works same as "0xFF"
    /// 
    /// 2. Plain Decimal Strings:
    ///    - "255" → 255
    ///    - "6699" → 6699
    ///    - "0" → 0
    ///    - Used as fallback when no "0x" prefix found
    /// 
    /// 3. Numeric JSON Values:
    ///    - 255 → 255
    ///    - 6699 → 6699
    ///    - Direct numeric representation in JSON
    /// 
    /// Error Handling:
    /// - Validates ushort range (0-65535) during parsing
    /// - Throws JsonException for invalid token types
    /// - Uses "0" fallback for null string values
    /// - Preserves original parsing exceptions for debugging
    /// 
    /// Performance Notes:
    /// - String.StartsWith uses OrdinalIgnoreCase for case-insensitive prefix check
    /// - ushort.Parse with NumberStyles.HexNumber for efficient hex parsing
    /// - Minimal string manipulation (single Substring call for "0x" removal)
    /// - Direct reader access avoids intermediate string allocations where possible
    /// </summary>
    /// <param name="reader">
    /// Utf8JsonReader positioned at the token to deserialize
    /// Must be at a String or Number token type for valid conversion
    /// </param>
    /// <param name="typeToConvert">
    /// Target type for conversion (always ushort for this converter)
    /// Required by JsonConverter interface but not used in implementation
    /// </param>
    /// <param name="options">
    /// JsonSerializerOptions containing serialization configuration
    /// Required by interface but not used in this simple converter implementation
    /// </param>
    /// <returns>
    /// ushort value parsed from the JSON input
    /// Range: 0 to 65535 (unsigned 16-bit integer)
    /// </returns>
    /// <exception cref="JsonException">
    /// Thrown when reader contains invalid token type (not String or Number)
    /// Thrown when string format cannot be parsed as valid ushort value
    /// Thrown when numeric value exceeds ushort range (0-65535)
    /// </exception>
    /// <example>
    /// <code>
    /// // JSON input examples and their parsed results:
    /// "0xFF"     → 255
    /// "0x1a2b"   → 6699
    /// "255"      → 255
    /// 255        → 255
    /// "0"        → 0
    /// </code>
    /// </example>
    public override ushort Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // CASE 1: JSON String Token - Handle both hex and decimal string formats
        if (reader.TokenType == JsonTokenType.String)
        {
            string? hexString = reader.GetString();

            // Handle hexadecimal strings with "0x" prefix (case-insensitive)
            if (hexString != null && hexString.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                // Remove "0x" prefix and parse as hexadecimal number
                // Uses NumberStyles.HexNumber for proper hex digit validation
                return ushort.Parse(hexString.Substring(2), NumberStyles.HexNumber);
            }
            else
            {
                // Fallback: Parse as decimal string or use "0" for null values
                // Provides compatibility with plain numeric strings in JSON
                return ushort.Parse(hexString ?? "0");
            }
        }
        // CASE 2: JSON Number Token - Direct numeric value parsing
        else if (reader.TokenType == JsonTokenType.Number)
        {
            // Direct extraction of unsigned 16-bit integer from JSON number
            // Automatically validates ushort range (0-65535)
            return reader.GetUInt16();
        }
        // CASE 3: Invalid Token Type - Error condition
        else
        {
            // Reject unsupported JSON token types with descriptive error message
            throw new JsonException("Invalid token type for ushort hex conversion.");
        }
    }
    /// <summary>
    /// Serializes ushort values to JSON as hexadecimal string representations
    /// Outputs consistent "0x" prefixed uppercase hexadecimal format for readability
    /// Provides human-friendly representation of hardware-related numeric values
    /// 
    /// Output Format Specifications:
    /// - Prefix: Always includes "0x" for clear hexadecimal indication
    /// - Case: Uppercase hexadecimal digits (A-F) for professional appearance
    /// - Padding: No zero-padding, uses minimal representation
    /// - Examples:
    ///   * 0 → "0x0"
    ///   * 255 → "0xFF" 
    ///   * 6699 → "0x1A2B"
    ///   * 65535 → "0xFFFF"
    /// 
    /// Design Rationale:
    /// - Human Readable: Hardware values are naturally expressed in hexadecimal
    /// - Consistent: Always produces same format regardless of input value
    /// - Professional: Uppercase hex follows common engineering conventions
    /// - Compact: Minimal representation without unnecessary zero padding
    /// - Standard: "0x" prefix is universally recognized hex indicator
    /// 
    /// Performance Characteristics:
    /// - Single string interpolation operation for efficient formatting
    /// - Direct WriteStringValue call avoids intermediate allocations
    /// - Uppercase formatting using standard :X format specifier
    /// - Minimal CPU overhead for simple numeric-to-hex conversion
    /// 
    /// JSON Schema Impact:
    /// - Output JSON property will be string type, not numeric
    /// - Maintains precision (no floating point conversion issues)
    /// - Enables human editing of configuration files with hex values
    /// - Compatible with version control diff tools (readable changes)
    /// </summary>
    /// <param name="writer">
    /// Utf8JsonWriter instance for writing JSON output
    /// Positioned to write the property value for the current ushort
    /// </param>
    /// <param name="value">
    /// ushort value to serialize as hexadecimal string
    /// Range: 0 to 65535 (all valid ushort values supported)
    /// </param>
    /// <param name="options">
    /// JsonSerializerOptions containing serialization configuration
    /// Required by JsonConverter interface but not used in this implementation
    /// </param>
    /// <example>
    /// <code>
    /// // ushort values and their serialized JSON representations:
    /// 0      → "0x0"
    /// 255    → "0xFF"
    /// 6699   → "0x1A2B"
    /// 65535  → "0xFFFF"
    /// </code>
    /// </example>
    public override void Write(Utf8JsonWriter writer, ushort value, JsonSerializerOptions options)
    {
        // Write ushort value as hexadecimal string with "0x" prefix and uppercase digits
        // Format: "0x" + uppercase hex representation (e.g., "0xFF", "0x1A2B")
        writer.WriteStringValue($"0x{value:X}");
    }
}
