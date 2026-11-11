using fNbt;
using SchematicBuilderMC.Enums;
using SchematicBuilderMC.Utils;

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
    /// Gets or sets the custom name of this entity (e.g. applied by a Name Tag or command).
    /// </summary>
    public string? CustomName
    {
        get
        {
            var tag = Data["CustomName"] ?? Data["custom_name"];
            if (tag is NbtString strTag)
            {
                return TextComponentHelper.ExtractText(strTag.Value);
            }
            return tag?.StringValue;
        }
        set
        {
            if (value == null)
            {
                Data.Remove("CustomName");
                Data.Remove("custom_name");
            }
            else
            {
                string jsonName = value.StartsWith('{') && value.EndsWith('}')
                    ? value
                    : $"{{\"text\":\"{TextComponentHelper.EscapeJsonString(value)}\"}}";
                Data["CustomName"] = new NbtString("CustomName", jsonName);
            }
        }
    }

    /// <summary>
    /// Gets or sets whether this entity's custom name is visible as a nameplate above the entity.
    /// </summary>
    public bool CustomNameVisible
    {
        get => (Data["CustomNameVisible"]?.IntValue ?? Data["custom_name_visible"]?.IntValue ?? 0) != 0;
        set => Data["CustomNameVisible"] = new NbtByte("CustomNameVisible", (byte)(value ? 1 : 0));
    }

    /// <summary>
    /// Gets or sets whether this entity is prevented from despawning naturally
    /// (standard Minecraft behavior when an entity is named using a Name Tag).
    /// </summary>
    public bool PersistenceRequired
    {
        get => (Data["PersistenceRequired"]?.IntValue ?? Data["persistence_required"]?.IntValue ?? 0) != 0;
        set => Data["PersistenceRequired"] = new NbtByte("PersistenceRequired", (byte)(value ? 1 : 0));
    }

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
    /// Assigns a custom name to this entity (such as applied by a Name Tag) and returns this instance.
    /// </summary>
    /// <param name="name">The custom name or JSON text component.</param>
    /// <param name="visible">Whether the name tag is rendered above the entity (default is true).</param>
    /// <param name="persistenceRequired">Whether to prevent the entity from despawning (default is true).</param>
    public Entity WithCustomName(string name, bool visible = true, bool persistenceRequired = true)
    {
        CustomName = name;
        CustomNameVisible = visible;
        if (persistenceRequired)
        {
            PersistenceRequired = true;
        }
        return this;
    }

    /// <summary>
    /// Sets whether persistence is required (prevents natural despawning) and returns this instance.
    /// </summary>
    public Entity WithPersistenceRequired(bool required = true)
    {
        PersistenceRequired = required;
        return this;
    }

    /// <summary>
    /// Renames this entity as if applying a Name Tag.
    /// Sets <see cref="CustomName"/>, sets <see cref="CustomNameVisible"/>, and marks <see cref="PersistenceRequired"/> as true.
    /// </summary>
    /// <param name="name">The name to assign.</param>
    /// <param name="visible">Whether the name tag is rendered above the entity.</param>
    public Entity ApplyNameTag(string name, bool visible = true) =>
        WithCustomName(name, visible, persistenceRequired: true);

    /// <summary>
    /// Renames this entity using a named <see cref="ItemStack"/> (such as a named Name Tag).
    /// </summary>
    /// <param name="nameTag">The named Name Tag item stack.</param>
    /// <param name="visible">Whether the name tag is rendered above the entity.</param>
    public Entity ApplyNameTag(ItemStack nameTag, bool visible = true)
    {
        ArgumentNullException.ThrowIfNull(nameTag);
        string? name = nameTag.DisplayName;
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("The provided Name Tag does not have a display name.", nameof(nameTag));
        }
        return ApplyNameTag(name, visible);
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
