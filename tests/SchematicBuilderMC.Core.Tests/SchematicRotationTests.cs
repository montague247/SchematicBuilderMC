using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;
using SchematicBuilderMC.Extensions;
using SchematicBuilderMC.Models;
using SchematicBuilderMC.Registries;
using SchematicBuilderMC.Schematics.Contraptions;
using SchematicBuilderMC.Schematics.Farms;

namespace SchematicBuilderMC.Core.Tests;

public class SchematicRotationTests
{
    [Theory]
    [InlineData(Direction.North, Rotation.None, Direction.North)]
    [InlineData(Direction.North, Rotation.Clockwise90, Direction.East)]
    [InlineData(Direction.North, Rotation.Clockwise180, Direction.South)]
    [InlineData(Direction.North, Rotation.Clockwise270, Direction.West)]
    [InlineData(Direction.West, Rotation.Clockwise90, Direction.North)]
    [InlineData(Direction.Up, Rotation.Clockwise90, Direction.Up)]
    [InlineData(Direction.Down, Rotation.Clockwise180, Direction.Down)]
    public void Direction_Rotate_MapsHorizontalsAndPreservesVerticals(Direction input, Rotation rotation, Direction expected)
    {
        Assert.Equal(expected, input.Rotate(rotation));
    }

    [Fact]
    public void BlockState_Rotate_UpdatesFacingAxisShapeRotationAndSideProperties()
    {
        var stairs = Minecraft.Blocks.OakStairs.WithFacing(Direction.North).TopHalf();
        Assert.Equal("east", stairs.Rotate(Rotation.Clockwise90).GetProperty("facing"));
        Assert.Equal("south", stairs.Rotate(Rotation.Clockwise180).GetProperty("facing"));
        Assert.Equal("west", stairs.Rotate(Rotation.Clockwise270).GetProperty("facing"));
        Assert.Equal("top", stairs.Rotate(Rotation.Clockwise90).GetProperty("half"));

        var log = Minecraft.Blocks.OakLog.WithAxis(Axis.X);
        Assert.Equal("z", log.Rotate(Rotation.Clockwise90).GetProperty("axis"));
        Assert.Equal("x", log.Rotate(Rotation.Clockwise180).GetProperty("axis"));
        Assert.Equal("z", log.Rotate(Rotation.Clockwise270).GetProperty("axis"));
        Assert.Equal("y", Minecraft.Blocks.OakLog.WithAxis(Axis.Y).Rotate(Rotation.Clockwise90).GetProperty("axis"));

        var straightRail = Minecraft.Blocks.Rail.WithProperty("shape", "north_south");
        Assert.Equal("east_west", straightRail.Rotate(Rotation.Clockwise90).GetProperty("shape"));
        Assert.Equal("north_south", straightRail.Rotate(Rotation.Clockwise180).GetProperty("shape"));

        var cornerRail = Minecraft.Blocks.Rail.WithProperty("shape", "north_east");
        Assert.Equal("south_east", cornerRail.Rotate(Rotation.Clockwise90).GetProperty("shape"));
        Assert.Equal("south_west", cornerRail.Rotate(Rotation.Clockwise180).GetProperty("shape"));
        Assert.Equal("north_west", cornerRail.Rotate(Rotation.Clockwise270).GetProperty("shape"));

        var ascendingRail = Minecraft.Blocks.PoweredRail.WithProperty("shape", "ascending_north");
        Assert.Equal("ascending_east", ascendingRail.Rotate(Rotation.Clockwise90).GetProperty("shape"));
        Assert.Equal("ascending_west", ascendingRail.Rotate(Rotation.Clockwise270).GetProperty("shape"));

        var sign = Minecraft.Blocks.OakSign.WithProperty("rotation", "2");
        Assert.Equal("6", sign.Rotate(Rotation.Clockwise90).GetProperty("rotation"));
        Assert.Equal("10", sign.Rotate(Rotation.Clockwise180).GetProperty("rotation"));
        Assert.Equal("14", sign.Rotate(Rotation.Clockwise270).GetProperty("rotation"));

        var wire = BlockState.Parse("minecraft:redstone_wire[east=none,north=side,south=up,west=none]");
        var rotatedWire = wire.Rotate(Rotation.Clockwise90);
        Assert.Equal("side", rotatedWire.GetProperty("east"));
        Assert.Equal("up", rotatedWire.GetProperty("west"));
        Assert.Equal("none", rotatedWire.GetProperty("north"));
        Assert.Equal("none", rotatedWire.GetProperty("south"));
    }

    [Fact]
    public void Schematic_Rotate_SwapsDimensionsTransformsCoordinatesAndFourStepRoundtripMatchesOriginal()
    {
        var schematic = SchematicBuilder.Create(3, 2, 5)
            .SetBlock(0, 0, 0, Minecraft.Blocks.Cobblestone)
            .SetBlock(2, 1, 0, Minecraft.Blocks.OakStairs.WithFacing(Direction.North))
            .SetChest(0, 0, 4, new ItemStack(Minecraft.Items.Diamond, count: 16, slot: 0))
            .Build();

        schematic.SetBlock(0, 0, 4, Minecraft.Blocks.Chest.WithFacing(Direction.South));

        var r90 = schematic.Rotate(Rotation.Clockwise90);
        Assert.Equal(5, r90.Width);
        Assert.Equal(2, r90.Height);
        Assert.Equal(3, r90.Length);

        // (x=0, z=0) -> (5 - 1 - 0, 0) = (4, 0)
        Assert.Equal(Minecraft.Blocks.Cobblestone, r90[4, 0, 0].Identifier);
        // (x=2, y=1, z=0) -> (4, 1, 2), facing North -> East
        Assert.Equal("east", r90[4, 1, 2].GetProperty("facing"));
        // (x=0, y=0, z=4) -> (5 - 1 - 4, 0, 0) = (0, 0, 0), facing South -> West
        Assert.Equal(Minecraft.Blocks.Chest, r90[0, 0, 0].Identifier);
        Assert.Equal("west", r90[0, 0, 0].GetProperty("facing"));
        Assert.Single(r90.GetItems(0, 0, 0));
        Assert.Equal(Minecraft.Items.Diamond, r90.GetItems(0, 0, 0)[0].Id);

        var r180 = schematic.Rotate(Rotation.Clockwise180);
        Assert.Equal(3, r180.Width);
        Assert.Equal(5, r180.Length);
        Assert.Equal(Minecraft.Blocks.Cobblestone, r180[2, 0, 4].Identifier);
        Assert.Equal("south", r180[0, 1, 4].GetProperty("facing"));
        Assert.Equal("north", r180[2, 0, 0].GetProperty("facing"));

        var r270 = schematic.Rotate(Rotation.Clockwise270);
        Assert.Equal(5, r270.Width);
        Assert.Equal(3, r270.Length);
        Assert.Equal(Minecraft.Blocks.Cobblestone, r270[0, 0, 2].Identifier);
        Assert.Equal("west", r270[0, 1, 0].GetProperty("facing"));
        Assert.Equal("east", r270[4, 0, 2].GetProperty("facing"));

        // Four 90-degree rotations return to the original schematic
        var r360 = r90.Rotate(Rotation.Clockwise90).Rotate(Rotation.Clockwise90).Rotate(Rotation.Clockwise90);
        Assert.Equal(schematic.Width, r360.Width);
        Assert.Equal(schematic.Height, r360.Height);
        Assert.Equal(schematic.Length, r360.Length);
        Assert.Equal(schematic[0, 0, 0], r360[0, 0, 0]);
        Assert.Equal(schematic[2, 1, 0], r360[2, 1, 0]);
        Assert.Equal(schematic[0, 0, 4], r360[0, 0, 4]);
        Assert.Equal(Minecraft.Items.Diamond, r360.GetItems(0, 0, 4)[0].Id);
    }

    [Fact]
    public void Schematic_Rotate_TransformsEntitiesWithZeroAndNonZeroOffset()
    {
        // 1. Without offset (local entity coordinates)
        var localSchem = SchematicBuilder.Create(3, 3, 5)
            .AddItemFrame(1.5, 1.5, 0.03125, Direction.North, new ItemStack(Minecraft.Items.Compass))
            .AddEntity(new Entity(Minecraft.Entities.ArmorStand, 0.5, 1.0, 1.5)
                .WithRotation(0f, 10f)
                .WithMotion(0.2, 0.0, -0.5))
            .Build();

        var localRot90 = localSchem.Rotate(Rotation.Clockwise90);
        Assert.Equal(2, localRot90.Entities.Count);

        var rotFrame = localRot90.Entities[0];
        Assert.Equal(5 - 0.03125, rotFrame.X, precision: 5);
        Assert.Equal(1.5, rotFrame.Y, precision: 5);
        Assert.Equal(1.5, rotFrame.Z, precision: 5);
        Assert.Equal(Direction.East, rotFrame.GetItemFrameData().Facing);

        var rotStand = localRot90.Entities[1];
        Assert.Equal(3.5, rotStand.X, precision: 5);
        Assert.Equal(1.0, rotStand.Y, precision: 5);
        Assert.Equal(0.5, rotStand.Z, precision: 5);
        Assert.Equal((90f, 10f), rotStand.GetRotation());
        var (mx, my, mz) = rotStand.GetMotion();
        Assert.Equal(0.5, mx, precision: 5);
        Assert.Equal(0.0, my, precision: 5);
        Assert.Equal(0.2, mz, precision: 5);

        // 2. With non-zero offset (block-coordinate AddEntity uses offset space)
        var offsetSchem = SchematicBuilder.Create(4, 4, 6)
            .WithOffset(-4, -1, 0)
            .AddEntity(1, 2, 1, new Entity(Minecraft.Entities.Allay))
            .Build();

        var offsetRot90 = offsetSchem.Rotate(Rotation.Clockwise90);
        Assert.Equal(new BlockPosition(-5, -1, -4), offsetRot90.Offset);
        var rotAllay = offsetRot90.Entities[0];
        // Unrotated local (1.5, 1.5) -> rotated local (6 - 1.5, 1.5) = (4.5, 1.5) + offset (-5, -4) = (-0.5, -2.5)
        Assert.Equal(-0.5, rotAllay.X, precision: 5);
        Assert.Equal(1.0, rotAllay.Y, precision: 5);
        Assert.Equal(-2.5, rotAllay.Z, precision: 5);
    }

    [Fact]
    public void SchematicBuilder_CreateAndWithRotation_RotatesOnBuild()
    {
        var schematicFromCreate = SchematicBuilder.Create(2, 2, 4, Rotation.Clockwise90)
            .SetBlock(0, 0, 0, Minecraft.Blocks.Chest.WithFacing(Direction.North))
            .Build();

        Assert.Equal(4, schematicFromCreate.Width);
        Assert.Equal(2, schematicFromCreate.Height);
        Assert.Equal(2, schematicFromCreate.Length);
        Assert.Equal(Minecraft.Blocks.Chest, schematicFromCreate[3, 0, 0].Identifier);
        Assert.Equal("east", schematicFromCreate[3, 0, 0].GetProperty("facing"));

        var schematicFromFluent = SchematicBuilder.Create(2, 2, 4)
            .WithRotation(Rotation.Clockwise270)
            .SetBlock(0, 0, 0, Minecraft.Blocks.Chest.WithFacing(Direction.North))
            .Build();

        Assert.Equal(4, schematicFromFluent.Width);
        Assert.Equal(2, schematicFromFluent.Length);
        Assert.Equal(Minecraft.Blocks.Chest, schematicFromFluent[0, 0, 1].Identifier);
        Assert.Equal("west", schematicFromFluent[0, 0, 1].GetProperty("facing"));

        Assert.Throws<ArgumentOutOfRangeException>(() => SchematicBuilder.Create(2, 2, 2, (Rotation)45));
        Assert.Throws<ArgumentOutOfRangeException>(() => SchematicBuilder.Create(2, 2, 2).WithRotation((Rotation)45));
    }

    [Fact]
    public void SchematicCreator_WithRotationBeforeCreate_ProducesRotatedSchematic()
    {
        var bambooCreator = new BambooFarmWithAllaysSchematicCreator()
            .WithRotation(Rotation.Clockwise90);

        var bambooRotated = bambooCreator.Build();
        Assert.Equal(34, bambooRotated.Width);
        Assert.Equal(12, bambooRotated.Height);
        Assert.Equal(35, bambooRotated.Length);
        Assert.Equal(BlockPosition.Zero, bambooRotated.Offset);
        Assert.Equal(Minecraft.Blocks.NoteBlock, bambooRotated[17, 9, 17].Identifier);
        Assert.Equal(4, bambooRotated.Entities.Count);
        Assert.Equal(32.5, bambooRotated.Entities[0].X, precision: 5);
        Assert.Equal(9.0, bambooRotated.Entities[0].Y, precision: 5);
        Assert.Equal(1.5, bambooRotated.Entities[0].Z, precision: 5);

        var flyingCreator = new FlatFylingMachineSchematicCreator
        {
            Rotation = Rotation.Clockwise90
        };
        var flyingRotated = flyingCreator.Build();
        Assert.Equal(2, flyingRotated.Width);
        Assert.Equal(2, flyingRotated.Height);
        Assert.Equal(20, flyingRotated.Length);

        var doorCreator = new CompactPistonDoorSchematicCreator
        {
            Size = PistonDoorSize.TwoByTwo,
            Facing = Direction.North,
            Rotation = Rotation.Clockwise90
        };
        var doorRotated = doorCreator.Build();
        Assert.Equal(3, doorRotated.Width);
        Assert.Equal(4, doorRotated.Height);
        Assert.Equal(6, doorRotated.Length);
        var rotatedTrigger = doorRotated[2, 1, 1];
        Assert.Equal(Minecraft.Blocks.Lever, rotatedTrigger.Identifier);
        Assert.Equal("east", rotatedTrigger.GetProperty("facing"));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new FlatFylingMachineSchematicCreator().WithRotation((Rotation)123));
    }
}
