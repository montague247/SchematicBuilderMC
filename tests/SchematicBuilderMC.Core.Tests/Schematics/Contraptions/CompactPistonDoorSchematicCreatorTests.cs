using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;
using SchematicBuilderMC.IO;
using SchematicBuilderMC.Models;
using SchematicBuilderMC.Registries;
using SchematicBuilderMC.Schematics.Contraptions;

namespace SchematicBuilderMC.Core.Tests.Schematics.Contraptions;

public sealed class CompactPistonDoorSchematicCreatorTests
{
    [Theory]
    [InlineData(PistonDoorSize.TwoByTwo, Direction.North, 6, 4, 3)]
    [InlineData(PistonDoorSize.TwoByTwo, Direction.South, 6, 4, 3)]
    [InlineData(PistonDoorSize.TwoByTwo, Direction.East, 3, 4, 6)]
    [InlineData(PistonDoorSize.TwoByTwo, Direction.West, 3, 4, 6)]
    [InlineData(PistonDoorSize.ThreeByThree, Direction.North, 7, 5, 3)]
    [InlineData(PistonDoorSize.ThreeByThree, Direction.South, 7, 5, 3)]
    [InlineData(PistonDoorSize.ThreeByThree, Direction.East, 3, 5, 7)]
    [InlineData(PistonDoorSize.ThreeByThree, Direction.West, 3, 5, 7)]
    public void Build_BothSizesAndAllOrientations_ProducesExpectedDimensionsPistonsAndTrigger(
        PistonDoorSize size,
        Direction facing,
        int expectedWidth,
        int expectedHeight,
        int expectedLength)
    {
        var creator = new CompactPistonDoorSchematicCreator
        {
            Size = size,
            Facing = facing,
            Powered = true
        };

        var schematic = creator.Build();

        Assert.Equal(expectedWidth, schematic.Width);
        Assert.Equal(expectedHeight, schematic.Height);
        Assert.Equal(expectedLength, schematic.Length);

        var triggerPos = creator.GetTriggerPosition();
        var lever = schematic[triggerPos];
        Assert.Equal(Minecraft.Blocks.Lever, lever.Identifier);
        Assert.Equal("wall", lever.GetProperty("face"));
        Assert.Equal(facing.ToString().ToLowerInvariant(), lever.GetProperty("facing"));
        Assert.Equal("true", lever.GetProperty("powered"));

        int stickyPistonCount = 0;
        int doorBlockCount = 0;
        for (int y = 0; y < schematic.Height; y++)
        {
            for (int z = 0; z < schematic.Length; z++)
            {
                for (int x = 0; x < schematic.Width; x++)
                {
                    var block = schematic[x, y, z];
                    if (block.Identifier == Minecraft.Blocks.StickyPiston)
                    {
                        stickyPistonCount++;
                        Assert.Equal("true", block.GetProperty("extended"));
                    }
                    else if (block.Identifier == creator.DoorBlock)
                    {
                        doorBlockCount++;
                    }
                }
            }
        }

        int expectedHorizontalPistons = (int)size * 2;
        int expectedVerticalPistons = size == PistonDoorSize.ThreeByThree ? 2 : 0;
        Assert.Equal(expectedHorizontalPistons + expectedVerticalPistons, stickyPistonCount);
        Assert.Equal((int)size * (int)size, doorBlockCount);
    }

    [Theory]
    [InlineData(Direction.North, 1, 1, 1, "east", 4, 1, 1, "west")]
    [InlineData(Direction.South, 4, 1, 1, "west", 1, 1, 1, "east")]
    [InlineData(Direction.East, 1, 1, 1, "south", 1, 1, 4, "north")]
    [InlineData(Direction.West, 1, 1, 4, "north", 1, 1, 1, "south")]
    public void Build_TwoByTwo_PlacesInwardFacingStickyPistons(
        Direction facing,
        int leftX, int leftY, int leftZ, string expectedLeftFacing,
        int rightX, int rightY, int rightZ, string expectedRightFacing)
    {
        var creator = new CompactPistonDoorSchematicCreator
        {
            Size = PistonDoorSize.TwoByTwo,
            Facing = facing
        };

        var schematic = creator.Build();

        var leftPiston = schematic[leftX, leftY, leftZ];
        var rightPiston = schematic[rightX, rightY, rightZ];

        Assert.Equal(Minecraft.Blocks.StickyPiston, leftPiston.Identifier);
        Assert.Equal(expectedLeftFacing, leftPiston.GetProperty("facing"));
        Assert.Equal(Minecraft.Blocks.StickyPiston, rightPiston.Identifier);
        Assert.Equal(expectedRightFacing, rightPiston.GetProperty("facing"));
    }

    [Fact]
    public void Build_ThreeByThree_PlacesVerticalCenterStickyPistons()
    {
        var creator = new CompactPistonDoorSchematicCreator
        {
            Size = PistonDoorSize.ThreeByThree,
            Facing = Direction.North
        };

        var schematic = creator.Build();

        Assert.Equal(Minecraft.Blocks.StickyPiston, schematic[3, 4, 1].Identifier);
        Assert.Equal("down", schematic[3, 4, 1].GetProperty("facing"));
        Assert.Equal(Minecraft.Blocks.StickyPiston, schematic[3, 0, 1].Identifier);
        Assert.Equal("up", schematic[3, 0, 1].GetProperty("facing"));
    }

    [Fact]
    public void Place_IntoLargerSchematic_PreservesUnrelatedBlocksAndRejectsOutOfBounds()
    {
        var door = new CompactPistonDoorSchematicCreator
        {
            Size = PistonDoorSize.TwoByTwo,
            Facing = Direction.North
        };

        var builder = SchematicBuilder.Create(10, 6, 8)
            .SetBlock(0, 0, 0, Minecraft.Blocks.DiamondBlock)
            .SetBlock(9, 5, 7, Minecraft.Blocks.GoldBlock)
            .Create(door, startX: 2, startY: 1, startZ: 2);

        var schematic = builder.Build();
        Assert.Equal(Minecraft.Blocks.DiamondBlock, schematic[0, 0, 0].Identifier);
        Assert.Equal(Minecraft.Blocks.GoldBlock, schematic[9, 5, 7].Identifier);
        Assert.Equal(Minecraft.Blocks.Lever, schematic[door.GetTriggerPosition(2, 1, 2)].Identifier);

        Assert.Throws<ArgumentOutOfRangeException>(() => builder.Create(door, startX: 6, startY: 1, startZ: 2));
        Assert.Equal(Minecraft.Blocks.GoldBlock, schematic[9, 5, 7].Identifier);
    }

    [Theory]
    [InlineData(Direction.Up)]
    [InlineData(Direction.Down)]
    public void Create_VerticalDirection_ThrowsArgumentOutOfRangeException(Direction invalidFacing)
    {
        var creator = new CompactPistonDoorSchematicCreator { Facing = invalidFacing };
        Assert.Throws<ArgumentOutOfRangeException>(() => creator.Create());
    }

    [Fact]
    public void SpongeRoundtrip_PreservesDoorBlockStates()
    {
        var creator = new CompactPistonDoorSchematicCreator
        {
            Size = PistonDoorSize.ThreeByThree,
            Facing = Direction.East,
            Powered = true
        };

        var schematic = creator.Build();
        byte[] bytes = SchematicFile.WriteToBytes(schematic, SchematicFormat.SpongeV2);
        var loaded = SchematicFile.Read(bytes, SchematicFormat.SpongeV2);

        Assert.Equal(schematic.Width, loaded.Width);
        Assert.Equal(schematic.Height, loaded.Height);
        Assert.Equal(schematic.Length, loaded.Length);

        var triggerPos = creator.GetTriggerPosition();
        Assert.Equal(schematic[triggerPos], loaded[triggerPos]);
        Assert.Equal("true", loaded[triggerPos].GetProperty("powered"));
    }
}
