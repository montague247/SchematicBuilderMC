using fNbt;

namespace SchematicBuilderMC.Models;

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

    /// <summary>
    /// Gets whether this block entity contains any inventory items.
    /// </summary>
    public bool HasItems => Data.Contains("Items") && Data["Items"] is NbtList { Count: > 0 };

    /// <summary>
    /// Gets all inventory items in this block entity.
    /// </summary>
    public IReadOnlyList<ItemStack> Items => GetItems();

    /// <summary>
    /// Retrieves all inventory items stored in the "Items" NBT tag of this block entity.
    /// </summary>
    public IReadOnlyList<ItemStack> GetItems()
    {
        if (Data["Items"] is not NbtList itemsList)
            return Array.Empty<ItemStack>();

        var list = new List<ItemStack>(itemsList.Count);
        foreach (var tag in itemsList)
        {
            if (tag is NbtCompound comp)
            {
                list.Add(ItemStack.FromNbt(comp));
            }
        }
        return list;
    }

    /// <summary>
    /// Gets the item in the specified slot, or null if empty.
    /// </summary>
    public ItemStack? GetItem(byte slot)
    {
        if (Data["Items"] is not NbtList itemsList)
            return null;

        foreach (var tag in itemsList)
        {
            if (tag is NbtCompound comp)
            {
                var item = ItemStack.FromNbt(comp);
                if (item.Slot == slot)
                    return item;
            }
        }
        return null;
    }

    /// <summary>
    /// Gets the item in the specified slot, or null if empty.
    /// </summary>
    public ItemStack? GetItem(int slot) => GetItem((byte)slot);

    /// <summary>
    /// Adds or replaces an item stack in this block entity's inventory.
    /// </summary>
    public BlockEntity AddItem(ItemStack item)
    {
        ArgumentNullException.ThrowIfNull(item);

        NbtList itemsList;
        if (Data["Items"] is NbtList existing)
        {
            itemsList = existing;
        }
        else
        {
            itemsList = new NbtList("Items", NbtTagType.Compound);
            Data["Items"] = itemsList;
        }

        // Remove existing item at the same slot if present
        for (int i = itemsList.Count - 1; i >= 0; i--)
        {
            if (itemsList[i] is NbtCompound comp)
            {
                byte slot = comp["Slot"]?.ByteValue ?? (byte)(comp["slot"]?.ByteValue ?? 0);
                if (slot == item.Slot)
                {
                    itemsList.RemoveAt(i);
                }
            }
        }

        itemsList.Add(item.ToNbt());
        return this;
    }

    /// <summary>
    /// Adds an item to this block entity. If slot is null, uses the first unoccupied slot.
    /// </summary>
    public BlockEntity AddItem(string itemId, int count = 1, byte? slot = null, NbtCompound? customData = null)
    {
        byte resolvedSlot = slot ?? GetFirstEmptySlot();
        return AddItem(new ItemStack(itemId, count, resolvedSlot, customData));
    }

    /// <summary>
    /// Fluent alias for <see cref="AddItem(ItemStack)"/>.
    /// </summary>
    public BlockEntity WithItem(ItemStack item) => AddItem(item);

    /// <summary>
    /// Fluent alias for <see cref="AddItem(string, int, byte?, NbtCompound?)"/>.
    /// </summary>
    public BlockEntity WithItem(string itemId, int count = 1, byte? slot = null, NbtCompound? customData = null) =>
        AddItem(itemId, count, slot, customData);

    /// <summary>
    /// Sets the item at the specified slot.
    /// </summary>
    public BlockEntity SetItem(byte slot, ItemStack item)
    {
        ArgumentNullException.ThrowIfNull(item);
        item.Slot = slot;
        return AddItem(item);
    }

    /// <summary>
    /// Sets the item at the specified slot.
    /// </summary>
    public BlockEntity SetItem(int slot, ItemStack item) => SetItem((byte)slot, item);

    /// <summary>
    /// Removes the item at the specified slot.
    /// </summary>
    public bool RemoveItem(byte slot)
    {
        if (Data["Items"] is not NbtList itemsList)
            return false;

        for (int i = 0; i < itemsList.Count; i++)
        {
            if (itemsList[i] is NbtCompound comp)
            {
                byte s = comp["Slot"]?.ByteValue ?? (byte)(comp["slot"]?.ByteValue ?? 0);
                if (s == slot)
                {
                    itemsList.RemoveAt(i);
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Removes the item at the specified slot.
    /// </summary>
    public bool RemoveItem(int slot) => RemoveItem((byte)slot);

    /// <summary>
    /// Clears all items from this block entity.
    /// </summary>
    public BlockEntity ClearItems()
    {
        Data.Remove("Items");
        return this;
    }

    /// <summary>
    /// Finds the lowest unused slot index (0 to 255).
    /// </summary>
    public byte GetFirstEmptySlot()
    {
        if (Data["Items"] is not NbtList itemsList || itemsList.Count == 0)
            return 0;

        var occupied = new HashSet<byte>();
        foreach (var tag in itemsList)
        {
            if (tag is NbtCompound comp)
            {
                byte s = comp["Slot"]?.ByteValue ?? (byte)(comp["slot"]?.ByteValue ?? 0);
                occupied.Add(s);
            }
        }

        for (int i = 0; i <= 255; i++)
        {
            if (!occupied.Contains((byte)i))
                return (byte)i;
        }

        return 255;
    }

    /// <summary>
    /// Creates a chest block entity with optional items.
    /// </summary>
    public static BlockEntity CreateChest(BlockPosition position, params ItemStack[] items) =>
        CreateContainer("minecraft:chest", position, items);

    /// <summary>
    /// Creates a chest block entity with items.
    /// </summary>
    public static BlockEntity CreateChest(BlockPosition position, IEnumerable<ItemStack> items) =>
        CreateContainer("minecraft:chest", position, items);

    /// <summary>
    /// Creates a chest block entity with optional items.
    /// </summary>
    public static BlockEntity CreateChest(int x, int y, int z, params ItemStack[] items) =>
        CreateChest(new BlockPosition(x, y, z), items);

    /// <summary>
    /// Creates a chest block entity with items.
    /// </summary>
    public static BlockEntity CreateChest(int x, int y, int z, IEnumerable<ItemStack> items) =>
        CreateChest(new BlockPosition(x, y, z), items);

    /// <summary>
    /// Creates a container block entity with the specified identifier and items.
    /// </summary>
    public static BlockEntity CreateContainer(string containerId, BlockPosition position, IEnumerable<ItemStack> items) =>
        CreateContainer(containerId, position, items?.ToArray() ?? Array.Empty<ItemStack>());

    /// <summary>
    /// Creates a container block entity with the specified identifier and items.
    /// </summary>
    public static BlockEntity CreateContainer(string containerId, BlockPosition position, params ItemStack[] items)
    {
        var be = new BlockEntity(containerId, position);
        if (items != null && items.Length > 0)
        {
            var usedSlots = new HashSet<byte>();
            foreach (var item in items)
            {
                var clone = item.Clone();
                if (usedSlots.Contains(clone.Slot))
                {
                    // Auto-assign next free slot
                    for (int s = 0; s <= 255; s++)
                    {
                        if (!usedSlots.Contains((byte)s))
                        {
                            clone.Slot = (byte)s;
                            break;
                        }
                    }
                }
                usedSlots.Add(clone.Slot);
                be.AddItem(clone);
            }
        }
        return be;
    }

    /// <summary>
    /// Creates a container block entity with the specified identifier and items.
    /// </summary>
    public static BlockEntity CreateContainer(string containerId, int x, int y, int z, params ItemStack[] items) =>
        CreateContainer(containerId, new BlockPosition(x, y, z), items);

    public override string ToString() => $"{Id} at {Position}";
}
