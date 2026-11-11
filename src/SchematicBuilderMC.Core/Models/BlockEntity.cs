using System.Text.Json;
using fNbt;
using SchematicBuilderMC.Utils;

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

    /// <summary>
    /// Gets or sets the custom name of this block entity (e.g. for named containers like chests or furnaces).
    /// </summary>
    public string? CustomName
    {
        get
        {
            var tag = Data["CustomName"] ?? Data["custom_name"];
            if (tag is NbtString strTag)
                return TextComponentHelper.ExtractText(strTag.Value);
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
    /// Sets the custom name of this block entity and returns this instance.
    /// </summary>
    public BlockEntity WithCustomName(string name)
    {
        CustomName = name;
        return this;
    }

    /// <summary>
    /// Gets whether this block entity represents a sign.
    /// </summary>
    public bool IsSign =>
        Id.Equals("minecraft:sign", StringComparison.OrdinalIgnoreCase) ||
        Id.EndsWith("_sign", StringComparison.OrdinalIgnoreCase) ||
        Id.Equals("minecraft:hanging_sign", StringComparison.OrdinalIgnoreCase) ||
        Id.EndsWith("_hanging_sign", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets whether this block entity represents a decorated pot.
    /// </summary>
    public bool IsDecoratedPot =>
        Id.Equals("minecraft:decorated_pot", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets whether this block entity represents a crafter.
    /// </summary>
    public bool IsCrafter =>
        Id.Equals("minecraft:crafter", StringComparison.OrdinalIgnoreCase);

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

    /// <summary>
    /// Retrieves sign front messages, back messages, and waxed state from this block entity.
    /// </summary>
    public (IReadOnlyList<string> FrontMessages, IReadOnlyList<string> BackMessages, bool IsWaxed) GetSignText()
    {
        var front = ExtractSignMessages(Data["front_text"] as NbtCompound);
        var back = ExtractSignMessages(Data["back_text"] as NbtCompound);

        // Fallback to legacy Text1..Text4 tags if modern front_text is empty
        if (front.Count == 0 && back.Count == 0)
        {
            for (int i = 1; i <= 4; i++)
            {
                if (Data[$"Text{i}"] is NbtString sTag)
                {
                    front.Add(ParseSignTextJson(sTag.Value));
                }
            }

            while (front.Count > 0 && string.IsNullOrEmpty(front[^1]))
            {
                front.RemoveAt(front.Count - 1);
            }
        }

        bool isWaxed = (Data["is_waxed"]?.ByteValue ?? 0) != 0;
        return (front, back, isWaxed);
    }

    private static List<string> ExtractSignMessages(NbtCompound? sideComp)
    {
        var list = new List<string>();
        if (sideComp == null) return list;

        if (sideComp["messages"] is NbtList msgList)
        {
            foreach (var tag in msgList)
            {
                if (tag is NbtString strTag)
                {
                    list.Add(ParseSignTextJson(strTag.Value));
                }
            }
        }

        while (list.Count > 0 && string.IsNullOrEmpty(list[^1]))
        {
            list.RemoveAt(list.Count - 1);
        }

        return list;
    }

    private static string ParseSignTextJson(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
        if (!raw.StartsWith('{')) return raw;

        try
        {
            using var doc = JsonDocument.Parse(raw);
            if (doc.RootElement.TryGetProperty("text", out var textProp))
            {
                return textProp.GetString() ?? raw;
            }
        }
        catch
        {
            // Ignore json parse error and keep raw
        }

        return raw;
    }

    /// <summary>
    /// Creates a sign block entity with optional front messages.
    /// </summary>
    public static BlockEntity CreateSign(BlockPosition position, params string[] frontMessages) =>
        CreateSign(position, (IEnumerable<string>)frontMessages);

    /// <summary>
    /// Creates a sign block entity with optional front messages.
    /// </summary>
    public static BlockEntity CreateSign(int x, int y, int z, params string[] frontMessages) =>
        CreateSign(new BlockPosition(x, y, z), (IEnumerable<string>)frontMessages);

    /// <summary>
    /// Creates a sign block entity with front messages, optional back messages, and waxed state.
    /// </summary>
    public static BlockEntity CreateSign(
        BlockPosition position,
        IEnumerable<string>? frontMessages = null,
        IEnumerable<string>? backMessages = null,
        bool isWaxed = false,
        string frontColor = "black",
        bool frontGlowing = false,
        string backColor = "black",
        bool backGlowing = false)
    {
        var be = new BlockEntity("minecraft:sign", position);
        var data = be.Data;

        var frontArr = frontMessages?.Take(4).ToList() ?? new List<string>();
        while (frontArr.Count < 4) frontArr.Add(string.Empty);

        var frontComp = new NbtCompound("front_text")
        {
            new NbtString("color", frontColor ?? "black"),
            new NbtByte("has_glowing_text", (byte)(frontGlowing ? 1 : 0))
        };
        var frontList = new NbtList("messages", NbtTagType.String);
        for (int i = 0; i < 4; i++)
        {
            string msg = frontArr[i] ?? string.Empty;
            frontList.Add(new NbtString(msg));
            data.Add(new NbtString($"Text{i + 1}", $"{{\"text\":\"{msg}\"}}"));
        }
        frontComp.Add(frontList);
        data.Add(frontComp);

        var backArr = backMessages?.Take(4).ToList() ?? new List<string>();
        while (backArr.Count < 4) backArr.Add(string.Empty);

        var backComp = new NbtCompound("back_text")
        {
            new NbtString("color", backColor ?? "black"),
            new NbtByte("has_glowing_text", (byte)(backGlowing ? 1 : 0))
        };
        var backList = new NbtList("messages", NbtTagType.String);
        foreach (var msg in backArr)
        {
            backList.Add(new NbtString(msg ?? string.Empty));
        }
        backComp.Add(backList);
        data.Add(backComp);

        data.Add(new NbtByte("is_waxed", (byte)(isWaxed ? 1 : 0)));
        return be;
    }

    /// <summary>
    /// Creates a sign block entity with front messages, optional back messages, and waxed state.
    /// </summary>
    public static BlockEntity CreateSign(
        int x, int y, int z,
        IEnumerable<string>? frontMessages = null,
        IEnumerable<string>? backMessages = null,
        bool isWaxed = false,
        string frontColor = "black",
        bool frontGlowing = false,
        string backColor = "black",
        bool backGlowing = false) =>
        CreateSign(new BlockPosition(x, y, z), frontMessages, backMessages, isWaxed, frontColor, frontGlowing, backColor, backGlowing);

    /// <summary>
    /// Retrieves item and sherds stored in a decorated pot block entity.
    /// </summary>
    public (ItemStack? Item, string? FrontSherd, string? BackSherd, string? LeftSherd, string? RightSherd) GetDecoratedPotData()
    {
        ItemStack? item = null;
        if (Data["item"] is NbtCompound itemComp)
        {
            item = ItemStack.FromNbt(itemComp);
        }

        string? front = null, back = null, left = null, right = null;
        if (Data["sherds"] is NbtCompound sherdsComp)
        {
            front = ExtractSherd(sherdsComp["front"]);
            back = ExtractSherd(sherdsComp["back"]);
            left = ExtractSherd(sherdsComp["left"]);
            right = ExtractSherd(sherdsComp["right"]);
        }
        else if (Data["sherds"] is NbtList sherdsList && sherdsList.Count >= 4)
        {
            back = ExtractSherd(sherdsList[0]);
            left = ExtractSherd(sherdsList[1]);
            right = ExtractSherd(sherdsList[2]);
            front = ExtractSherd(sherdsList[3]);
        }

        return (item, front, back, left, right);
    }

    private static string? ExtractSherd(NbtTag? tag)
    {
        if (tag is NbtCompound comp && comp["id"] is NbtString idStr)
        {
            return idStr.Value;
        }
        if (tag is NbtString sTag)
        {
            return sTag.Value;
        }
        return null;
    }

    /// <summary>
    /// Creates a decorated pot block entity with an optional item and optional custom sherds.
    /// </summary>
    public static BlockEntity CreateDecoratedPot(
        BlockPosition position,
        ItemStack? item = null,
        string? frontSherd = null,
        string? backSherd = null,
        string? leftSherd = null,
        string? rightSherd = null)
    {
        var be = new BlockEntity("minecraft:decorated_pot", position);
        var data = be.Data;

        if (item != null)
        {
            var itemComp = item.ToNbt();
            itemComp.Name = "item";
            data.Add(itemComp);
        }

        string f = frontSherd ?? "minecraft:brick";
        string b = backSherd ?? "minecraft:brick";
        string l = leftSherd ?? "minecraft:brick";
        string r = rightSherd ?? "minecraft:brick";

        var sherdsComp = new NbtCompound("sherds")
        {
            new NbtCompound("back") { new NbtString("id", b.Contains(':') ? b : "minecraft:" + b) },
            new NbtCompound("left") { new NbtString("id", l.Contains(':') ? l : "minecraft:" + l) },
            new NbtCompound("right") { new NbtString("id", r.Contains(':') ? r : "minecraft:" + r) },
            new NbtCompound("front") { new NbtString("id", f.Contains(':') ? f : "minecraft:" + f) }
        };
        data.Add(sherdsComp);

        return be;
    }

    /// <summary>
    /// Creates a decorated pot block entity with an optional item and optional custom sherds.
    /// </summary>
    public static BlockEntity CreateDecoratedPot(
        int x, int y, int z,
        ItemStack? item = null,
        string? frontSherd = null,
        string? backSherd = null,
        string? leftSherd = null,
        string? rightSherd = null) =>
        CreateDecoratedPot(new BlockPosition(x, y, z), item, frontSherd, backSherd, leftSherd, rightSherd);

    /// <summary>
    /// Creates a decorated pot block entity with an item.
    /// </summary>
    public static BlockEntity CreateDecoratedPot(int x, int y, int z, ItemStack item) =>
        CreateDecoratedPot(new BlockPosition(x, y, z), item);

    /// <summary>
    /// Creates a decorated pot block entity with an item.
    /// </summary>
    public static BlockEntity CreateDecoratedPot(BlockPosition position, ItemStack item) =>
        CreateDecoratedPot(position, item, null, null, null, null);

    /// <summary>
    /// Retrieves disabled slots from this crafter block entity.
    /// </summary>
    public IReadOnlyList<int> GetDisabledSlots()
    {
        if (Data["disabled_slots"] is NbtIntArray intArray)
        {
            return intArray.Value;
        }
        return Array.Empty<int>();
    }

    /// <summary>
    /// Creates a crafter block entity with optional disabled slots and items.
    /// </summary>
    public static BlockEntity CreateCrafter(
        BlockPosition position,
        IEnumerable<int>? disabledSlots = null,
        IEnumerable<ItemStack>? items = null)
    {
        var be = new BlockEntity("minecraft:crafter", position);
        var data = be.Data;

        data.Add(new NbtInt("crafting_ticks_remaining", 0));
        data.Add(new NbtInt("triggered", 0));

        if (disabledSlots != null)
        {
            data.Add(new NbtIntArray("disabled_slots", disabledSlots.ToArray()));
        }

        if (items != null)
        {
            var itemsList = new NbtList("Items", NbtTagType.Compound);
            foreach (var item in items)
            {
                itemsList.Add(item.ToNbt());
            }
            data.Add(itemsList);
        }

        return be;
    }

    /// <summary>
    /// Creates a crafter block entity with disabled slots.
    /// </summary>
    public static BlockEntity CreateCrafter(int x, int y, int z, params int[] disabledSlots) =>
        CreateCrafter(new BlockPosition(x, y, z), disabledSlots, null);

    /// <summary>
    /// Creates a crafter block entity with disabled slots and items.
    /// </summary>
    public static BlockEntity CreateCrafter(int x, int y, int z, IEnumerable<int>? disabledSlots, params ItemStack[] items) =>
        CreateCrafter(new BlockPosition(x, y, z), disabledSlots, items);

    public override string ToString() => $"{Id} at {Position}";
}
