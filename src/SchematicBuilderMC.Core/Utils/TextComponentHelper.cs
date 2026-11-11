using System.Text;
using System.Text.Json;

namespace SchematicBuilderMC.Utils;

/// <summary>
/// Helper utilities for parsing and creating Minecraft JSON text components
/// (e.g. item display names, entity custom names, and sign text).
/// </summary>
public static class TextComponentHelper
{
    /// <summary>
    /// Extracts plain text from a Minecraft JSON text component or returns the raw string if not JSON.
    /// Handles standard JSON text objects (including formatting attributes like "italic": false),
    /// nested "extra" arrays, translate keys, and plain strings.
    /// </summary>
    /// <param name="jsonOrText">The JSON text component string or plain text.</param>
    /// <returns>The extracted human-readable text.</returns>
    public static string ExtractText(string? jsonOrText)
    {
        if (string.IsNullOrWhiteSpace(jsonOrText))
            return string.Empty;

        string trimmed = jsonOrText.Trim();

        // If it doesn't start with JSON delimiters, return trimmed string as-is
        if (!trimmed.StartsWith('{') && !trimmed.StartsWith('[') && !trimmed.StartsWith('"'))
        {
            return trimmed;
        }

        try
        {
            using var doc = JsonDocument.Parse(trimmed);
            var sb = new StringBuilder();
            AppendTextFromElement(doc.RootElement, sb);
            string result = sb.ToString();
            return !string.IsNullOrEmpty(result) ? result : trimmed;
        }
        catch
        {
            // If JSON parsing fails, fallback to original string
            return trimmed;
        }
    }

    /// <summary>
    /// Creates a Minecraft JSON text component string for the specified text.
    /// </summary>
    /// <param name="text">The text content.</param>
    /// <param name="italic">Whether the text should be explicitly italicized (null to omit italic property).</param>
    /// <returns>A JSON text component string.</returns>
    public static string CreateTextComponent(string text, bool? italic = null)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (text.StartsWith('{') && text.EndsWith('}'))
        {
            return text;
        }

        string escaped = EscapeJsonString(text);
        if (italic.HasValue)
        {
            return $"{{\"text\":\"{escaped}\",\"italic\":{(italic.Value ? "true" : "false")}}}";
        }

        return $"{{\"text\":\"{escaped}\"}}";
    }

    /// <summary>
    /// Escapes special characters for use in a JSON string.
    /// </summary>
    public static string EscapeJsonString(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";

        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\r", "\\r")
            .Replace("\n", "\\n")
            .Replace("\t", "\\t");
    }

    private static void AppendTextFromElement(JsonElement element, StringBuilder sb)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                sb.Append(element.GetString());
                break;

            case JsonValueKind.Object:
                if (element.TryGetProperty("text", out var textProp) && textProp.ValueKind == JsonValueKind.String)
                {
                    sb.Append(textProp.GetString());
                }
                else if (element.TryGetProperty("translate", out var transProp) && transProp.ValueKind == JsonValueKind.String)
                {
                    sb.Append(transProp.GetString());
                }

                if (element.TryGetProperty("extra", out var extraProp) && extraProp.ValueKind == JsonValueKind.Array)
                {
                    foreach (var child in extraProp.EnumerateArray())
                    {
                        AppendTextFromElement(child, sb);
                    }
                }
                break;

            case JsonValueKind.Array:
                foreach (var child in element.EnumerateArray())
                {
                    AppendTextFromElement(child, sb);
                }
                break;
        }
    }
}
