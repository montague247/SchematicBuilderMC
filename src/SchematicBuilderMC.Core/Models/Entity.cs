using fNbt;
using SchematicBuilderMC.Enums;

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

    /// <summary>
    /// Gets whether this entity is a regular item frame or glow item frame.
    /// </summary>
    public bool IsItemFrame =>
        Id.Equals("minecraft:item_frame", StringComparison.OrdinalIgnoreCase) ||
        Id.Equals("minecraft:glow_item_frame", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets whether this entity is a glow item frame.
    /// </summary>
    public bool IsGlowItemFrame =>
        Id.Equals("minecraft:glow_item_frame", StringComparison.OrdinalIgnoreCase);

    public Entity(string id, double x, double y, double z, NbtCompound? data = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        Id = id.Contains(':') ? id : "minecraft:" + id;
        X = x;
        Y = y;
        Z = z;
        Data = data ?? new NbtCompound();
    }

    /// <summary>
    /// Retrieves item frame data: Facing, Item, Rotation (0..7), IsInvisible, IsFixed, IsGlow.
    /// </summary>
    public (Direction Facing, ItemStack? Item, int Rotation, bool IsInvisible, bool IsFixed, bool IsGlow) GetItemFrameData()
    {
        bool isGlow = IsGlowItemFrame;

        // Facing: 0=down, 1=up, 2=north, 3=south, 4=west, 5=east
        byte facingByte = (byte)(Data["Facing"]?.IntValue ?? Data["facing"]?.IntValue ?? 2);
        Direction facing = DirectionFromFacingByte(facingByte);

        ItemStack? item = null;
        if (Data["Item"] is NbtCompound itemComp)
        {
            var parsed = ItemStack.FromNbt(itemComp);
            if (parsed.Id != "minecraft:air" && !string.IsNullOrWhiteSpace(parsed.Id))
            {
                item = parsed;
            }
        }
        else if (Data["item"] is NbtCompound itemCompLower)
        {
            var parsed = ItemStack.FromNbt(itemCompLower);
            if (parsed.Id != "minecraft:air" && !string.IsNullOrWhiteSpace(parsed.Id))
            {
                item = parsed;
            }
        }

        int rotation = Data["ItemRotation"]?.IntValue ?? Data["item_rotation"]?.IntValue ?? 0;
        bool invisible = (Data["Invisible"]?.IntValue ?? Data["invisible"]?.IntValue ?? 0) != 0;
        bool fixedTag = (Data["Fixed"]?.IntValue ?? Data["fixed"]?.IntValue ?? 0) != 0;

        return (facing, item, rotation, invisible, fixedTag, isGlow);
    }

    /// <summary>
    /// Creates an item frame entity.
    /// </summary>
    public static Entity CreateItemFrame(
        double x, double y, double z,
        Direction facing = Direction.North,
        ItemStack? item = null,
        int rotation = 0,
        bool isGlow = false,
        bool isInvisible = false,
        bool isFixed = false)
    {
        string id = isGlow ? "minecraft:glow_item_frame" : "minecraft:item_frame";
        var entity = new Entity(id, x, y, z);
        entity.Data["Facing"] = new NbtByte("Facing", FacingByteFromDirection(facing));

        if (item != null)
        {
            var itemComp = item.ToNbt();
            itemComp.Name = "Item";
            itemComp.Remove("Slot");
            entity.Data.Add(itemComp);
        }

        if (rotation != 0)
        {
            entity.Data["ItemRotation"] = new NbtByte("ItemRotation", (byte)rotation);
        }

        if (isInvisible)
        {
            entity.Data["Invisible"] = new NbtByte("Invisible", 1);
        }

        if (isFixed)
        {
            entity.Data["Fixed"] = new NbtByte("Fixed", 1);
        }

        return entity;
    }

    /// <summary>
    /// Maps a Minecraft Facing byte (0..5) to <see cref="Direction"/>.
    /// </summary>
    public static Direction DirectionFromFacingByte(byte facing) => facing switch
    {
        0 => Direction.Down,
        1 => Direction.Up,
        2 => Direction.North,
        3 => Direction.South,
        4 => Direction.West,
        5 => Direction.East,
        _ => Direction.North
    };

    /// <summary>
    /// Maps a <see cref="Direction"/> to a Minecraft Facing byte (0..5).
    /// </summary>
    public static byte FacingByteFromDirection(Direction direction) => direction switch
    {
        Direction.Down => 0,
        Direction.Up => 1,
        Direction.North => 2,
        Direction.South => 3,
        Direction.West => 4,
        Direction.East => 5,
        _ => 2
    };

    public Entity Clone()
    {
        return new Entity(Id, X, Y, Z, (NbtCompound)Data.Clone());
    }

    public override string ToString() => $"{Id} at ({X:F2}, {Y:F2}, {Z:F2})";
}
