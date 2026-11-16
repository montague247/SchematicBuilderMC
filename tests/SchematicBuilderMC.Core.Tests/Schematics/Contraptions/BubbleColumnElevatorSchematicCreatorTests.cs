using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;
using SchematicBuilderMC.IO;
using SchematicBuilderMC.Registries;
using SchematicBuilderMC.Schematics.Contraptions;

namespace SchematicBuilderMC.Core.Tests.Schematics.Contraptions;

public sealed class BubbleColumnElevatorSchematicCreatorTests
{
    [Theory]
    [InlineData(3, Direction.North, 5, 4, 4)]
    [InlineData(10, Direction.South, 5, 11, 4)]
    [InlineData(10, Direction.East, 4, 11, 5)]
    [InlineData(16, Direction.West, 4, 17, 5)]
    public void Build_ValidHeightsAndOrientations_ProducesExpectedBoundsAndBlocks(
        int shaftHeight,
        Direction facing,
        int expectedWidth,
        int expectedHeight,
        int expectedLength)
    {
        var creator = new BubbleColumnElevatorSchematicCreator
        {
            ShaftHeight = shaftHeight,
            Facing = facing
        };

        var schematic = creator.Build();

        Assert.Equal(expectedWidth, schematic.Width);
        Assert.Equal(expectedHeight, schematic.Height);
        Assert.Equal(expectedLength, schematic.Length);

        int soulSandCount = 0;
        int magmaCount = 0;
        int waterSourceCount = 0;
        int doorCount = 0;
        string expectedDoorFacing = facing.ToString().ToLowerInvariant();

        for (int y = 0; y < schematic.Height; y++)
        {
            for (int z = 0; z < schematic.Length; z++)
            {
                for (int x = 0; x < schematic.Width; x++)
                {
                    var block = schematic[x, y, z];
                    if (block.Identifier == Minecraft.Blocks.SoulSand)
                    {
                        soulSandCount++;
                        Assert.Equal(0, y);
                    }
                    else if (block.Identifier == Minecraft.Blocks.MagmaBlock)
                    {
                        magmaCount++;
                        Assert.Equal(0, y);
                    }
                    else if (block.Identifier == Minecraft.Blocks.Water)
                    {
                        waterSourceCount++;
                        Assert.Equal("0", block.GetProperty("level"));
                    }
                    else if (block.Identifier == creator.DoorBlock)
                    {
                        doorCount++;
                        Assert.Equal(expectedDoorFacing, block.GetProperty("facing"));
                        Assert.Equal("false", block.GetProperty("open"));
                    }
                }
            }
        }

        Assert.Equal(1, soulSandCount);
        Assert.Equal(1, magmaCount);
        Assert.Equal(shaftHeight * 2, waterSourceCount);
        Assert.Equal(shaftHeight == 3 ? 6 : 8, doorCount);
    }

    [Theory]
    [InlineData(Direction.North, 1, 2, 3, 2)]
    [InlineData(Direction.South, 3, 1, 1, 1)]
    [InlineData(Direction.East, 1, 1, 1, 3)]
    [InlineData(Direction.West, 2, 3, 2, 1)]
    public void Build_AllHorizontalOrientations_PlacesSoulSandAndMagmaAtExpectedColumns(
        Direction facing,
        int soulX, int soulZ,
        int magmaX, int magmaZ)
    {
        var creator = new BubbleColumnElevatorSchematicCreator
        {
            ShaftHeight = 6,
            Facing = facing
        };

        var schematic = creator.Build();

        Assert.Equal(Minecraft.Blocks.SoulSand, schematic[soulX, 0, soulZ].Identifier);
        Assert.Equal(Minecraft.Blocks.MagmaBlock, schematic[magmaX, 0, magmaZ].Identifier);

        for (int y = 1; y <= 6; y++)
        {
            Assert.Equal(Minecraft.Blocks.Water, schematic[soulX, y, soulZ].Identifier);
            Assert.Equal("0", schematic[soulX, y, soulZ].GetProperty("level"));
            Assert.Equal(Minecraft.Blocks.Water, schematic[magmaX, y, magmaZ].Identifier);
            Assert.Equal("0", schematic[magmaX, y, magmaZ].GetProperty("level"));
        }
    }

    [Theory]
    [InlineData(2)]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_InvalidShaftHeight_ThrowsArgumentOutOfRangeException(int invalidHeight)
    {
        var creator = new BubbleColumnElevatorSchematicCreator { ShaftHeight = invalidHeight };
        Assert.Throws<ArgumentOutOfRangeException>(() => creator.Create());
    }

    [Theory]
    [InlineData(Direction.Up)]
    [InlineData(Direction.Down)]
    public void Create_VerticalFacing_ThrowsArgumentOutOfRangeException(Direction invalidFacing)
    {
        var creator = new BubbleColumnElevatorSchematicCreator { Facing = invalidFacing };
        Assert.Throws<ArgumentOutOfRangeException>(() => creator.Create());
    }

    [Fact]
    public void Place_IntoLargerSchematic_ValidatesBounds()
    {
        var elevator = new BubbleColumnElevatorSchematicCreator { ShaftHeight = 5, Facing = Direction.North };
        var builder = SchematicBuilder.Create(8, 8, 8).Create(elevator, 1, 1, 1);
        var schematic = builder.Build();

        Assert.Equal(Minecraft.Blocks.SoulSand, schematic[2, 1, 3].Identifier);
        Assert.Throws<ArgumentOutOfRangeException>(() => builder.Create(elevator, 5, 1, 1));
    }

    [Fact]
    public void SpongeRoundtrip_PreservesWaterLevelsAndDoorStates()
    {
        var creator = new BubbleColumnElevatorSchematicCreator
        {
            ShaftHeight = 8,
            Facing = Direction.East
        };

        var schematic = creator.Build();
        byte[] bytes = SchematicFile.WriteToBytes(schematic, SchematicFormat.SpongeV2);
        var loaded = SchematicFile.Read(bytes, SchematicFormat.SpongeV2);

        Assert.Equal(schematic.Width, loaded.Width);
        Assert.Equal(schematic.Height, loaded.Height);
        Assert.Equal(schematic.Length, loaded.Length);
        Assert.Equal(Minecraft.Blocks.SoulSand, loaded[1, 0, 1].Identifier);
        Assert.Equal("0", loaded[1, 5, 1].GetProperty("level"));
        Assert.Equal("east", loaded[2, 1, 1].GetProperty("facing"));
    }
}
