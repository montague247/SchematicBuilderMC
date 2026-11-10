using fNbt;

namespace SchematicBuilderMC.Models;

/// <summary>
/// Represents a Minecraft entity inside the schematic (e.g. armor stand, item frame, painting).
/// </summary>
public sealed class Entity
{
    /// <summary>
    /// The namespaced entity ID (e.g. "minecraft:armor_stand").
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Position coordinates (X, Y, Z) in floating point.
    /// </summary>
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }

    /// <summary>
    /// Additional NBT data for the entity.
    /// </summary>
    public NbtCompound Data { get; set; }

    public Entity(string id, double x, double y, double z, NbtCompound? data = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        Id = id.Contains(':') ? id : "minecraft:" + id;
        X = x;
        Y = y;
        Z = z;
        Data = data ?? new NbtCompound();
    }

    public Entity Clone()
    {
        return new Entity(Id, X, Y, Z, (NbtCompound)Data.Clone());
    }

    public override string ToString() => $"{Id} at ({X:F2}, {Y:F2}, {Z:F2})";
}
