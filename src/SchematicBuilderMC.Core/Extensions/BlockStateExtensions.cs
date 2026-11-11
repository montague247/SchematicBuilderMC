using SchematicBuilderMC.Enums;
using SchematicBuilderMC.Models;

namespace SchematicBuilderMC.Extensions;

/// <summary>
/// Extension methods for convenient and strongly-typed block property configuration on block identifiers and <see cref="BlockState"/> instances.
/// </summary>
public static class BlockStateExtensions
{
    /// <summary>
    /// Creates a new <see cref="BlockState"/> from the block identifier with the specified property set.
    /// </summary>
    public static BlockState WithProperty(this string identifier, string key, string value) =>
        new BlockState(identifier).WithProperty(key, value);

    /// <summary>
    /// Converts a block identifier string to a <see cref="BlockState"/>.
    /// </summary>
    public static BlockState ToBlockState(this string identifier) =>
        BlockState.Parse(identifier);

    #region Repeater Level & Delay (delay, locked)

    /// <summary>
    /// Sets the repeater delay ticks (1 to 4).
    /// </summary>
    public static BlockState WithDelay(this BlockState state, int delay) =>
        state.WithProperty("delay", Math.Clamp(delay, 1, 4).ToString());

    /// <summary>
    /// Sets the repeater delay ticks (1 to 4).
    /// </summary>
    public static BlockState WithDelay(this string identifier, int delay) =>
        new BlockState(identifier).WithDelay(delay);

    /// <summary>
    /// Sets the repeater level/delay (1 to 4). Alias for <see cref="WithDelay(BlockState, int)"/>.
    /// </summary>
    public static BlockState WithRepeaterLevel(this BlockState state, int level) =>
        state.WithDelay(level);

    /// <summary>
    /// Sets the repeater level/delay (1 to 4). Alias for <see cref="WithDelay(string, int)"/>.
    /// </summary>
    public static BlockState WithRepeaterLevel(this string identifier, int level) =>
        new BlockState(identifier).WithDelay(level);

    /// <summary>
    /// Sets the locked property of a repeater or hopper.
    /// </summary>
    public static BlockState WithLocked(this BlockState state, bool locked = true) =>
        state.WithProperty("locked", locked ? "true" : "false");

    /// <summary>
    /// Sets the locked property of a repeater or hopper.
    /// </summary>
    public static BlockState WithLocked(this string identifier, bool locked = true) =>
        new BlockState(identifier).WithLocked(locked);

    #endregion

    #region Comparator Mode (mode)

    /// <summary>
    /// Configures whether a redstone comparator is in subtraction mode ("subtract") or comparison mode ("compare").
    /// </summary>
    public static BlockState WithSubtractMode(this BlockState state, bool subtract = true) =>
        state.WithProperty("mode", subtract ? "subtract" : "compare");

    /// <summary>
    /// Configures whether a redstone comparator is in subtraction mode ("subtract") or comparison mode ("compare").
    /// </summary>
    public static BlockState WithSubtractMode(this string identifier, bool subtract = true) =>
        new BlockState(identifier).WithSubtractMode(subtract);

    /// <summary>
    /// Sets a redstone comparator into subtraction mode.
    /// </summary>
    public static BlockState InSubtractMode(this BlockState state) =>
        state.WithSubtractMode(true);

    /// <summary>
    /// Sets a redstone comparator into subtraction mode.
    /// </summary>
    public static BlockState InSubtractMode(this string identifier) =>
        new BlockState(identifier).InSubtractMode();

    /// <summary>
    /// Sets a redstone comparator into comparison mode.
    /// </summary>
    public static BlockState InCompareMode(this BlockState state) =>
        state.WithSubtractMode(false);

    /// <summary>
    /// Sets a redstone comparator into comparison mode.
    /// </summary>
    public static BlockState InCompareMode(this string identifier) =>
        new BlockState(identifier).InCompareMode();

    /// <summary>
    /// Sets the comparator operating mode using <see cref="ComparatorMode"/>.
    /// </summary>
    public static BlockState WithComparatorMode(this BlockState state, ComparatorMode mode) =>
        state.WithProperty("mode", mode == ComparatorMode.Subtract ? "subtract" : "compare");

    /// <summary>
    /// Sets the comparator operating mode using <see cref="ComparatorMode"/>.
    /// </summary>
    public static BlockState WithComparatorMode(this string identifier, ComparatorMode mode) =>
        new BlockState(identifier).WithComparatorMode(mode);

    /// <summary>
    /// Sets the mode property on a block state (e.g. comparator "compare"/"subtract", structure block "save"/"load").
    /// </summary>
    public static BlockState WithMode(this BlockState state, string mode) =>
        state.WithProperty("mode", mode.ToLowerInvariant());

    /// <summary>
    /// Sets the mode property on a block state.
    /// </summary>
    public static BlockState WithMode(this string identifier, string mode) =>
        new BlockState(identifier).WithMode(mode);

    #endregion

    #region Lever Up / Down & Attachment Face (face, powered)

    /// <summary>
    /// Sets the attachment face of a switch/lever/button ("floor", "wall", "ceiling").
    /// </summary>
    public static BlockState WithFace(this BlockState state, string face) =>
        state.WithProperty("face", face.ToLowerInvariant());

    /// <summary>
    /// Sets the attachment face of a switch/lever/button.
    /// </summary>
    public static BlockState WithFace(this string identifier, string face) =>
        new BlockState(identifier).WithFace(face);

    /// <summary>
    /// Sets the attachment face using <see cref="BlockFace"/>.
    /// </summary>
    public static BlockState WithFace(this BlockState state, BlockFace face) =>
        state.WithFace(face.ToString());

    /// <summary>
    /// Sets the attachment face using <see cref="BlockFace"/>.
    /// </summary>
    public static BlockState WithFace(this string identifier, BlockFace face) =>
        new BlockState(identifier).WithFace(face);

    /// <summary>
    /// Places the lever or button on the floor (ground).
    /// </summary>
    public static BlockState OnFloor(this BlockState state) =>
        state.WithFace("floor");

    /// <summary>
    /// Places the lever or button on the floor (ground).
    /// </summary>
    public static BlockState OnFloor(this string identifier) =>
        new BlockState(identifier).OnFloor();

    /// <summary>
    /// Places the lever or button on the ceiling (hanging).
    /// </summary>
    public static BlockState OnCeiling(this BlockState state) =>
        state.WithFace("ceiling");

    /// <summary>
    /// Places the lever or button on the ceiling (hanging).
    /// </summary>
    public static BlockState OnCeiling(this string identifier) =>
        new BlockState(identifier).OnCeiling();

    /// <summary>
    /// Places the lever or button on a wall.
    /// </summary>
    public static BlockState OnWall(this BlockState state) =>
        state.WithFace("wall");

    /// <summary>
    /// Places the lever or button on a wall.
    /// </summary>
    public static BlockState OnWall(this string identifier) =>
        new BlockState(identifier).OnWall();

    /// <summary>
    /// Sets the powered state of a redstone component, lever, button, door, or pressure plate.
    /// </summary>
    public static BlockState WithPowered(this BlockState state, bool powered = true) =>
        state.WithProperty("powered", powered ? "true" : "false");

    /// <summary>
    /// Sets the powered state of a redstone component.
    /// </summary>
    public static BlockState WithPowered(this string identifier, bool powered = true) =>
        new BlockState(identifier).WithPowered(powered);

    /// <summary>
    /// Sets the block state to powered (true).
    /// </summary>
    public static BlockState Powered(this BlockState state) =>
        state.WithPowered(true);

    /// <summary>
    /// Sets the block state to powered (true).
    /// </summary>
    public static BlockState Powered(this string identifier) =>
        new BlockState(identifier).Powered();

    /// <summary>
    /// Sets the block state to unpowered (false).
    /// </summary>
    public static BlockState Unpowered(this BlockState state) =>
        state.WithPowered(false);

    /// <summary>
    /// Sets the block state to unpowered (false).
    /// </summary>
    public static BlockState Unpowered(this string identifier) =>
        new BlockState(identifier).Unpowered();

    /// <summary>
    /// Flips a lever down (powered = true).
    /// </summary>
    public static BlockState LeverDown(this BlockState state) =>
        state.WithPowered(true);

    /// <summary>
    /// Flips a lever down (powered = true).
    /// </summary>
    public static BlockState LeverDown(this string identifier) =>
        new BlockState(identifier).LeverDown();

    /// <summary>
    /// Flips a lever up (powered = false).
    /// </summary>
    public static BlockState LeverUp(this BlockState state) =>
        state.WithPowered(false);

    /// <summary>
    /// Flips a lever up (powered = false).
    /// </summary>
    public static BlockState LeverUp(this string identifier) =>
        new BlockState(identifier).LeverUp();

    /// <summary>
    /// Configures complete lever state including powered, attachment face, and facing direction.
    /// </summary>
    public static BlockState WithLeverState(this BlockState state, bool powered, string face = "floor", string facing = "north") =>
        state.WithPowered(powered).WithFace(face).WithFacing(facing);

    /// <summary>
    /// Configures complete lever state including powered, attachment face, and facing direction.
    /// </summary>
    public static BlockState WithLeverState(this string identifier, bool powered, string face = "floor", string facing = "north") =>
        new BlockState(identifier).WithLeverState(powered, face, facing);

    #endregion

    #region Facing & Direction (facing)

    /// <summary>
    /// Sets the facing direction ("north", "south", "east", "west", "up", "down").
    /// </summary>
    public static BlockState WithFacing(this BlockState state, string facing) =>
        state.WithProperty("facing", facing.ToLowerInvariant());

    /// <summary>
    /// Sets the facing direction.
    /// </summary>
    public static BlockState WithFacing(this string identifier, string facing) =>
        new BlockState(identifier).WithFacing(facing);

    /// <summary>
    /// Sets the facing direction using <see cref="Direction"/>.
    /// </summary>
    public static BlockState WithFacing(this BlockState state, Direction direction) =>
        state.WithFacing(direction.ToString());

    /// <summary>
    /// Sets the facing direction using <see cref="Direction"/>.
    /// </summary>
    public static BlockState WithFacing(this string identifier, Direction direction) =>
        new BlockState(identifier).WithFacing(direction);

    public static BlockState FacingNorth(this BlockState state) => state.WithFacing("north");
    public static BlockState FacingNorth(this string identifier) => new BlockState(identifier).FacingNorth();

    public static BlockState FacingSouth(this BlockState state) => state.WithFacing("south");
    public static BlockState FacingSouth(this string identifier) => new BlockState(identifier).FacingSouth();

    public static BlockState FacingEast(this BlockState state) => state.WithFacing("east");
    public static BlockState FacingEast(this string identifier) => new BlockState(identifier).FacingEast();

    public static BlockState FacingWest(this BlockState state) => state.WithFacing("west");
    public static BlockState FacingWest(this string identifier) => new BlockState(identifier).FacingWest();

    public static BlockState FacingUp(this BlockState state) => state.WithFacing("up");
    public static BlockState FacingUp(this string identifier) => new BlockState(identifier).FacingUp();

    public static BlockState FacingDown(this BlockState state) => state.WithFacing("down");
    public static BlockState FacingDown(this string identifier) => new BlockState(identifier).FacingDown();

    #endregion

    #region Redstone Power (power)

    /// <summary>
    /// Sets the redstone power signal strength (0 to 15).
    /// </summary>
    public static BlockState WithPower(this BlockState state, int power) =>
        state.WithProperty("power", Math.Clamp(power, 0, 15).ToString());

    /// <summary>
    /// Sets the redstone power signal strength (0 to 15).
    /// </summary>
    public static BlockState WithPower(this string identifier, int power) =>
        new BlockState(identifier).WithPower(power);

    #endregion

    #region Open / Closed (open)

    /// <summary>
    /// Sets the open state for doors, trapdoors, fence gates, or barrels.
    /// </summary>
    public static BlockState WithOpen(this BlockState state, bool open = true) =>
        state.WithProperty("open", open ? "true" : "false");

    /// <summary>
    /// Sets the open state for doors, trapdoors, fence gates, or barrels.
    /// </summary>
    public static BlockState WithOpen(this string identifier, bool open = true) =>
        new BlockState(identifier).WithOpen(open);

    public static BlockState Open(this BlockState state) => state.WithOpen(true);
    public static BlockState Open(this string identifier) => new BlockState(identifier).Open();

    public static BlockState Closed(this BlockState state) => state.WithOpen(false);
    public static BlockState Closed(this string identifier) => new BlockState(identifier).Closed();

    #endregion

    #region Half (half)

    /// <summary>
    /// Sets the half property ("top", "bottom", "upper", "lower").
    /// </summary>
    public static BlockState WithHalf(this BlockState state, string half) =>
        state.WithProperty("half", half.ToLowerInvariant());

    /// <summary>
    /// Sets the half property.
    /// </summary>
    public static BlockState WithHalf(this string identifier, string half) =>
        new BlockState(identifier).WithHalf(half);

    /// <summary>
    /// Sets the half property using <see cref="BlockHalf"/>.
    /// </summary>
    public static BlockState WithHalf(this BlockState state, BlockHalf half) =>
        state.WithHalf(half.ToString());

    /// <summary>
    /// Sets the half property using <see cref="BlockHalf"/>.
    /// </summary>
    public static BlockState WithHalf(this string identifier, BlockHalf half) =>
        new BlockState(identifier).WithHalf(half);

    public static BlockState TopHalf(this BlockState state) => state.WithHalf("top");
    public static BlockState TopHalf(this string identifier) => new BlockState(identifier).TopHalf();

    public static BlockState BottomHalf(this BlockState state) => state.WithHalf("bottom");
    public static BlockState BottomHalf(this string identifier) => new BlockState(identifier).BottomHalf();

    public static BlockState UpperHalf(this BlockState state) => state.WithHalf("upper");
    public static BlockState UpperHalf(this string identifier) => new BlockState(identifier).UpperHalf();

    public static BlockState LowerHalf(this BlockState state) => state.WithHalf("lower");
    public static BlockState LowerHalf(this string identifier) => new BlockState(identifier).LowerHalf();

    #endregion

    #region Waterlogged (waterlogged)

    /// <summary>
    /// Sets whether the block is waterlogged.
    /// </summary>
    public static BlockState WithWaterlogged(this BlockState state, bool waterlogged = true) =>
        state.WithProperty("waterlogged", waterlogged ? "true" : "false");

    /// <summary>
    /// Sets whether the block is waterlogged.
    /// </summary>
    public static BlockState WithWaterlogged(this string identifier, bool waterlogged = true) =>
        new BlockState(identifier).WithWaterlogged(waterlogged);

    #endregion

    #region Lit (lit)

    /// <summary>
    /// Sets whether the block is lit (e.g. campfires, furnaces, redstone ore, candles).
    /// </summary>
    public static BlockState WithLit(this BlockState state, bool lit = true) =>
        state.WithProperty("lit", lit ? "true" : "false");

    /// <summary>
    /// Sets whether the block is lit.
    /// </summary>
    public static BlockState WithLit(this string identifier, bool lit = true) =>
        new BlockState(identifier).WithLit(lit);

    #endregion

    #region Axis (axis)

    /// <summary>
    /// Sets the orientation axis ("x", "y", "z") for logs, pillars, and chains.
    /// </summary>
    public static BlockState WithAxis(this BlockState state, string axis) =>
        state.WithProperty("axis", axis.ToLowerInvariant());

    /// <summary>
    /// Sets the orientation axis for logs, pillars, and chains.
    /// </summary>
    public static BlockState WithAxis(this string identifier, string axis) =>
        new BlockState(identifier).WithAxis(axis);

    /// <summary>
    /// Sets the orientation axis using <see cref="Axis"/>.
    /// </summary>
    public static BlockState WithAxis(this BlockState state, Axis axis) =>
        state.WithAxis(axis.ToString());

    /// <summary>
    /// Sets the orientation axis using <see cref="Axis"/>.
    /// </summary>
    public static BlockState WithAxis(this string identifier, Axis axis) =>
        new BlockState(identifier).WithAxis(axis);

    #endregion

    #region Extended & InWall (extended, in_wall)

    /// <summary>
    /// Sets whether a piston is extended.
    /// </summary>
    public static BlockState WithExtended(this BlockState state, bool extended = true) =>
        state.WithProperty("extended", extended ? "true" : "false");

    /// <summary>
    /// Sets whether a piston is extended.
    /// </summary>
    public static BlockState WithExtended(this string identifier, bool extended = true) =>
        new BlockState(identifier).WithExtended(extended);

    /// <summary>
    /// Sets whether a fence gate is attached to a wall (lowers gate height).
    /// </summary>
    public static BlockState WithInWall(this BlockState state, bool inWall = true) =>
        state.WithProperty("in_wall", inWall ? "true" : "false");

    /// <summary>
    /// Sets whether a fence gate is attached to a wall.
    /// </summary>
    public static BlockState WithInWall(this string identifier, bool inWall = true) =>
        new BlockState(identifier).WithInWall(inWall);

    #endregion

    #region Door Hinge (hinge)

    /// <summary>
    /// Sets the hinge side for doors ("left", "right").
    /// </summary>
    public static BlockState WithHinge(this BlockState state, string hinge) =>
        state.WithProperty("hinge", hinge.ToLowerInvariant());

    /// <summary>
    /// Sets the hinge side for doors.
    /// </summary>
    public static BlockState WithHinge(this string identifier, string hinge) =>
        new BlockState(identifier).WithHinge(hinge);

    /// <summary>
    /// Sets the hinge side for doors using <see cref="DoorHinge"/>.
    /// </summary>
    public static BlockState WithHinge(this BlockState state, DoorHinge hinge) =>
        state.WithHinge(hinge.ToString());

    /// <summary>
    /// Sets the hinge side for doors using <see cref="DoorHinge"/>.
    /// </summary>
    public static BlockState WithHinge(this string identifier, DoorHinge hinge) =>
        new BlockState(identifier).WithHinge(hinge);

    #endregion

    #region Level (level)

    /// <summary>
    /// Sets the level property (fluids, cauldrons, composters, light blocks).
    /// </summary>
    public static BlockState WithLevel(this BlockState state, int level) =>
        state.WithProperty("level", level.ToString());

    /// <summary>
    /// Sets the level property.
    /// </summary>
    public static BlockState WithLevel(this string identifier, int level) =>
        new BlockState(identifier).WithLevel(level);

    #endregion

    #region Shape & Type (shape, type)

    /// <summary>
    /// Sets the shape property for stairs or rails.
    /// </summary>
    public static BlockState WithShape(this BlockState state, string shape) =>
        state.WithProperty("shape", shape.ToLowerInvariant());

    /// <summary>
    /// Sets the shape property for stairs or rails.
    /// </summary>
    public static BlockState WithShape(this string identifier, string shape) =>
        new BlockState(identifier).WithShape(shape);

    /// <summary>
    /// Sets the stairs shape using <see cref="StairsShape"/>.
    /// </summary>
    public static BlockState WithStairsShape(this BlockState state, StairsShape shape) =>
        state.WithShape(shape switch
        {
            StairsShape.InnerLeft => "inner_left",
            StairsShape.InnerRight => "inner_right",
            StairsShape.OuterLeft => "outer_left",
            StairsShape.OuterRight => "outer_right",
            _ => "straight"
        });

    /// <summary>
    /// Sets the stairs shape using <see cref="StairsShape"/>.
    /// </summary>
    public static BlockState WithStairsShape(this string identifier, StairsShape shape) =>
        new BlockState(identifier).WithStairsShape(shape);

    /// <summary>
    /// Sets the type property (e.g. slab "top"/"bottom"/"double", chest "single"/"left"/"right").
    /// </summary>
    public static BlockState WithType(this BlockState state, string type) =>
        state.WithProperty("type", type.ToLowerInvariant());

    /// <summary>
    /// Sets the type property.
    /// </summary>
    public static BlockState WithType(this string identifier, string type) =>
        new BlockState(identifier).WithType(type);

    /// <summary>
    /// Sets slab type using <see cref="SlabType"/>.
    /// </summary>
    public static BlockState WithSlabType(this BlockState state, SlabType type) =>
        state.WithType(type.ToString());

    /// <summary>
    /// Sets slab type using <see cref="SlabType"/>.
    /// </summary>
    public static BlockState WithSlabType(this string identifier, SlabType type) =>
        new BlockState(identifier).WithSlabType(type);

    /// <summary>
    /// Sets slab type using a string ("top", "bottom", "double").
    /// </summary>
    public static BlockState WithSlabType(this BlockState state, string type) =>
        state.WithType(type);

    /// <summary>
    /// Sets slab type using a string ("top", "bottom", "double").
    /// </summary>
    public static BlockState WithSlabType(this string identifier, string type) =>
        new BlockState(identifier).WithSlabType(type);

    /// <summary>
    /// Sets chest connection type using <see cref="ChestType"/>.
    /// </summary>
    public static BlockState WithChestType(this BlockState state, ChestType type) =>
        state.WithType(type.ToString());

    /// <summary>
    /// Sets chest connection type using <see cref="ChestType"/>.
    /// </summary>
    public static BlockState WithChestType(this string identifier, ChestType type) =>
        new BlockState(identifier).WithChestType(type);

    /// <summary>
    /// Sets chest connection type using a string ("single", "left", "right").
    /// </summary>
    public static BlockState WithChestType(this BlockState state, string type) =>
        state.WithType(type);

    /// <summary>
    /// Sets chest connection type using a string ("single", "left", "right").
    /// </summary>
    public static BlockState WithChestType(this string identifier, string type) =>
        new BlockState(identifier).WithChestType(type);

    #endregion

    #region Enum String Converters

    /// <summary>
    /// Converts a <see cref="Direction"/> enum to its lowercase Minecraft state string.
    /// </summary>
    public static string ToStateString(this Direction direction) => direction.ToString().ToLowerInvariant();

    /// <summary>
    /// Converts a <see cref="BlockFace"/> enum to its lowercase Minecraft state string.
    /// </summary>
    public static string ToStateString(this BlockFace face) => face.ToString().ToLowerInvariant();

    /// <summary>
    /// Converts a <see cref="ComparatorMode"/> enum to its lowercase Minecraft state string.
    /// </summary>
    public static string ToStateString(this ComparatorMode mode) => mode.ToString().ToLowerInvariant();

    /// <summary>
    /// Converts a <see cref="BlockHalf"/> enum to its lowercase Minecraft state string.
    /// </summary>
    public static string ToStateString(this BlockHalf half) => half.ToString().ToLowerInvariant();

    /// <summary>
    /// Converts a <see cref="DoorHinge"/> enum to its lowercase Minecraft state string.
    /// </summary>
    public static string ToStateString(this DoorHinge hinge) => hinge.ToString().ToLowerInvariant();

    /// <summary>
    /// Converts a <see cref="SlabType"/> enum to its lowercase Minecraft state string.
    /// </summary>
    public static string ToStateString(this SlabType slabType) => slabType.ToString().ToLowerInvariant();

    /// <summary>
    /// Converts a <see cref="ChestType"/> enum to its lowercase Minecraft state string.
    /// </summary>
    public static string ToStateString(this ChestType chestType) => chestType.ToString().ToLowerInvariant();

    /// <summary>
    /// Converts a <see cref="StairsShape"/> enum to its lowercase Minecraft state string.
    /// </summary>
    public static string ToStateString(this StairsShape shape) => shape switch
    {
        StairsShape.InnerLeft => "inner_left",
        StairsShape.InnerRight => "inner_right",
        StairsShape.OuterLeft => "outer_left",
        StairsShape.OuterRight => "outer_right",
        _ => "straight"
    };

    /// <summary>
    /// Converts an <see cref="Axis"/> enum to its lowercase Minecraft state string.
    /// </summary>
    public static string ToStateString(this Axis axis) => axis.ToString().ToLowerInvariant();

    #endregion
}
