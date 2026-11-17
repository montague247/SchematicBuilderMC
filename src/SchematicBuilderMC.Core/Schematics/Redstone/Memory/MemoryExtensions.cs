using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;

namespace SchematicBuilderMC.Schematics.Redstone.Memory;

/// <summary>
/// Fluent <see cref="SchematicBuilder"/> extensions for placing Redstone memory and sequential logic circuits.
/// </summary>
public static class MemoryExtensions
{
    public static SchematicBuilder AddRsLatch(this SchematicBuilder builder, int x, int y, int z, RsLatchMode mode = RsLatchMode.RsNor, Direction facing = Direction.East, bool includeInputButtons = false, bool includeOutputLamp = false, Rotation rotation = Rotation.None) =>
        new RsLatchSchematicCreator { Mode = mode, Facing = facing, IncludeInputButtons = includeInputButtons, IncludeOutputLamp = includeOutputLamp }.WithRotation(rotation).Place(builder, x, y, z);

    public static SchematicBuilder AddDLatch(this SchematicBuilder builder, int x, int y, int z, bool activeHighWrite = true, Direction facing = Direction.East, bool includeInputLevers = false, bool includeOutputLamp = false, Rotation rotation = Rotation.None) =>
        new DLatchSchematicCreator { ActiveHighWrite = activeHighWrite, Facing = facing, IncludeInputLevers = includeInputLevers, IncludeOutputLamp = includeOutputLamp }.WithRotation(rotation).Place(builder, x, y, z);

    public static SchematicBuilder AddDFlipFlop(this SchematicBuilder builder, int x, int y, int z, Direction facing = Direction.East, bool includeInputLevers = false, bool includeOutputLamp = false, Rotation rotation = Rotation.None) =>
        new DFlipFlopSchematicCreator { Facing = facing, IncludeInputLevers = includeInputLevers, IncludeOutputLamp = includeOutputLamp }.WithRotation(rotation).Place(builder, x, y, z);

    public static SchematicBuilder AddTFlipFlop(this SchematicBuilder builder, int x, int y, int z, TFlipFlopMode mode = TFlipFlopMode.CopperBulb, Direction facing = Direction.East, bool includeInputButton = false, bool includeOutputLamp = false, Rotation rotation = Rotation.None) =>
        new TFlipFlopSchematicCreator { Mode = mode, Facing = facing, IncludeInputButton = includeInputButton, IncludeOutputLamp = includeOutputLamp }.WithRotation(rotation).Place(builder, x, y, z);

    public static SchematicBuilder AddBinaryCounter(this SchematicBuilder builder, int x, int y, int z, int bitCount = 4, Direction facing = Direction.East, bool includeInputButton = false, bool includeOutputLamps = false, Rotation rotation = Rotation.None) =>
        new BinaryCounterSchematicCreator { BitCount = bitCount, Facing = facing, IncludeInputButton = includeInputButton, IncludeOutputLamps = includeOutputLamps }.WithRotation(rotation).Place(builder, x, y, z);

    public static SchematicBuilder AddShiftRegister(this SchematicBuilder builder, int x, int y, int z, int bitCount = 4, Direction facing = Direction.East, bool includeInputControls = false, bool includeOutputLamps = false, Rotation rotation = Rotation.None) =>
        new ShiftRegisterSchematicCreator { BitCount = bitCount, Facing = facing, IncludeInputControls = includeInputControls, IncludeOutputLamps = includeOutputLamps }.WithRotation(rotation).Place(builder, x, y, z);
}
