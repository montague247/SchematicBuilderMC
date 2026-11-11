namespace SchematicBuilderMC.Generators;

/// <summary>
/// Specifies the code structure format to generate for fluent builder code.
/// </summary>
public enum CodeGenerationMode
{
    /// <summary>
    /// Generates a local variable assignment statement (e.g., <c>var schematic = SchematicBuilder.Create(...)...Build();</c>).
    /// </summary>
    Statement,

    /// <summary>
    /// Generates only the fluent builder call chain expression (e.g., <c>SchematicBuilder.Create(...)...Build()</c>).
    /// </summary>
    Expression,

    /// <summary>
    /// Generates a static factory method returning the schematic (e.g., <c>public static Schematic CreateSchematic() { ... }</c>).
    /// </summary>
    Method,

    /// <summary>
    /// Generates a complete standalone C# source file with using directives, namespace, static class, and factory method.
    /// </summary>
    File
}
