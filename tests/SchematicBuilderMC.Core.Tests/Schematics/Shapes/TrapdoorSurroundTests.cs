using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;
using SchematicBuilderMC.IO;
using SchematicBuilderMC.Models;
using SchematicBuilderMC.Registries;
using SchematicBuilderMC.Schematics.Shapes;

namespace SchematicBuilderMC.Core.Tests.Schematics.Shapes;

public sealed class TrapdoorSurroundTests
{
    [Fact]
    public void SurroundWithTrapdoors_SingleBlock_DefaultsToBottomAndOpen()
    {
        var schematic = SchematicBuilder.Create(3, 1, 3)
            .SetBlock(1, 0, 1, Minecraft.Blocks.Glowstone)
            .SurroundWithTrapdoors(1, 0, 1)
            .Build();

        Assert.Equal(Minecraft.Blocks.Glowstone, schematic[1, 0, 1].Identifier);

        AssertTrapdoor(schematic[1, 0, 0], Minecraft.Blocks.OakTrapdoor, "north", "bottom", "true");
        AssertTrapdoor(schematic[1, 0, 2], Minecraft.Blocks.OakTrapdoor, "south", "bottom", "true");
        AssertTrapdoor(schematic[2, 0, 1], Minecraft.Blocks.OakTrapdoor, "east", "bottom", "true");
        AssertTrapdoor(schematic[0, 0, 1], Minecraft.Blocks.OakTrapdoor, "west", "bottom", "true");

        Assert.Equal(BlockState.Air, schematic[0, 0, 0]);
        Assert.Equal(BlockState.Air, schematic[2, 0, 0]);
        Assert.Equal(BlockState.Air, schematic[0, 0, 2]);
        Assert.Equal(BlockState.Air, schematic[2, 0, 2]);
    }

    [Theory]
    [InlineData(true, true, "bottom", "true")]
    [InlineData(false, true, "top", "true")]
    [InlineData(true, false, "bottom", "false")]
    [InlineData(false, false, "top", "false")]
    public void SurroundWithTrapdoors_BottomAndOpenOptions_SetExpectedProperties(
        bool bottom,
        bool open,
        string expectedHalf,
        string expectedOpen)
    {
        var schematic = new Schematic(3, 1, 3);
        schematic.SetBlock(1, 0, 1, Minecraft.Blocks.SeaLantern);
        schematic.SurroundWithTrapdoors(
            new BlockPosition(1, 0, 1),
            Minecraft.Blocks.SpruceTrapdoor,
            bottom: bottom,
            open: open,
            powered: false,
            waterlogged: false);

        AssertTrapdoor(schematic[1, 0, 0], Minecraft.Blocks.SpruceTrapdoor, "north", expectedHalf, expectedOpen);
        AssertTrapdoor(schematic[1, 0, 2], Minecraft.Blocks.SpruceTrapdoor, "south", expectedHalf, expectedOpen);
        AssertTrapdoor(schematic[2, 0, 1], Minecraft.Blocks.SpruceTrapdoor, "east", expectedHalf, expectedOpen);
        AssertTrapdoor(schematic[0, 0, 1], Minecraft.Blocks.SpruceTrapdoor, "west", expectedHalf, expectedOpen);
    }

    [Fact]
    public void SurroundWithTrapdoors_MultiBlockBox_OnlyPlacesOnExposedPerimeterFaces()
    {
        // 3x2x2 block volume at (1..3, 0..1, 1..2) inside 5x2x4 schematic
        var schematic = SchematicBuilder.Create(5, 2, 4)
            .Fill(1, 0, 1, 3, 1, 2, Minecraft.Blocks.OakLog)
            .SurroundWithTrapdoors(1, 0, 1, width: 3, height: 2, length: 2, trapdoorBlock: Minecraft.Blocks.DarkOakTrapdoor, bottom: false, open: true)
            .Build();

        // Interior blocks remain OakLog
        for (int y = 0; y < 2; y++)
        {
            for (int z = 1; z <= 2; z++)
            {
                for (int x = 1; x <= 3; x++)
                {
                    Assert.Equal(Minecraft.Blocks.OakLog, schematic[x, y, z].Identifier);
                }
            }

            // North/South/East/West perimeter ring across both heights
            for (int x = 1; x <= 3; x++)
            {
                AssertTrapdoor(schematic[x, y, 0], Minecraft.Blocks.DarkOakTrapdoor, "north", "top", "true");
                AssertTrapdoor(schematic[x, y, 3], Minecraft.Blocks.DarkOakTrapdoor, "south", "top", "true");
            }

            for (int z = 1; z <= 2; z++)
            {
                AssertTrapdoor(schematic[0, y, z], Minecraft.Blocks.DarkOakTrapdoor, "west", "top", "true");
                AssertTrapdoor(schematic[4, y, z], Minecraft.Blocks.DarkOakTrapdoor, "east", "top", "true");
            }
        }
    }

    [Fact]
    public void SurroundWithTrapdoors_ArbitraryPositionList_HandlesLShapeAndPreservesExistingBlocks()
    {
        BlockPosition[] lShape =
        [
            new(1, 0, 1),
            new(2, 0, 1),
            new(1, 0, 2)
        ];

        var schematic = SchematicBuilder.Create(4, 1, 4)
            .SetBlock(0, 0, 1, Minecraft.Blocks.Stone) // pre-existing block should not be overwritten by default
            .SurroundWithTrapdoors(lShape, Minecraft.Blocks.CopperTrapdoor)
            .Build();

        Assert.Equal(Minecraft.Blocks.Stone, schematic[0, 0, 1].Identifier);
        AssertTrapdoor(schematic[1, 0, 0], Minecraft.Blocks.CopperTrapdoor, "north", "bottom", "true");
        AssertTrapdoor(schematic[2, 0, 0], Minecraft.Blocks.CopperTrapdoor, "north", "bottom", "true");
        AssertTrapdoor(schematic[3, 0, 1], Minecraft.Blocks.CopperTrapdoor, "east", "bottom", "true");
        AssertTrapdoor(schematic[0, 0, 2], Minecraft.Blocks.CopperTrapdoor, "west", "bottom", "true");
        AssertTrapdoor(schematic[1, 0, 3], Minecraft.Blocks.CopperTrapdoor, "south", "bottom", "true");
        Assert.Equal(Minecraft.Blocks.CopperTrapdoor, schematic[2, 0, 2].Identifier); // inside corner filled once
    }

    [Fact]
    public void TrapdoorSurroundCreator_BuildsAndRoundTripsSpongeV2()
    {
        var creator = new TrapdoorSurroundSchematicCreator
        {
            Block = Minecraft.Blocks.Shroomlight,
            TrapdoorBlock = Minecraft.Blocks.MangroveTrapdoor,
            Width = 2,
            Height = 3,
            Length = 1,
            Bottom = false,
            Open = false
        };

        var schematic = creator.Build();
        Assert.Equal(4, schematic.Width);
        Assert.Equal(3, schematic.Height);
        Assert.Equal(3, schematic.Length);

        string outputPath = Path.Combine(SchematicCreatorTests.TestOutputDirectory, "TrapdoorSurround.schem");
        SchematicFile.Write(schematic, outputPath, SchematicFormat.SpongeV2);
        var readBack = SchematicFile.Read(outputPath, SchematicFormat.SpongeV2);

        Assert.Equal(Minecraft.Blocks.Shroomlight, readBack[1, 0, 1].Identifier);
        AssertTrapdoor(readBack[1, 2, 0], Minecraft.Blocks.MangroveTrapdoor, "north", "top", "false");
    }

    [Fact]
    public void SurroundWithTrapdoors_InvalidArguments_ThrowExpectedExceptions()
    {
        var builder = SchematicBuilder.Create(3, 1, 3);

        Assert.Throws<ArgumentOutOfRangeException>(() => builder.SurroundWithTrapdoors(0, 0, 0)); // neighbor at x = -1 out of bounds
        Assert.Throws<ArgumentOutOfRangeException>(() => builder.SurroundWithTrapdoors(1, 0, 1, width: 0, height: 1, length: 1));
        Assert.Throws<ArgumentException>(() => builder.SurroundWithTrapdoors(Array.Empty<BlockPosition>()));
        Assert.Throws<ArgumentOutOfRangeException>(() => builder.SetTrapdoor(1, 0, 1, facing: Direction.Up));
    }

    private static void AssertTrapdoor(BlockState state, string expectedId, string expectedFacing, string expectedHalf, string expectedOpen)
    {
        Assert.Equal(expectedId, state.Identifier);
        Assert.Equal(expectedFacing, state.Properties["facing"]);
        Assert.Equal(expectedHalf, state.Properties["half"]);
        Assert.Equal(expectedOpen, state.Properties["open"]);
    }
}
