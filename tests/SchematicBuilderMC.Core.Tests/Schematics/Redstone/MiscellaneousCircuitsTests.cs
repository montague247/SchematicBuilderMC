using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;
using SchematicBuilderMC.IO;
using SchematicBuilderMC.Registries;
using SchematicBuilderMC.Schematics.Redstone.Miscellaneous;

namespace SchematicBuilderMC.Core.Tests.Schematics.Redstone;

public sealed class MiscellaneousCircuitsTests
{
    [Fact]
    public void AbbaCircuit_ConfigurableOutputsAndStepDelay_PlacesSymmetricForwardAndReverseLanes()
    {
        var abba = new AbbaCircuitSchematicCreator
        {
            OutputCount = 4,
            StepDelay = TimeSpan.FromMilliseconds(400), // 8 gt step delay -> delay=4 repeater per step
            Facing = Direction.East,
            IncludeInputLever = true,
            IncludeOutputLamps = true
        };

        Assert.Equal(8, abba.GetActualStepTicks());
        var schematic = abba.Build();

        int lampCount = 0;
        for (int x = 0; x < schematic.Width; x++)
        {
            for (int y = 0; y < schematic.Height; y++)
            {
                for (int z = 0; z < schematic.Length; z++)
                {
                    if (schematic[x, y, z].Identifier == Minecraft.Blocks.RedstoneLamp) lampCount++;
                }
            }
        }

        Assert.Equal(4, lampCount);
    }

    [Theory]
    [InlineData(BudMode.Observer)]
    [InlineData(BudMode.PistonBud)]
    [InlineData(BudMode.TorchDropper)]
    [InlineData(BudMode.PlaceOnly)]
    public void BlockUpdateDetector_AllModes_BuildsAndRoundTrips(BudMode mode)
    {
        var schematic = new BlockUpdateDetectorSchematicCreator
        {
            Mode = mode,
            Facing = Direction.North,
            IncludeOutputLamp = true
        }.Build();

        string serializedPath = Path.Combine(SchematicCreatorTests.TestOutputDirectory, $"BUD_{mode}.schem");
        SchematicFile.Write(schematic, serializedPath, SchematicFormat.SpongeV2);
        var readBack = SchematicFile.Read(serializedPath, SchematicFormat.SpongeV2);
        Assert.Equal(schematic.Width, readBack.Width);
        Assert.Equal(schematic.Height, readBack.Height);
        Assert.Equal(schematic.Length, readBack.Length);
    }

    [Theory]
    [InlineData(MultiplexerMode.Multiplexer, 1)]
    [InlineData(MultiplexerMode.Multiplexer, 2)]
    [InlineData(MultiplexerMode.Demultiplexer, 2)]
    public void Multiplexer_AndDemultiplexer_BuildExpectedChannels(MultiplexerMode mode, int stages)
    {
        var schematic = new MultiplexerSchematicCreator
        {
            Mode = mode,
            Stages = stages,
            Facing = Direction.West,
            IncludeInputLevers = true,
            IncludeOutputLamps = true
        }.Build();

        Assert.Equal(7, schematic.Width);
        Assert.Equal((stages * 4) - 1, schematic.Length);
    }

    [Fact]
    public void Randomizer_AndPistonExtender_ContainExpectedItemsAndPistons()
    {
        var randomizer = new RandomizerSchematicCreator
        {
            Mode = RandomizerMode.DropperArray,
            OutputCount = 3,
            IncludeInputButton = true,
            IncludeOutputLamps = true
        }.Build();

        var droppers = randomizer.BlockEntities.Where(be => be.Id == Minecraft.BlockEntities.Dropper).ToList();
        Assert.Equal(3, droppers.Count);
        Assert.All(droppers, d => Assert.Equal(2, d.Items.Count));

        var triple = new PistonExtenderSchematicCreator
        {
            Reach = 3,
            Facing = Direction.East,
            IncludeInputLever = true
        }.Build();

        Assert.Equal(Minecraft.Blocks.StickyPiston, triple[1, 1, 0].Identifier);
        Assert.Equal(Minecraft.Blocks.StickyPiston, triple[2, 1, 0].Identifier);
        Assert.Equal(Minecraft.Blocks.StickyPiston, triple[3, 1, 0].Identifier);
        Assert.Equal(Minecraft.Blocks.Glass, triple[4, 1, 0].Identifier);
    }

    [Fact]
    public void MiscellaneousExtensions_PlaceAllCircuitsIntoSharedBuilder()
    {
        var combined = SchematicBuilder.Create(32, 6, 32)
            .AddAbbaCircuit(0, 0, 0, outputCount: 2, includeInputLever: true)
            .AddBlockUpdateDetector(0, 0, 8, BudMode.Observer)
            .AddMultiplexer(10, 0, 0, MultiplexerMode.Multiplexer)
            .AddRandomizer(10, 0, 6, RandomizerMode.TokenCoinFlip)
            .AddPistonExtender(10, 0, 10, reach: 2, includeInputLever: true)
            .Build();

        Assert.Equal(Minecraft.Blocks.StickyPiston, combined[11, 1, 10].Identifier);
        Assert.Equal(Minecraft.Blocks.Glass, combined[13, 1, 10].Identifier);
    }

    [Fact]
    public void MiscellaneousCreators_InvalidArguments_Throw()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new AbbaCircuitSchematicCreator { OutputCount = 1 }.Build());
        Assert.Throws<ArgumentOutOfRangeException>(() => new BlockUpdateDetectorSchematicCreator { Facing = Direction.Down }.Build());
        Assert.Throws<ArgumentOutOfRangeException>(() => new MultiplexerSchematicCreator { Stages = 0 }.Build());
        Assert.Throws<ArgumentOutOfRangeException>(() => new RandomizerSchematicCreator { OutputCount = 0 }.Build());
        Assert.Throws<ArgumentOutOfRangeException>(() => new PistonExtenderSchematicCreator { Reach = 1 }.Build());
        Assert.Throws<ArgumentOutOfRangeException>(() => new PistonExtenderSchematicCreator { Reach = 7 }.Build());
    }
}
