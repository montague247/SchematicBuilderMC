using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;

namespace SchematicBuilderMC.Schematics.Redstone.Logic;

/// <summary>
/// Fluent <see cref="SchematicBuilder"/> extensions for placing Redstone logic gates.
/// </summary>
public static class LogicExtensions
{
    public static SchematicBuilder AddLogicGate(
        this SchematicBuilder builder,
        int x,
        int y,
        int z,
        LogicGateType gateType,
        Direction facing = Direction.East,
        bool includeInputLevers = false,
        bool includeOutputLamp = false,
        Rotation rotation = Rotation.None)
    {
        return new LogicGateSchematicCreator
        {
            GateType = gateType,
            Facing = facing,
            IncludeInputLevers = includeInputLevers,
            IncludeOutputLamp = includeOutputLamp
        }.WithRotation(rotation).Place(builder, x, y, z);
    }

    public static SchematicBuilder AddNotGate(this SchematicBuilder builder, int x, int y, int z, Direction facing = Direction.East, bool includeInputLever = false, bool includeOutputLamp = false, Rotation rotation = Rotation.None) =>
        builder.AddLogicGate(x, y, z, LogicGateType.Not, facing, includeInputLever, includeOutputLamp, rotation);

    public static SchematicBuilder AddOrGate(this SchematicBuilder builder, int x, int y, int z, Direction facing = Direction.East, bool includeInputLevers = false, bool includeOutputLamp = false, Rotation rotation = Rotation.None) =>
        builder.AddLogicGate(x, y, z, LogicGateType.Or, facing, includeInputLevers, includeOutputLamp, rotation);

    public static SchematicBuilder AddNorGate(this SchematicBuilder builder, int x, int y, int z, Direction facing = Direction.East, bool includeInputLevers = false, bool includeOutputLamp = false, Rotation rotation = Rotation.None) =>
        builder.AddLogicGate(x, y, z, LogicGateType.Nor, facing, includeInputLevers, includeOutputLamp, rotation);

    public static SchematicBuilder AddAndGate(this SchematicBuilder builder, int x, int y, int z, Direction facing = Direction.East, bool includeInputLevers = false, bool includeOutputLamp = false, Rotation rotation = Rotation.None) =>
        builder.AddLogicGate(x, y, z, LogicGateType.And, facing, includeInputLevers, includeOutputLamp, rotation);

    public static SchematicBuilder AddNandGate(this SchematicBuilder builder, int x, int y, int z, Direction facing = Direction.East, bool includeInputLevers = false, bool includeOutputLamp = false, Rotation rotation = Rotation.None) =>
        builder.AddLogicGate(x, y, z, LogicGateType.Nand, facing, includeInputLevers, includeOutputLamp, rotation);

    public static SchematicBuilder AddXorGate(this SchematicBuilder builder, int x, int y, int z, Direction facing = Direction.East, bool includeInputLevers = false, bool includeOutputLamp = false, Rotation rotation = Rotation.None) =>
        builder.AddLogicGate(x, y, z, LogicGateType.Xor, facing, includeInputLevers, includeOutputLamp, rotation);

    public static SchematicBuilder AddXnorGate(this SchematicBuilder builder, int x, int y, int z, Direction facing = Direction.East, bool includeInputLevers = false, bool includeOutputLamp = false, Rotation rotation = Rotation.None) =>
        builder.AddLogicGate(x, y, z, LogicGateType.Xnor, facing, includeInputLevers, includeOutputLamp, rotation);

    public static SchematicBuilder AddImplyGate(this SchematicBuilder builder, int x, int y, int z, Direction facing = Direction.East, bool includeInputLevers = false, bool includeOutputLamp = false, Rotation rotation = Rotation.None) =>
        builder.AddLogicGate(x, y, z, LogicGateType.Imply, facing, includeInputLevers, includeOutputLamp, rotation);

    public static SchematicBuilder AddNimplyGate(this SchematicBuilder builder, int x, int y, int z, Direction facing = Direction.East, bool includeInputLevers = false, bool includeOutputLamp = false, Rotation rotation = Rotation.None) =>
        builder.AddLogicGate(x, y, z, LogicGateType.Nimply, facing, includeInputLevers, includeOutputLamp, rotation);
}
