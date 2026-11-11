namespace SchematicBuilderMC.Generators;

/// <summary>
/// Options for configuring the Minecraft registry code generator.
/// </summary>
public sealed class GeneratorOptions
{
    /// <summary>
    /// The target C# namespace for the generated classes. Default is "SchematicBuilderMC.Registries".
    /// </summary>
    public string TargetNamespace { get; set; } = "SchematicBuilderMC.Registries";

    /// <summary>
    /// The root static class name. Default is "Minecraft".
    /// </summary>
    public string RootClassName { get; set; } = "Minecraft";

    /// <summary>
    /// The Minecraft version string to include in documentation and constants. Default is "26.3".
    /// </summary>
    public string MinecraftVersion { get; set; } = "26.3";

    /// <summary>
    /// Whether to generate the Blocks registry class. Default is true.
    /// </summary>
    public bool IncludeBlocks { get; set; } = true;

    /// <summary>
    /// Whether to generate the Items registry class. Default is true.
    /// </summary>
    public bool IncludeItems { get; set; } = true;

    /// <summary>
    /// Whether to generate the BlockEntities and BlockEntityTypes registry classes. Default is true.
    /// </summary>
    public bool IncludeBlockEntities { get; set; } = true;

    /// <summary>
    /// Whether to generate the Entities and EntityTypes registry classes. Default is true.
    /// </summary>
    public bool IncludeEntities { get; set; } = true;

    /// <summary>
    /// Whether to generate the Biomes registry class. Default is true.
    /// </summary>
    public bool IncludeBiomes { get; set; } = true;

    /// <summary>
    /// Whether to generate the Fluids registry class. Default is true.
    /// </summary>
    public bool IncludeFluids { get; set; } = true;

    /// <summary>
    /// Whether to generate read-only collections (e.g. <c>All</c>). Default is true.
    /// </summary>
    public bool GenerateCollections { get; set; } = true;

    /// <summary>
    /// Whether to generate helper methods (e.g. <c>IsValid</c>). Default is true.
    /// </summary>
    public bool GenerateValidationHelpers { get; set; } = true;

    /// <summary>
    /// Optional dictionary of additional registry names to C# nested class names (e.g. "enchantment" -> "Enchantments").
    /// </summary>
    public Dictionary<string, string>? CustomRegistries { get; set; }
}
