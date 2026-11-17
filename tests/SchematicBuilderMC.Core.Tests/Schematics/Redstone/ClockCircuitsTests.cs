using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;
using SchematicBuilderMC.IO;
using SchematicBuilderMC.Registries;
using SchematicBuilderMC.Schematics.Redstone.Clocks;

namespace SchematicBuilderMC.Core.Tests.Schematics.Redstone;

public sealed class ClockCircuitsTests
{
    [Theory]
    [InlineData(ClockType.Repeater)]
    [InlineData(ClockType.TorchRepeater)]
    [InlineData(ClockType.TorchLoop)]
    [InlineData(ClockType.Comparator)]
    [InlineData(ClockType.Hopper)]
    [InlineData(ClockType.Observer)]
    [InlineData(ClockType.Piston)]
    [InlineData(ClockType.Minecart)]
    public void UnifiedClock_AllClockTypes_BuildAndRoundTripSpongeV2(ClockType clockType)
    {
        var schematic = new ClockSchematicCreator
        {
            ClockType = clockType,
            Period = TimeSpan.FromSeconds(2),
            Facing = Direction.South,
            IncludeToggleLever = true,
            IncludeOutputLamp = true
        }.Build();

        Assert.True(schematic.Width >= 2);
        Assert.True(schematic.Height >= 2);
        Assert.True(schematic.Length >= 2);

        string serializedPath = Path.Combine(SchematicCreatorTests.TestOutputDirectory, $"Clock_{clockType}.schem");
        SchematicFile.Write(schematic, serializedPath, SchematicFormat.SpongeV2);
        var readBack = SchematicFile.Read(serializedPath, SchematicFormat.SpongeV2);
        Assert.Equal(schematic.Width, readBack.Width);
        Assert.Equal(schematic.Height, readBack.Height);
        Assert.Equal(schematic.Length, readBack.Length);
    }

    [Fact]
    public void HopperClock_OptionalPeriod_MapsExactEthoItemCountAcrossStacks()
    {
        var clock16s = new HopperClockSchematicCreator
        {
            Period = TimeSpan.FromSeconds(128), // 2560 gt -> 160 items @ 16 gt/item (3 stacks: 64 + 64 + 32)
            IncludeToggleLever = true,
            IncludeOutputLamp = true
        };
        Assert.Equal(160, clock16s.GetItemCount());
        Assert.Equal(2560, clock16s.GetActualPeriodTicks());

        var schematic = clock16s.Build();
        var hopper = schematic.BlockEntities.Single(be => be.Items.Count > 0);
        Assert.Equal(3, hopper.Items.Count);
        Assert.Equal(160, hopper.Items.Sum(i => i.Count));

        // Comparators point away from the hoppers in Java blockstate convention (input facing = toward hopper)
        Assert.Equal("east", schematic[1, 1, 0].Properties["facing"]);
        Assert.Equal("west", schematic[4, 1, 0].Properties["facing"]);
        Assert.Equal("true", schematic[1, 1, 0].Properties["powered"]);
        Assert.Equal(Minecraft.Blocks.SmoothStone, schematic[0, 1, 0].Identifier);
        Assert.Equal(Minecraft.Blocks.SmoothStone, schematic[5, 1, 0].Identifier);
        Assert.Equal(Minecraft.Blocks.RedstoneWire, schematic[0, 1, 1].Identifier);
        Assert.Equal("side", schematic[0, 1, 1].Properties["north"]);
        Assert.Equal("side", schematic[0, 1, 1].Properties["east"]);

        // Redstone block starts over the locked second hopper so the unlocked first hopper self-starts on paste
        Assert.Equal(Minecraft.Blocks.RedstoneBlock, schematic[3, 1, 1].Identifier);
        Assert.Equal(Minecraft.Blocks.Lever, schematic[0, 2, 0].Identifier);
        Assert.Equal(Models.BlockState.Air, schematic[2, 2, 1]);
        Assert.Equal(Models.BlockState.Air, schematic[3, 2, 1]);
        Assert.Equal(Minecraft.Blocks.RedstoneLamp, schematic[6, 1, 0].Identifier);
    }

    [Fact]
    public void RepeaterAndTorchClocks_TimingCalculations_ResolveAsExpected()
    {
        var rep = new RepeaterClockSchematicCreator { Period = TimeSpan.FromSeconds(3), Facing = Direction.East };
        Assert.Equal(60, rep.GetActualPeriodTicks());

        var torchRep = new TorchClockSchematicCreator { Mode = TorchClockMode.TorchRepeater, PeriodTicks = 12 };
        Assert.Equal(12, torchRep.GetActualPeriodTicks());

        var torchLoop = new TorchClockSchematicCreator { Mode = TorchClockMode.TorchLoop, TorchCount = 6 };
        Assert.Equal(7, torchLoop.GetResolvedTorchCount());
        Assert.Equal(28, torchLoop.GetActualPeriodTicks());
    }

    [Fact]
    public void MinecartClock_PlacesMinecartEntityAndRails()
    {
        var schematic = new MinecartClockSchematicCreator
        {
            Period = TimeSpan.FromSeconds(4),
            Facing = Direction.East,
            IncludeOutputLamp = true
        }.Build();

        Assert.Single(schematic.Entities);
        Assert.Equal(Minecraft.Entities.Minecart, schematic.Entities[0].Id);
        Assert.Contains("Motion", schematic.Entities[0].Data.Names);
    }

    [Fact]
    public void ClockExtensions_PlacesMultipleClocksInSharedBuilder()
    {
        var schematic = SchematicBuilder.Create(32, 6, 32)
            .AddRepeaterClock(0, 0, 0, TimeSpan.FromSeconds(1), includeOutputLamp: true)
            .AddTorchClock(0, 0, 4, TimeSpan.FromMilliseconds(800))
            .AddComparatorClock(0, 0, 8)
            .AddHopperClock(10, 0, 0, TimeSpan.FromSeconds(8))
            .AddObserverClock(10, 0, 4)
            .AddPistonClock(10, 0, 8)
            .Build();

        Assert.Equal(Minecraft.Blocks.Hopper, schematic[12, 1, 0].Identifier);
        Assert.Equal(Minecraft.Blocks.StickyPiston, schematic[11, 1, 4].Identifier);
        Assert.Equal(Minecraft.Blocks.Observer, schematic[12, 1, 4].Identifier);
    }

    [Fact]
    public void ClockCreators_InvalidArguments_Throw()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ClockSchematicCreator { ClockType = (ClockType)999 }.Build());
        Assert.Throws<ArgumentOutOfRangeException>(() => new RepeaterClockSchematicCreator { PeriodTicks = 2 }.Build());
        Assert.Throws<ArgumentOutOfRangeException>(() => new MinecartClockSchematicCreator { TrackLength = 2 }.Build());
    }
}
