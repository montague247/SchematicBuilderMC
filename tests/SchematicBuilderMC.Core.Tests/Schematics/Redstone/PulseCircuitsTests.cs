using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;
using SchematicBuilderMC.IO;
using SchematicBuilderMC.Registries;
using SchematicBuilderMC.Schematics.Redstone.Pulse;

namespace SchematicBuilderMC.Core.Tests.Schematics.Redstone;

public sealed class PulseCircuitsTests
{
    [Theory]
    [InlineData(PulseGeneratorMode.StickyPistonMonostable)]
    [InlineData(PulseGeneratorMode.ObserverMonostable)]
    [InlineData(PulseGeneratorMode.RepeaterCutoffLimiter)]
    public void PulseGenerator_AllModesAndDurations_BuildsAndRoundTrips(PulseGeneratorMode mode)
    {
        var creator = new PulseGeneratorSchematicCreator
        {
            Mode = mode,
            PulseDuration = TimeSpan.FromMilliseconds(300), // 6 gt = delay 3
            Facing = Direction.South,
            IncludeInputLever = true,
            IncludeOutputLamp = true
        };

        Assert.Equal(6, creator.GetActualPulseTicks());
        var schematic = creator.Build();

        string serializedPath = Path.Combine(SchematicCreatorTests.TestOutputDirectory, $"PulseGen_{mode}.schem");
        SchematicFile.Write(schematic, serializedPath, SchematicFormat.SpongeV2);
        var readBack = SchematicFile.Read(serializedPath, SchematicFormat.SpongeV2);
        Assert.Equal(schematic.Width, readBack.Width);
        Assert.Equal(schematic.Height, readBack.Height);
        Assert.Equal(schematic.Length, readBack.Length);
    }

    [Theory]
    [InlineData(EdgeDetectorType.RisingEdge)]
    [InlineData(EdgeDetectorType.FallingEdge)]
    [InlineData(EdgeDetectorType.DualEdge)]
    [InlineData(EdgeDetectorType.InvertedRisingEdge)]
    public void EdgeDetector_AllEdgeTypes_BuildExpectedComponents(EdgeDetectorType edgeType)
    {
        var schematic = new EdgeDetectorSchematicCreator
        {
            EdgeType = edgeType,
            PulseDurationTicks = 4,
            Facing = Direction.West,
            IncludeInputLever = true,
            IncludeOutputLamp = true
        }.Build();

        Assert.True(schematic.Width >= 5);
        Assert.True(schematic.Height >= 2);
    }

    [Fact]
    public void PulseExtender_HopperTimerRepeaterAndComparator_ComputeExactTimingsAndInventory()
    {
        var hopperExt = new PulseExtenderSchematicCreator
        {
            Mode = PulseExtenderMode.HopperTimer,
            Duration = TimeSpan.FromSeconds(10), // 200 gt -> 25 items @ 8 gt/item
            Facing = Direction.East,
            IncludeInputButton = true,
            IncludeOutputLamp = true
        };
        Assert.Equal(25, hopperExt.GetHopperItemCount());
        Assert.Equal(200, hopperExt.GetActualDurationTicks());

        var hopperSchem = hopperExt.Build();
        Assert.NotEmpty(hopperSchem.BlockEntities);
        Assert.Equal(25, hopperSchem.BlockEntities.Single(be => be.Items.Count > 0).Items[0].Count);

        var repSchem = new PulseExtenderSchematicCreator
        {
            Mode = PulseExtenderMode.Repeater,
            DurationTicks = 32,
            IncludeOutputLamp = true
        }.Build();
        Assert.Equal(11, repSchem.Width);

        var cmpSchem = new PulseExtenderSchematicCreator
        {
            Mode = PulseExtenderMode.ComparatorDecay,
            DurationTicks = 60
        }.Build();
        Assert.Equal(2, cmpSchem.Length);
    }

    [Fact]
    public void PulseMultiplier_AndPulseDivider_HandleCountsAndDivisors()
    {
        var taps = new PulseMultiplierSchematicCreator
        {
            Mode = PulseMultiplierMode.RepeaterTaps,
            PulseCount = 5,
            PulseSpacingTicks = 6,
            IncludeInputButton = true,
            IncludeOutputLamp = true
        }.Build();
        Assert.True(taps.Width >= 10);

        var burst = new PulseMultiplierSchematicCreator
        {
            Mode = PulseMultiplierMode.DropperBurst,
            PulseCount = 70,
            IncludeOutputLamp = true
        }.Build();
        var dropper = burst.BlockEntities.Single(be => be.Id == Minecraft.BlockEntities.Dropper);
        Assert.Equal(70, dropper.Items.Sum(i => i.Count));

        var bulbDiv = new PulseDividerSchematicCreator
        {
            Mode = PulseDividerMode.CopperBulbBinary,
            Divisor = 8,
            IncludeInputButton = true,
            IncludeOutputLamp = true
        };
        Assert.Equal(3, bulbDiv.GetBinaryStageCount());
        Assert.Equal(8, bulbDiv.GetEffectiveDivisor());
        var bulbSchem = bulbDiv.Build();
        Assert.Equal(Minecraft.Blocks.WaxedCopperBulb, bulbSchem[2, 1, 0].Identifier);

        var ringDiv = new PulseDividerSchematicCreator
        {
            Mode = PulseDividerMode.DropperRing,
            Divisor = 5
        }.Build();
        Assert.Equal(5, ringDiv.BlockEntities.Single(be => be.Id == Minecraft.BlockEntities.Dropper).Items[0].Count);
    }

    [Fact]
    public void PulseLengthDetector_AndFluentExtensions_PlaceIntoSharedBuilder()
    {
        var shortDetector = new PulseLengthDetectorSchematicCreator { MinimumTicks = 6, Facing = Direction.North };
        Assert.Equal(6, shortDetector.GetActualMinimumTicks());
        var shortBuilt = shortDetector.Build();
        Assert.Equal(Minecraft.Blocks.Repeater, shortBuilt[0, 1, 3].Identifier);
        Assert.Equal(Minecraft.Blocks.RedstoneWallTorch, shortBuilt[1, 1, 2].Identifier);

        var combined = SchematicBuilder.Create(32, 8, 32)
            .AddPulseGenerator(0, 0, 0, includeInputLever: true, includeOutputLamp: true)
            .AddEdgeDetector(0, 0, 3, EdgeDetectorType.DualEdge)
            .AddPulseExtender(0, 0, 6, PulseExtenderMode.HopperTimer, duration: TimeSpan.FromSeconds(4))
            .AddPulseMultiplier(10, 0, 0, pulseCount: 3)
            .AddPulseDivider(10, 0, 5, divisor: 4)
            .AddPulseLengthDetector(10, 0, 10, minimumDuration: TimeSpan.FromSeconds(1), includeInputLever: true, includeOutputLamp: true)
            .Build();

        Assert.Equal(Minecraft.Blocks.RedstoneWire, combined[16, 1, 10].Identifier);
        Assert.Equal(Minecraft.Blocks.RedstoneLamp, combined[17, 1, 10].Identifier);
    }

    [Fact]
    public void PulseCreators_InvalidArguments_Throw()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PulseGeneratorSchematicCreator { PulseDurationTicks = 1 }.Build());
        Assert.Throws<ArgumentOutOfRangeException>(() => new PulseMultiplierSchematicCreator { PulseCount = 1 }.Build());
        Assert.Throws<ArgumentOutOfRangeException>(() => new PulseDividerSchematicCreator { Divisor = 1 }.Build());
        Assert.Throws<ArgumentOutOfRangeException>(() => new PulseLengthDetectorSchematicCreator { Facing = Direction.Up }.Build());
    }
}
