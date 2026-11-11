using fNbt;
using SchematicBuilderMC.Registries;
using SchematicBuilderMC.Utils;

namespace SchematicBuilderMC.Models;

/// <summary>
/// Represents an inventory item stack with an identifier, count, slot index, and optional custom NBT data.
/// Commonly used in container block entities such as chests, barrels, dispensers, droppers, hoppers, and furnaces.
/// </summary>
public sealed class ItemStack : IEquatable<ItemStack>
{
    private string _id;
    private int _count;

    /// <summary>
    /// Gets or sets the namespaced item identifier (e.g., "minecraft:diamond").
    /// </summary>
    public string Id
    {
        get => _id;
        set
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            _id = value.Contains(':') ? value : "minecraft:" + value;
        }
    }

    /// <summary>
    /// Gets or sets the quantity of items in this stack (clamped to &gt;= 1).
    /// </summary>
    public int Count
    {
        get => _count;
        set => _count = Math.Max(1, value);
    }

    /// <summary>
    /// Gets or sets the slot index inside the container inventory (e.g. 0 to 26 for standard chests).
    /// </summary>
    public byte Slot { get; set; }

    /// <summary>
    /// Gets or sets additional custom NBT data (such as display name, lore, enchantments, components).
    /// </summary>
    public NbtCompound? CustomData { get; set; }

    /// <summary>
    /// Gets whether this item stack represents a Name Tag (<c>minecraft:name_tag</c>).
    /// </summary>
    public bool IsNameTag => Id.Equals("minecraft:name_tag", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or sets the custom display name of the item.
    /// Reads from and writes to either modern data components (<c>minecraft:custom_name</c>)
    /// or legacy NBT tags (<c>display.Name</c>).
    /// </summary>
    public string? DisplayName
    {
        get => GetDisplayName();
        set
        {
            if (value == null)
            {
                RemoveDisplayName();
            }
            else
            {
                WithDisplayName(value);
            }
        }
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ItemStack"/>.
    /// </summary>
    /// <param name="id">The namespaced item identifier (e.g., <c>Minecraft.Items.Diamond</c>).</param>
    /// <param name="count">The item count (default is 1).</param>
    /// <param name="slot">The inventory slot index (default is 0).</param>
    /// <param name="customData">Optional custom NBT tag/components compound.</param>
    public ItemStack(string id, int count = 1, byte slot = 0, NbtCompound? customData = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        _id = id.Contains(':') ? id : "minecraft:" + id;
        _count = Math.Max(1, count);
        Slot = slot;
        CustomData = customData;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ItemStack"/> with an integer slot index.
    /// </summary>
    public ItemStack(string id, int count, int slot, NbtCompound? customData = null)
        : this(id, count, (byte)slot, customData)
    {
    }

    /// <summary>
    /// Sets the item stack count and returns this instance.
    /// </summary>
    public ItemStack WithCount(int count)
    {
        Count = count;
        return this;
    }

    /// <summary>
    /// Sets the container inventory slot index and returns this instance.
    /// </summary>
    public ItemStack WithSlot(byte slot)
    {
        Slot = slot;
        return this;
    }

    /// <summary>
    /// Sets the container inventory slot index and returns this instance.
    /// </summary>
    public ItemStack WithSlot(int slot)
    {
        Slot = (byte)slot;
        return this;
    }

    /// <summary>
    /// Sets the custom NBT data for this item stack and returns this instance.
    /// </summary>
    public ItemStack WithCustomData(NbtCompound customData)
    {
        CustomData = customData;
        return this;
    }

    /// <summary>
    /// Sets a custom display name for this item stack.
    /// Supports plain text or raw Minecraft JSON text component.
    /// Updates both legacy <c>display.Name</c> and modern data component <c>minecraft:custom_name</c>.
    /// </summary>
    /// <param name="name">The display name text or JSON text component.</param>
    public ItemStack WithDisplayName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        CustomData ??= new NbtCompound();

        string jsonName = name.StartsWith('{') && name.EndsWith('}')
            ? name
            : $"{{\"text\":\"{TextComponentHelper.EscapeJsonString(name)}\"}}";

        // Modern 1.20.5+ component support
        if (CustomData.Name == "components" || CustomData.Contains("minecraft:custom_name") || CustomData.Contains("custom_name"))
        {
            CustomData["minecraft:custom_name"] = new NbtString("minecraft:custom_name", jsonName);
        }
        else if (CustomData["components"] is NbtCompound componentsComp)
        {
            componentsComp["minecraft:custom_name"] = new NbtString("minecraft:custom_name", jsonName);
        }

        // Legacy display.Name tag support (when not purely a modern components compound)
        if (CustomData.Name != "components")
        {
            NbtCompound displayCompound;
            if (CustomData.Contains("display") && CustomData["display"] is NbtCompound existingDisplay)
            {
                displayCompound = existingDisplay;
            }
            else
            {
                displayCompound = new NbtCompound("display");
                CustomData["display"] = displayCompound;
            }

            displayCompound["Name"] = new NbtString("Name", jsonName);
        }

        return this;
    }

    /// <summary>
    /// Renames this item stack as if applying a Name Tag.
    /// </summary>
    /// <param name="name">The name to apply to the item.</param>
    public ItemStack ApplyNameTag(string name) => WithDisplayName(name);

    /// <summary>
    /// Renames this item stack using the display name from another item stack (such as a named Name Tag).
    /// </summary>
    /// <param name="nameTag">The named Name Tag item stack.</param>
    public ItemStack ApplyNameTag(ItemStack nameTag)
    {
        ArgumentNullException.ThrowIfNull(nameTag);
        string? name = nameTag.DisplayName;
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("The provided Name Tag does not have a display name.", nameof(nameTag));
        }
        return WithDisplayName(name);
    }

    /// <summary>
    /// Creates a new <see cref="ItemStack"/> representing a named Name Tag (<c>minecraft:name_tag</c>).
    /// </summary>
    /// <param name="customName">The custom name assigned to the Name Tag.</param>
    /// <param name="count">The quantity of Name Tags (default is 1).</param>
    /// <param name="slot">The inventory slot index (default is 0).</param>
    public static ItemStack CreateNameTag(string customName, int count = 1, byte slot = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(customName);
        return new ItemStack(Minecraft.Items.NameTag, count, slot).WithDisplayName(customName);
    }

    /// <summary>
    /// Creates a new <see cref="ItemStack"/> representing a named Name Tag with an integer slot index.
    /// </summary>
    /// <param name="customName">The custom name assigned to the Name Tag.</param>
    /// <param name="count">The quantity of Name Tags.</param>
    /// <param name="slot">The inventory slot index.</param>
    public static ItemStack CreateNameTag(string customName, int count, int slot) =>
        CreateNameTag(customName, count, (byte)slot);

    /// <summary>
    /// Retrieves the clean human-readable display name of this item stack, or <c>null</c> if not named.
    /// Checks modern data components (<c>minecraft:custom_name</c>, <c>minecraft:item_name</c>)
    /// as well as legacy NBT tag (<c>display.Name</c>).
    /// </summary>
    public string? GetDisplayName()
    {
        if (CustomData == null) return null;

        // 1. Direct component tags
        if (CustomData["minecraft:custom_name"] is NbtString compName)
            return TextComponentHelper.ExtractText(compName.Value);
        if (CustomData["custom_name"] is NbtString compNameShort)
            return TextComponentHelper.ExtractText(compNameShort.Value);
        if (CustomData["minecraft:item_name"] is NbtString itemName)
            return TextComponentHelper.ExtractText(itemName.Value);
        if (CustomData["item_name"] is NbtString itemNameShort)
            return TextComponentHelper.ExtractText(itemNameShort.Value);

        // 2. Nested "components" compound
        if (CustomData["components"] is NbtCompound components)
        {
            if (components["minecraft:custom_name"] is NbtString nCompName)
                return TextComponentHelper.ExtractText(nCompName.Value);
            if (components["custom_name"] is NbtString nCompNameShort)
                return TextComponentHelper.ExtractText(nCompNameShort.Value);
            if (components["minecraft:item_name"] is NbtString nItemName)
                return TextComponentHelper.ExtractText(nItemName.Value);
            if (components["item_name"] is NbtString nItemNameShort)
                return TextComponentHelper.ExtractText(nItemNameShort.Value);
        }

        // 3. Legacy "display" compound
        if (CustomData["display"] is NbtCompound display && display["Name"] is NbtString nameTag)
        {
            return TextComponentHelper.ExtractText(nameTag.Value);
        }

        // 4. Nested "tag" compound
        if (CustomData["tag"] is NbtCompound tagComp && tagComp["display"] is NbtCompound nestedDisplay && nestedDisplay["Name"] is NbtString nestedName)
        {
            return TextComponentHelper.ExtractText(nestedName.Value);
        }

        return null;
    }

    /// <summary>
    /// Removes the custom display name from this item stack.
    /// </summary>
    public void RemoveDisplayName()
    {
        if (CustomData == null) return;

        CustomData.Remove("minecraft:custom_name");
        CustomData.Remove("custom_name");
        CustomData.Remove("minecraft:item_name");
        CustomData.Remove("item_name");

        if (CustomData["components"] is NbtCompound components)
        {
            components.Remove("minecraft:custom_name");
            components.Remove("custom_name");
            components.Remove("minecraft:item_name");
            components.Remove("item_name");
        }

        if (CustomData["display"] is NbtCompound display)
        {
            display.Remove("Name");
            if (display.Count == 0)
            {
                CustomData.Remove("display");
            }
        }

        if (CustomData["tag"] is NbtCompound tagComp && tagComp["display"] is NbtCompound tagDisplay)
        {
            tagDisplay.Remove("Name");
            if (tagDisplay.Count == 0)
            {
                tagComp.Remove("display");
            }
        }
    }

    /// <summary>
    /// Adds lore lines to this item stack's display tag.
    /// </summary>
    /// <param name="loreLines">One or more lore strings.</param>
    public ItemStack WithLore(params string[] loreLines)
    {
        if (loreLines == null || loreLines.Length == 0) return this;

        CustomData ??= new NbtCompound();
        NbtCompound displayCompound;

        if (CustomData.Contains("display") && CustomData["display"] is NbtCompound existingDisplay)
        {
            displayCompound = existingDisplay;
        }
        else
        {
            displayCompound = new NbtCompound("display");
            CustomData["display"] = displayCompound;
        }

        NbtList loreList;
        if (displayCompound.Contains("Lore") && displayCompound["Lore"] is NbtList existingLore)
        {
            loreList = existingLore;
        }
        else
        {
            loreList = new NbtList("Lore", NbtTagType.String);
            displayCompound["Lore"] = loreList;
        }

        foreach (var line in loreLines)
        {
            string jsonLine = line.StartsWith('{') && line.EndsWith('}')
                ? line
                : $"{{\"text\":\"{EscapeJsonString(line)}\"}}";

            loreList.Add(new NbtString(jsonLine));
        }

        return this;
    }

    /// <summary>
    /// Adds an enchantment to this item stack.
    /// </summary>
    /// <param name="enchantmentId">The enchantment identifier (e.g. "minecraft:sharpness" or "sharpness").</param>
    /// <param name="level">The enchantment level (default is 1).</param>
    public ItemStack WithEnchantment(string enchantmentId, short level = 1)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(enchantmentId);

        string fullId = enchantmentId.Contains(':') ? enchantmentId : "minecraft:" + enchantmentId;
        CustomData ??= new NbtCompound();

        NbtList enchList;
        if (CustomData.Contains("Enchantments") && CustomData["Enchantments"] is NbtList existingEnch)
        {
            enchList = existingEnch;
        }
        else
        {
            enchList = new NbtList("Enchantments", NbtTagType.Compound);
            CustomData["Enchantments"] = enchList;
        }

        enchList.Add(new NbtCompound
        {
            new NbtString("id", fullId),
            new NbtShort("lvl", level)
        });

        return this;
    }

    /// <summary>
    /// Sets the durability damage for this item stack.
    /// </summary>
    /// <param name="damage">Durability damage value.</param>
    public ItemStack WithDamage(int damage)
    {
        CustomData ??= new NbtCompound();
        CustomData["Damage"] = new NbtInt("Damage", damage);
        return this;
    }

    /// <summary>
    /// Converts this item stack to a standard Minecraft container item <see cref="NbtCompound"/>.
    /// </summary>
    public NbtCompound ToNbt()
    {
        var compound = new NbtCompound
        {
            new NbtByte("Slot", Slot),
            new NbtString("id", Id),
            new NbtByte("Count", (byte)Math.Clamp(Count, 1, 127))
        };

        if (CustomData != null && CustomData.Count > 0)
        {
            if (CustomData.Name is "tag" or "components")
            {
                compound.Add((NbtCompound)CustomData.Clone());
            }
            else
            {
                var tagClone = (NbtCompound)CustomData.Clone();
                tagClone.Name = "tag";
                compound.Add(tagClone);
            }
        }

        return compound;
    }

    /// <summary>
    /// Creates an <see cref="ItemStack"/> instance from a Minecraft container item <see cref="NbtCompound"/>.
    /// </summary>
    /// <param name="compound">The NBT compound representing an item inside a container.</param>
    public static ItemStack FromNbt(NbtCompound compound)
    {
        ArgumentNullException.ThrowIfNull(compound);

        byte slot = 0;
        if (compound["Slot"] is NbtTag slotTag)
        {
            slot = (byte)slotTag.IntValue;
        }
        else if (compound["slot"] is NbtTag lowerSlotTag)
        {
            slot = (byte)lowerSlotTag.IntValue;
        }

        string id = compound["id"]?.StringValue ?? compound["Id"]?.StringValue ?? "minecraft:air";

        int count = 1;
        if (compound["Count"] is NbtTag countTag)
        {
            count = Math.Max(1, countTag.IntValue);
        }
        else if (compound["count"] is NbtTag lowerCountTag)
        {
            count = Math.Max(1, lowerCountTag.IntValue);
        }

        NbtCompound? customData = null;
        if (compound["tag"] is NbtCompound tagComp)
        {
            customData = (NbtCompound)tagComp.Clone();
        }
        else if (compound["components"] is NbtCompound componentsComp)
        {
            customData = (NbtCompound)componentsComp.Clone();
        }

        return new ItemStack(id, count, slot, customData);
    }

    /// <summary>
    /// Implicitly converts a string item identifier to an <see cref="ItemStack"/> with count 1 and slot 0.
    /// </summary>
    public static implicit operator ItemStack(string itemId) => new(itemId);

    /// <summary>
    /// Creates a deep copy of this item stack.
    /// </summary>
    public ItemStack Clone()
    {
        return new ItemStack(Id, Count, Slot, CustomData != null ? (NbtCompound)CustomData.Clone() : null);
    }

    public bool Equals(ItemStack? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return string.Equals(Id, other.Id, StringComparison.OrdinalIgnoreCase) &&
               Count == other.Count &&
               Slot == other.Slot;
    }

    public override bool Equals(object? obj) =>
        obj is ItemStack other && Equals(other);

    public override int GetHashCode() =>
        HashCode.Combine(StringComparer.OrdinalIgnoreCase.GetHashCode(Id), Count, Slot);

    public static bool operator ==(ItemStack? left, ItemStack? right) =>
        Equals(left, right);

    public static bool operator !=(ItemStack? left, ItemStack? right) =>
        !Equals(left, right);

    public override string ToString() => $"{Count}x {Id} (Slot {Slot})";

    private static string EscapeJsonString(string value) =>
        value.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
