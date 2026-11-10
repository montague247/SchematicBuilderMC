using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;
using SchematicBuilderMC.Extensions;
using SchematicBuilderMC.Generators;
using SchematicBuilderMC.IO;
using SchematicBuilderMC.Models;
using SchematicBuilderMC.Registries;
using Xunit;

namespace SchematicBuilderMC.Core.Tests;

public class AdditionalBlockStatesTests
{
    [Theory]
    [InlineData(1, "1")]
    [InlineData(2, "2")]
    [InlineData(3, "3")]
    [InlineData(4, "4")]
    public void Repeater_DelayAndLevel_ConfiguresCorrectProperty(int delay, string expected)
    {
        var stateFromDelay = Minecraft.Blocks.Repeater.WithDelay(delay);
        Assert.Equal(expected, stateFromDelay.GetProperty("delay"));

        var stateFromLevel = Minecraft.Blocks.Repeater.WithRepeaterLevel(delay);
        Assert.Equal(expected, stateFromLevel.GetProperty("delay"));
    }

    [Fact]
    public void Repeater_DelayClamping_LimitsBetween1And4()
    {
        var min = Minecraft.Blocks.Repeater.WithDelay(0);
        Assert.Equal("1", min.GetProperty("delay"));

        var max = Minecraft.Blocks.Repeater.WithDelay(10);
        Assert.Equal("4", max.GetProperty("delay"));
    }

    [Fact]
    public void Repeater_LockedAndFacing_WorksFluently()
    {
        var state = Minecraft.Blocks.Repeater
            .FacingNorth()
            .WithDelay(3)
            .WithLocked(true)
            .WithPowered(false);

        Assert.Equal("north", state.GetProperty("facing"));
        Assert.Equal("3", state.GetProperty("delay"));
        Assert.Equal("true", state.GetProperty("locked"));
        Assert.Equal("false", state.GetProperty("powered"));
        Assert.Equal("minecraft:repeater[delay=3,facing=north,locked=true,powered=false]", state.ToString());
    }

    [Fact]
    public void Comparator_SubtractionMode_ConfiguresCorrectly()
    {
        var subtractState = Minecraft.Blocks.Comparator.WithSubtractMode();
        Assert.Equal("subtract", subtractState.GetProperty("mode"));

        var subtractViaEnum = Minecraft.Blocks.Comparator.WithComparatorMode(ComparatorMode.Subtract);
        Assert.Equal("subtract", subtractViaEnum.GetProperty("mode"));

        var inSubtract = Minecraft.Blocks.Comparator.InSubtractMode();
        Assert.Equal("subtract", inSubtract.GetProperty("mode"));

        var compareState = Minecraft.Blocks.Comparator.InCompareMode();
        Assert.Equal("compare", compareState.GetProperty("mode"));

        var compareViaMethod = Minecraft.Blocks.Comparator.WithSubtractMode(false);
        Assert.Equal("compare", compareViaMethod.GetProperty("mode"));
    }

    [Fact]
    public void Lever_UpDownAndAttachmentFaces_ConfigureCorrectly()
    {
        // Lever on floor, switched down (active/powered)
        var floorDown = Minecraft.Blocks.Lever
            .OnFloor()
            .FacingEast()
            .LeverDown();

        Assert.Equal("floor", floorDown.GetProperty("face"));
        Assert.Equal("east", floorDown.GetProperty("facing"));
        Assert.Equal("true", floorDown.GetProperty("powered"));

        // Lever on ceiling, switched up (inactive/unpowered)
        var ceilingUp = Minecraft.Blocks.Lever
            .OnCeiling()
            .FacingNorth()
            .LeverUp();

        Assert.Equal("ceiling", ceilingUp.GetProperty("face"));
        Assert.Equal("north", ceilingUp.GetProperty("facing"));
        Assert.Equal("false", ceilingUp.GetProperty("powered"));

        // Lever on wall with enum
        var wallLever = Minecraft.Blocks.Lever
            .WithFace(BlockFace.Wall)
            .WithFacing(Direction.South)
            .WithPowered(true);

        Assert.Equal("wall", wallLever.GetProperty("face"));
        Assert.Equal("south", wallLever.GetProperty("facing"));
        Assert.Equal("true", wallLever.GetProperty("powered"));

        // WithLeverState combined helper
        var combined = Minecraft.Blocks.Lever.WithLeverState(powered: true, face: "ceiling", facing: "west");
        Assert.Equal("true", combined.GetProperty("powered"));
        Assert.Equal("ceiling", combined.GetProperty("face"));
        Assert.Equal("west", combined.GetProperty("facing"));
    }

    [Fact]
    public void RedstoneWire_Power_ClampsBetween0And15()
    {
        var normal = Minecraft.Blocks.RedstoneWire.WithPower(15);
        Assert.Equal("15", normal.GetProperty("power"));

        var underflow = Minecraft.Blocks.RedstoneWire.WithPower(-5);
        Assert.Equal("0", underflow.GetProperty("power"));

        var overflow = Minecraft.Blocks.RedstoneWire.WithPower(20);
        Assert.Equal("15", overflow.GetProperty("power"));
    }

    [Fact]
    public void DoorsTrapdoorsPistons_AdditionalStates_ConfigureCorrectly()
    {
        var door = Minecraft.Blocks.OakDoor
            .WithFacing("east")
            .WithHinge(DoorHinge.Right)
            .WithOpen(true)
            .BottomHalf()
            .WithPowered(false);

        Assert.Equal("east", door.GetProperty("facing"));
        Assert.Equal("right", door.GetProperty("hinge"));
        Assert.Equal("true", door.GetProperty("open"));
        Assert.Equal("bottom", door.GetProperty("half"));
        Assert.Equal("false", door.GetProperty("powered"));

        var piston = Minecraft.Blocks.StickyPiston
            .WithFacing(Direction.Up)
            .WithExtended(true);

        Assert.Equal("up", piston.GetProperty("facing"));
        Assert.Equal("true", piston.GetProperty("extended"));

        var stairs = Minecraft.Blocks.OakStairs
            .WithFacing(Direction.North)
            .WithHalf(BlockHalf.Top)
            .WithStairsShape(StairsShape.InnerLeft)
            .WithWaterlogged(true);

        Assert.Equal("north", stairs.GetProperty("facing"));
        Assert.Equal("top", stairs.GetProperty("half"));
        Assert.Equal("inner_left", stairs.GetProperty("shape"));
        Assert.Equal("true", stairs.GetProperty("waterlogged"));

        var log = Minecraft.Blocks.OakLog.WithAxis(Axis.Y);
        Assert.Equal("y", log.GetProperty("axis"));
    }

    [Fact]
    public void SchematicBuilder_DedicatedRedstoneMethods_WorkSeamlessly()
    {
        var schematic = SchematicBuilder.Create(5, 5, 5)
            .SetRepeater(0, 1, 0, facing: "north", delay: 3, locked: true, powered: true)
            .SetComparator(1, 1, 0, facing: "south", subtractMode: true, powered: true)
            .SetLever(2, 1, 0, face: "ceiling", facing: "west", powered: true)
            .SetRedstoneWire(3, 1, 0, power: 14)
            .SetPiston(4, 1, 0, facing: "up", sticky: true, extended: true)
            .Build();

        // Verify Repeater
        var rep = schematic[0, 1, 0];
        Assert.Equal("minecraft:repeater", rep.Identifier);
        Assert.Equal("north", rep.GetProperty("facing"));
        Assert.Equal("3", rep.GetProperty("delay"));
        Assert.Equal("true", rep.GetProperty("locked"));
        Assert.Equal("true", rep.GetProperty("powered"));

        // Verify Comparator
        var comp = schematic[1, 1, 0];
        Assert.Equal("minecraft:comparator", comp.Identifier);
        Assert.Equal("south", comp.GetProperty("facing"));
        Assert.Equal("subtract", comp.GetProperty("mode"));
        Assert.Equal("true", comp.GetProperty("powered"));

        // Verify Lever
        var lev = schematic[2, 1, 0];
        Assert.Equal("minecraft:lever", lev.Identifier);
        Assert.Equal("ceiling", lev.GetProperty("face"));
        Assert.Equal("west", lev.GetProperty("facing"));
        Assert.Equal("true", lev.GetProperty("powered"));

        // Verify Redstone wire
        var wire = schematic[3, 1, 0];
        Assert.Equal("minecraft:redstone_wire", wire.Identifier);
        Assert.Equal("14", wire.GetProperty("power"));

        // Verify Piston
        var pis = schematic[4, 1, 0];
        Assert.Equal("minecraft:sticky_piston", pis.Identifier);
        Assert.Equal("up", pis.GetProperty("facing"));
        Assert.Equal("true", pis.GetProperty("extended"));
    }

    [Fact]
    public void SchematicBuilder_EnumOverloads_WorkSeamlessly()
    {
        var schematic = SchematicBuilder.Create(5, 5, 5)
            .SetRepeater(0, 1, 0, Direction.North, delay: 3, locked: true, powered: true)
            .SetComparator(1, 1, 0, Direction.South, subtractMode: true, powered: true)
            .SetLever(2, 1, 0, BlockFace.Ceiling, Direction.West, powered: true)
            .SetPiston(3, 1, 0, Direction.Up, sticky: true, extended: true)
            .Build();

        // Verify Repeater
        var rep = schematic[0, 1, 0];
        Assert.Equal("minecraft:repeater", rep.Identifier);
        Assert.Equal("north", rep.GetProperty("facing"));
        Assert.Equal("3", rep.GetProperty("delay"));
        Assert.Equal("true", rep.GetProperty("locked"));
        Assert.Equal("true", rep.GetProperty("powered"));

        // Verify Comparator
        var comp = schematic[1, 1, 0];
        Assert.Equal("minecraft:comparator", comp.Identifier);
        Assert.Equal("south", comp.GetProperty("facing"));
        Assert.Equal("subtract", comp.GetProperty("mode"));
        Assert.Equal("true", comp.GetProperty("powered"));

        // Verify Lever
        var lev = schematic[2, 1, 0];
        Assert.Equal("minecraft:lever", lev.Identifier);
        Assert.Equal("ceiling", lev.GetProperty("face"));
        Assert.Equal("west", lev.GetProperty("facing"));
        Assert.Equal("true", lev.GetProperty("powered"));

        // Verify Piston
        var pis = schematic[3, 1, 0];
        Assert.Equal("minecraft:sticky_piston", pis.Identifier);
        Assert.Equal("up", pis.GetProperty("facing"));
        Assert.Equal("true", pis.GetProperty("extended"));
    }

    [Fact]
    public void Schematic_EnumOverloads_WorkSeamlessly()
    {
        var schematic = new Schematic(5, 5, 5);
        schematic.SetRepeater(0, 1, 0, Direction.East, delay: 2);
        schematic.SetComparator(1, 1, 0, Direction.West, subtractMode: true);
        schematic.SetLever(2, 1, 0, BlockFace.Floor, Direction.North, powered: true);
        schematic.SetPiston(3, 1, 0, Direction.Down, sticky: false, extended: true);

        Assert.Equal("east", schematic[0, 1, 0].GetProperty("facing"));
        Assert.Equal("2", schematic[0, 1, 0].GetProperty("delay"));

        Assert.Equal("west", schematic[1, 1, 0].GetProperty("facing"));
        Assert.Equal("subtract", schematic[1, 1, 0].GetProperty("mode"));

        Assert.Equal("floor", schematic[2, 1, 0].GetProperty("face"));
        Assert.Equal("north", schematic[2, 1, 0].GetProperty("facing"));
        Assert.Equal("true", schematic[2, 1, 0].GetProperty("powered"));

        Assert.Equal("down", schematic[3, 1, 0].GetProperty("facing"));
        Assert.Equal("true", schematic[3, 1, 0].GetProperty("extended"));
        Assert.Equal("minecraft:piston", schematic[3, 1, 0].Identifier);
    }

    [Fact]
    public void FluentBuilderCodeGenerator_GeneratesSemanticCallsForRepeaterComparatorLever()
    {
        var schematic = SchematicBuilder.Create(4, 2, 2)
            .SetBlock(0, 0, 0, Minecraft.Blocks.Repeater.WithFacing("north").WithDelay(4))
            .SetBlock(1, 0, 0, Minecraft.Blocks.Comparator.WithFacing("south").WithSubtractMode())
            .SetBlock(2, 0, 0, Minecraft.Blocks.Lever.WithFace("floor").WithFacing("east").WithPowered(true))
            .SetBlock(3, 0, 0, Minecraft.Blocks.RedstoneWire.WithPower(15))
            .Build();

        string code = FluentBuilderCodeGenerator.Generate(schematic);

        Assert.Contains("Minecraft.Blocks.Repeater.WithDelay(4).WithFacing(\"north\")", code);
        Assert.Contains("Minecraft.Blocks.Comparator.WithFacing(\"south\").WithSubtractMode()", code);
        Assert.Contains("Minecraft.Blocks.Lever.WithFace(\"floor\").WithFacing(\"east\").WithPowered(true)", code);
        Assert.Contains("Minecraft.Blocks.RedstoneWire.WithPower(15)", code);
    }

    [Fact]
    public void SpongeSchematic_Roundtrip_PreservesAllAdditionalBlockStates()
    {
        var original = SchematicBuilder.Create(5, 3, 5)
            .SetRepeater(0, 1, 0, facing: "north", delay: 2, locked: false, powered: true)
            .SetComparator(1, 1, 0, facing: "east", subtractMode: true, powered: false)
            .SetLever(2, 1, 0, face: "wall", facing: "south", powered: true)
            .SetRedstoneWire(3, 1, 0, power: 12)
            .SetPiston(4, 1, 0, facing: "down", sticky: true, extended: false)
            .Build();

        byte[] bytes = SchematicFile.WriteToBytes(original, SchematicFormat.SpongeV2);
        var loaded = SchematicFile.Read(bytes, SchematicFormat.SpongeV2);

        var rep = loaded[0, 1, 0];
        Assert.Equal("2", rep.GetProperty("delay"));
        Assert.Equal("north", rep.GetProperty("facing"));
        Assert.Equal("true", rep.GetProperty("powered"));

        var comp = loaded[1, 1, 0];
        Assert.Equal("subtract", comp.GetProperty("mode"));
        Assert.Equal("east", comp.GetProperty("facing"));

        var lev = loaded[2, 1, 0];
        Assert.Equal("wall", lev.GetProperty("face"));
        Assert.Equal("south", lev.GetProperty("facing"));
        Assert.Equal("true", lev.GetProperty("powered"));

        var wire = loaded[3, 1, 0];
        Assert.Equal("12", wire.GetProperty("power"));

        var pis = loaded[4, 1, 0];
        Assert.Equal("down", pis.GetProperty("facing"));
        Assert.Equal("minecraft:sticky_piston", pis.Identifier);
    }
}
