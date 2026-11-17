using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;

namespace SchematicBuilderMC.Schematics.Redstone.Pulse;

/// <summary>
/// Fluent <see cref="SchematicBuilder"/> extensions for placing Redstone pulse circuits.
/// </summary>
public static class PulseExtensions
{
    public static SchematicBuilder AddPulseGenerator(
        this SchematicBuilder builder,
        int x,
        int y,
        int z,
        PulseGeneratorMode mode = PulseGeneratorMode.StickyPistonMonostable,
        TimeSpan? pulseDuration = null,
        Direction facing = Direction.East,
        bool includeInputLever = false,
        bool includeOutputLamp = false,
        Rotation rotation = Rotation.None)
    {
        return new PulseGeneratorSchematicCreator
        {
            Mode = mode,
            PulseDuration = pulseDuration,
            Facing = facing,
            IncludeInputLever = includeInputLever,
            IncludeOutputLamp = includeOutputLamp
        }.WithRotation(rotation).Place(builder, x, y, z);
    }

    public static SchematicBuilder AddEdgeDetector(
        this SchematicBuilder builder,
        int x,
        int y,
        int z,
        EdgeDetectorType edgeType = EdgeDetectorType.RisingEdge,
        TimeSpan? pulseDuration = null,
        Direction facing = Direction.East,
        bool includeInputLever = false,
        bool includeOutputLamp = false,
        Rotation rotation = Rotation.None)
    {
        return new EdgeDetectorSchematicCreator
        {
            EdgeType = edgeType,
            PulseDuration = pulseDuration,
            Facing = facing,
            IncludeInputLever = includeInputLever,
            IncludeOutputLamp = includeOutputLamp
        }.WithRotation(rotation).Place(builder, x, y, z);
    }

    public static SchematicBuilder AddPulseExtender(
        this SchematicBuilder builder,
        int x,
        int y,
        int z,
        PulseExtenderMode mode = PulseExtenderMode.ComparatorDecay,
        TimeSpan? duration = null,
        Direction facing = Direction.East,
        bool includeInputButton = false,
        bool includeOutputLamp = false,
        Rotation rotation = Rotation.None)
    {
        return new PulseExtenderSchematicCreator
        {
            Mode = mode,
            Duration = duration,
            Facing = facing,
            IncludeInputButton = includeInputButton,
            IncludeOutputLamp = includeOutputLamp
        }.WithRotation(rotation).Place(builder, x, y, z);
    }

    public static SchematicBuilder AddPulseMultiplier(
        this SchematicBuilder builder,
        int x,
        int y,
        int z,
        int pulseCount = 4,
        PulseMultiplierMode mode = PulseMultiplierMode.RepeaterTaps,
        TimeSpan? pulseSpacing = null,
        Direction facing = Direction.East,
        bool includeInputButton = false,
        bool includeOutputLamp = false,
        Rotation rotation = Rotation.None)
    {
        return new PulseMultiplierSchematicCreator
        {
            Mode = mode,
            PulseCount = pulseCount,
            PulseSpacing = pulseSpacing,
            Facing = facing,
            IncludeInputButton = includeInputButton,
            IncludeOutputLamp = includeOutputLamp
        }.WithRotation(rotation).Place(builder, x, y, z);
    }

    public static SchematicBuilder AddPulseDivider(
        this SchematicBuilder builder,
        int x,
        int y,
        int z,
        int divisor = 4,
        PulseDividerMode mode = PulseDividerMode.CopperBulbBinary,
        Direction facing = Direction.East,
        bool includeInputButton = false,
        bool includeOutputLamp = false,
        Rotation rotation = Rotation.None)
    {
        return new PulseDividerSchematicCreator
        {
            Mode = mode,
            Divisor = divisor,
            Facing = facing,
            IncludeInputButton = includeInputButton,
            IncludeOutputLamp = includeOutputLamp
        }.WithRotation(rotation).Place(builder, x, y, z);
    }

    public static SchematicBuilder AddPulseLengthDetector(
        this SchematicBuilder builder,
        int x,
        int y,
        int z,
        TimeSpan? minimumDuration = null,
        Direction facing = Direction.East,
        bool includeInputLever = false,
        bool includeOutputLamp = false,
        Rotation rotation = Rotation.None)
    {
        return new PulseLengthDetectorSchematicCreator
        {
            MinimumDuration = minimumDuration,
            Facing = facing,
            IncludeInputLever = includeInputLever,
            IncludeOutputLamp = includeOutputLamp
        }.WithRotation(rotation).Place(builder, x, y, z);
    }
}
