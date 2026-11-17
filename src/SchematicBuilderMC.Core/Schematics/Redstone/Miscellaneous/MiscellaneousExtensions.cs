using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;

namespace SchematicBuilderMC.Schematics.Redstone.Miscellaneous;

/// <summary>
/// Fluent <see cref="SchematicBuilder"/> extensions for placing miscellaneous Redstone circuits.
/// </summary>
public static class MiscellaneousExtensions
{
    public static SchematicBuilder AddAbbaCircuit(this SchematicBuilder builder, int x, int y, int z, int outputCount = 3, TimeSpan? stepDelay = null, Direction facing = Direction.East, bool includeInputLever = false, bool includeOutputLamps = true, Rotation rotation = Rotation.None) =>
        new AbbaCircuitSchematicCreator { OutputCount = outputCount, StepDelay = stepDelay, Facing = facing, IncludeInputLever = includeInputLever, IncludeOutputLamps = includeOutputLamps }.WithRotation(rotation).Place(builder, x, y, z);

    public static SchematicBuilder AddBlockUpdateDetector(this SchematicBuilder builder, int x, int y, int z, BudMode mode = BudMode.PistonBud, Direction facing = Direction.East, bool includeOutputLamp = true, Rotation rotation = Rotation.None) =>
        new BlockUpdateDetectorSchematicCreator { Mode = mode, Facing = facing, IncludeOutputLamp = includeOutputLamp }.WithRotation(rotation).Place(builder, x, y, z);

    public static SchematicBuilder AddMultiplexer(this SchematicBuilder builder, int x, int y, int z, MultiplexerMode mode = MultiplexerMode.Multiplexer, int stages = 1, Direction facing = Direction.East, bool includeInputLevers = false, bool includeOutputLamps = false, Rotation rotation = Rotation.None) =>
        new MultiplexerSchematicCreator { Mode = mode, Stages = stages, Facing = facing, IncludeInputLevers = includeInputLevers, IncludeOutputLamps = includeOutputLamps }.WithRotation(rotation).Place(builder, x, y, z);

    public static SchematicBuilder AddRandomizer(this SchematicBuilder builder, int x, int y, int z, RandomizerMode mode = RandomizerMode.TokenCoinFlip, int outputCount = 2, Direction facing = Direction.East, bool includeInputButton = false, bool includeOutputLamps = false, Rotation rotation = Rotation.None) =>
        new RandomizerSchematicCreator { Mode = mode, OutputCount = outputCount, Facing = facing, IncludeInputButton = includeInputButton, IncludeOutputLamps = includeOutputLamps }.WithRotation(rotation).Place(builder, x, y, z);

    public static SchematicBuilder AddPistonExtender(this SchematicBuilder builder, int x, int y, int z, int reach = 2, Direction facing = Direction.East, bool includeInputLever = false, Rotation rotation = Rotation.None) =>
        new PistonExtenderSchematicCreator { Reach = reach, Facing = facing, IncludeInputLever = includeInputLever }.WithRotation(rotation).Place(builder, x, y, z);
}
