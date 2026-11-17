using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;
using SchematicBuilderMC.IO;
using SchematicBuilderMC.Registries;
using SchematicBuilderMC.Schematics.Redstone.Memory;

namespace SchematicBuilderMC.Core.Tests.Schematics.Redstone;

public sealed class MemoryCircuitsTests
{
    [Theory]
    [InlineData(RsLatchMode.RsNor)]
    [InlineData(RsLatchMode.Dropper)]
    [InlineData(RsLatchMode.Piston)]
    public void RsLatch_AllModes_BuildsAndRoundTrips(RsLatchMode mode)
    {
        var schematic = new RsLatchSchematicCreator
        {
            Mode = mode,
            Facing = Direction.East,
            IncludeInputButtons = true,
            IncludeOutputLamp = true
        }.Build();

        string serializedPath = Path.Combine(SchematicCreatorTests.TestOutputDirectory, $"RsLatch_{mode}.schem");
        SchematicFile.Write(schematic, serializedPath, SchematicFormat.SpongeV2);
        var readBack = SchematicFile.Read(serializedPath, SchematicFormat.SpongeV2);
        Assert.Equal(schematic.Width, readBack.Width);
        Assert.Equal(schematic.Height, readBack.Height);
        Assert.Equal(schematic.Length, readBack.Length);

        if (mode == RsLatchMode.Dropper)
        {
            Assert.Contains(readBack.BlockEntities, be => be.Items.Count == 1 && be.Items[0].Count == 1);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DLatch_AndDFlipFlop_PlaceLockedRepeatersAndInverters(bool activeHigh)
    {
        var dLatch = new DLatchSchematicCreator
        {
            ActiveHighWrite = activeHigh,
            Facing = Direction.West,
            IncludeInputLevers = true,
            IncludeOutputLamp = true
        }.Build();
        Assert.Equal(7, dLatch.Width);
        Assert.Equal(2, dLatch.Length);

        var dff = new DFlipFlopSchematicCreator
        {
            Facing = Direction.East,
            IncludeInputLevers = true,
            IncludeOutputLamp = true
        }.Build();
        Assert.Equal(9, dff.Width);
        Assert.Equal(3, dff.Length);
        Assert.Equal(Minecraft.Blocks.Repeater, dff[2, 1, 2].Identifier);
        Assert.Equal(Minecraft.Blocks.Repeater, dff[6, 1, 2].Identifier);
        Assert.Equal("true", dff[6, 1, 2].Properties["locked"]);
    }

    [Theory]
    [InlineData(TFlipFlopMode.CopperBulb)]
    [InlineData(TFlipFlopMode.Piston)]
    [InlineData(TFlipFlopMode.DropperHopper)]
    public void TFlipFlop_AllModes_BuildsExpectedMemoryComponents(TFlipFlopMode mode)
    {
        var schematic = new TFlipFlopSchematicCreator
        {
            Mode = mode,
            Facing = Direction.South,
            IncludeInputButton = true,
            IncludeOutputLamp = true
        }.Build();

        Assert.True(schematic.Width >= 1);
        Assert.True(schematic.Length >= 4);
    }

    [Fact]
    public void BinaryCounter_AndShiftRegister_ScaleByBitCountAndPlaceViaExtensions()
    {
        var counter8 = new BinaryCounterSchematicCreator
        {
            BitCount = 8,
            Facing = Direction.East,
            IncludeInputButton = true,
            IncludeOutputLamps = true
        }.Build();

        int bulbCount = 0;
        int lampCount = 0;
        for (int x = 0; x < counter8.Width; x++)
        {
            for (int y = 0; y < counter8.Height; y++)
            {
                for (int z = 0; z < counter8.Length; z++)
                {
                    if (counter8[x, y, z].Identifier == Minecraft.Blocks.WaxedCopperBulb) bulbCount++;
                    if (counter8[x, y, z].Identifier == Minecraft.Blocks.RedstoneLamp) lampCount++;
                }
            }
        }

        Assert.Equal(8, bulbCount);
        Assert.Equal(8, lampCount);

        var combined = SchematicBuilder.Create(32, 6, 32)
            .AddRsLatch(0, 0, 0, RsLatchMode.RsNor)
            .AddDLatch(0, 0, 4)
            .AddDFlipFlop(0, 0, 8)
            .AddTFlipFlop(10, 0, 0)
            .AddBinaryCounter(10, 0, 4, bitCount: 4, includeOutputLamps: true)
            .AddShiftRegister(10, 0, 10, bitCount: 4, includeInputControls: true, includeOutputLamps: true)
            .Build();

        Assert.Equal(Minecraft.Blocks.StoneButton, combined[10, 2, 10].Identifier);
        Assert.Equal(Minecraft.Blocks.RedstoneLamp, combined[13, 1, 13].Identifier);
        Assert.Equal(Minecraft.Blocks.RedstoneLamp, combined[19, 1, 13].Identifier);
    }

    [Fact]
    public void MemoryCreators_InvalidArguments_Throw()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RsLatchSchematicCreator { Facing = Direction.Up }.Build());
        Assert.Throws<ArgumentOutOfRangeException>(() => new BinaryCounterSchematicCreator { BitCount = 0 }.Build());
        Assert.Throws<ArgumentOutOfRangeException>(() => new BinaryCounterSchematicCreator { BitCount = 17 }.Build());
        Assert.Throws<ArgumentOutOfRangeException>(() => new ShiftRegisterSchematicCreator { BitCount = 1 }.Build());
    }
}
