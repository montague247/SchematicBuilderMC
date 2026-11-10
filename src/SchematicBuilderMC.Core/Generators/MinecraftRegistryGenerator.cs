using System.Reflection;
using System.Text;
using System.Text.Json;

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

/// <summary>
/// Code generator that creates strongly-typed Minecraft registry classes (Blocks, Items, Entities, etc.)
/// from Minecraft registry JSON data.
/// </summary>
public static class MinecraftRegistryGenerator
{
    private const string DefaultMcmetaUrlPattern =
        "https://raw.githubusercontent.com/misode/mcmeta/refs/tags/{0}-summary/registries/data.json";

    private static readonly HashSet<string> ReservedObjectMembers = new(StringComparer.Ordinal)
    {
        "Equals", "GetHashCode", "GetType", "ToString"
    };

    private static readonly HashSet<string> CSharpKeywords = new(StringComparer.Ordinal)
    {
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
        "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else",
        "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for",
        "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
        "long", "namespace", "new", "null", "object", "operator", "out", "override", "params",
        "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed",
        "short", "sizeof", "stackalloc", "static", "string", "struct", "switch", "this",
        "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort",
        "using", "virtual", "void", "volatile", "while"
    };

    /// <summary>
    /// Converts a Minecraft snake_case identifier (e.g. "stone_bricks" or "minecraft:stone_bricks")
    /// into a valid PascalCase C# identifier (e.g. "StoneBricks").
    /// </summary>
    public static string ToPascalCase(string identifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);

        // Strip namespace if "minecraft:" or isolate the path
        int colon = identifier.IndexOf(':');
        string name = colon >= 0 ? identifier[(colon + 1)..] : identifier;

        var sb = new StringBuilder();
        var parts = name.Split(new[] { '_', '-', '/', '.' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var part in parts)
        {
            if (part.Length == 0) continue;
            sb.Append(char.ToUpperInvariant(part[0]));
            if (part.Length > 1)
            {
                sb.Append(part[1..]);
            }
        }

        string result = sb.ToString();
        if (result.Length == 0)
        {
            result = "Unknown";
        }

        if (char.IsDigit(result[0]))
        {
            result = "_" + result;
        }

        if (CSharpKeywords.Contains(result))
        {
            result = "@" + result;
        }

        return result;
    }

    /// <summary>
    /// Normalizes a resource location into standard "minecraft:name" format.
    /// </summary>
    public static string NormalizeIdentifier(string identifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        identifier = identifier.Trim().ToLowerInvariant();
        return identifier.Contains(':') ? identifier : "minecraft:" + identifier;
    }

    /// <summary>
    /// Parses registry entries from a JSON string. Supports both MCMeta format and vanilla reports registries.json.
    /// </summary>
    public static Dictionary<string, List<string>> ParseRegistryJson(string jsonContent)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jsonContent);

        using var doc = JsonDocument.Parse(jsonContent);
        var root = doc.RootElement;
        var result = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new FormatException("Registry JSON root must be a JSON object.");
        }

        foreach (var property in root.EnumerateObject())
        {
            string key = property.Name;
            var list = new List<string>();

            if (property.Value.ValueKind == JsonValueKind.Array)
            {
                // Format 1: mcmeta summary list of strings: ["stone", "cobblestone"]
                foreach (var element in property.Value.EnumerateArray())
                {
                    if (element.ValueKind == JsonValueKind.String)
                    {
                        string? str = element.GetString();
                        if (!string.IsNullOrWhiteSpace(str))
                        {
                            list.Add(NormalizeIdentifier(str));
                        }
                    }
                }
            }
            else if (property.Value.ValueKind == JsonValueKind.Object)
            {
                // Format 2: vanilla server reports: {"entries": {"minecraft:air": {...}, ...}}
                if (property.Value.TryGetProperty("entries", out var entriesProp) &&
                    entriesProp.ValueKind == JsonValueKind.Object)
                {
                    foreach (var entry in entriesProp.EnumerateObject())
                    {
                        list.Add(NormalizeIdentifier(entry.Name));
                    }
                }
                else
                {
                    // Format 3: direct key-value object
                    foreach (var entry in property.Value.EnumerateObject())
                    {
                        list.Add(NormalizeIdentifier(entry.Name));
                    }
                }
            }

            list.Sort(StringComparer.Ordinal);
            result[key] = list;
        }

        return result;
    }

    /// <summary>
    /// Retrieves the bundled Minecraft 26.3 registry JSON content embedded in the assembly.
    /// </summary>
    public static string GetBundledRegistryJson()
    {
        var assembly = typeof(MinecraftRegistryGenerator).Assembly;
        const string resourceName = "SchematicBuilderMC.Resources.registries_26_3.json";

        var stream = assembly.GetManifestResourceStream(resourceName)
            ?? assembly.GetManifestResourceStream("SchematicBuilderMC.Core.Resources.registries_26_3.json");

        if (stream == null)
        {
            var matching = assembly.GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith("registries_26_3.json", StringComparison.OrdinalIgnoreCase));
            if (matching != null)
            {
                stream = assembly.GetManifestResourceStream(matching);
            }
        }

        if (stream != null)
        {
            using (stream)
            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }

        // Fallback: search relative to AppContext.BaseDirectory
        string candidatePath = Path.Combine(AppContext.BaseDirectory, "Resources", "registries_26_3.json");
        if (File.Exists(candidatePath))
        {
            return File.ReadAllText(candidatePath);
        }

        throw new FileNotFoundException($"Bundled registry resource '{resourceName}' could not be located.");
    }

    /// <summary>
    /// Downloads registry JSON data for the given Minecraft version from the mcmeta community repository.
    /// </summary>
    public static async Task<string> DownloadRegistryJsonAsync(
        string version = "26.3",
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
            string url = string.Format(DefaultMcmetaUrlPattern, version);
            var response = await httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
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
    /// Generates a complete C# source code string containing all requested Minecraft registries.
    /// </summary>
    public static string GenerateSourceCode(string jsonContent, GeneratorOptions? options = null)
    {
        options ??= new GeneratorOptions();
        var registries = ParseRegistryJson(jsonContent);

        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine($"namespace {options.TargetNamespace};");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// Provides strongly-typed identifiers and registries for Minecraft {options.MinecraftVersion}.");
        sb.AppendLine("/// </summary>");
        sb.AppendLine($"public static partial class {options.RootClassName}");
        sb.AppendLine("{");
        sb.AppendLine($"    /// <summary>The Minecraft version these registries represent.</summary>");
        sb.AppendLine($"    public const string Version = \"{options.MinecraftVersion}\";");
        sb.AppendLine();

        var planned = GetPlannedRegistries(options);
        foreach (var (registryKey, className, comment, aliases) in planned)
        {
            if (TryFindRegistry(registries, registryKey, out var entries))
            {
                AppendRegistryClass(sb, className, comment, entries, options, indent: "    ");
                if (aliases != null)
                {
                    foreach (var alias in aliases)
                    {
                        AppendRegistryClass(sb, alias, $"Alias for <see cref=\"{className}\"/>.", entries, options, indent: "    ");
                    }
                }
            }
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    /// <summary>
    /// Generates a dictionary of separate C# source files (filename -> file content) for partial class layout.
    /// </summary>
    public static Dictionary<string, string> GenerateSourceFiles(string jsonContent, GeneratorOptions? options = null)
    {
        options ??= new GeneratorOptions();
        var registries = ParseRegistryJson(jsonContent);
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // 1. Root Minecraft.cs
        var rootSb = new StringBuilder();
        rootSb.AppendLine("// <auto-generated/>");
        rootSb.AppendLine("#nullable enable");
        rootSb.AppendLine();
        rootSb.AppendLine($"namespace {options.TargetNamespace};");
        rootSb.AppendLine();
        rootSb.AppendLine("/// <summary>");
        rootSb.AppendLine($"/// Provides strongly-typed identifiers and registries for Minecraft {options.MinecraftVersion}.");
        rootSb.AppendLine("/// </summary>");
        rootSb.AppendLine($"public static partial class {options.RootClassName}");
        rootSb.AppendLine("{");
        rootSb.AppendLine($"    /// <summary>The Minecraft version these registries represent.</summary>");
        rootSb.AppendLine($"    public const string Version = \"{options.MinecraftVersion}\";");
        rootSb.AppendLine("}");
        result[$"{options.RootClassName}.cs"] = rootSb.ToString();

        // 2. Individual partial classes
        var planned = GetPlannedRegistries(options);
        foreach (var (registryKey, className, comment, aliases) in planned)
        {
            if (TryFindRegistry(registries, registryKey, out var entries))
            {
                var sb = new StringBuilder();
                sb.AppendLine("// <auto-generated/>");
                sb.AppendLine("#nullable enable");
                sb.AppendLine();
                sb.AppendLine($"namespace {options.TargetNamespace};");
                sb.AppendLine();
                sb.AppendLine($"public static partial class {options.RootClassName}");
                sb.AppendLine("{");

                AppendRegistryClass(sb, className, comment, entries, options, indent: "    ");

                if (aliases != null)
                {
                    foreach (var alias in aliases)
                    {
                        AppendRegistryClass(sb, alias, $"Alias for <see cref=\"{className}\"/>.", entries, options, indent: "    ");
                    }
                }

                sb.AppendLine("}");
                result[$"{options.RootClassName}.{className}.cs"] = sb.ToString();
            }
        }

        return result;
    }

    /// <summary>
    /// Generates registry source files and writes them directly to the specified target directory.
    /// </summary>
    public static void GenerateToDirectory(string jsonContent, string targetDirectory, GeneratorOptions? options = null)
    {
        Directory.CreateDirectory(targetDirectory);
        var files = GenerateSourceFiles(jsonContent, options);
        foreach (var (fileName, content) in files)
        {
            string fullPath = Path.Combine(targetDirectory, fileName);
            File.WriteAllText(fullPath, content, Encoding.UTF8);
        }
    }

    /// <summary>
    /// Generates a single combined registry source file at the specified output path.
    /// </summary>
    public static void GenerateToFile(string jsonContent, string targetFilePath, GeneratorOptions? options = null)
    {
        string? dir = Path.GetDirectoryName(targetFilePath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        string content = GenerateSourceCode(jsonContent, options);
        File.WriteAllText(targetFilePath, content, Encoding.UTF8);
    }

    private static void AppendRegistryClass(
        StringBuilder sb,
        string className,
        string comment,
        List<string> entries,
        GeneratorOptions options,
        string indent)
    {
        sb.AppendLine($"{indent}/// <summary>");
        sb.AppendLine($"{indent}/// {comment}");
        sb.AppendLine($"{indent}/// </summary>");
        sb.AppendLine($"{indent}public static partial class {className}");
        sb.AppendLine($"{indent}{{");

        var constantNames = new List<string>(entries.Count);

        foreach (var entry in entries)
        {
            string propertyName = ToPascalCase(entry);
            constantNames.Add(propertyName);

            string memberModifier = ReservedObjectMembers.Contains(propertyName)
                ? "public new const string"
                : "public const string";

            sb.AppendLine($"{indent}    /// <summary>{entry}</summary>");
            sb.AppendLine($"{indent}    {memberModifier} {propertyName} = \"{entry}\";");
            sb.AppendLine();
        }

        if (options.GenerateCollections)
        {
            sb.AppendLine($"{indent}    /// <summary>All registered identifiers in <see cref=\"{className}\"/>.</summary>");
            sb.AppendLine($"{indent}    public static readonly System.Collections.Generic.IReadOnlyList<string> All = new string[]");
            sb.AppendLine($"{indent}    {{");
            foreach (var constName in constantNames)
            {
                sb.AppendLine($"{indent}        {constName},");
            }
            sb.AppendLine($"{indent}    }};");
            sb.AppendLine();
            sb.AppendLine($"{indent}    private static readonly System.Collections.Generic.HashSet<string> AllSet = new(All, System.StringComparer.OrdinalIgnoreCase);");
            sb.AppendLine();
        }

        if (options.GenerateValidationHelpers)
        {
            sb.AppendLine($"{indent}    /// <summary>Determines whether the specified identifier is a registered member of <see cref=\"{className}\"/>.</summary>");
            sb.AppendLine($"{indent}    public static bool IsValid(string? identifier)");
            sb.AppendLine($"{indent}    {{");
            sb.AppendLine($"{indent}        if (string.IsNullOrWhiteSpace(identifier)) return false;");
            sb.AppendLine($"{indent}        int bracket = identifier.IndexOf('[');");
            sb.AppendLine($"{indent}        if (bracket >= 0) identifier = identifier[..bracket].Trim();");
            sb.AppendLine($"{indent}        if (!identifier.Contains(':')) identifier = \"minecraft:\" + identifier;");
            sb.AppendLine($"{indent}        return AllSet.Contains(identifier);");
            sb.AppendLine($"{indent}    }}");
        }

        sb.AppendLine($"{indent}}}");
        sb.AppendLine();
    }

    private static List<(string RegistryKey, string ClassName, string Comment, string[]? Aliases)> GetPlannedRegistries(GeneratorOptions options)
    {
        var list = new List<(string, string, string, string[]?)>();

        if (options.IncludeBlocks)
        {
            list.Add(("block", "Blocks", $"Minecraft {options.MinecraftVersion} block identifiers.", null));
        }

        if (options.IncludeItems)
        {
            list.Add(("item", "Items", $"Minecraft {options.MinecraftVersion} item identifiers.", null));
        }

        if (options.IncludeBlockEntities)
        {
            list.Add(("block_entity_type", "BlockEntities", $"Minecraft {options.MinecraftVersion} block entity identifiers.", new[] { "BlockEntityTypes" }));
        }

        if (options.IncludeEntities)
        {
            list.Add(("entity_type", "Entities", $"Minecraft {options.MinecraftVersion} entity type identifiers.", new[] { "EntityTypes" }));
        }

        if (options.IncludeBiomes)
        {
            list.Add(("worldgen/biome", "Biomes", $"Minecraft {options.MinecraftVersion} biome identifiers.", null));
        }

        if (options.IncludeFluids)
        {
            list.Add(("fluid", "Fluids", $"Minecraft {options.MinecraftVersion} fluid identifiers.", null));
        }

        if (options.CustomRegistries != null)
        {
            foreach (var (key, customClass) in options.CustomRegistries)
            {
                list.Add((key, customClass, $"Minecraft {options.MinecraftVersion} {customClass} identifiers.", null));
            }
        }

        return list;
    }

    private static bool TryFindRegistry(Dictionary<string, List<string>> dict, string key, out List<string> entries)
    {
        if (dict.TryGetValue(key, out entries!)) return true;
        if (dict.TryGetValue("minecraft:" + key, out entries!)) return true;

        // Try last segment for keys like "worldgen/biome"
        int slash = key.LastIndexOf('/');
        if (slash >= 0 && dict.TryGetValue(key[(slash + 1)..], out entries!)) return true;

        entries = new List<string>();
        return false;
    }
}
