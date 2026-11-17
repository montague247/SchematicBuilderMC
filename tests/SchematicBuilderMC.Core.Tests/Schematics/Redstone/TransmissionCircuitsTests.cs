using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;
using SchematicBuilderMC.IO;
using SchematicBuilderMC.Models;
using SchematicBuilderMC.Registries;
using SchematicBuilderMC.Schematics.Redstone.Transmission;

namespace SchematicBuilderMC.Core.Tests.Schematics.Redstone;

public sealed class TransmissionCircuitsTests
{
    [Fact]
    public void TransmissionLine_LongLineAndOptionalTiming_PlacesBoostAndDelayRepeaters()
    {
        var boostOnly = new TransmissionLineSchematicCreator { Length = 32, Facing = Direction.East };
        var boostSchematic = boostOnly.Build();
        Assert.Equal(32, boostSchematic.Width);
        Assert.Equal(2, boostSchematic.Height);
        Assert.Equal(1, boostSchematic.Length);
        Assert.Equal(Minecraft.Blocks.Repeater, boostSchematic[15, 1, 0].Identifier);
        Assert.Equal(Minecraft.Blocks.Repeater, boostSchematic[31, 1, 0].Identifier);
        Assert.Equal(4, boostOnly.GetTotalDelayTicks());

        var timed = new TransmissionLineSchematicCreator
        {
            Length = 10,
            Delay = TimeSpan.FromMilliseconds(1600), // 32 game ticks = 16 redstone ticks -> 4 max-delay repeaters
            IncludeInputLever = true,
            IncludeOutputLamp = true
        };
        var timedSchematic = timed.Build();
        Assert.Equal(12, timedSchematic.Width);
        Assert.Equal(32, timed.GetTotalDelayTicks());
        Assert.Equal(4, timed.GetRepeaterSchedule().Count);
        Assert.Equal(Minecraft.Blocks.Lever, timedSchematic[0, 1, 0].Identifier);
        Assert.Equal(Minecraft.Blocks.RedstoneLamp, timedSchematic[11, 1, 0].Identifier);
    }

    [Theory]
    [InlineData(Direction.East, 8, 2, 1)]
    [InlineData(Direction.South, 1, 2, 8)]
    [InlineData(Direction.West, 8, 2, 1)]
    [InlineData(Direction.North, 1, 2, 8)]
    public void TransmissionLine_AllDirections_RotatesBoundingBox(Direction facing, int expectedW, int expectedH, int expectedL)
    {
        var schematic = new TransmissionLineSchematicCreator { Length = 8, Facing = facing }.Build();
        Assert.Equal(expectedW, schematic.Width);
        Assert.Equal(expectedH, schematic.Height);
        Assert.Equal(expectedL, schematic.Length);
    }

    [Theory]
    [InlineData(VerticalTransmissionMode.RedstoneLadder)]
    [InlineData(VerticalTransmissionMode.RedstoneStaircase)]
    [InlineData(VerticalTransmissionMode.TorchTowerUp)]
    [InlineData(VerticalTransmissionMode.TorchTowerDown)]
    [InlineData(VerticalTransmissionMode.ObserverTowerUp)]
    [InlineData(VerticalTransmissionMode.ObserverTowerDown)]
    [InlineData(VerticalTransmissionMode.PistonTowerUp)]
    [InlineData(VerticalTransmissionMode.PistonTowerDown)]
    public void VerticalTransmission_AllModes_BuildsValidSchematic(VerticalTransmissionMode mode)
    {
        var creator = new VerticalTransmissionSchematicCreator
        {
            Mode = mode,
            Height = 16,
            Facing = Direction.South,
            IncludeInputLever = true,
            IncludeOutputLamp = true
        };

        var schematic = creator.Build();
        Assert.True(schematic.Height >= 16);

        string serializedPath = Path.Combine(SchematicCreatorTests.TestOutputDirectory, $"Vertical_{mode}.schem");
        SchematicFile.Write(schematic, serializedPath, SchematicFormat.SpongeV2);
        var readBack = SchematicFile.Read(serializedPath, SchematicFormat.SpongeV2);
        Assert.Equal(schematic.Width, readBack.Width);
        Assert.Equal(schematic.Height, readBack.Height);
        Assert.Equal(schematic.Length, readBack.Length);
    }

    [Fact]
    public void VerticalTransmission_TorchTowerDown_UsesWallTorchesAtNoDownwardLeakagePattern()
    {
        var schematic = new VerticalTransmissionSchematicCreator
        {
            Mode = VerticalTransmissionMode.TorchTowerDown,
            Height = 5,
            Facing = Direction.East,
            IncludeOutputLamp = true
        }.Build();

        Assert.Equal(3, schematic.Width);
        Assert.Equal(6, schematic.Height);
        Assert.Equal(1, schematic.Length);
        Assert.Equal(Minecraft.Blocks.RedstoneWallTorch, schematic[1, 4, 0].Identifier);
        Assert.Equal(Minecraft.Blocks.RedstoneWallTorch, schematic[1, 3, 0].Identifier);
        Assert.Equal(Minecraft.Blocks.RedstoneLamp, schematic[2, 0, 0].Identifier);
    }

    [Theory]
    [InlineData(SignalBridgeMode.RedstoneBridge, 5, 5)]
    [InlineData(SignalBridgeMode.RepeaterBridge, 5, 4)]
    public void SignalBridge_BothModes_IsolatesCrossingLines(SignalBridgeMode mode, int span, int expectedHeight)
    {
        var schematic = new SignalBridgeSchematicCreator
        {
            Mode = mode,
            SpanLength = span,
            Facing = Direction.East,
            DelayTicks = 6,
            IncludeInputLevers = true,
            IncludeOutputLamps = true
        }.Build();

        Assert.Equal(span, schematic.Width);
        Assert.Equal(expectedHeight, schematic.Height);
        Assert.Equal(span, schematic.Length);

        if (mode == SignalBridgeMode.RedstoneBridge)
        {
            Assert.Equal(BlockState.Air, schematic[2, 2, 2]);
            Assert.Equal(Minecraft.Blocks.RedstoneWire, schematic[2, 4, 2].Identifier);
        }
        else
        {
            Assert.Equal(Minecraft.Blocks.Repeater, schematic[2, 1, 2].Identifier);
            Assert.Equal(Minecraft.Blocks.Repeater, schematic[2, 3, 2].Identifier);
            Assert.Equal("3", schematic[2, 1, 2].Properties["delay"]);
        }
    }

    [Theory]
    [InlineData(TwoWayRepeaterMode.Comparison, 5, 2, 2)]
    [InlineData(TwoWayRepeaterMode.LockedRepeater, 5, 2, 3)]
    public void TwoWayRepeater_BothModes_HaveExpectedFootprintAndComponents(
        TwoWayRepeaterMode mode,
        int expectedW,
        int expectedH,
        int expectedL)
    {
        var schematic = new TwoWayRepeaterSchematicCreator
        {
            Mode = mode,
            Facing = Direction.East,
            Delay = TimeSpan.FromMilliseconds(400),
            IncludeInputLevers = true
        }.Build();

        Assert.Equal(expectedW, schematic.Width);
        Assert.Equal(expectedH, schematic.Height);
        Assert.Equal(expectedL, schematic.Length);
    }

    [Fact]
    public void InstantRepeater_ChainedAndFluentExtensions_PlaceIntoSharedBuilder()
    {
        var builder = SchematicBuilder.Create(30, 20, 30)
            .AddTransmissionLine(0, 0, 0, length: 6, includeInputLever: true, includeOutputLamp: true)
            .AddVerticalTransmission(0, 0, 3, VerticalTransmissionMode.ObserverTowerUp, height: 6)
            .AddSignalBridge(0, 0, 6)
            .AddTwoWayRepeater(8, 0, 0)
            .AddInstantRepeater(8, 0, 4, stageCount: 2, includeInputLever: true, includeOutputLamp: true);

        var combined = builder.Build();
        Assert.Equal(Minecraft.Blocks.StickyPiston, combined[11, 2, 6].Identifier);
        Assert.Equal(Minecraft.Blocks.RedstoneTorch, combined[11, 1, 4].Identifier);
        Assert.Equal(Minecraft.Blocks.RedstoneLamp, combined[19, 1, 5].Identifier);
    }

    [Fact]
    public void TransmissionCreators_InvalidParameters_ThrowExpectedExceptions()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TransmissionLineSchematicCreator { Facing = Direction.Up }.Build());
        Assert.Throws<ArgumentOutOfRangeException>(() => new TransmissionLineSchematicCreator { Length = 0 }.Build());
        Assert.Throws<ArgumentOutOfRangeException>(() => new VerticalTransmissionSchematicCreator { Height = 1 }.Build());
        Assert.Throws<ArgumentOutOfRangeException>(() => new SignalBridgeSchematicCreator { SpanLength = 2 }.Build());
        Assert.Throws<ArgumentOutOfRangeException>(() => new InstantRepeaterSchematicCreator { StageCount = 0 }.Build());
    }
}
