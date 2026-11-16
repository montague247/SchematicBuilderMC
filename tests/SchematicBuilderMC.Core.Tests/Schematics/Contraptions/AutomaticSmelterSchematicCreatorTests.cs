using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;
using SchematicBuilderMC.IO;
using SchematicBuilderMC.Models;
using SchematicBuilderMC.Registries;
using SchematicBuilderMC.Schematics.Contraptions;

namespace SchematicBuilderMC.Core.Tests.Schematics.Contraptions;

public sealed class AutomaticSmelterSchematicCreatorTests
{
    [Theory]
    [InlineData(SmelterType.Furnace, Minecraft.Blocks.Furnace)]
    [InlineData(SmelterType.BlastFurnace, Minecraft.Blocks.BlastFurnace)]
    [InlineData(SmelterType.Smoker, Minecraft.Blocks.Smoker)]
    public void Build_AllMachineTypes_PlacesCorrectProcessingBlockAndBounds(SmelterType machineType, string expectedBlockId)
    {
        var creator = new AutomaticSmelterSchematicCreator
        {
            MachineType = machineType,
            Facing = Direction.North
        };

        var schematic = creator.Build();

        Assert.Equal(2, schematic.Width);
        Assert.Equal(4, schematic.Height);
        Assert.Equal(2, schematic.Length);

        Assert.Equal(expectedBlockId, schematic[0, 1, 1].Identifier);
        Assert.Equal("north", schematic[0, 1, 1].GetProperty("facing"));
        Assert.NotNull(schematic.GetBlockEntity(0, 1, 1));
        Assert.Equal(expectedBlockId, schematic.GetBlockEntity(0, 1, 1)!.Id);
    }

    [Theory]
    [InlineData(Direction.North, "north", "west")]
    [InlineData(Direction.South, "south", "east")]
    [InlineData(Direction.East, "east", "north")]
    [InlineData(Direction.West, "west", "south")]
    public void Build_AllOrientations_SetsCorrectHopperAndChestDirections(
        Direction facing,
        string expectedFrontFacing,
        string expectedFuelHopperFacing)
    {
        var creator = new AutomaticSmelterSchematicCreator
        {
            MachineType = SmelterType.BlastFurnace,
            Facing = facing
        };

        var schematic = creator.Build();

        int chestCount = 0;
        int outputHopperCount = 0;
        int fuelHopperCount = 0;
        int inputHopperCount = 0;

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
                    }
                    else if (block.Identifier == Minecraft.Blocks.Hopper)
                    {
                        string hopperFacing = block.GetProperty("facing")!;
                        if (y == 0)
                        {
                            outputHopperCount++;
                            Assert.Equal(expectedFrontFacing, hopperFacing);
                        }
                        else if (y == 1)
                        {
                            fuelHopperCount++;
                            Assert.Equal(expectedFuelHopperFacing, hopperFacing);
                        }
                        else if (y == 2)
                        {
                            inputHopperCount++;
                            Assert.Equal("down", hopperFacing);
                        }
                    }
                }
            }
        }

        Assert.Equal(3, chestCount);
        Assert.Equal(1, outputHopperCount);
        Assert.Equal(1, fuelHopperCount);
        Assert.Equal(1, inputHopperCount);
    }

    [Fact]
    public void Build_WithOptionalInventories_AndSpongeRoundtrip_PreservesItems()
    {
        var creator = new AutomaticSmelterSchematicCreator
        {
            MachineType = SmelterType.Smoker,
            Facing = Direction.South,
            InputItems = [new ItemStack(Minecraft.Items.Beef, count: 64, slot: 0)],
            FuelItems = [new ItemStack(Minecraft.Items.Coal, count: 32, slot: 1)],
            OutputItems = [new ItemStack(Minecraft.Items.CookedBeef, count: 16, slot: 2)]
        };

        var schematic = creator.Build();
        byte[] bytes = SchematicFile.WriteToBytes(schematic, SchematicFormat.SpongeV2);
        var loaded = SchematicFile.Read(bytes, SchematicFormat.SpongeV2);

        // For Direction.South, (u, y, v) maps to (1 - u, y, 1 - v):
        // Output chest (0, 0, 0) -> (1, 0, 1)
        // Fuel chest (1, 2, 1) -> (0, 2, 0)
        // Input chest (0, 3, 1) -> (1, 3, 0)
        Assert.Equal(Minecraft.Items.CookedBeef, loaded.GetItems(1, 0, 1)[0].Id);
        Assert.Equal(16, loaded.GetItems(1, 0, 1)[0].Count);

        Assert.Equal(Minecraft.Items.Coal, loaded.GetItems(0, 2, 0)[0].Id);
        Assert.Equal(32, loaded.GetItems(0, 2, 0)[0].Count);

        Assert.Equal(Minecraft.Items.Beef, loaded.GetItems(1, 3, 0)[0].Id);
        Assert.Equal(64, loaded.GetItems(1, 3, 0)[0].Count);

        // Write to file
        string spongePath = Path.Combine(SchematicCreatorTests.TestOutputDirectory, $"{schematic.Metadata.Name}.schem");
        SchematicFile.Write(schematic, spongePath, SchematicFormat.SpongeV2);
    }

    [Fact]
    public void Create_InvalidFacingOrChestSlot_ThrowsExpectedExceptions()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new AutomaticSmelterSchematicCreator { Facing = Direction.Up }.Create());

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new AutomaticSmelterSchematicCreator
            {
                InputItems = [new ItemStack(Minecraft.Items.IronOre, count: 1, slot: 27)]
            }.Create());

        var smelter = new AutomaticSmelterSchematicCreator();
        var builder = SchematicBuilder.Create(3, 4, 3).Create(smelter, 2, 0, 0);
        Assert.Equal(4, builder.Width);
        Assert.Equal(4, builder.Height);
        Assert.Equal(3, builder.Length);
        Assert.Throws<ArgumentOutOfRangeException>(() => builder.Create(smelter, -1, 0, 0));
    }
}
