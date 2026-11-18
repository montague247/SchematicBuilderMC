using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;

namespace SchematicBuilderMC.Schematics.Redstone.Clocks;

/// <summary>
/// Fluent <see cref="SchematicBuilder"/> extensions for placing configurable Redstone clocks.
/// </summary>
public static class ClockExtensions
{
    public static SchematicBuilder AddClock(
        this SchematicBuilder builder,
        int x,
        int y,
        int z,
        ClockType clockType = ClockType.Repeater,
        TimeSpan? period = null,
        Direction facing = Direction.East,
        bool includeToggleLever = false,
        bool includeOutputLamp = false,
        Rotation rotation = Rotation.None)
    {
        return new ClockSchematicCreator
        {
            ClockType = clockType,
            Period = period,
            Facing = facing,
            IncludeToggleLever = includeToggleLever,
            IncludeOutputLamp = includeOutputLamp
        }.WithRotation(rotation).Place(builder, x, y, z);
    }

    public static SchematicBuilder AddRepeaterClock(this SchematicBuilder builder, int x, int y, int z, TimeSpan? period = null, Direction facing = Direction.East, bool includeToggleLever = false, bool includeOutputLamp = false, Rotation rotation = Rotation.None) =>
        builder.AddClock(x, y, z, ClockType.Repeater, period, facing, includeToggleLever, includeOutputLamp, rotation);

    public static SchematicBuilder AddTorchClock(this SchematicBuilder builder, int x, int y, int z, TimeSpan? period = null, Direction facing = Direction.East, bool includeToggleLever = false, bool includeOutputLamp = false, Rotation rotation = Rotation.None) =>
        builder.AddClock(x, y, z, ClockType.TorchRepeater, period, facing, includeToggleLever, includeOutputLamp, rotation);

    public static SchematicBuilder AddComparatorClock(this SchematicBuilder builder, int x, int y, int z, TimeSpan? period = null, Direction facing = Direction.East, bool includeToggleLever = true, bool includeOutputLamp = false, Rotation rotation = Rotation.None) =>
        builder.AddClock(x, y, z, ClockType.Comparator, period, facing, includeToggleLever, includeOutputLamp, rotation);

    public static SchematicBuilder AddHopperClock(
        this SchematicBuilder builder,
        int x,
        int y,
        int z,
        TimeSpan? period = null,
        Direction facing = Direction.East,
        bool includeToggleLever = false,
        bool includeOutputLamp = false,
        bool oneWide = false,
        HopperClockTorchPlacement torchPlacement = HopperClockTorchPlacement.None,
        int torchCount = 1,
        Rotation rotation = Rotation.None) =>
        new HopperClockSchematicCreator
        {
            Period = period,
            Facing = facing,
            OneWide = oneWide,
            TorchPlacement = torchPlacement,
            TorchCount = torchCount,
            IncludeToggleLever = includeToggleLever,
            IncludeOutputLamp = includeOutputLamp
        }.WithRotation(rotation).Place(builder, x, y, z);

    public static SchematicBuilder AddObserverClock(this SchematicBuilder builder, int x, int y, int z, TimeSpan? period = null, Direction facing = Direction.East, bool includeToggleLever = true, bool includeOutputLamp = false, Rotation rotation = Rotation.None) =>
        builder.AddClock(x, y, z, ClockType.Observer, period, facing, includeToggleLever, includeOutputLamp, rotation);

    public static SchematicBuilder AddPistonClock(this SchematicBuilder builder, int x, int y, int z, TimeSpan? period = null, Direction facing = Direction.East, bool includeToggleLever = false, bool includeOutputLamp = false, Rotation rotation = Rotation.None) =>
        builder.AddClock(x, y, z, ClockType.Piston, period, facing, includeToggleLever, includeOutputLamp, rotation);

    public static SchematicBuilder AddMinecartClock(this SchematicBuilder builder, int x, int y, int z, TimeSpan? period = null, Direction facing = Direction.East, bool includeOutputLamp = false, Rotation rotation = Rotation.None) =>
        builder.AddClock(x, y, z, ClockType.Minecart, period, facing, includeToggleLever: false, includeOutputLamp, rotation);
}
