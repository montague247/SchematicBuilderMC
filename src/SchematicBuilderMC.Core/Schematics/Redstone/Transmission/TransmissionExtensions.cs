using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;

namespace SchematicBuilderMC.Schematics.Redstone.Transmission;

/// <summary>
/// Fluent <see cref="SchematicBuilder"/> extensions for placing Redstone transmission circuits.
/// </summary>
public static class TransmissionExtensions
{
    public static SchematicBuilder AddTransmissionLine(
        this SchematicBuilder builder,
        int x,
        int y,
        int z,
        int length = 16,
        Direction facing = Direction.East,
        TimeSpan? delay = null,
        bool includeInputLever = false,
        bool includeOutputLamp = false,
        Rotation rotation = Rotation.None)
    {
        return new TransmissionLineSchematicCreator
        {
            Length = length,
            Facing = facing,
            Delay = delay,
            IncludeInputLever = includeInputLever,
            IncludeOutputLamp = includeOutputLamp
        }.WithRotation(rotation).Place(builder, x, y, z);
    }

    public static SchematicBuilder AddVerticalTransmission(
        this SchematicBuilder builder,
        int x,
        int y,
        int z,
        VerticalTransmissionMode mode = VerticalTransmissionMode.TorchTowerUp,
        int height = 8,
        Direction facing = Direction.East,
        bool includeInputLever = false,
        bool includeOutputLamp = false,
        Rotation rotation = Rotation.None)
    {
        return new VerticalTransmissionSchematicCreator
        {
            Mode = mode,
            Height = height,
            Facing = facing,
            IncludeInputLever = includeInputLever,
            IncludeOutputLamp = includeOutputLamp
        }.WithRotation(rotation).Place(builder, x, y, z);
    }

    public static SchematicBuilder AddSignalBridge(
        this SchematicBuilder builder,
        int x,
        int y,
        int z,
        SignalBridgeMode mode = SignalBridgeMode.RedstoneBridge,
        int spanLength = 5,
        Direction facing = Direction.East,
        TimeSpan? delay = null,
        Rotation rotation = Rotation.None)
    {
        return new SignalBridgeSchematicCreator
        {
            Mode = mode,
            SpanLength = spanLength,
            Facing = facing,
            Delay = delay
        }.WithRotation(rotation).Place(builder, x, y, z);
    }

    public static SchematicBuilder AddTwoWayRepeater(
        this SchematicBuilder builder,
        int x,
        int y,
        int z,
        TwoWayRepeaterMode mode = TwoWayRepeaterMode.Comparison,
        Direction facing = Direction.East,
        TimeSpan? delay = null,
        bool includeInputLevers = false,
        Rotation rotation = Rotation.None)
    {
        return new TwoWayRepeaterSchematicCreator
        {
            Mode = mode,
            Facing = facing,
            Delay = delay,
            IncludeInputLevers = includeInputLevers
        }.WithRotation(rotation).Place(builder, x, y, z);
    }

    public static SchematicBuilder AddInstantRepeater(
        this SchematicBuilder builder,
        int x,
        int y,
        int z,
        int stageCount = 1,
        Direction facing = Direction.East,
        bool includeInputLever = false,
        bool includeOutputLamp = false,
        Rotation rotation = Rotation.None)
    {
        return new InstantRepeaterSchematicCreator
        {
            StageCount = stageCount,
            Facing = facing,
            IncludeInputLever = includeInputLever,
            IncludeOutputLamp = includeOutputLamp
        }.WithRotation(rotation).Place(builder, x, y, z);
    }
}
