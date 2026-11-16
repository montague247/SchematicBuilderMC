using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;
using SchematicBuilderMC.IO;
using SchematicBuilderMC.Registries;
using SchematicBuilderMC.Schematics.Contraptions;

namespace SchematicBuilderMC.Core.Tests.Schematics.Contraptions;

public sealed class DirectionalFlyingMachineSchematicCreatorTests
{
    [Theory]
    [InlineData(Direction.South, "south", "north", 2, 2, 4)]
    [InlineData(Direction.North, "north", "south", 2, 2, 4)]
    [InlineData(Direction.East, "east", "west", 4, 2, 2)]
    [InlineData(Direction.West, "west", "east", 4, 2, 2)]
    public void Build_AllHorizontalDirections_ProducesExpectedLayoutAndStates(
        Direction travelDirection,
        string expectedForward,
        string expectedOpposite,
        int expectedWidth,
        int expectedHeight,
        int expectedLength)
    {
        var creator = new DirectionalFlyingMachineSchematicCreator
        {
            TravelDirection = travelDirection,
            IncludeStarter = true,
            IncludeEndStop = false
        };

        var schematic = creator.Build();

        Assert.Equal(expectedWidth, schematic.Width);
        Assert.Equal(expectedHeight, schematic.Height);
        Assert.Equal(expectedLength, schematic.Length);

        int slimeCount = 0;
        int forwardPistonCount = 0;
        int backwardPistonCount = 0;
        int forwardObserverCount = 0;
        int backwardObserverCount = 0;
        int noteBlockCount = 0;

        for (int y = 0; y < schematic.Height; y++)
        {
            for (int z = 0; z < schematic.Length; z++)
            {
                for (int x = 0; x < schematic.Width; x++)
                {
                    var block = schematic[x, y, z];
                    if (block.Identifier == Minecraft.Blocks.SlimeBlock)
                    {
                        slimeCount++;
                    }
                    else if (block.Identifier == Minecraft.Blocks.StickyPiston)
                    {
                        string facing = block.GetProperty("facing")!;
                        if (facing == expectedForward) forwardPistonCount++;
                        else if (facing == expectedOpposite) backwardPistonCount++;
                    }
                    else if (block.Identifier == Minecraft.Blocks.Observer)
                    {
                        string facing = block.GetProperty("facing")!;
                        if (facing == expectedForward) forwardObserverCount++;
                        else if (facing == expectedOpposite) backwardObserverCount++;
                    }
                    else if (block.Identifier == Minecraft.Blocks.NoteBlock)
                    {
                        noteBlockCount++;
                    }
                }
            }
        }

        Assert.Equal(4, slimeCount);
        Assert.Equal(1, forwardPistonCount);
        Assert.Equal(1, backwardPistonCount);
        Assert.Equal(1, forwardObserverCount);
        Assert.Equal(1, backwardObserverCount);
        Assert.Equal(1, noteBlockCount);
    }

    [Fact]
    public void Build_WithHoneyBlocksStarterAndEndStop_ConfiguresBlocksAndDimensionsCorrectly()
    {
        var creator = new DirectionalFlyingMachineSchematicCreator
        {
            TravelDirection = Direction.East,
            UseHoneyBlocks = true,
            IncludeStarter = false,
            IncludeEndStop = true,
            StopDistance = 5
        };

        var schematic = creator.Build();

        Assert.Equal(9, schematic.Width);
        Assert.Equal(1, schematic.Height);
        Assert.Equal(2, schematic.Length);

        // Honey blocks at front and rear halves
        Assert.Equal(Minecraft.Blocks.HoneyBlock, schematic[0, 0, 0].Identifier);
        Assert.Equal(Minecraft.Blocks.HoneyBlock, schematic[1, 0, 0].Identifier);
        Assert.Equal(Minecraft.Blocks.HoneyBlock, schematic[2, 0, 1].Identifier);
        Assert.Equal(Minecraft.Blocks.HoneyBlock, schematic[3, 0, 1].Identifier);

        // End-stop obsidian at x = 3 + 5 = 8, z = 1 (u = 0 maps to z = 1 for East)
        Assert.Equal(Minecraft.Blocks.Obsidian, schematic[8, 0, 1].Identifier);
    }

    [Theory]
    [InlineData(Direction.Up)]
    [InlineData(Direction.Down)]
    [InlineData((Direction)99)]
    public void Create_UnsupportedDirection_ThrowsArgumentOutOfRangeException(Direction invalidDirection)
    {
        var creator = new DirectionalFlyingMachineSchematicCreator { TravelDirection = invalidDirection };
        Assert.Throws<ArgumentOutOfRangeException>(() => creator.Create());
    }

    [Fact]
    public void Create_InvalidStopDistanceOrOutOfBoundsPlacement_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new DirectionalFlyingMachineSchematicCreator { StopDistance = 0 }.Create());

        var machine = new DirectionalFlyingMachineSchematicCreator
        {
            TravelDirection = Direction.South,
            IncludeStarter = true
        };
        var builder = SchematicBuilder.Create(4, 2, 4);
        Assert.Throws<ArgumentOutOfRangeException>(() => builder.Create(machine, 0, 0, 1));
    }

    [Fact]
    public void SpongeRoundtrip_PreservesObserverAndPistonFacings()
    {
        var creator = new DirectionalFlyingMachineSchematicCreator
        {
            TravelDirection = Direction.West,
            UseHoneyBlocks = true,
            IncludeStarter = true,
            IncludeEndStop = true,
            StopDistance = 3
        };

        var schematic = creator.Build();
        byte[] bytes = SchematicFile.WriteToBytes(schematic, SchematicFormat.SpongeV2);
        var loaded = SchematicFile.Read(bytes, SchematicFormat.SpongeV2);

        Assert.Equal(7, loaded.Width);
        Assert.Equal(2, loaded.Height);
        Assert.Equal(2, loaded.Length);

        // West maps (u, y, v) -> (6 - v, y, u):
        // (0, 0, 0) -> (6, 0, 0) Observer facing east
        // (0, 0, 1) -> (5, 0, 0) StickyPiston facing west
        // (0, 1, 0) -> (6, 1, 0) NoteBlock starter
        // (0, 0, 6) -> (0, 0, 0) Obsidian end-stop
        Assert.Equal(Minecraft.Blocks.Observer, loaded[6, 0, 0].Identifier);
        Assert.Equal("east", loaded[6, 0, 0].GetProperty("facing"));
        Assert.Equal(Minecraft.Blocks.StickyPiston, loaded[5, 0, 0].Identifier);
        Assert.Equal("west", loaded[5, 0, 0].GetProperty("facing"));
        Assert.Equal(Minecraft.Blocks.NoteBlock, loaded[6, 1, 0].Identifier);
        Assert.Equal(Minecraft.Blocks.Obsidian, loaded[0, 0, 0].Identifier);

        // Write to file
        string spongePath = Path.Combine(SchematicCreatorTests.TestOutputDirectory, $"{schematic.Metadata.Name}.schem");
        SchematicFile.Write(schematic, spongePath, SchematicFormat.SpongeV2);
    }
}
