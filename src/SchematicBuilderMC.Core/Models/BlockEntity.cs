using fNbt;

namespace SchematicBuilderMC.Core;

/// <summary>
/// Represents a tile/block entity attached to a block in the schematic (e.g. chest, sign, beacon).
/// </summary>
public sealed class BlockEntity
{
    /// <summary>
    /// The namespaced block entity ID (e.g. "minecraft:chest").
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Position of the block entity relative to the schematic origin.
    /// </summary>
    public BlockPosition Position { get; set; }

    /// <summary>
    /// Additional NBT data for the block entity.
    /// </summary>
    public NbtCompound Data { get; set; }

    public BlockEntity(string id, BlockPosition position, NbtCompound? data = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        Id = id.Contains(':') ? id : "minecraft:" + id;
        Position = position;
        Data = data ?? new NbtCompound();
    }

    public BlockEntity Clone()
    {
        return new BlockEntity(Id, Position, (NbtCompound)Data.Clone());
    }

    public override string ToString() => $"{Id} at {Position}";
}
