namespace SchematicBuilderMC.Generators;

/// <summary>
/// Options for configuring <see cref="MinecraftRecipesGenerator"/>.
/// </summary>
public sealed class RecipesGeneratorOptions
{
    /// <summary>
    /// Target C# namespace for the generated classes. Default is <c>SchematicBuilderMC.Registries</c>.
    /// </summary>
    public string TargetNamespace { get; set; } = "SchematicBuilderMC.Registries";

    /// <summary>
    /// Root static class name. Default is <c>Minecraft</c>.
    /// </summary>
    public string RootClassName { get; set; } = "Minecraft";

    /// <summary>
    /// Nested recipes container class name. Default is <c>Recipes</c>.
    /// </summary>
    public string RecipesClassName { get; set; } = "Recipes";

    /// <summary>
    /// Nested class name for recipe identifier constants. Default is <c>Ids</c>.
    /// </summary>
    public string RecipeIdsClassName { get; set; } = "Ids";

    /// <summary>
    /// Nested class name for item tag identifier constants. Default is <c>TagIds</c>.
    /// </summary>
    public string TagIdsClassName { get; set; } = "TagIds";

    /// <summary>
    /// Minecraft version string to include in documentation comments. Default is <c>26.3</c>.
    /// </summary>
    public string MinecraftVersion { get; set; } = "26.3";

    /// <summary>
    /// Whether to generate strongly-typed recipe ID constants (<see cref="RecipeIdsClassName"/>). Default is <c>true</c>.
    /// </summary>
    public bool IncludeRecipeIds { get; set; } = true;

    /// <summary>
    /// Whether to generate strongly-typed item tag ID constants (<see cref="TagIdsClassName"/>). Default is <c>true</c>.
    /// </summary>
    public bool IncludeTagIds { get; set; } = true;

    /// <summary>
    /// Whether to generate read-only <c>All</c> identifier collections. Default is <c>true</c>.
    /// </summary>
    public bool GenerateCollections { get; set; } = true;

    /// <summary>
    /// Whether to generate <c>IsValid</c> helper methods. Default is <c>true</c>.
    /// </summary>
    public bool GenerateValidationHelpers { get; set; } = true;
}
