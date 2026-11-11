namespace SchematicBuilderMC.Generators;

/// <summary>
/// Options for configuring the fluent builder source code generator.
/// </summary>
public sealed class FluentCodeGeneratorOptions
{
    /// <summary>
    /// The output code format structure. Default is <see cref="CodeGenerationMode.Statement"/>.
    /// </summary>
    public CodeGenerationMode Mode { get; set; } = CodeGenerationMode.Statement;

    /// <summary>
    /// The variable name used in <see cref="CodeGenerationMode.Statement"/> mode. Default is "schematic".
    /// </summary>
    public string VariableName { get; set; } = "schematic";

    /// <summary>
    /// The class name used in <see cref="CodeGenerationMode.File"/> mode. Default is "GeneratedSchematic".
    /// </summary>
    public string ClassName { get; set; } = "GeneratedSchematic";

    /// <summary>
    /// The factory method name used in <see cref="CodeGenerationMode.Method"/> and <see cref="CodeGenerationMode.File"/> modes. Default is "CreateSchematic".
    /// </summary>
    public string MethodName { get; set; } = "CreateSchematic";

    /// <summary>
    /// The target C# namespace used in <see cref="CodeGenerationMode.File"/> mode. Default is "SchematicBuilderMC".
    /// </summary>
    public string TargetNamespace { get; set; } = "SchematicBuilderMC";

    /// <summary>
    /// Whether to refer to blocks using strongly-typed <c>Minecraft.Blocks</c> constants where available. Default is true.
    /// When false, block state strings are emitted as string literals.
    /// </summary>
    public bool UseStronglyTypedBlocks { get; set; } = true;

    /// <summary>
    /// Whether to refer to block entities and entities using strongly-typed <c>Minecraft.BlockEntities</c>
    /// and <c>Minecraft.Entities</c> constants where available. Default is true.
    /// </summary>
    public bool UseStronglyTypedEntities { get; set; } = true;

    /// <summary>
    /// Whether to refer to items using strongly-typed <c>Minecraft.Items</c> constants where available. Default is true.
    /// When false, item identifier strings are emitted as string literals.
    /// </summary>
    public bool UseStronglyTypedItems { get; set; } = true;

    /// <summary>
    /// Whether to optimize contiguous 3D regions of identical blocks using <c>.Fill(...)</c> calls. Default is true.
    /// When false, every block is set individually with <c>.SetBlock(...)</c>.
    /// </summary>
    public bool OptimizeFills { get; set; } = true;

    /// <summary>
    /// Whether to omit air blocks from generation, since <see cref="SchematicBuilder.Create"/> initializes all blocks to Air. Default is true.
    /// </summary>
    public bool OmitAir { get; set; } = true;

    /// <summary>
    /// Whether to always emit <c>.WithOffset(...)</c> even if the schematic offset is (0, 0, 0). Default is false.
    /// </summary>
    public bool AlwaysIncludeOffset { get; set; } = false;

    /// <summary>
    /// Whether to generate semantic property helper methods (e.g., <c>.WithDelay(3)</c>, <c>.WithSubtractMode()</c>, <c>.WithFacing("north")</c>, <c>.WithPowered(true)</c>)
    /// instead of generic <c>.WithProperty("key", "value")</c> calls. Default is true.
    /// </summary>
    public bool UseSemanticPropertyMethods { get; set; } = true;

    /// <summary>
    /// Number of spaces per indentation level. Default is 4.
    /// </summary>
    public int IndentSpaces { get; set; } = 4;

    /// <summary>
    /// Whether to use file-scoped namespace declarations (<c>namespace Foo;</c>) in <see cref="CodeGenerationMode.File"/> mode. Default is true.
    /// </summary>
    public bool UseFileScopedNamespace { get; set; } = true;

    /// <summary>
    /// Gets the string representing one indentation level.
    /// </summary>
    public string IndentString => new(' ', Math.Max(0, IndentSpaces));
}
