using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using SchematicBuilderMC.Registries;

namespace SchematicBuilderMC.Generators;

/// <summary>
/// Code and data generator that creates strongly-typed Minecraft recipe and item-tag identifier classes
/// (and extracts combined recipe JSON payloads from bundled resources, mcmeta summaries, or Minecraft JARs/datapacks).
/// </summary>
public static class MinecraftRecipesGenerator
{
    private const string DefaultMcmetaRecipeSummaryUrlPattern =
        "https://raw.githubusercontent.com/misode/mcmeta/refs/tags/{0}-summary/data/recipe/data.json";

    private const string DefaultMcmetaItemTagSummaryUrlPattern =
        "https://raw.githubusercontent.com/misode/mcmeta/refs/tags/{0}-summary/data/tag/item/data.json";

    /// <summary>
    /// Converts a Minecraft recipe or tag identifier (e.g. <c>minecraft:oak_planks</c> or <c>#minecraft:planks</c>)
    /// into a valid PascalCase C# identifier.
    /// </summary>
    public static string ToPascalCase(string identifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        return MinecraftRegistryGenerator.ToPascalCase(identifier.Trim().TrimStart('#'));
    }

    /// <summary>
    /// Retrieves the bundled Minecraft 26.3 recipe JSON content embedded in the assembly.
    /// </summary>
    public static string GetBundledRecipeJson() => RecipeRegistry.GetBundledRecipeJson();

    /// <summary>
    /// Parses recipe and item tag data from bundled or mcmeta-style recipe JSON.
    /// </summary>
    public static RecipeRegistry ParseRecipeJson(string jsonContent) => RecipeRegistry.FromJson(jsonContent);

    /// <summary>
    /// Extracts all vanilla recipes and item tags from a Minecraft client or server <c>.jar</c> archive
    /// into the combined JSON format consumed by <see cref="RecipeRegistry.FromJson"/> and this generator.
    /// </summary>
    public static string ExtractRecipeJsonFromJar(string jarPath, string minecraftVersion = Minecraft.Version)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jarPath);
        if (!File.Exists(jarPath))
        {
            throw new FileNotFoundException($"Minecraft jar file was not found at '{jarPath}'.", jarPath);
        }

        using var archive = ZipFile.OpenRead(jarPath);
        var recipes = new SortedDictionary<string, JsonNode?>(StringComparer.Ordinal);
        var itemTags = new SortedDictionary<string, JsonNode?>(StringComparer.Ordinal);

        const string recipePrefix = "data/minecraft/recipe/";
        const string tagPrefix = "data/minecraft/tags/item/";

        foreach (ZipArchiveEntry entry in archive.Entries)
        {
            if (!entry.FullName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (entry.FullName.StartsWith(recipePrefix, StringComparison.Ordinal))
            {
                string id = RecipeRegistry.NormalizeRecipeId(entry.FullName[recipePrefix.Length..^5]);
                using var stream = entry.Open();
                recipes[id] = JsonNode.Parse(stream);
            }
            else if (entry.FullName.StartsWith(tagPrefix, StringComparison.Ordinal))
            {
                string tagId = RecipeRegistry.NormalizeItemOrTagIdentifier("#" + entry.FullName[tagPrefix.Length..^5]);
                using var stream = entry.Open();
                var tagDoc = JsonNode.Parse(stream)?.AsObject();
                itemTags[tagId] = tagDoc?["values"]?.DeepClone() ?? new JsonArray();
            }
        }

        return BuildCombinedJsonPayload(minecraftVersion, itemTags, recipes);
    }

    /// <summary>
    /// Extracts all recipes and item tags from a vanilla/datapack <c>data/minecraft</c> root directory
    /// into the combined JSON format consumed by <see cref="RecipeRegistry.FromJson"/> and this generator.
    /// </summary>
    public static string ExtractRecipeJsonFromDirectory(string dataMinecraftDirectory, string minecraftVersion = Minecraft.Version)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataMinecraftDirectory);
        if (!Directory.Exists(dataMinecraftDirectory))
        {
            throw new DirectoryNotFoundException($"Directory '{dataMinecraftDirectory}' does not exist.");
        }

        var recipes = new SortedDictionary<string, JsonNode?>(StringComparer.Ordinal);
        var itemTags = new SortedDictionary<string, JsonNode?>(StringComparer.Ordinal);

        string recipeDir = Path.Combine(dataMinecraftDirectory, "recipe");
        if (Directory.Exists(recipeDir))
        {
            foreach (string file in Directory.EnumerateFiles(recipeDir, "*.json", SearchOption.AllDirectories))
            {
                string relative = Path.GetRelativePath(recipeDir, file).Replace('\\', '/');
                string id = RecipeRegistry.NormalizeRecipeId(relative[..^5]);
                recipes[id] = JsonNode.Parse(File.ReadAllText(file));
            }
        }

        string tagDir = Path.Combine(dataMinecraftDirectory, "tags", "item");
        if (Directory.Exists(tagDir))
        {
            foreach (string file in Directory.EnumerateFiles(tagDir, "*.json", SearchOption.AllDirectories))
            {
                string relative = Path.GetRelativePath(tagDir, file).Replace('\\', '/');
                string tagId = RecipeRegistry.NormalizeItemOrTagIdentifier("#" + relative[..^5]);
                var tagDoc = JsonNode.Parse(File.ReadAllText(file))?.AsObject();
                itemTags[tagId] = tagDoc?["values"]?.DeepClone() ?? new JsonArray();
            }
        }

        return BuildCombinedJsonPayload(minecraftVersion, itemTags, recipes);
    }

    /// <summary>
    /// Downloads recipe and item-tag summary JSON data for the given Minecraft version from mcmeta
    /// and combines them into the unified recipe JSON payload.
    /// </summary>
    public static async Task<string> DownloadRecipeJsonAsync(
        string version = Minecraft.Version,
        HttpClient? httpClient = null,
        CancellationToken cancellationToken = default)
    {
        bool disposeClient = false;
        if (httpClient == null)
        {
            httpClient = new HttpClient();
            disposeClient = true;
        }

        try
        {
            string recipeUrl = string.Format(DefaultMcmetaRecipeSummaryUrlPattern, version);
            string tagUrl = string.Format(DefaultMcmetaItemTagSummaryUrlPattern, version);

            string recipesRaw = await httpClient.GetStringAsync(recipeUrl, cancellationToken).ConfigureAwait(false);
            string tagsRaw = await httpClient.GetStringAsync(tagUrl, cancellationToken).ConfigureAwait(false);

            var recipes = new SortedDictionary<string, JsonNode?>(StringComparer.Ordinal);
            var recipesObj = JsonNode.Parse(recipesRaw)?.AsObject();
            if (recipesObj != null)
            {
                foreach (var kvp in recipesObj)
                {
                    recipes[RecipeRegistry.NormalizeRecipeId(kvp.Key)] = kvp.Value?.DeepClone();
                }
            }

            var itemTags = new SortedDictionary<string, JsonNode?>(StringComparer.Ordinal);
            var tagsObj = JsonNode.Parse(tagsRaw)?.AsObject();
            if (tagsObj != null)
            {
                foreach (var kvp in tagsObj)
                {
                    string tagId = RecipeRegistry.NormalizeItemOrTagIdentifier("#" + kvp.Key.TrimStart('#'));
                    JsonNode? values = kvp.Value is JsonObject obj && obj["values"] != null
                        ? obj["values"]!.DeepClone()
                        : kvp.Value?.DeepClone() ?? new JsonArray();
                    itemTags[tagId] = values;
                }
            }

            return BuildCombinedJsonPayload(version, itemTags, recipes);
        }
        finally
        {
            if (disposeClient)
            {
                httpClient.Dispose();
            }
        }
    }

    /// <summary>
    /// Generates a single combined C# source code string containing <c>Ids</c> and <c>TagIds</c> classes under <c>Minecraft.Recipes</c>.
    /// </summary>
    public static string GenerateSourceCode(string jsonContent, RecipesGeneratorOptions? options = null)
    {
        options ??= new RecipesGeneratorOptions();
        var registry = ParseRecipeJson(jsonContent);

        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine($"namespace {options.TargetNamespace};");
        sb.AppendLine();
        sb.AppendLine($"public static partial class {options.RootClassName}");
        sb.AppendLine("{");
        sb.AppendLine($"    public static partial class {options.RecipesClassName}");
        sb.AppendLine("    {");

        foreach (var (_, className, comment, entries, isTagRegistry) in GetPlannedSections(registry, options))
        {
            MinecraftRegistryGenerator.AppendIdentifierConstantsClass(
                sb,
                className,
                comment,
                entries,
                options.GenerateCollections,
                options.GenerateValidationHelpers,
                isTagRegistry,
                indent: "        ");
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");
        return sb.ToString();
    }

    /// <summary>
    /// Generates a dictionary of separate C# partial class source files (<c>filename -&gt; file content</c>).
    /// </summary>
    public static Dictionary<string, string> GenerateSourceFiles(string jsonContent, RecipesGeneratorOptions? options = null)
    {
        options ??= new RecipesGeneratorOptions();
        var registry = ParseRecipeJson(jsonContent);
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (fileName, className, comment, entries, isTagRegistry) in GetPlannedSections(registry, options))
        {
            var sb = new StringBuilder();
            sb.AppendLine("// <auto-generated/>");
            sb.AppendLine("#nullable enable");
            sb.AppendLine();
            sb.AppendLine($"namespace {options.TargetNamespace};");
            sb.AppendLine();
            sb.AppendLine($"public static partial class {options.RootClassName}");
            sb.AppendLine("{");
            sb.AppendLine($"    public static partial class {options.RecipesClassName}");
            sb.AppendLine("    {");

            MinecraftRegistryGenerator.AppendIdentifierConstantsClass(
                sb,
                className,
                comment,
                entries,
                options.GenerateCollections,
                options.GenerateValidationHelpers,
                isTagRegistry,
                indent: "        ");

            sb.AppendLine("    }");
            sb.AppendLine("}");
            result[fileName] = sb.ToString();
        }

        return result;
    }

    /// <summary>
    /// Generates recipe identifier source files and writes them directly to <paramref name="targetDirectory"/>.
    /// </summary>
    public static void GenerateToDirectory(string jsonContent, string targetDirectory, RecipesGeneratorOptions? options = null) =>
        MinecraftRegistryGenerator.WriteGeneratedFiles(GenerateSourceFiles(jsonContent, options), targetDirectory);

    /// <summary>
    /// Generates a single combined recipe identifier source file at <paramref name="targetFilePath"/>.
    /// </summary>
    public static void GenerateToFile(string jsonContent, string targetFilePath, RecipesGeneratorOptions? options = null)
    {
        string? dir = Path.GetDirectoryName(targetFilePath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        File.WriteAllText(targetFilePath, GenerateSourceCode(jsonContent, options), new UTF8Encoding(false));
    }

    private static List<(string FileName, string ClassName, string Comment, IReadOnlyList<string> Entries, bool IsTagRegistry)> GetPlannedSections(
        RecipeRegistry registry,
        RecipesGeneratorOptions options)
    {
        var sections = new List<(string, string, string, IReadOnlyList<string>, bool)>();

        if (options.IncludeRecipeIds)
        {
            var recipeIds = registry.All.Select(r => r.Id).OrderBy(id => id, StringComparer.Ordinal).ToList();
            sections.Add((
                $"{options.RootClassName}.{options.RecipesClassName}.{options.RecipeIdsClassName}.cs",
                options.RecipeIdsClassName,
                $"Minecraft {options.MinecraftVersion} recipe identifiers.",
                recipeIds,
                false));
        }

        if (options.IncludeTagIds)
        {
            var tagIds = registry.Tags.Keys.OrderBy(id => id, StringComparer.Ordinal).ToList();
            sections.Add((
                $"{options.RootClassName}.{options.RecipesClassName}.{options.TagIdsClassName}.cs",
                options.TagIdsClassName,
                $"Minecraft {options.MinecraftVersion} recipe item tag identifiers.",
                tagIds,
                true));
        }

        return sections;
    }

    private static string BuildCombinedJsonPayload(
        string version,
        SortedDictionary<string, JsonNode?> itemTags,
        SortedDictionary<string, JsonNode?> recipes)
    {
        var root = new JsonObject
        {
            ["version"] = version,
            ["item_tags"] = new JsonObject(itemTags.Select(kvp => KeyValuePair.Create(kvp.Key, kvp.Value))),
            ["recipes"] = new JsonObject(recipes.Select(kvp => KeyValuePair.Create(kvp.Key, kvp.Value)))
        };

        return root.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
    }
}
