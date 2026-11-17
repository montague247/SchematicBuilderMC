using fNbt;
using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;
using SchematicBuilderMC.IO;
using SchematicBuilderMC.Models;
using SchematicBuilderMC.Registries;
using SchematicBuilderMC.Schematics.Contraptions;

namespace SchematicBuilderMC.Core.Tests.Schematics.Contraptions;

public sealed class TntDuperSchematicCreatorTests
{
    [Theory]
    [InlineData(Direction.South, 2, 3, 4, 0, 1, 0, 1, 0, 3, "north_south", "north")]
    [InlineData(Direction.North, 2, 3, 4, 1, 1, 3, 0, 0, 0, "north_south", "south")]
    [InlineData(Direction.East, 4, 3, 2, 0, 1, 1, 3, 0, 0, "east_west", "west")]
    [InlineData(Direction.West, 4, 3, 2, 3, 1, 0, 0, 0, 1, "east_west", "east")]
    public void Build_AllHorizontalDirections_PlacesCoreBlocksEntitiesAndAlignments(
        Direction facing,
        int expectedWidth,
        int expectedHeight,
        int expectedLength,
        int expectedTriggerX,
        int expectedTriggerY,
        int expectedTriggerZ,
        int expectedDropX,
        int expectedDropY,
        int expectedDropZ,
        string expectedRailShape,
        string expectedCoralFacing)
    {
        var creator = new TntDuperSchematicCreator
        {
            Facing = facing,
            IncludeTriggerButton = true
        };

        var schematic = creator.Build();

        Assert.Equal(expectedWidth, schematic.Width);
        Assert.Equal(expectedHeight, schematic.Height);
        Assert.Equal(expectedLength, schematic.Length);

        var dropPos = creator.GetDropPosition();
        Assert.Equal(new BlockPosition(expectedDropX, expectedDropY, expectedDropZ), dropPos);
        Assert.Equal(Minecraft.Blocks.Tnt, schematic[dropPos.X, dropPos.Y, dropPos.Z].Identifier);

        var triggerPos = creator.GetTriggerPosition();
        Assert.Equal(new BlockPosition(expectedTriggerX, expectedTriggerY, expectedTriggerZ), triggerPos);
        Assert.Equal(Minecraft.Blocks.Target, schematic[triggerPos.X, triggerPos.Y, triggerPos.Z].Identifier);

        var buttonBlock = schematic[triggerPos.X, triggerPos.Y + 1, triggerPos.Z];
        Assert.Equal(Minecraft.Blocks.StoneButton, buttonBlock.Identifier);
        Assert.Equal("floor", buttonBlock.GetProperty("face"));
        Assert.Equal("false", buttonBlock.GetProperty("powered"));

        int stickyPistonCount = 0;
        int slimeCount = 0;
        int detectorRailCount = 0;
        int coralFanCount = 0;
        int tntCount = 0;
        int wallCount = 0;

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
                        Assert.Equal(facing.ToString().ToLowerInvariant(), block.GetProperty("facing"));
                        Assert.Equal("false", block.GetProperty("extended"));
                    }
                    else if (block.Identifier == Minecraft.Blocks.SlimeBlock)
                    {
                        slimeCount++;
                    }
                    else if (block.Identifier == Minecraft.Blocks.DetectorRail)
                    {
                        detectorRailCount++;
                        Assert.Equal("true", block.GetProperty("powered"));
                        Assert.Equal(expectedRailShape, block.GetProperty("shape"));
                        Assert.Equal("false", block.GetProperty("waterlogged"));
                    }
                    else if (block.Identifier == Minecraft.Blocks.DeadTubeCoralWallFan)
                    {
                        coralFanCount++;
                        Assert.Equal(expectedCoralFacing, block.GetProperty("facing"));
                        Assert.Equal("false", block.GetProperty("waterlogged"));
                    }
                    else if (block.Identifier == Minecraft.Blocks.Tnt)
                    {
                        tntCount++;
                    }
                    else if (block.Identifier == Minecraft.Blocks.CobblestoneWall)
                    {
                        wallCount++;
                    }
                }
            }
        }

        Assert.Equal(1, stickyPistonCount);
        Assert.Equal(3, slimeCount);
        Assert.Equal(1, detectorRailCount);
        Assert.Equal(1, coralFanCount);
        Assert.Equal(1, tntCount);
        Assert.Equal(1, wallCount);

        var minecart = Assert.Single(schematic.Entities);
        Assert.Equal(Minecraft.Entities.Minecart, minecart.Id);
    }

    [Theory]
    [InlineData(Direction.South, 6, 8)]
    [InlineData(Direction.North, 6, 8)]
    [InlineData(Direction.East, 6, 8)]
    [InlineData(Direction.West, 6, 8)]
    public void WithDropPosition_AndPlaceAtDropPosition_AlignTntAtExactTargetCoordinates(
        Direction facing,
        int targetX,
        int targetZ)
    {
        // Standalone WithDropPosition builder alignment
        var standalone = new TntDuperSchematicCreator { Facing = facing }
            .WithDropPosition(targetX, targetZ, dropClearance: 4)
            .Build();

        Assert.Equal(Minecraft.Blocks.Tnt, standalone[targetX, 4, targetZ].Identifier);
        for (int y = 0; y < 4; y++)
        {
            Assert.Equal(BlockState.Air, standalone[targetX, y, targetZ]);
        }

        // Fluent placement inside an existing schematic auto-extending bounds cleanly
        var builder = SchematicBuilder.Create(3, 3, 3);
        var duper = new TntDuperSchematicCreator { Facing = facing };
        builder.CreateAtDropPosition(duper, targetX, targetY: 5, targetZ);
        var placed = builder.Build();

        Assert.Equal(Minecraft.Blocks.Tnt, placed[targetX, 5, targetZ].Identifier);
        for (int y = 0; y < 5; y++)
        {
            Assert.Equal(BlockState.Air, placed[targetX, y, targetZ]);
        }
    }

    [Fact]
    public void Build_InvalidArgumentsOrOutOfBoundsDropTarget_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TntDuperSchematicCreator { Facing = Direction.Up }.Build());
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TntDuperSchematicCreator { DropClearance = -1 }.Build());
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TntDuperSchematicCreator { Facing = Direction.South }.WithDropPosition(0, 3).Build());
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TntDuperSchematicCreator { Facing = Direction.South }.WithDropPosition(1, 2).Build());
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            SchematicBuilder.Create(4, 4, 4).CreateAtDropPosition(
                new TntDuperSchematicCreator { Facing = Direction.East },
                targetX: 2,
                targetY: 0,
                targetZ: 0));
    }

    [Fact]
    public void Build_WithRotationAndWriteReadSponge_PreservesBlocksAndMinecartEntity()
    {
        var creator = new TntDuperSchematicCreator
        {
            Facing = Direction.South,
            IncludeTriggerButton = true
        }.WithDropPosition(5, 6, dropClearance: 3);

        var schematic = creator.Build();
        string outputPath = Path.Combine(SchematicCreatorTests.TestOutputDirectory, "TntDuper.schem");
        SchematicFile.Write(schematic, outputPath, SchematicFormat.SpongeV2);

        var readBack = SchematicFile.Read(outputPath, SchematicFormat.SpongeV2);
        Assert.Equal(Minecraft.Blocks.Tnt, readBack[5, 3, 6].Identifier);
        Assert.Equal(Minecraft.Blocks.Target, readBack[4, 4, 3].Identifier);
        var minecart = Assert.Single(readBack.Entities);
        Assert.Equal(Minecraft.Entities.Minecart, minecart.Id);

        // Verify WorldEdit SpongeV2 entity NBT compliance (top-level Rotation tag on entity compound)
        var nbt = new NbtFile();
        nbt.LoadFromFile(outputPath);
        var entitiesList = Assert.IsType<NbtList>(nbt.RootTag["Entities"]);
        var entityCompound = Assert.IsType<NbtCompound>(entitiesList[0]);
        Assert.Equal(Minecraft.Entities.Minecart, entityCompound["Id"]?.StringValue);
        Assert.False(entityCompound.Contains("Data"));
        var rotation = Assert.IsType<NbtList>(entityCompound["Rotation"]);
        Assert.Equal(2, rotation.Count);

        var rotated = new TntDuperSchematicCreator { Facing = Direction.South }
            .WithRotation(Rotation.Clockwise90)
            .Build();
        Assert.Equal(4, rotated.Width);
        Assert.Equal(3, rotated.Height);
        Assert.Equal(2, rotated.Length);
        Assert.Single(rotated.Entities);
    }
}
