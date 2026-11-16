using System.Collections.ObjectModel;
using System.Text;
using SchematicBuilderMC.Enums;

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

    /// <summary>
    /// Returns a new <see cref="BlockState"/> with directional properties rotated clockwise around the Y axis.
    /// </summary>
    public BlockState Rotate(Rotation rotation)
    {
        if (!Enum.IsDefined(rotation))
        {
            throw new ArgumentOutOfRangeException(nameof(rotation), rotation, "Unsupported rotation value.");
        }

        if (rotation == Rotation.None || Properties.Count == 0)
        {
            return this;
        }

        int steps = (int)rotation / 90;
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (key, value) in Properties)
        {
            string lowerKey = key.ToLowerInvariant();
            switch (lowerKey)
            {
                case "facing":
                    dict[key] = RotateCardinalToken(value, steps);
                    break;

                case "axis":
                    if (steps % 2 == 1)
                    {
                        dict[key] = value.ToLowerInvariant() switch
                        {
                            "x" => "z",
                            "z" => "x",
                            _ => value
                        };
                    }
                    else
                    {
                        dict[key] = value;
                    }
                    break;

                case "rotation":
                    if (int.TryParse(value, out int rotVal))
                    {
                        dict[key] = ((rotVal + steps * 4) & 15).ToString();
                    }
                    else
                    {
                        dict[key] = value;
                    }
                    break;

                case "shape":
                    dict[key] = RotateShapeValue(value, steps);
                    break;

                case "north":
                case "east":
                case "south":
                case "west":
                    dict[RotateCardinalToken(lowerKey, steps)] = value;
                    break;

                default:
                    dict[key] = value;
                    break;
            }
        }

        return new BlockState(Identifier, dict);
    }

    private static string RotateCardinalToken(string value, int steps)
    {
        string current = value.ToLowerInvariant();
        for (int i = 0; i < steps; i++)
        {
            current = current switch
            {
                "north" => "east",
                "east" => "south",
                "south" => "west",
                "west" => "north",
                _ => value
            };
        }
        return current;
    }

    private static string RotateShapeValue(string shape, int steps)
    {
        string lower = shape.ToLowerInvariant();
        for (int i = 0; i < steps; i++)
        {
            lower = lower switch
            {
                "north_south" => "east_west",
                "east_west" => "north_south",
                "north_east" => "south_east",
                "south_east" => "south_west",
                "south_west" => "north_west",
                "north_west" => "north_east",
                "ascending_north" => "ascending_east",
                "ascending_east" => "ascending_south",
                "ascending_south" => "ascending_west",
                "ascending_west" => "ascending_north",
                _ => lower
            };
        }
        return lower;
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
