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

    /// <summary>
    /// Gets whether this entity is any type of minecart (regular, chest, furnace, hopper, tnt, spawner, command block).
    /// </summary>
    public bool IsMinecart =>
        Id.Equals("minecraft:minecart", StringComparison.OrdinalIgnoreCase) ||
        Id.EndsWith("_minecart", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Initializes a new instance of <see cref="Entity"/> with an entity ID and optional custom NBT data.
    /// Position defaults to (0, 0, 0), suitable for passengers riding vehicles.
    /// </summary>
    public Entity(string id, NbtCompound? data = null)
        : this(id, 0, 0, 0, data)
    {
    }

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

    /// <summary>
    /// Gets whether this entity has any passenger entities riding it.
    /// </summary>
    public bool HasPassengers =>
        Data.Contains("Passengers") && Data["Passengers"] is NbtList { Count: > 0 };

    /// <summary>
    /// Retrieves all passenger entities riding this entity.
    /// </summary>
    public IReadOnlyList<Entity> GetPassengers()
    {
        if (Data["Passengers"] is not NbtList pList)
            return Array.Empty<Entity>();

        var list = new List<Entity>(pList.Count);
        foreach (var tag in pList)
        {
            if (tag is NbtCompound pComp)
            {
                list.Add(FromNbt(pComp, X, Y, Z));
            }
        }
        return list;
    }

    /// <summary>
    /// Adds a passenger entity to ride this entity.
    /// </summary>
    public Entity AddPassenger(Entity passenger)
    {
        ArgumentNullException.ThrowIfNull(passenger);

        NbtList passengersList;
        if (Data["Passengers"] is NbtList existing)
        {
            passengersList = existing;
        }
        else
        {
            passengersList = new NbtList("Passengers", NbtTagType.Compound);
            Data["Passengers"] = passengersList;
        }

        passengersList.Add(passenger.ToNbt());
        return this;
    }

    /// <summary>
    /// Adds a passenger entity to ride this entity and returns this instance.
    /// </summary>
    public Entity WithPassenger(Entity passenger) => AddPassenger(passenger);

    /// <summary>
    /// Adds one or more passenger entities to ride this entity and returns this instance.
    /// </summary>
    public Entity WithPassengers(params Entity[] passengers)
    {
        if (passengers != null)
        {
            foreach (var p in passengers)
            {
                AddPassenger(p);
            }
        }
        return this;
    }

    /// <summary>
    /// Retrieves velocity vector (motionX, motionY, motionZ) stored in this entity's "Motion" tag.
    /// </summary>
    public (double X, double Y, double Z) GetMotion()
    {
        if (Data["Motion"] is NbtList mList && mList.Count >= 3)
        {
            return (mList[0].DoubleValue, mList[1].DoubleValue, mList[2].DoubleValue);
        }
        return (0, 0, 0);
    }

    /// <summary>
    /// Sets initial velocity vector (motionX, motionY, motionZ) for this entity.
    /// </summary>
    public Entity SetMotion(double x, double y, double z)
    {
        Data["Motion"] = new NbtList("Motion", NbtTagType.Double)
        {
            new NbtDouble(x),
            new NbtDouble(y),
            new NbtDouble(z)
        };
        return this;
    }

    /// <summary>
    /// Sets initial velocity vector (motionX, motionY, motionZ) for this entity and returns this instance.
    /// </summary>
    public Entity WithMotion(double x, double y, double z) => SetMotion(x, y, z);

    /// <summary>
    /// Retrieves minecart-specific data: Passengers, Motion (X, Y, Z), IsFlipped, and MinecartType.
    /// </summary>
    public (IReadOnlyList<Entity> Passengers, double MotionX, double MotionY, double MotionZ, bool IsFlipped, string MinecartType) GetMinecartData()
    {
        var passengers = GetPassengers();
        var (mx, my, mz) = GetMotion();
        bool isFlipped = (Data["FlippedRotation"]?.IntValue ?? Data["flipped_rotation"]?.IntValue ?? 0) != 0;
        return (passengers, mx, my, mz, isFlipped, Id);
    }

    /// <summary>
    /// Creates a minecart entity with an optional passenger.
    /// </summary>
    public static Entity CreateMinecart(
        double x, double y, double z,
        Entity? passenger = null,
        double motionX = 0, double motionY = 0, double motionZ = 0,
        bool isFlipped = false,
        string minecartType = "minecraft:minecart")
    {
        var passengers = passenger != null ? new[] { passenger } : null;
        return CreateMinecart(x, y, z, passengers, motionX, motionY, motionZ, isFlipped, minecartType);
    }

    /// <summary>
    /// Creates a minecart entity with multiple passengers.
    /// </summary>
    public static Entity CreateMinecart(
        double x, double y, double z,
        IEnumerable<Entity>? passengers = null,
        double motionX = 0, double motionY = 0, double motionZ = 0,
        bool isFlipped = false,
        string minecartType = "minecraft:minecart")
    {
        var entity = new Entity(minecartType, x, y, z);
        if (passengers != null)
        {
            foreach (var p in passengers)
            {
                entity.AddPassenger(p);
            }
        }
        if (motionX != 0 || motionY != 0 || motionZ != 0)
        {
            entity.SetMotion(motionX, motionY, motionZ);
        }
        if (isFlipped)
        {
            entity.Data["FlippedRotation"] = new NbtByte("FlippedRotation", 1);
        }
        return entity;
    }

    /// <summary>
    /// Sets equipment for a living entity slot (e.g. "mainhand", "offhand", "head", "chest", "legs", "feet").
    /// </summary>
    public Entity WithEquipment(string slot, ItemStack item)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slot);
        ArgumentNullException.ThrowIfNull(item);

        NbtCompound eqComp;
        if (Data["equipment"] is NbtCompound existing)
        {
            eqComp = existing;
        }
        else
        {
            eqComp = new NbtCompound("equipment");
            Data["equipment"] = eqComp;
        }

        var itemComp = item.ToNbt();
        itemComp.Name = slot.ToLowerInvariant();
        itemComp.Remove("Slot");
        eqComp[itemComp.Name] = itemComp;
        return this;
    }

    public Entity WithMainHand(ItemStack item) => WithEquipment("mainhand", item);
    public Entity WithOffHand(ItemStack item) => WithEquipment("offhand", item);
    public Entity WithHead(ItemStack item) => WithEquipment("head", item);
    public Entity WithChest(ItemStack item) => WithEquipment("chest", item);
    public Entity WithLegs(ItemStack item) => WithEquipment("legs", item);
    public Entity WithFeet(ItemStack item) => WithEquipment("feet", item);

    /// <summary>
    /// Sets drop chance for an equipment slot.
    /// </summary>
    public Entity WithDropChance(string slot, float chance)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slot);

        NbtCompound dropComp;
        if (Data["drop_chances"] is NbtCompound existing)
        {
            dropComp = existing;
        }
        else
        {
            dropComp = new NbtCompound("drop_chances");
            Data["drop_chances"] = dropComp;
        }

        dropComp[slot.ToLowerInvariant()] = new NbtFloat(slot.ToLowerInvariant(), chance);
        return this;
    }

    /// <summary>
    /// Converts this entity to a Minecraft entity NBT compound.
    /// </summary>
    public NbtCompound ToNbt()
    {
        var compound = (NbtCompound)Data.Clone();
        compound["id"] = new NbtString("id", Id);
        if (!compound.Contains("Pos"))
        {
            compound["Pos"] = new NbtList("Pos", NbtTagType.Double)
            {
                new NbtDouble(X),
                new NbtDouble(Y),
                new NbtDouble(Z)
            };
        }
        return compound;
    }

    /// <summary>
    /// Creates an <see cref="Entity"/> from a Minecraft entity NBT compound.
    /// </summary>
    public static Entity FromNbt(NbtCompound compound, double fallbackX = 0, double fallbackY = 0, double fallbackZ = 0)
    {
        ArgumentNullException.ThrowIfNull(compound);

        string id = compound["id"]?.StringValue ?? "minecraft:pig";
        double x = fallbackX;
        double y = fallbackY;
        double z = fallbackZ;

        if (compound["Pos"] is NbtList posList && posList.Count >= 3)
        {
            x = posList[0].DoubleValue;
            y = posList[1].DoubleValue;
            z = posList[2].DoubleValue;
        }

        var dataClone = (NbtCompound)compound.Clone();
        dataClone.Remove("id");
        dataClone.Remove("Pos");

        return new Entity(id, x, y, z, dataClone);
    }

    public override string ToString() => $"{Id} at ({X:F2}, {Y:F2}, {Z:F2})";
}
