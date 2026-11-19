using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;
using SchematicBuilderMC.IO;
using SchematicBuilderMC.Models;
using SchematicBuilderMC.Registries;
using SchematicBuilderMC.Schematics.Shapes;

namespace SchematicBuilderMC.Core.Tests.Schematics.Shapes;

public sealed class WaterChannelTests
{
    [Fact]
    public void WaterChannel_Default8Blocks_UsesBlueIceAndWaterLevels0To7()
    {
        var schematic = new WaterChannelSchematicCreator { Length = 8 }.Build();

        Assert.Equal(8, schematic.Width);
        Assert.Equal(2, schematic.Height);
        Assert.Equal(1, schematic.Length);

        for (int x = 0; x < 8; x++)
        {
            Assert.Equal(Minecraft.Blocks.BlueIce, schematic[x, 0, 0].Identifier);
            Assert.Equal(Minecraft.Blocks.Water, schematic[x, 1, 0].Identifier);
            Assert.Equal(x.ToString(), schematic[x, 1, 0].Properties["level"]);
        }
    }

    [Fact]
    public void WaterChannel_Long17Blocks_PlacesDividerAndSecondWaterSegment()
    {
        var schematic = new WaterChannelSchematicCreator
        {
            Length = 17,
            Facing = Direction.East,
            BaseBlock = Minecraft.Blocks.PackedIce,
            TrapdoorBlock = Minecraft.Blocks.SpruceTrapdoor
        }.Build();

        Assert.Equal(17, schematic.Width);
        Assert.Equal(2, schematic.Height);
        Assert.Equal(1, schematic.Length);

        // Segment 1: u=0..7 -> water level 0..7
        for (int i = 0; i < 8; i++)
        {
            Assert.Equal(Minecraft.Blocks.PackedIce, schematic[i, 0, 0].Identifier);
            Assert.Equal(Minecraft.Blocks.Water, schematic[i, 1, 0].Identifier);
            Assert.Equal(i.ToString(), schematic[i, 1, 0].Properties["level"]);
        }

        // Divider at u=8: closed top-half trapdoor allowing items underneath while blocking reverse flow
        Assert.Equal(Minecraft.Blocks.SpruceTrapdoor, schematic[8, 1, 0].Identifier);
        Assert.Equal("top", schematic[8, 1, 0].Properties["half"]);
        Assert.Equal("false", schematic[8, 1, 0].Properties["open"]);

        // Segment 2: u=9..16 -> water level 0..7
        for (int i = 9; i < 17; i++)
        {
            Assert.Equal(Minecraft.Blocks.Water, schematic[i, 1, 0].Identifier);
            Assert.Equal((i - 9).ToString(), schematic[i, 1, 0].Properties["level"]);
        }
    }

    [Fact]
    public void WaterChannel_9Blocks_LeavesFinal9ThBlockDryAboveBlueIce()
    {
        var schematic = new WaterChannelSchematicCreator { Length = 9 }.Build();

        for (int i = 0; i < 8; i++)
        {
            Assert.Equal(Minecraft.Blocks.Water, schematic[i, 1, 0].Identifier);
        }

        Assert.Equal(Minecraft.Blocks.BlueIce, schematic[8, 0, 0].Identifier);
        Assert.Equal(BlockState.Air, schematic[8, 1, 0]);
    }

    [Fact]
    public void WaterChannel_AllOptionalTrapdoorsEnabled_SurroundsBaseGuidesWaterAndCoversTop()
    {
        var creator = new WaterChannelSchematicCreator
        {
            Length = 6,
            Facing = Direction.East,
            TrapdoorBlock = Minecraft.Blocks.DarkOakTrapdoor,
            SurroundBaseWithTrapdoors = true,
            GuideWaterWithTrapdoors = true,
            CoverTopWithTrapdoors = true
        };

        var schematic = creator.Build();

        Assert.Equal(8, schematic.Width); // 1 back + 6 channel + 1 front (base surround)
        Assert.Equal(3, schematic.Height); // base + water/guide + top cover
        Assert.Equal(3, schematic.Length); // left + channel + right

        // Back trapdoors at x=0 (base surround & water stop cap)
        Assert.Equal(Minecraft.Blocks.DarkOakTrapdoor, schematic[0, 0, 1].Identifier);
        Assert.Equal("west", schematic[0, 0, 1].Properties["facing"]);
        Assert.Equal("true", schematic[0, 0, 1].Properties["open"]);

        Assert.Equal(Minecraft.Blocks.DarkOakTrapdoor, schematic[0, 1, 1].Identifier);
        Assert.Equal("west", schematic[0, 1, 1].Properties["facing"]);
        Assert.Equal("true", schematic[0, 1, 1].Properties["open"]);

        // Front trapdoor surrounds base at y=0, while water exit at y=1 remains open
        Assert.Equal(Minecraft.Blocks.DarkOakTrapdoor, schematic[7, 0, 1].Identifier);
        Assert.Equal("east", schematic[7, 0, 1].Properties["facing"]);
        Assert.Equal(BlockState.Air, schematic[7, 1, 1]);

        for (int i = 0; i < 6; i++)
        {
            int x = i + 1;
            Assert.Equal(Minecraft.Blocks.BlueIce, schematic[x, 0, 1].Identifier);
            Assert.Equal(Minecraft.Blocks.Water, schematic[x, 1, 1].Identifier);

            // Base & water side trapdoors on North (z=0) and South (z=2)
            Assert.Equal("north", schematic[x, 0, 0].Properties["facing"]);
            Assert.Equal("south", schematic[x, 0, 2].Properties["facing"]);
            Assert.Equal("north", schematic[x, 1, 0].Properties["facing"]);
            Assert.Equal("south", schematic[x, 1, 2].Properties["facing"]);
            Assert.Equal("true", schematic[x, 1, 0].Properties["open"]);
            Assert.Equal("bottom", schematic[x, 1, 0].Properties["half"]);

            // Top cover trapdoor at y=2 (closed, bottom half right on top of the water channel)
            Assert.Equal(Minecraft.Blocks.DarkOakTrapdoor, schematic[x, 2, 1].Identifier);
            Assert.Equal("bottom", schematic[x, 2, 1].Properties["half"]);
            Assert.Equal("false", schematic[x, 2, 1].Properties["open"]);
        }

        string serializedPath = Path.Combine(SchematicCreatorTests.TestOutputDirectory, "WaterChannel.schem");
        SchematicFile.Write(schematic, serializedPath, SchematicFormat.SpongeV2);
        var readBack = SchematicFile.Read(serializedPath, SchematicFormat.SpongeV2);
        Assert.Equal(schematic.Width, readBack.Width);
        Assert.Equal(schematic.Height, readBack.Height);
        Assert.Equal(schematic.Length, readBack.Length);
    }

    [Theory]
    [InlineData(Direction.East, 6, 2, 3)]
    [InlineData(Direction.West, 6, 2, 3)]
    [InlineData(Direction.South, 3, 2, 6)]
    [InlineData(Direction.North, 3, 2, 6)]
    public void WaterChannel_AllHorizontalDirections_BuildsValidOrientedChannel(
        Direction facing,
        int expectedWidth,
        int expectedHeight,
        int expectedLength)
    {
        var schematic = new WaterChannelSchematicCreator
        {
            Length = 5,
            Facing = facing,
            GuideWaterWithTrapdoors = true
        }.Build();

        Assert.Equal(expectedWidth, schematic.Width);
        Assert.Equal(expectedHeight, schematic.Height);
        Assert.Equal(expectedLength, schematic.Length);
    }

    [Fact]
    public void SchematicBuilder_SetWaterChannel_PlacesInsideLargerSchematicAndValidatesBounds()
    {
        var schematic = SchematicBuilder.Create(15, 4, 5)
            .SetWaterChannel(2, 0, 2, length: 10, facing: Direction.East, guideWaterWithTrapdoors: true, coverTopWithTrapdoors: true)
            .Build();

        Assert.Equal(Minecraft.Blocks.BlueIce, schematic[2, 0, 2].Identifier);
        Assert.Equal(Minecraft.Blocks.Water, schematic[2, 1, 2].Identifier);
        Assert.Equal(Minecraft.Blocks.OakTrapdoor, schematic[10, 1, 2].Identifier);
        Assert.Equal(Minecraft.Blocks.Water, schematic[11, 1, 2].Identifier);
        Assert.Equal(Minecraft.Blocks.OakTrapdoor, schematic[11, 2, 2].Identifier);

        Assert.Throws<ArgumentOutOfRangeException>(() => new WaterChannelSchematicCreator { Length = 0 }.Build());
        Assert.Throws<ArgumentOutOfRangeException>(() => new WaterChannelSchematicCreator { Facing = Direction.Up }.Build());
        Assert.Throws<ArgumentOutOfRangeException>(() => SchematicBuilder.Create(4, 2, 3).SetWaterChannel(0, 0, 1, length: 5));
    }
}
