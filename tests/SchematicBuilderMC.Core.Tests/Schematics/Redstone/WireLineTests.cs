using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;
using SchematicBuilderMC.IO;
using SchematicBuilderMC.Models;
using SchematicBuilderMC.Registries;
using SchematicBuilderMC.Schematics.Redstone.Transmission;

namespace SchematicBuilderMC.Core.Tests.Schematics.Redstone;

public sealed class WireLineTests
{
    [Fact]
    public void SetWireLine_Length15ZeroOffset_PlacesOnlyRedstoneWire()
    {
        var schematic = new Schematic(20, 3, 3);
        schematic.SetWireLine(1, 1, 1, Direction.East, length: 15, supportBlock: Minecraft.Blocks.SmoothStone);

        for (int i = 0; i < 15; i++)
        {
            Assert.Equal(Minecraft.Blocks.SmoothStone, schematic[1 + i, 0, 1].Identifier);
            var wire = schematic[1 + i, 1, 1];
            Assert.Equal(Minecraft.Blocks.RedstoneWire, wire.Identifier);
            Assert.Equal("side", wire.Properties["east"]);
            Assert.Equal("side", wire.Properties["west"]);
        }
    }

    [Fact]
    public void SetWireLine_Length16And32_InsertsRepeaterAfter15Dust()
    {
        var schematic16 = new Schematic(16, 2, 1);
        schematic16.SetWireLine(0, 1, 0, Direction.East, length: 16, supportBlock: Minecraft.Blocks.SmoothStone);

        for (int i = 0; i < 15; i++)
        {
            Assert.Equal(Minecraft.Blocks.RedstoneWire, schematic16[i, 1, 0].Identifier);
        }

        Assert.Equal(Minecraft.Blocks.Repeater, schematic16[15, 1, 0].Identifier);
        Assert.Equal("west", schematic16[15, 1, 0].Properties["facing"]);

        var schematic32 = SchematicBuilder.Create(32, 2, 1)
            .SetWireLine(0, 1, 0, Direction.East, length: 32, repeaterDelay: 3, supportBlock: Minecraft.Blocks.SmoothStone)
            .Build();

        for (int i = 0; i < 32; i++)
        {
            if (i is 15 or 31)
            {
                Assert.Equal(Minecraft.Blocks.Repeater, schematic32[i, 1, 0].Identifier);
                Assert.Equal("west", schematic32[i, 1, 0].Properties["facing"]);
                Assert.Equal("3", schematic32[i, 1, 0].Properties["delay"]);
            }
            else
            {
                Assert.Equal(Minecraft.Blocks.RedstoneWire, schematic32[i, 1, 0].Identifier);
            }
        }
    }

    [Theory]
    [InlineData(10, 10, new[] { 5 })]
    [InlineData(5, 15, new[] { 0 })]
    [InlineData(22, 10, new[] { 5, 21 })]
    public void SetWireLine_WithDustOffset_AccountsForExistingDustCount(
        int length,
        int dustOffset,
        int[] expectedRepeaterIndices)
    {
        var schematic = SchematicBuilder.Create(length, 2, 1)
            .SetWireLine(new BlockPosition(0, 1, 0), Direction.East, length, dustOffset: dustOffset, supportBlock: Minecraft.Blocks.Stone)
            .Build();

        for (int i = 0; i < length; i++)
        {
            string expected = expectedRepeaterIndices.Contains(i)
                ? Minecraft.Blocks.Repeater
                : Minecraft.Blocks.RedstoneWire;
            Assert.Equal(expected, schematic[i, 1, 0].Identifier);
        }
    }

    [Theory]
    [InlineData(Direction.East, 1, 0, "west", "east", "west")]
    [InlineData(Direction.West, -1, 0, "east", "east", "west")]
    [InlineData(Direction.South, 0, 1, "north", "north", "south")]
    [InlineData(Direction.North, 0, -1, "south", "north", "south")]
    public void SetWireLine_AllHorizontalDirections_OrientsRepeatersAndWireCorrectly(
        Direction direction,
        int dx,
        int dz,
        string expectedRepeaterFacing,
        string axisPropA,
        string axisPropB)
    {
        var builder = SchematicBuilder.Create(40, 2, 40);
        int startX = 20;
        int startZ = 20;
        builder.SetWireLine(startX, 1, startZ, direction, length: 18, dustOffset: 0, powered: true);

        var schematic = builder.Build();
        Assert.Equal("15", schematic[startX, 1, startZ].Properties["power"]);
        Assert.Equal("side", schematic[startX, 1, startZ].Properties[axisPropA]);
        Assert.Equal("side", schematic[startX, 1, startZ].Properties[axisPropB]);

        var repeater = schematic[startX + (15 * dx), 1, startZ + (15 * dz)];
        Assert.Equal(Minecraft.Blocks.Repeater, repeater.Identifier);
        Assert.Equal(expectedRepeaterFacing, repeater.Properties["facing"]);
        Assert.Equal("true", repeater.Properties["powered"]);

        Assert.Equal("15", schematic[startX + (16 * dx), 1, startZ + (16 * dz)].Properties["power"]);
        Assert.Equal("14", schematic[startX + (17 * dx), 1, startZ + (17 * dz)].Properties["power"]);
    }

    [Fact]
    public void SetWireLine_PoweredWithOffset_ComputesRemainingSignalStrength()
    {
        var schematic = new Schematic(10, 2, 1);
        schematic.SetWireLine(0, 1, 0, Direction.East, length: 8, dustOffset: 10, powered: true);

        // Indices 0..4 represent dust 11..15 => power 5..1; index 5 is repeater; indices 6..7 reset to 15..14.
        Assert.Equal("5", schematic[0, 1, 0].Properties["power"]);
        Assert.Equal("1", schematic[4, 1, 0].Properties["power"]);
        Assert.Equal(Minecraft.Blocks.Repeater, schematic[5, 1, 0].Identifier);
        Assert.Equal("true", schematic[5, 1, 0].Properties["powered"]);
        Assert.Equal("15", schematic[6, 1, 0].Properties["power"]);
        Assert.Equal("14", schematic[7, 1, 0].Properties["power"]);

        string serializedPath = Path.Combine(SchematicCreatorTests.TestOutputDirectory, "WireLine_Offset.schem");
        SchematicFile.Write(schematic, serializedPath, SchematicFormat.SpongeV2);
        var readBack = SchematicFile.Read(serializedPath, SchematicFormat.SpongeV2);
        Assert.Equal(Minecraft.Blocks.Repeater, readBack[5, 1, 0].Identifier);
    }

    [Fact]
    public void TransmissionLineCreator_AndExtension_SupportDustOffset()
    {
        var creator = new TransmissionLineSchematicCreator
        {
            Length = 8,
            DustOffset = 12,
            Facing = Direction.East
        }.Build();

        // Dust 13, 14, 15 -> Repeater at index 3 -> Dust 1..4
        Assert.Equal(Minecraft.Blocks.RedstoneWire, creator[0, 1, 0].Identifier);
        Assert.Equal(Minecraft.Blocks.RedstoneWire, creator[2, 1, 0].Identifier);
        Assert.Equal(Minecraft.Blocks.Repeater, creator[3, 1, 0].Identifier);
        Assert.Equal(Minecraft.Blocks.RedstoneWire, creator[7, 1, 0].Identifier);

        var viaExtension = SchematicBuilder.Create(8, 2, 1)
            .AddTransmissionLine(0, 0, 0, length: 8, dustOffset: 12)
            .Build();
        Assert.Equal(Minecraft.Blocks.Repeater, viaExtension[3, 1, 0].Identifier);
    }

    [Fact]
    public void SetWireLine_InvalidArguments_ThrowExpectedExceptions()
    {
        var schematic = new Schematic(10, 2, 1);

        Assert.Throws<ArgumentOutOfRangeException>(() => schematic.SetWireLine(0, 1, 0, Direction.Up, 5));
        Assert.Throws<ArgumentOutOfRangeException>(() => schematic.SetWireLine(0, 1, 0, Direction.East, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => schematic.SetWireLine(0, 1, 0, Direction.East, 5, dustOffset: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => schematic.SetWireLine(0, 1, 0, Direction.East, 5, dustOffset: 16));
        Assert.Throws<ArgumentOutOfRangeException>(() => schematic.SetWireLine(0, 1, 0, Direction.East, 5, repeaterDelay: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => schematic.SetWireLine(0, 1, 0, Direction.East, 5, repeaterDelay: 5));
        Assert.Throws<ArgumentOutOfRangeException>(() => schematic.SetWireLine(0, 1, 0, Direction.East, 11));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TransmissionLineSchematicCreator { Length = 4, DustOffset = 16 }.Build());
    }
}
