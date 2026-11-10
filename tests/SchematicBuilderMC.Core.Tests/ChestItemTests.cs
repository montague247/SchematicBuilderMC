using fNbt;
using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;
using SchematicBuilderMC.Extensions;
using SchematicBuilderMC.Generators;
using SchematicBuilderMC.IO;
using SchematicBuilderMC.Models;
using SchematicBuilderMC.Registries;
using Xunit;

namespace SchematicBuilderMC.Core.Tests;

public class ChestItemTests
{
    [Fact]
    public void ItemStack_Constructor_NormalizesIdentifierAndInitializesDefaults()
    {
        var item = new ItemStack("diamond", count: 64, slot: 2);
        Assert.Equal("minecraft:diamond", item.Id);
        Assert.Equal(64, item.Count);
        Assert.Equal(2, item.Slot);
        Assert.Null(item.CustomData);

        var itemNamespaced = new ItemStack("minecraft:iron_ingot");
        Assert.Equal("minecraft:iron_ingot", itemNamespaced.Id);
        Assert.Equal(1, itemNamespaced.Count);
        Assert.Equal(0, itemNamespaced.Slot);
    }

    [Fact]
    public void ItemStack_CountClamping_EnsuresAtLeastOne()
    {
        var item = new ItemStack("minecraft:stick", count: 0);
        Assert.Equal(1, item.Count);

        item.Count = -5;
        Assert.Equal(1, item.Count);

        item.WithCount(0);
        Assert.Equal(1, item.Count);
    }

    [Fact]
    public void ItemStack_ImplicitConversionFromString_Works()
    {
        ItemStack item = Minecraft.Items.Diamond;
        Assert.Equal("minecraft:diamond", item.Id);
        Assert.Equal(1, item.Count);
        Assert.Equal(0, item.Slot);
    }

    [Fact]
    public void ItemStack_FluentWithMethods_ConfigureProperties()
    {
        var item = new ItemStack(Minecraft.Items.NetheriteSword)
            .WithCount(1)
            .WithSlot(4)
            .WithDisplayName("Excalibur")
            .WithLore("Forged in starlight", "Deals immense damage")
            .WithEnchantment(Minecraft.Items.Chest, 5) // or any enchantment id
            .WithDamage(10);

        Assert.Equal(4, item.Slot);
        Assert.Equal(1, item.Count);
        Assert.NotNull(item.CustomData);
        Assert.True(item.CustomData.Contains("display"));
        Assert.True(item.CustomData.Contains("Enchantments"));
        Assert.True(item.CustomData.Contains("Damage"));
    }

    [Fact]
    public void ItemStack_ToNbtAndFromNbt_RoundtripsSuccessfully()
    {
        var original = new ItemStack(Minecraft.Items.GoldenApple, count: 16, slot: 8)
            .WithDisplayName("Super Apple")
            .WithEnchantment("minecraft:sharpness", 3);

        var nbt = original.ToNbt();
        Assert.Equal((byte)8, nbt["Slot"]?.ByteValue);
        Assert.Equal("minecraft:golden_apple", nbt["id"]?.StringValue);
        Assert.Equal((byte)16, nbt["Count"]?.ByteValue);

        var deserialized = ItemStack.FromNbt(nbt);
        Assert.Equal(original.Id, deserialized.Id);
        Assert.Equal(original.Count, deserialized.Count);
        Assert.Equal(original.Slot, deserialized.Slot);
        Assert.NotNull(deserialized.CustomData);
        Assert.True(deserialized.CustomData.Contains("display"));
        Assert.True(deserialized.CustomData.Contains("Enchantments"));
    }

    [Fact]
    public void BlockEntity_AddItem_AddsAndAutoAssignsSlots()
    {
        var chest = BlockEntity.CreateChest(new BlockPosition(1, 2, 3));
        Assert.False(chest.HasItems);
        Assert.Empty(chest.GetItems());

        chest.AddItem(Minecraft.Items.Diamond, 64);
        Assert.True(chest.HasItems);
        Assert.Single(chest.GetItems());
        Assert.Equal((byte)0, chest.GetItems()[0].Slot);

        // Second item without explicit slot should take next free slot (1)
        chest.AddItem(Minecraft.Items.GoldIngot, 32);
        Assert.Equal(2, chest.GetItems().Count);
        Assert.Equal((byte)0, chest.GetItem(0)?.Slot);
        Assert.Equal("minecraft:diamond", chest.GetItem(0)?.Id);
        Assert.Equal((byte)1, chest.GetItem(1)?.Slot);
        Assert.Equal("minecraft:gold_ingot", chest.GetItem(1)?.Id);

        // Explicit slot
        chest.AddItem(Minecraft.Items.Emerald, 16, slot: 10);
        Assert.Equal(3, chest.GetItems().Count);
        Assert.Equal((byte)10, chest.GetItem(10)?.Slot);
        Assert.Equal("minecraft:emerald", chest.GetItem(10)?.Id);

        // Next auto-slot should be 2 (since 0, 1, 10 are used)
        chest.AddItem(Minecraft.Items.IronIngot, 8);
        Assert.Equal(4, chest.GetItems().Count);
        Assert.Equal((byte)2, chest.GetItem(2)?.Slot);
    }

    [Fact]
    public void BlockEntity_SetItem_And_RemoveItem_WorkCorrectly()
    {
        var chest = BlockEntity.CreateChest(0, 0, 0,
            new ItemStack(Minecraft.Items.Diamond, 64, slot: 5),
            new ItemStack(Minecraft.Items.Emerald, 32, slot: 6));

        Assert.Equal(2, chest.GetItems().Count);
        Assert.NotNull(chest.GetItem(5));
        Assert.NotNull(chest.GetItem(6));

        // Replace slot 5
        chest.SetItem(5, new ItemStack(Minecraft.Items.NetheriteIngot, 10));
        Assert.Equal("minecraft:netherite_ingot", chest.GetItem(5)?.Id);
        Assert.Equal(10, chest.GetItem(5)?.Count);
        Assert.Equal(2, chest.GetItems().Count);

        // Remove slot 6
        bool removed = chest.RemoveItem(6);
        Assert.True(removed);
        Assert.Null(chest.GetItem(6));
        Assert.Single(chest.GetItems());

        // Clear all
        chest.ClearItems();
        Assert.False(chest.HasItems);
        Assert.Empty(chest.GetItems());
    }

    [Fact]
    public void BlockEntity_CreateChest_AutoAssignsSlotsForMultipleItems()
    {
        var chest = BlockEntity.CreateChest(new BlockPosition(0, 0, 0),
            Minecraft.Items.Diamond,
            Minecraft.Items.Emerald,
            Minecraft.Items.GoldIngot);

        var items = chest.GetItems();
        Assert.Equal(3, items.Count);
        Assert.Equal((byte)0, items[0].Slot);
        Assert.Equal((byte)1, items[1].Slot);
        Assert.Equal((byte)2, items[2].Slot);
    }

    [Fact]
    public void Schematic_SetChest_And_AddItem_WorkCorrectly()
    {
        var schematic = new Schematic(5, 5, 5);

        schematic.SetChest(1, 1, 1,
            new ItemStack(Minecraft.Items.Diamond, 64, slot: 0),
            new ItemStack(Minecraft.Items.GoldenApple, 8, slot: 1));

        Assert.Equal(Minecraft.Blocks.Chest, schematic[1, 1, 1]);
        var be = schematic.GetBlockEntity(1, 1, 1);
        Assert.NotNull(be);
        Assert.Equal(Minecraft.BlockEntities.Chest, be.Id);

        var items = schematic.GetItems(1, 1, 1);
        Assert.Equal(2, items.Count);
        Assert.Equal(64, items[0].Count);
        Assert.Equal(8, items[1].Count);

        // Adding another item to the same chest
        schematic.AddItem(1, 1, 1, Minecraft.Items.IronSword, 1, slot: 4);
        var updatedItems = schematic.GetItems(1, 1, 1);
        Assert.Equal(3, updatedItems.Count);
        Assert.Equal("minecraft:iron_sword", be.GetItem(4)?.Id);
    }

    [Fact]
    public void SchematicBuilder_SetChest_And_AddItem_FluentBuilding()
    {
        var schematic = SchematicBuilder.Create(4, 3, 4)
            .SetBlock(1, 1, 1, Minecraft.Blocks.Chest.WithProperty("facing", "south"))
            .SetChest(1, 1, 1,
                new ItemStack(Minecraft.Items.Diamond, 64, slot: 0),
                new ItemStack(Minecraft.Items.Bow, 1, slot: 5))
            .AddItem(1, 1, 1, Minecraft.Items.Arrow, 64)
            .Build();

        Assert.Equal("minecraft:chest[facing=south]", schematic[1, 1, 1].ToString());
        var be = schematic.GetBlockEntity(1, 1, 1);
        Assert.NotNull(be);
        Assert.Equal(3, be.GetItems().Count);
        Assert.Equal("minecraft:diamond", be.GetItem(0)?.Id);
        Assert.Equal("minecraft:bow", be.GetItem(5)?.Id);
        Assert.Equal("minecraft:arrow", be.GetItem(1)?.Id); // slot 0 was taken, slot 1 was free
    }

    [Fact]
    public void SpongeSchematic_Roundtrip_PreservesChestItems()
    {
        var original = SchematicBuilder.Create(3, 3, 3)
            .SetChest(1, 1, 1,
                new ItemStack(Minecraft.Items.Diamond, 64, slot: 0),
                new ItemStack(Minecraft.Items.GoldenApple, 16, slot: 2),
                new ItemStack(Minecraft.Items.IronSword, 1, slot: 4).WithDisplayName("Sponge Blade"))
            .Build();

        byte[] bytes = SchematicFile.WriteToBytes(original, SchematicFormat.SpongeV2);
        var loaded = SchematicFile.Read(bytes, SchematicFormat.SpongeV2);

        var be = loaded.GetBlockEntity(1, 1, 1);
        Assert.NotNull(be);
        Assert.Equal(Minecraft.BlockEntities.Chest, be.Id);
        Assert.True(be.HasItems);

        var items = be.GetItems();
        Assert.Equal(3, items.Count);

        var diamond = be.GetItem(0);
        Assert.NotNull(diamond);
        Assert.Equal("minecraft:diamond", diamond.Id);
        Assert.Equal(64, diamond.Count);

        var apple = be.GetItem(2);
        Assert.NotNull(apple);
        Assert.Equal("minecraft:golden_apple", apple.Id);
        Assert.Equal(16, apple.Count);

        var sword = be.GetItem(4);
        Assert.NotNull(sword);
        Assert.Equal("minecraft:iron_sword", sword.Id);
        Assert.Equal(1, sword.Count);
        Assert.NotNull(sword.CustomData);
        Assert.True(sword.CustomData.Contains("display"));
    }

    [Fact]
    public void ClassicSchematic_Roundtrip_PreservesChestItems()
    {
        var original = SchematicBuilder.Create(3, 3, 3)
            .SetChest(1, 1, 1,
                new ItemStack(Minecraft.Items.Diamond, 64, slot: 0),
                new ItemStack(Minecraft.Items.Emerald, 32, slot: 1))
            .Build();

        byte[] bytes = SchematicFile.WriteToBytes(original, SchematicFormat.Classic);
        var loaded = SchematicFile.Read(bytes, SchematicFormat.Classic);

        var be = loaded.GetBlockEntity(1, 1, 1);
        Assert.NotNull(be);
        Assert.True(be.HasItems);

        var items = be.GetItems();
        Assert.Equal(2, items.Count);

        var diamond = be.GetItem(0);
        Assert.NotNull(diamond);
        Assert.Equal("minecraft:diamond", diamond.Id);
        Assert.Equal(64, diamond.Count);

        var emerald = be.GetItem(1);
        Assert.NotNull(emerald);
        Assert.Equal("minecraft:emerald", emerald.Id);
        Assert.Equal(32, emerald.Count);
    }

    [Fact]
    public void FluentBuilderCodeGenerator_FormatsChestWithItemsFluently()
    {
        var schematic = SchematicBuilder.Create(5, 4, 5)
            .SetBlock(2, 1, 3, Minecraft.Blocks.Chest.WithFacing("north"))
            .SetChest(2, 1, 3,
                new ItemStack(Minecraft.Items.Diamond, count: 64, slot: 0),
                new ItemStack(Minecraft.Items.GoldenApple, count: 16, slot: 1),
                new ItemStack(Minecraft.Items.IronSword, count: 1, slot: 4).WithDisplayName("Watchtower Blade"))
            .Build();

        string code = FluentBuilderCodeGenerator.Generate(schematic);
        Assert.Contains(".SetBlock(2, 1, 3, Minecraft.Blocks.Chest.WithFacing(\"north\"))", code);
        Assert.Contains(".SetChest(2, 1, 3,", code);
        Assert.Contains("new ItemStack(Minecraft.Items.Diamond, count: 64, slot: 0),", code);
        Assert.Contains("new ItemStack(Minecraft.Items.GoldenApple, count: 16, slot: 1),", code);
        Assert.Contains("new ItemStack(Minecraft.Items.IronSword, count: 1, slot: 4).WithDisplayName(\"Watchtower Blade\"))", code);
        Assert.DoesNotContain(".AddBlockEntity(", code);
    }

    [Fact]
    public void FluentBuilderCodeGenerator_RawStringsOption_EmitsRawItemStrings()
    {
        var schematic = SchematicBuilder.Create(2, 2, 2)
            .SetChest(0, 0, 0,
                new ItemStack(Minecraft.Items.Diamond, 64, slot: 0))
            .Build();

        var options = new FluentCodeGeneratorOptions { UseStronglyTypedItems = false };
        string code = FluentBuilderCodeGenerator.Generate(schematic, options);
        Assert.Contains("new ItemStack(\"minecraft:diamond\", count: 64, slot: 0)", code);
    }
}
