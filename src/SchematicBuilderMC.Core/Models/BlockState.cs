using System.Collections.ObjectModel;
using System.Text;

namespace SchematicBuilderMC.Models;

/// <summary>
/// Represents a Minecraft block state, consisting of a namespaced identifier and optional properties.
/// </summary>
public sealed class BlockState : IEquatable<BlockState>
{
    private static readonly ReadOnlyDictionary<string, string> EmptyProperties =
        new(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));

    public static readonly BlockState Air = new("minecraft:air");

    private readonly Lazy<string> _canonicalString;

    /// <summary>
    /// Gets the namespaced block identifier (e.g., "minecraft:oak_stairs").
    /// </summary>
    public string Identifier { get; }

    /// <summary>
    /// Gets the block properties (e.g., facing=north, half=top).
    /// </summary>
    public IReadOnlyDictionary<string, string> Properties { get; }

    /// <summary>
    /// Gets whether this block represents an air block (air, cave_air, void_air).
    /// </summary>
    public bool IsAir =>
        Identifier.Equals("minecraft:air", StringComparison.OrdinalIgnoreCase) ||
        Identifier.Equals("minecraft:cave_air", StringComparison.OrdinalIgnoreCase) ||
        Identifier.Equals("minecraft:void_air", StringComparison.OrdinalIgnoreCase);

    public BlockState(string identifier, IDictionary<string, string>? properties = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);

        Identifier = NormalizeIdentifier(identifier);

        if (properties == null || properties.Count == 0)
        {
            Properties = EmptyProperties;
        }
        else
        {
            var dict = new Dictionary<string, string>(properties, StringComparer.OrdinalIgnoreCase);
            Properties = new ReadOnlyDictionary<string, string>(dict);
        }

        _canonicalString = new Lazy<string>(ComputeCanonicalString);
    }

    /// <summary>
    /// Parses a block state string (e.g., "minecraft:oak_stairs[facing=north,half=bottom]").
    /// </summary>
    public static BlockState Parse(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        text = text.Trim();
        int openBracket = text.IndexOf('[');
        if (openBracket < 0)
        {
            return new BlockState(text);
        }

        int closeBracket = text.IndexOf(']', openBracket);
        if (closeBracket < 0)
        {
            closeBracket = text.Length;
        }

        string identifier = text[..openBracket].Trim();
        string propertiesPart = text.Substring(openBracket + 1, closeBracket - openBracket - 1).Trim();

        var properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrEmpty(propertiesPart))
        {
            string[] pairs = propertiesPart.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var pair in pairs)
            {
                int eq = pair.IndexOf('=');
                if (eq > 0)
                {
                    string key = pair[..eq].Trim();
                    string val = pair[(eq + 1)..].Trim();
                    properties[key] = val;
                }
            }
        }

        return new BlockState(identifier, properties);
    }

    public static bool TryParse(string? text, out BlockState? blockState)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            blockState = null;
            return false;
        }

        try
        {
            blockState = Parse(text);
            return true;
        }
        catch
        {
            blockState = null;
            return false;
        }
    }

    /// <summary>
    /// Returns a new BlockState with the specified property set.
    /// </summary>
    public BlockState WithProperty(string key, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);

        var dict = new Dictionary<string, string>(Properties, StringComparer.OrdinalIgnoreCase)
        {
            [key] = value
        };
        return new BlockState(Identifier, dict);
    }

    /// <summary>
    /// Returns a new BlockState with the specified property removed.
    /// </summary>
    public BlockState WithoutProperty(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (!Properties.ContainsKey(key))
            return this;

        var dict = new Dictionary<string, string>(Properties, StringComparer.OrdinalIgnoreCase);
        dict.Remove(key);
        return new BlockState(Identifier, dict);
    }

    /// <summary>
    /// Gets a property value, or null if not found.
    /// </summary>
    public string? GetProperty(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return Properties.TryGetValue(key, out var val) ? val : null;
    }

    private static string NormalizeIdentifier(string identifier)
    {
        identifier = identifier.Trim().ToLowerInvariant();
        if (!identifier.Contains(':'))
        {
            identifier = "minecraft:" + identifier;
        }
        return identifier;
    }

    private string ComputeCanonicalString()
    {
        if (Properties.Count == 0)
        {
            return Identifier;
        }

        var sb = new StringBuilder(Identifier);
        sb.Append('[');
        bool first = true;
        foreach (var kvp in Properties.OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase))
        {
            if (!first) sb.Append(',');
            sb.Append(kvp.Key).Append('=').Append(kvp.Value);
            first = false;
        }
        sb.Append(']');
        return sb.ToString();
    }

    public override string ToString() => _canonicalString.Value;

    public bool Equals(BlockState? other)
    {
        if (ReferenceEquals(this, other)) return true;
        if (other is null) return false;
        return string.Equals(ToString(), other.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    public override bool Equals(object? obj) => Equals(obj as BlockState);

    public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(ToString());

    public static bool operator ==(BlockState? left, BlockState? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(BlockState? left, BlockState? right) => !(left == right);

    public static bool operator ==(BlockState? left, string? right)
    {
        if (left is null) return right is null;
        if (right is null) return false;
        if (!right.Contains('['))
        {
            return left.Properties.Count == 0 &&
                   string.Equals(left.Identifier, NormalizeIdentifier(right), StringComparison.OrdinalIgnoreCase);
        }
        return left.Equals(Parse(right));
    }

    public static bool operator !=(BlockState? left, string? right) => !(left == right);

    public static bool operator ==(string? left, BlockState? right) => right == left;

    public static bool operator !=(string? left, BlockState? right) => !(right == left);

    public static implicit operator BlockState(string text) => Parse(text);
}
