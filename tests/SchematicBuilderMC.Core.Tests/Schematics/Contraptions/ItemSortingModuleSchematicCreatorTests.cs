using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;
using SchematicBuilderMC.IO;
using SchematicBuilderMC.Models;
using SchematicBuilderMC.Registries;
using SchematicBuilderMC.Schematics.Contraptions;

namespace SchematicBuilderMC.Core.Tests.Schematics.Contraptions;

public sealed class ItemSortingModuleSchematicCreatorTests
{
    [Theory]
    [InlineData(1, Direction.North, 1, 4, 6)]
    [InlineData(3, Direction.North, 3, 4, 6)]
    [InlineData(3, Direction.South, 3, 4, 6)]
    [InlineData(3, Direction.East, 6, 4, 3)]
    [InlineData(3, Direction.West, 6, 4, 3)]
    public void Build_SingleAndMultiModuleAllOrientations_ProducesExpectedDimensions(
        int moduleCount,
        Direction facing,
        int expectedWidth,
        int expectedHeight,
        int expectedLength)
    {
        var creator = new ItemSortingModuleSchematicCreator
        {
            ModuleCount = moduleCount,
            Facing = facing,
            FilterItems = [Minecraft.Items.Diamond, Minecraft.Items.GoldIngot, Minecraft.Items.IronIngot]
        };

        var schematic = creator.Build();

        Assert.Equal(expectedWidth, schematic.Width);
        Assert.Equal(expectedHeight, schematic.Height);
        Assert.Equal(expectedLength, schematic.Length);
        Assert.Equal(moduleCount * 2, schematic.BlockEntities.Count);
    }

    [Theory]
    [InlineData(Direction.North, "north", "south", "east")]
    [InlineData(Direction.South, "south", "north", "west")]
    [InlineData(Direction.East, "east", "west", "south")]
    [InlineData(Direction.West, "west", "east", "north")]
    public void Build_SingleModule_PlacesBlocksWithCorrectFacingAndFilterHopperNbt(
        Direction facing,
        string expectedFrontFacing,
        string expectedOppositeFacing,
        string expectedLateralFacing)
    {
        var creator = new ItemSortingModuleSchematicCreator
        {
            ModuleCount = 1,
            Facing = facing,
            FilterItems = [Minecraft.Items.Emerald],
            FilterItemCount = 41,
            ChestItems = [new ItemStack(Minecraft.Items.Emerald, count: 16, slot: 0)]
        };

        var schematic = creator.Build();

        int chestCount = 0;
        int comparatorCount = 0;
        int repeaterCount = 0;
        int torchCount = 0;
        int filterHopperCount = 0;
        int streamHopperCount = 0;

        for (int y = 0; y < schematic.Height; y++)
        {
            for (int z = 0; z < schematic.Length; z++)
            {
                for (int x = 0; x < schematic.Width; x++)
                {
                    var block = schematic[x, y, z];
                    if (block.Identifier == Minecraft.Blocks.Chest)
                    {
                        chestCount++;
                        Assert.Equal(expectedFrontFacing, block.GetProperty("facing"));
                        var chestItems = schematic.GetItems(x, y, z);
                        Assert.Single(chestItems);
                        Assert.Equal(Minecraft.Items.Emerald, chestItems[0].Id);
                        Assert.Equal(16, chestItems[0].Count);
                    }
                    else if (block.Identifier == Minecraft.Blocks.Comparator)
                    {
                        comparatorCount++;
                        Assert.Equal(expectedOppositeFacing, block.GetProperty("facing"));
                        Assert.Equal("compare", block.GetProperty("mode"));
                    }
                    else if (block.Identifier == Minecraft.Blocks.Repeater)
                    {
                        repeaterCount++;
                        Assert.Equal(expectedFrontFacing, block.GetProperty("facing"));
                    }
                    else if (block.Identifier == Minecraft.Blocks.RedstoneWallTorch)
                    {
                        torchCount++;
                        Assert.Equal(expectedFrontFacing, block.GetProperty("facing"));
                    }
                    else if (block.Identifier == Minecraft.Blocks.Hopper)
                    {
                        string hopperFacing = block.GetProperty("facing")!;
                        if (y == 2)
                        {
                            filterHopperCount++;
                            Assert.Equal(expectedOppositeFacing, hopperFacing);
                            var filterItems = schematic.GetItems(x, y, z);
                            Assert.Equal(5, filterItems.Count);
                            Assert.Equal(Minecraft.Items.Emerald, filterItems[0].Id);
                            Assert.Equal(41, filterItems[0].Count);
                            for (int slot = 1; slot <= 4; slot++)
                            {
                                Assert.Equal(Minecraft.Items.Stick, filterItems[slot].Id);
                                Assert.Equal((byte)slot, filterItems[slot].Slot);
                                Assert.Equal("Filter", filterItems[slot].DisplayName);
                            }
                        }
                        else if (y == 3)
                        {
                            streamHopperCount++;
                            Assert.Equal(expectedLateralFacing, hopperFacing);
                        }
                    }
                }
            }
        }

        Assert.Equal(1, chestCount);
        Assert.Equal(1, comparatorCount);
        Assert.Equal(1, repeaterCount);
        Assert.Equal(1, torchCount);
        Assert.Equal(1, filterHopperCount);
        Assert.Equal(1, streamHopperCount);
    }

    [Fact]
    public void Build_MultiModuleRow_AssignsDistinctFilterItemsAcrossAdjacentSlices()
    {
        var creator = new ItemSortingModuleSchematicCreator
        {
            ModuleCount = 3,
            Facing = Direction.North,
            FilterItems = [Minecraft.Items.Diamond, Minecraft.Items.GoldIngot, Minecraft.Items.IronIngot]
        };

        var schematic = creator.Build();

        Assert.Equal(Minecraft.Items.Diamond, schematic.GetItems(0, 2, 1)[0].Id);
        Assert.Equal(Minecraft.Items.GoldIngot, schematic.GetItems(1, 2, 1)[0].Id);
        Assert.Equal(Minecraft.Items.IronIngot, schematic.GetItems(2, 2, 1)[0].Id);

        // Write to file
        string spongePath = Path.Combine(SchematicCreatorTests.TestOutputDirectory, $"{schematic.Metadata.Name}.schem");
        SchematicFile.Write(schematic, spongePath, SchematicFormat.SpongeV2);
    }

    [Fact]
    public void Create_InvalidConfiguration_ThrowsExpectedExceptions()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ItemSortingModuleSchematicCreator { ModuleCount = 0 }.Create());

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ItemSortingModuleSchematicCreator { FilterItemCount = 65 }.Create());

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ItemSortingModuleSchematicCreator { Facing = Direction.Up }.Create());

        Assert.Throws<ArgumentException>(() =>
            new ItemSortingModuleSchematicCreator { FilterItems = [] }.Create());

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ItemSortingModuleSchematicCreator
            {
                ChestItems = [new ItemStack(Minecraft.Items.Diamond, count: 1, slot: 27)]
            }.Create());

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ItemSortingModuleSchematicCreator
            {
                ChestItems = [new ItemStack(Minecraft.Items.Diamond, count: 65, slot: 0)]
            }.Create());
    }

    [Fact]
    public void Place_IntoLargerSchematic_AndSpongeRoundtrip_PreservesInventoriesAndStates()
    {
        var sorter = new ItemSortingModuleSchematicCreator
        {
            ModuleCount = 2,
            Facing = Direction.West,
            FilterItems = [Minecraft.Items.Redstone, Minecraft.Items.GlowstoneDust],
            ChestItems = [new ItemStack(Minecraft.Items.Redstone, count: 32, slot: 2)]
        };

        var schematic = SchematicBuilder.Create(10, 6, 6)
            .Create(sorter, startX: 2, startY: 1, startZ: 2)
            .Build();

        byte[] bytes = SchematicFile.WriteToBytes(schematic, SchematicFormat.SpongeV2);
        var loaded = SchematicFile.Read(bytes, SchematicFormat.SpongeV2);

        Assert.Equal(4, loaded.BlockEntities.Count);
        Assert.Equal("west", loaded[2, 1, 3].GetProperty("facing"));
        Assert.Equal(Minecraft.Items.Redstone, loaded.GetItems(2, 1, 3)[0].Id);
        Assert.Equal(32, loaded.GetItems(2, 1, 3)[0].Count);
        Assert.Equal(Minecraft.Items.Redstone, loaded.GetItems(3, 3, 3)[0].Id);
        Assert.Equal(Minecraft.Items.GlowstoneDust, loaded.GetItems(3, 3, 2)[0].Id);
    }
}
