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
    [InlineData(Direction.North, 4, 4, 6, 1, 0, 4, 0, 0, 4, "west", 2, 0, 4, "east", 2, 0, 1, 2, 2, 2, true)]
    [InlineData(Direction.South, 4, 4, 6, 2, 0, 1, 3, 0, 1, "east", 1, 0, 1, "west", 1, 0, 4, 1, 2, 3, true)]
    [InlineData(Direction.East, 6, 4, 4, 1, 0, 1, 1, 0, 0, "north", 1, 0, 2, "south", 4, 0, 2, 3, 2, 2, false)]
    [InlineData(Direction.West, 6, 4, 4, 4, 0, 2, 4, 0, 3, "south", 4, 0, 1, "north", 1, 0, 1, 2, 2, 1, false)]
    public void Build_AllHorizontalDirections_PlacesVerifiedOneShotLayoutAndOpenDropColumn(
        Direction facing,
        int expectedWidth,
        int expectedHeight,
        int expectedLength,
        int expectedTriggerX,
        int expectedTriggerY,
        int expectedTriggerZ,
        int expectedButtonX,
        int expectedButtonY,
        int expectedButtonZ,
        string expectedButtonFacing,
        int expectedTorchX,
        int expectedTorchY,
        int expectedTorchZ,
        string expectedTorchFacing,
        int expectedDropX,
        int expectedDropY,
        int expectedDropZ,
        int expectedTntX,
        int expectedTntY,
        int expectedTntZ,
        bool expectEastWestWire)
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
        for (int y = 0; y < 4; y++)
        {
            Assert.Equal(BlockState.Air, schematic[dropPos.X, y, dropPos.Z]);
        }

        var tntPos = creator.GetTntBlockPosition();
        Assert.Equal(new BlockPosition(expectedTntX, expectedTntY, expectedTntZ), tntPos);
        Assert.Equal(Minecraft.Blocks.Tnt, schematic[tntPos.X, tntPos.Y, tntPos.Z].Identifier);
        Assert.Equal("false", schematic[tntPos.X, tntPos.Y, tntPos.Z].GetProperty("unstable"));

        var triggerPos = creator.GetTriggerPosition();
        Assert.Equal(new BlockPosition(expectedTriggerX, expectedTriggerY, expectedTriggerZ), triggerPos);
        Assert.Equal(Minecraft.Blocks.Stone, schematic[triggerPos.X, triggerPos.Y, triggerPos.Z].Identifier);

        var buttonBlock = schematic[expectedButtonX, expectedButtonY, expectedButtonZ];
        Assert.Equal(Minecraft.Blocks.StoneButton, buttonBlock.Identifier);
        Assert.Equal("wall", buttonBlock.GetProperty("face"));
        Assert.Equal(expectedButtonFacing, buttonBlock.GetProperty("facing"));
        Assert.Equal("false", buttonBlock.GetProperty("powered"));

        var torchBlock = schematic[expectedTorchX, expectedTorchY, expectedTorchZ];
        Assert.Equal(Minecraft.Blocks.RedstoneWallTorch, torchBlock.Identifier);
        Assert.Equal(expectedTorchFacing, torchBlock.GetProperty("facing"));
        Assert.Equal("true", torchBlock.GetProperty("lit"));

        int stickyPistonCount = 0;
        int slimeCount = 0;
        int obsidianCount = 0;
        int redstoneBlockCount = 0;
        int wireCount = 0;
        int coralFanCount = 0;
        int tntCount = 0;
        int stoneCount = 0;

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
                    else if (block.Identifier == Minecraft.Blocks.Obsidian)
                    {
                        obsidianCount++;
                    }
                    else if (block.Identifier == Minecraft.Blocks.RedstoneBlock)
                    {
                        redstoneBlockCount++;
                    }
                    else if (block.Identifier == Minecraft.Blocks.RedstoneWire)
                    {
                        wireCount++;
                        Assert.Equal("15", block.GetProperty("power"));
                        Assert.Equal(expectEastWestWire ? "side" : "none", block.GetProperty("east"));
                        Assert.Equal(expectEastWestWire ? "side" : "none", block.GetProperty("west"));
                        Assert.Equal(expectEastWestWire ? "none" : "side", block.GetProperty("north"));
                        Assert.Equal(expectEastWestWire ? "none" : "side", block.GetProperty("south"));
                    }
                    else if (block.Identifier == Minecraft.Blocks.DeadTubeCoralFan)
                    {
                        coralFanCount++;
                        Assert.Equal("false", block.GetProperty("waterlogged"));
                    }
                    else if (block.Identifier == Minecraft.Blocks.Tnt)
                    {
                        tntCount++;
                    }
                    else if (block.Identifier == Minecraft.Blocks.Stone)
                    {
                        stoneCount++;
                    }
                }
            }
        }

        Assert.Equal(1, stickyPistonCount);
        Assert.Equal(9, slimeCount);
        Assert.Equal(1, obsidianCount);
        Assert.Equal(1, redstoneBlockCount);
        Assert.Equal(1, wireCount);
        Assert.Equal(1, coralFanCount);
        Assert.Equal(1, tntCount);
        Assert.Equal(2, stoneCount); // Trigger block + front stopper wall
        Assert.Empty(schematic.Entities);
    }

    [Theory]
    [InlineData(Direction.North, 6, 8)]
    [InlineData(Direction.South, 6, 8)]
    [InlineData(Direction.East, 6, 8)]
    [InlineData(Direction.West, 6, 8)]
    public void WithDropPosition_AndPlaceAtDropPosition_AlignDropColumnAtExactTargetCoordinates(
        Direction facing,
        int targetX,
        int targetZ)
    {
        var standalone = new TntDuperSchematicCreator { Facing = facing }
            .WithDropPosition(targetX, targetZ, dropClearance: 4)
            .Build();

        for (int y = 0; y < 8; y++)
        {
            Assert.Equal(BlockState.Air, standalone[targetX, y, targetZ]);
        }

        var dropOffset = new TntDuperSchematicCreator { Facing = facing }.GetDropOffset();
        var tntPos = new TntDuperSchematicCreator { Facing = facing }
            .GetTntBlockPosition(targetX - dropOffset.X, 4, targetZ - dropOffset.Z);
        Assert.Equal(Minecraft.Blocks.Tnt, standalone[tntPos.X, tntPos.Y, tntPos.Z].Identifier);

        var builder = SchematicBuilder.Create(3, 3, 3);
        var duper = new TntDuperSchematicCreator { Facing = facing };
        builder.CreateAtDropPosition(duper, targetX, targetY: 5, targetZ);
        var placed = builder.Build();

        for (int y = 0; y < 9; y++)
        {
            Assert.Equal(BlockState.Air, placed[targetX, y, targetZ]);
        }

        var placedTntPos = duper.GetTntBlockPosition(targetX - dropOffset.X, 5, targetZ - dropOffset.Z);
        Assert.Equal(Minecraft.Blocks.Tnt, placed[placedTntPos.X, placedTntPos.Y, placedTntPos.Z].Identifier);
    }

    [Fact]
    public void Build_InvalidArgumentsOrOutOfBoundsDropTarget_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TntDuperSchematicCreator { Facing = Direction.Up }.Build());
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TntDuperSchematicCreator { DropClearance = -1 }.Build());
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TntDuperSchematicCreator { Facing = Direction.South }.WithDropPosition(0, 4).Build());
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TntDuperSchematicCreator { Facing = Direction.South }.WithDropPosition(1, 3).Build());
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            SchematicBuilder.Create(4, 4, 4).CreateAtDropPosition(
                new TntDuperSchematicCreator { Facing = Direction.East },
                targetX: 3,
                targetY: 0,
                targetZ: 2));
    }

    [Fact]
    public void Build_WithRotationAndWriteReadSponge_RoundTripsCleanly()
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
        var dropOffset = creator.GetDropOffset();
        int startX = 5 - dropOffset.X;
        int startZ = 6 - dropOffset.Z;
        var tntPos = creator.GetTntBlockPosition(startX, 3, startZ);
        var triggerPos = creator.GetTriggerPosition(startX, 3, startZ);

        Assert.Equal(BlockState.Air, readBack[5, 3, 6]);
        Assert.Equal(Minecraft.Blocks.Tnt, readBack[tntPos.X, tntPos.Y, tntPos.Z].Identifier);
        Assert.Equal(Minecraft.Blocks.Stone, readBack[triggerPos.X, triggerPos.Y, triggerPos.Z].Identifier);
        Assert.Empty(readBack.Entities);

        var rotated = new TntDuperSchematicCreator { Facing = Direction.South }
            .WithRotation(Rotation.Clockwise90)
            .Build();
        Assert.Equal(6, rotated.Width);
        Assert.Equal(4, rotated.Height);
        Assert.Equal(4, rotated.Length);
    }
}
