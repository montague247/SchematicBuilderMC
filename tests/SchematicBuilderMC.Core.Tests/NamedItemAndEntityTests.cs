using fNbt;
using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;
using SchematicBuilderMC.Generators;
using SchematicBuilderMC.Models;
using SchematicBuilderMC.Registries;
using SchematicBuilderMC.Utils;
using Xunit;

namespace SchematicBuilderMC.Core.Tests;

public class NamedItemAndEntityTests
{
    [Fact]
    public void TextComponentHelper_ExtractText_HandlesVariousFormats()
    {
        // Plain text
        Assert.Equal("", TextComponentHelper.ExtractText(null));
        Assert.Equal("", TextComponentHelper.ExtractText("   "));
        Assert.Equal("Simple Name", TextComponentHelper.ExtractText("Simple Name"));

        // Single text component
        Assert.Equal("Excalibur", TextComponentHelper.ExtractText("{\"text\":\"Excalibur\"}"));

        // Anvil / Name Tag format with italic: false
        Assert.Equal("Watchtower Blade", TextComponentHelper.ExtractText("{\"text\":\"Watchtower Blade\",\"italic\":false}"));

        // Multiple formatting tags
        Assert.Equal("Legendary Bow", TextComponentHelper.ExtractText("{\"text\":\"Legendary Bow\",\"color\":\"gold\",\"bold\":true,\"italic\":false}"));

        // Extra array
        Assert.Equal("Hello World", TextComponentHelper.ExtractText("{\"text\":\"Hello \",\"extra\":[{\"text\":\"World\"}]}"));
        Assert.Equal("Part 1 Part 2", TextComponentHelper.ExtractText("{\"text\":\"\",\"extra\":[{\"text\":\"Part 1 \"},{\"text\":\"Part 2\"}]}"));

        // Quoted JSON string
        Assert.Equal("Quoted Name", TextComponentHelper.ExtractText("\"Quoted Name\""));

        // Array of components
        Assert.Equal("AB", TextComponentHelper.ExtractText("[{\"text\":\"A\"},{\"text\":\"B\"}]"));

        // Malformed JSON falls back gracefully
        Assert.Equal("{broken json", TextComponentHelper.ExtractText("{broken json"));
    }

    [Fact]
    public void TextComponentHelper_CreateTextComponent_FormatsCorrectly()
    {
        Assert.Equal("{\"text\":\"My Name\"}", TextComponentHelper.CreateTextComponent("My Name"));
        Assert.Equal("{\"text\":\"My Name\",\"italic\":false}", TextComponentHelper.CreateTextComponent("My Name", italic: false));
        Assert.Equal("{\"text\":\"My Name\",\"italic\":true}", TextComponentHelper.CreateTextComponent("My Name", italic: true));
        Assert.Equal("{\"text\":\"Already\"}", TextComponentHelper.CreateTextComponent("{\"text\":\"Already\"}"));
    }

    [Fact]
    public void ItemStack_CreateNameTag_SetsProperDefaults()
    {
        var nameTag = ItemStack.CreateNameTag("Bessie", count: 2, slot: 5);

        Assert.True(nameTag.IsNameTag);
        Assert.Equal(Minecraft.Items.NameTag, nameTag.Id);
        Assert.Equal(2, nameTag.Count);
        Assert.Equal(5, nameTag.Slot);
        Assert.Equal("Bessie", nameTag.DisplayName);
    }

    [Fact]
    public void ItemStack_DisplayName_ReadsFromLegacyAndModernFormats()
    {
        // Legacy display.Name with italic: false (anvil output)
        var legacyItem = new ItemStack(Minecraft.Items.DiamondSword, 1, 0, new NbtCompound
        {
            new NbtCompound("display")
            {
                new NbtString("Name", "{\"text\":\"Frostmourne\",\"italic\":false}")
            }
        });
        Assert.Equal("Frostmourne", legacyItem.DisplayName);

        // Modern 1.20.5+ component format
        var modernItem = new ItemStack(Minecraft.Items.NetheriteAxe, 1, 0, new NbtCompound
        {
            new NbtString("minecraft:custom_name", "{\"text\":\"Worldbreaker\",\"italic\":false}")
        });
        Assert.Equal("Worldbreaker", modernItem.DisplayName);

        // Nested inside components compound
        var nestedCompItem = new ItemStack(Minecraft.Items.Bow, 1, 0, new NbtCompound
        {
            new NbtCompound("components")
            {
                new NbtString("minecraft:custom_name", "{\"text\":\"Sunstrider\"}")
            }
        });
        Assert.Equal("Sunstrider", nestedCompItem.DisplayName);

        // Plain string without JSON
        var plainItem = new ItemStack(Minecraft.Items.IronSword, 1, 0, new NbtCompound
        {
            new NbtCompound("display")
            {
                new NbtString("Name", "Simple Plain Blade")
            }
        });
        Assert.Equal("Simple Plain Blade", plainItem.DisplayName);
    }

    [Fact]
    public void ItemStack_DisplayName_SetterAndRemover_WorksCorrectly()
    {
        var item = new ItemStack(Minecraft.Items.Shears);
        Assert.Null(item.DisplayName);

        item.DisplayName = "Golden Clippers";
        Assert.Equal("Golden Clippers", item.DisplayName);

        item.DisplayName = null;
        Assert.Null(item.DisplayName);
    }

    [Fact]
    public void ItemStack_ApplyNameTag_RenamesItem()
    {
        var sword = new ItemStack(Minecraft.Items.DiamondSword);
        sword.ApplyNameTag("Dragon Slayer");
        Assert.Equal("Dragon Slayer", sword.DisplayName);

        var nameTag = ItemStack.CreateNameTag("Godslayer");
        sword.ApplyNameTag(nameTag);
        Assert.Equal("Godslayer", sword.DisplayName);

        var emptyNameTag = new ItemStack(Minecraft.Items.NameTag);
        Assert.Throws<ArgumentException>(() => sword.ApplyNameTag(emptyNameTag));
    }

    [Fact]
    public void Entity_CustomName_ConfiguresEntityAndDefaults()
    {
        var cow = new Entity(Minecraft.Entities.Cow, 10.5, 64.0, 10.5);
        Assert.Null(cow.CustomName);
        Assert.False(cow.CustomNameVisible);
        Assert.False(cow.PersistenceRequired);

        cow.WithCustomName("Bessie");
        Assert.Equal("Bessie", cow.CustomName);
        Assert.True(cow.CustomNameVisible);
        Assert.True(cow.PersistenceRequired);

        // Apply via named ItemStack Name Tag
        var nameTag = ItemStack.CreateNameTag("Dinnerbone");
        cow.ApplyNameTag(nameTag);
        Assert.Equal("Dinnerbone", cow.CustomName);
        Assert.True(cow.CustomNameVisible);
        Assert.True(cow.PersistenceRequired);
    }

    [Fact]
    public void SchematicAndBuilder_AddNamedEntity_AddsSuccessfully()
    {
        var schematic = SchematicBuilder.Create(5, 5, 5)
            .AddEntity(Minecraft.Entities.Sheep, 2.5, 1.0, 2.5, customName: "jeb_")
            .Build();

        Assert.Single(schematic.Entities);
        var sheep = schematic.Entities[0];
        Assert.Equal("minecraft:sheep", sheep.Id);
        Assert.Equal("jeb_", sheep.CustomName);
        Assert.True(sheep.CustomNameVisible);
        Assert.True(sheep.PersistenceRequired);
    }

    [Fact]
    public void BlockEntity_CustomName_WorksForContainers()
    {
        var chest = new BlockEntity(Minecraft.BlockEntities.Chest, new BlockPosition(1, 1, 1));
        Assert.Null(chest.CustomName);

        chest.WithCustomName("Loot Cache");
        Assert.Equal("Loot Cache", chest.CustomName);

        chest.CustomName = null;
        Assert.Null(chest.CustomName);
    }

    [Fact]
    public void FluentBuilderCodeGenerator_NamedItemsWithAnvilFormat_EmitsCleanDisplayName()
    {
        // Anvil format: {"text":"...","italic":false}
        var chestBe = new BlockEntity(Minecraft.BlockEntities.Chest, new BlockPosition(1, 1, 1));
        var item = new ItemStack(Minecraft.Items.IronSword, 1, 0, new NbtCompound
        {
            new NbtCompound("display")
            {
                new NbtString("Name", "{\"text\":\"Watchtower Blade\",\"italic\":false}")
            }
        });
        chestBe.AddItem(item);

        var schematic = SchematicBuilder.Create(3, 3, 3)
            .SetBlock(1, 1, 1, Minecraft.Blocks.Chest)
            .AddBlockEntity(chestBe)
            .Build();

        string code = FluentBuilderCodeGenerator.Generate(schematic);

        // Should NOT emit raw JSON component
        Assert.DoesNotContain("{\"text\":", code);
        Assert.DoesNotContain("\"italic\":false", code);
        Assert.Contains(".WithDisplayName(\"Watchtower Blade\")", code);
    }

    [Fact]
    public void FluentBuilderCodeGenerator_NamedEntity_EmitsWithCustomName()
    {
        var schematic = SchematicBuilder.Create(5, 5, 5)
            .AddEntity(new Entity(Minecraft.Entities.Cow, 2.5, 1.0, 2.5).WithCustomName("Bessie"))
            .Build();

        string code = FluentBuilderCodeGenerator.Generate(schematic);

        Assert.Contains(".AddEntity(new Entity(Minecraft.Entities.Cow, 2.5, 1.0, 2.5).WithCustomName(\"Bessie\"))", code);
        // Should NOT need fNbt using or raw NbtCompound for a simple named entity
        Assert.DoesNotContain("new NbtCompound", code);
        Assert.DoesNotContain("using fNbt;", code);
    }

    [Fact]
    public void FluentBuilderCodeGenerator_NamedItemInItemFrame_EmitsCleanDisplayName()
    {
        var namedItem = new ItemStack(Minecraft.Items.NameTag, 1, 0, new NbtCompound
        {
            new NbtCompound("display")
            {
                new NbtString("Name", "{\"text\":\"Special Filter\",\"italic\":false}")
            }
        });

        var schematic = SchematicBuilder.Create(3, 3, 3)
            .AddItemFrame(1.5, 1.5, 0.03125, Direction.North, namedItem)
            .Build();

        string code = FluentBuilderCodeGenerator.Generate(schematic);

        Assert.DoesNotContain("{\"text\":", code);
        Assert.Contains("new ItemStack(Minecraft.Items.NameTag).WithDisplayName(\"Special Filter\")", code);
    }
}
