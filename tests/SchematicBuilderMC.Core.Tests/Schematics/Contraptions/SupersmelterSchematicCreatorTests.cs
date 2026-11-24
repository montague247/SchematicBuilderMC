using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;
using SchematicBuilderMC.IO;
using SchematicBuilderMC.Models;
using SchematicBuilderMC.Registries;
using SchematicBuilderMC.Schematics.Contraptions;

namespace SchematicBuilderMC.Core.Tests.Schematics.Contraptions;

public sealed class SupersmelterSchematicCreatorTests
{
    [Fact]
    public void Build_Default_ProducesNamedSupersmelterWith32FurnacesAndDecoration()
    {
        var creator = new SupersmelterSchematicCreator()
        {
            MachineType = SmelterType.Smoker
        };
        var schematic = creator.Build();

        Assert.Equal("Supersmelter", schematic.Metadata.Name);
        Assert.Equal(6, schematic.Width);
        Assert.Equal(10, schematic.Height);
        Assert.Equal(19, schematic.Length);

        int furnaceCount = 0;
        int composterCount = 0;
        for (int x = 0; x < schematic.Width; x++)
        {
            for (int y = 0; y < schematic.Height; y++)
            {
                for (int z = 0; z < schematic.Length; z++)
                {
                    string id = schematic[x, y, z].Identifier;
                    if (id == Minecraft.Blocks.Smoker)
                    {
                        furnaceCount++;
                    }
                    else if (id == Minecraft.Blocks.Composter)
                    {
                        composterCount++;
                    }
                }
            }
        }

        Assert.Equal(32, furnaceCount);
        Assert.Equal(9, composterCount);
        Assert.Equal(Minecraft.Blocks.RedstoneLamp, schematic[1, 8, 1].Identifier);
        Assert.Equal(Minecraft.Blocks.Chest, schematic[2, 3, 17].Identifier);
        Assert.Equal(Minecraft.Blocks.Chest, schematic[3, 3, 17].Identifier);
        Assert.Equal("south", schematic[2, 3, 17].Properties["facing"]);
        Assert.Equal("right", schematic[2, 3, 17].Properties["type"]);
        Assert.Equal("left", schematic[3, 3, 17].Properties["type"]);
        Assert.Equal(32 + 6, schematic.BlockEntities.Count);

        string spongePath = Path.Combine(SchematicCreatorTests.TestOutputDirectory, $"Supersmelter_{creator.MachineType}.schem");
        SchematicFile.Write(schematic, spongePath, SchematicFormat.SpongeV2);
        var loaded = SchematicFile.Read(spongePath, SchematicFormat.SpongeV2);
        Assert.Equal("Supersmelter", loaded.Metadata.Name);
        Assert.Equal(schematic.Width, loaded.Width);
        Assert.Equal(schematic.Height, loaded.Height);
        Assert.Equal(schematic.Length, loaded.Length);
    }

    [Fact]
    public void Build_IncludeDecorationFalse_OmitsCompostersAndReplacesLampWithWire()
    {
        var schematic = new SupersmelterSchematicCreator
        {
            IncludeDecoration = false
        }.Build();

        for (int x = 0; x < schematic.Width; x++)
        {
            for (int y = 0; y < schematic.Height; y++)
            {
                for (int z = 0; z < schematic.Length; z++)
                {
                    Assert.NotEqual(Minecraft.Blocks.Composter, schematic[x, y, z].Identifier);
                    Assert.NotEqual(Minecraft.Blocks.RedstoneLamp, schematic[x, y, z].Identifier);
                }
            }
        }

        Assert.Equal(Minecraft.Blocks.RedstoneWire, schematic[1, 8, 1].Identifier);
        Assert.Equal(Minecraft.Blocks.Air, schematic[0, 3, 16].Identifier);
        Assert.Equal(Minecraft.Blocks.Air, schematic[1, 7, 17].Identifier);
        Assert.Equal(Minecraft.Blocks.GrayStainedGlass, schematic[2, 5, 1].Identifier);
        Assert.Equal(Minecraft.Blocks.GrayWool, schematic[1, 6, 1].Identifier);
    }

    [Theory]
    [InlineData(SmelterType.Furnace, Minecraft.Blocks.Furnace)]
    [InlineData(SmelterType.BlastFurnace, Minecraft.Blocks.BlastFurnace)]
    [InlineData(SmelterType.Smoker, Minecraft.Blocks.Smoker)]
    public void Build_SupportsAllMachineTypes(SmelterType machineType, string expectedBlockId)
    {
        var schematic = new SupersmelterSchematicCreator
        {
            MachineType = machineType
        }.Build();

        Assert.Equal(expectedBlockId, schematic[0, 3, 0].Identifier);
        Assert.Equal(expectedBlockId, schematic[4, 3, 15].Identifier);
    }

    [Theory]
    [InlineData(SmelterType.Smoker, Minecraft.Blocks.Smoker)]
    [InlineData(SmelterType.BlastFurnace, Minecraft.Blocks.BlastFurnace)]
    public void Build_FastSmelterTypes_UseAdjustedFastClock(SmelterType machineType, string expectedMachineBlock)
    {
        var schematic = new SupersmelterSchematicCreator
        {
            MachineType = machineType
        }.Build();

        Assert.Equal(expectedMachineBlock, schematic[0, 3, 0].Identifier);
        Assert.Equal(Minecraft.Blocks.Comparator, schematic[2, 9, 13].Identifier);
        Assert.Equal("compare", schematic[2, 9, 13].Properties["mode"]);
        Assert.Equal(Minecraft.Blocks.RedstoneWire, schematic[2, 9, 14].Identifier);
        Assert.Equal(Minecraft.Blocks.Repeater, schematic[3, 9, 11].Identifier);
        Assert.Equal("2", schematic[3, 9, 11].Properties["delay"]);
        Assert.Equal(Minecraft.Blocks.Comparator, schematic[2, 8, 15].Identifier);
        Assert.Equal(Minecraft.Blocks.Comparator, schematic[3, 8, 13].Identifier);
        Assert.Equal(Minecraft.Blocks.GrayWool, schematic[3, 8, 14].Identifier);
        Assert.Equal(Minecraft.Blocks.Comparator, schematic[3, 8, 15].Identifier);
        Assert.Equal(Minecraft.Blocks.GrayWool, schematic[2, 8, 14].Identifier);
        Assert.Equal(Minecraft.Blocks.GrayStainedGlass, schematic[2, 8, 13].Identifier);
    }

    [Fact]
    public void Build_UseFastClockOverride_SelectsExplicitClockCircuit()
    {
        var furnaceWithFastClock = new SupersmelterSchematicCreator
        {
            MachineType = SmelterType.Furnace,
            UseFastClock = true
        }.Build();
        Assert.Equal(Minecraft.Blocks.Comparator, furnaceWithFastClock[2, 9, 13].Identifier);
        Assert.Equal("compare", furnaceWithFastClock[2, 9, 13].Properties["mode"]);
        Assert.Equal("2", furnaceWithFastClock[3, 9, 11].Properties["delay"]);

        var smokerWithStandardClock = new SupersmelterSchematicCreator
        {
            MachineType = SmelterType.Smoker,
            UseFastClock = false
        }.Build();
        Assert.Equal(Minecraft.Blocks.Air, smokerWithStandardClock[2, 9, 13].Identifier);
        Assert.Equal("3", smokerWithStandardClock[3, 9, 11].Properties["delay"]);
        Assert.Equal(Minecraft.Blocks.Comparator, smokerWithStandardClock[2, 8, 14].Identifier);
        Assert.Equal(Minecraft.Blocks.Comparator, smokerWithStandardClock[3, 8, 14].Identifier);
        Assert.Equal("compare", smokerWithStandardClock[3, 8, 14].Properties["mode"]);
    }

    [Theory]
    [InlineData(Direction.South, 6, 19, "south")]
    [InlineData(Direction.West, 19, 6, "west")]
    [InlineData(Direction.North, 6, 19, "north")]
    [InlineData(Direction.East, 19, 6, "east")]
    public void Build_SupportsAllHorizontalFacings(Direction facing, int expectedWidth, int expectedLength, string expectedChestFacing)
    {
        var schematic = new SupersmelterSchematicCreator
        {
            Facing = facing
        }.Build();

        Assert.Equal(expectedWidth, schematic.Width);
        Assert.Equal(10, schematic.Height);
        Assert.Equal(expectedLength, schematic.Length);

        bool foundChest = false;
        for (int x = 0; x < schematic.Width; x++)
        {
            for (int z = 0; z < schematic.Length; z++)
            {
                var block = schematic[x, 3, z];
                if (block.Identifier == Minecraft.Blocks.Chest)
                {
                    Assert.Equal(expectedChestFacing, block.Properties["facing"]);
                    foundChest = true;
                }
            }
        }

        Assert.True(foundChest);
    }

    [Fact]
    public void PlaceAndFluentExtension_SupportItemsAndCustomBlocks()
    {
        var builder = SchematicBuilder.Create(10, 12, 22)
            .Create(new SupersmelterSchematicCreator
            {
                CircuitBlock = Minecraft.Blocks.SmoothStone,
                GlassBlock = Minecraft.Blocks.Glass,
                InputItems = [new ItemStack(Minecraft.Items.IronOre, count: 64, slot: 0), new ItemStack(Minecraft.Items.GoldOre, count: 32, slot: 30)],
                FuelItems = [new ItemStack(Minecraft.Items.Coal, count: 64, slot: 2)],
                OutputItems = [new ItemStack(Minecraft.Items.IronIngot, count: 10, slot: 28)]
            }, startX: 2, startY: 1, startZ: 1);

        var schematic = builder.Build();
        Assert.Equal(Minecraft.Blocks.SmoothStone, schematic[4, 1, 15].Identifier);
        Assert.Equal(Minecraft.Blocks.Glass, schematic[4, 6, 2].Identifier);

        Assert.Equal(Minecraft.Items.IronOre, schematic.GetItems(4, 8, 18)[0].Id);
        Assert.Equal(Minecraft.Items.GoldOre, schematic.GetItems(5, 8, 18)[0].Id);
        Assert.Equal(Minecraft.Items.Coal, schematic.GetItems(4, 6, 18)[0].Id);
        Assert.Equal(Minecraft.Items.IronIngot, schematic.GetItems(5, 4, 18)[0].Id);
    }

    [Fact]
    public void Build_InvalidArguments_ThrowsExpectedExceptions()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SupersmelterSchematicCreator { Facing = Direction.Up }.Build());
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SupersmelterSchematicCreator { MachineType = (SmelterType)999 }.Build());
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SupersmelterSchematicCreator { InputItems = [new ItemStack(Minecraft.Items.Coal, count: 1, slot: 54)] }.Build());
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SupersmelterSchematicCreator { FuelItems = [new ItemStack(Minecraft.Items.Coal, count: 65, slot: 0)] }.Build());
    }
}
