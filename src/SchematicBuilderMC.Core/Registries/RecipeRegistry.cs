using System.Text.Json;

namespace SchematicBuilderMC.Registries;

/// <summary>
/// Searchable registry of Minecraft crafting, smelting, stonecutting, smithing, and brewing recipes
/// along with resolved item tags.
/// </summary>
public sealed class RecipeRegistry
{
    private const string BundledResourceName = "SchematicBuilderMC.Resources.recipes_26_3.json";

    private static readonly Lazy<RecipeRegistry> BundledInstance = new(LoadBundled);

    private static readonly string[] AdditionalIngredientKeys =
    [
        "ingredient",
        "template",
        "base",
        "addition",
        "input",
        "material",
        "reagent",
        "source",
        "target",
        "dye",
        "fuel",
        "shell",
        "star",
        "banner",
        "map",
        "back",
        "front",
        "left",
        "right"
    ];

    private readonly Dictionary<string, MinecraftRecipe> _byId;
    private readonly Dictionary<string, IReadOnlyList<MinecraftRecipe>> _byResult;
    private readonly Dictionary<RecipeType, IReadOnlyList<MinecraftRecipe>> _byType;
    private readonly Dictionary<string, IReadOnlyList<MinecraftRecipe>> _byCategory;
    private readonly Dictionary<string, IReadOnlyList<MinecraftRecipe>> _byGroup;
    private readonly Dictionary<string, IReadOnlyList<MinecraftRecipe>> _byExactIngredient;
    private readonly Dictionary<string, IReadOnlyList<MinecraftRecipe>> _byExpandedIngredient;
    private readonly Dictionary<MinecraftRecipe, IReadOnlySet<string>> _expandedItemsByRecipe;

    private RecipeRegistry(
        string version,
        IReadOnlyList<MinecraftRecipe> recipes,
        IReadOnlyDictionary<string, IReadOnlySet<string>> tags)
    {
        Version = version;
        All = recipes;
        Tags = tags;

        _byId = new Dictionary<string, MinecraftRecipe>(recipes.Count, StringComparer.OrdinalIgnoreCase);
        var byResult = new Dictionary<string, List<MinecraftRecipe>>(StringComparer.OrdinalIgnoreCase);
        var byType = new Dictionary<RecipeType, List<MinecraftRecipe>>();
        var byCategory = new Dictionary<string, List<MinecraftRecipe>>(StringComparer.OrdinalIgnoreCase);
        var byGroup = new Dictionary<string, List<MinecraftRecipe>>(StringComparer.OrdinalIgnoreCase);
        var byExactIngredient = new Dictionary<string, List<MinecraftRecipe>>(StringComparer.OrdinalIgnoreCase);
        var byExpandedIngredient = new Dictionary<string, List<MinecraftRecipe>>(StringComparer.OrdinalIgnoreCase);
        _expandedItemsByRecipe = new Dictionary<MinecraftRecipe, IReadOnlySet<string>>(recipes.Count);

        foreach (MinecraftRecipe recipe in recipes)
        {
            _byId[recipe.Id] = recipe;

            if (recipe.ResultItemId != null)
            {
                AddIndex(byResult, recipe.ResultItemId, recipe);
            }

            if (!byType.TryGetValue(recipe.Type, out var typeList))
            {
                typeList = [];
                byType[recipe.Type] = typeList;
            }

            typeList.Add(recipe);

            if (!string.IsNullOrWhiteSpace(recipe.Category))
            {
                AddIndex(byCategory, recipe.Category, recipe);
            }

            if (!string.IsNullOrWhiteSpace(recipe.Group))
            {
                AddIndex(byGroup, recipe.Group, recipe);
            }

            var exactSeen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var expandedSeen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (RecipeIngredient ingredient in recipe.Ingredients)
            {
                foreach (string value in ingredient.Values)
                {
                    if (exactSeen.Add(value))
                    {
                        AddIndex(byExactIngredient, value, recipe);
                    }

                    if (expandedSeen.Add(value))
                    {
                        AddIndex(byExpandedIngredient, value, recipe);
                    }

                    if (value.StartsWith('#') && tags.TryGetValue(value, out var tagMembers))
                    {
                        foreach (string member in tagMembers)
                        {
                            if (expandedSeen.Add(member))
                            {
                                AddIndex(byExpandedIngredient, member, recipe);
                            }
                        }
                    }
                }
            }

            _expandedItemsByRecipe[recipe] = expandedSeen;
        }

        _byResult = FreezeIndex(byResult);
        _byType = byType.ToDictionary(kvp => kvp.Key, kvp => (IReadOnlyList<MinecraftRecipe>)kvp.Value.AsReadOnly());
        _byCategory = FreezeIndex(byCategory);
        _byGroup = FreezeIndex(byGroup);
        _byExactIngredient = FreezeIndex(byExactIngredient);
        _byExpandedIngredient = FreezeIndex(byExpandedIngredient);
    }

    /// <summary>Default bundled Minecraft 26.3 recipe registry.</summary>
    public static RecipeRegistry Default => BundledInstance.Value;

    /// <summary>Minecraft version represented by this registry.</summary>
    public string Version { get; }

    /// <summary>All registered recipes sorted by identifier.</summary>
    public IReadOnlyList<MinecraftRecipe> All { get; }

    /// <summary>Total number of registered recipes.</summary>
    public int Count => All.Count;

    /// <summary>Flattened item tag map (<c>#minecraft:tag</c> -> resolved concrete item IDs).</summary>
    public IReadOnlyDictionary<string, IReadOnlySet<string>> Tags { get; }

    /// <summary>Normalizes a recipe identifier into <c>minecraft:name</c> form.</summary>
    public static string NormalizeRecipeId(string recipeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(recipeId);
        string normalized = recipeId.Trim().ToLowerInvariant();
        return normalized.Contains(':') ? normalized : "minecraft:" + normalized;
    }

    /// <summary>Normalizes an item (<c>minecraft:name</c>) or item tag (<c>#minecraft:name</c>) identifier.</summary>
    public static string NormalizeItemOrTagIdentifier(string identifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        string trimmed = identifier.Trim().ToLowerInvariant();
        if (trimmed.StartsWith('#'))
        {
            string tagBody = trimmed[1..];
            return tagBody.Contains(':') ? "#" + tagBody : "#minecraft:" + tagBody;
        }

        return trimmed.Contains(':') ? trimmed : "minecraft:" + trimmed;
    }

    /// <summary>Loads the bundled Minecraft 26.3 recipe registry from embedded resources.</summary>
    public static RecipeRegistry LoadBundled() => FromJson(GetBundledRecipeJson());

    /// <summary>Retrieves the bundled Minecraft 26.3 recipe JSON payload.</summary>
    public static string GetBundledRecipeJson()
    {
        var assembly = typeof(RecipeRegistry).Assembly;
        var stream = assembly.GetManifestResourceStream(BundledResourceName)
            ?? assembly.GetManifestResourceStream("SchematicBuilderMC.Core.Resources.recipes_26_3.json");

        if (stream == null)
        {
            string? matching = assembly.GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith("recipes_26_3.json", StringComparison.OrdinalIgnoreCase));
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

        string candidatePath = Path.Combine(AppContext.BaseDirectory, "Resources", "recipes_26_3.json");
        if (File.Exists(candidatePath))
        {
            return File.ReadAllText(candidatePath);
        }

        throw new FileNotFoundException($"Bundled recipe resource '{BundledResourceName}' could not be located.");
    }

    /// <summary>Parses a <see cref="RecipeRegistry"/> from bundled/mcmeta-style recipe JSON.</summary>
    public static RecipeRegistry FromJson(string jsonContent)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jsonContent);

        using var doc = JsonDocument.Parse(jsonContent);
        JsonElement root = doc.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new FormatException("Recipe JSON root must be a JSON object.");
        }

        string version = root.TryGetProperty("version", out var versionProp) && versionProp.ValueKind == JsonValueKind.String
            ? versionProp.GetString() ?? Minecraft.Version
            : Minecraft.Version;

        var rawTags = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        if (root.TryGetProperty("item_tags", out var tagsProp) && tagsProp.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty tagEntry in tagsProp.EnumerateObject())
            {
                string tagKey = NormalizeItemOrTagIdentifier(tagEntry.Name.StartsWith('#') ? tagEntry.Name : "#" + tagEntry.Name);
                var values = new List<string>();
                if (tagEntry.Value.ValueKind == JsonValueKind.Array)
                {
                    foreach (JsonElement val in tagEntry.Value.EnumerateArray())
                    {
                        if (val.ValueKind == JsonValueKind.String)
                        {
                            string? text = val.GetString();
                            if (!string.IsNullOrWhiteSpace(text))
                            {
                                values.Add(NormalizeItemOrTagIdentifier(text));
                            }
                        }
                    }
                }

                rawTags[tagKey] = values;
            }
        }

        var resolvedTags = ResolveAllTags(rawTags);

        JsonElement recipesElement = root.TryGetProperty("recipes", out var recipesProp) && recipesProp.ValueKind == JsonValueKind.Object
            ? recipesProp
            : root;

        var recipes = new List<MinecraftRecipe>();
        foreach (JsonProperty entry in recipesElement.EnumerateObject())
        {
            if (entry.Value.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            recipes.Add(ParseRecipe(NormalizeRecipeId(entry.Name), entry.Value));
        }

        recipes.Sort((a, b) => string.Compare(a.Id, b.Id, StringComparison.Ordinal));
        return new RecipeRegistry(version, recipes.AsReadOnly(), resolvedTags);
    }

    /// <summary>Determines whether the specified recipe identifier exists in this registry.</summary>
    public bool IsValid(string? recipeId)
    {
        if (string.IsNullOrWhiteSpace(recipeId))
        {
            return false;
        }

        return _byId.ContainsKey(NormalizeRecipeId(recipeId));
    }

    /// <summary>Tries to retrieve a recipe by its identifier (e.g. <c>hopper</c> or <c>minecraft:hopper</c>).</summary>
    public bool TryGetById(string recipeId, out MinecraftRecipe? recipe)
    {
        if (string.IsNullOrWhiteSpace(recipeId))
        {
            recipe = null;
            return false;
        }

        return _byId.TryGetValue(NormalizeRecipeId(recipeId), out recipe);
    }

    /// <summary>Retrieves a recipe by identifier or throws <see cref="KeyNotFoundException"/>.</summary>
    public MinecraftRecipe GetById(string recipeId)
    {
        if (TryGetById(recipeId, out MinecraftRecipe? recipe) && recipe != null)
        {
            return recipe;
        }

        throw new KeyNotFoundException($"Recipe '{recipeId}' was not found in the Minecraft {Version} recipe registry.");
    }

    /// <summary>Resolves an item tag (e.g. <c>#minecraft:planks</c>) to all concrete item IDs it contains.</summary>
    public IReadOnlySet<string> ResolveTag(string tagId)
    {
        if (string.IsNullOrWhiteSpace(tagId))
        {
            return new HashSet<string>();
        }

        string normalized = NormalizeItemOrTagIdentifier("#" + tagId.Trim().TrimStart('#'));
        return Tags.TryGetValue(normalized, out var members) ? members : new HashSet<string>();
    }

    /// <summary>Finds all recipes producing the given item identifier.</summary>
    public IReadOnlyList<MinecraftRecipe> FindByResult(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            return [];
        }

        return _byResult.TryGetValue(NormalizeItemOrTagIdentifier(itemId), out var list) ? list : [];
    }

    /// <summary>Finds all recipes using the specified item or tag identifier.</summary>
    public IReadOnlyList<MinecraftRecipe> FindByIngredient(string itemOrTagId, bool expandTags = true)
    {
        if (string.IsNullOrWhiteSpace(itemOrTagId))
        {
            return [];
        }

        string normalized = NormalizeItemOrTagIdentifier(itemOrTagId);
        var index = expandTags ? _byExpandedIngredient : _byExactIngredient;
        return index.TryGetValue(normalized, out var list) ? list : [];
    }

    /// <summary>Finds all recipes of the specified <see cref="RecipeType"/>.</summary>
    public IReadOnlyList<MinecraftRecipe> FindByType(RecipeType type) =>
        _byType.TryGetValue(type, out var list) ? list : [];

    /// <summary>Finds all recipes belonging to the specified recipe book category.</summary>
    public IReadOnlyList<MinecraftRecipe> FindByCategory(string category)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            return [];
        }

        return _byCategory.TryGetValue(category.Trim(), out var list) ? list : [];
    }

    /// <summary>Finds all recipes belonging to the specified recipe book group.</summary>
    public IReadOnlyList<MinecraftRecipe> FindByGroup(string group)
    {
        if (string.IsNullOrWhiteSpace(group))
        {
            return [];
        }

        return _byGroup.TryGetValue(group.Trim(), out var list) ? list : [];
    }

    /// <summary>
    /// Searches recipes using optional text query, result item, ingredient (with optional tag expansion), type, category, and group filters.
    /// </summary>
    public IReadOnlyList<MinecraftRecipe> Search(
        string? query = null,
        string? result = null,
        string? ingredient = null,
        RecipeType? type = null,
        string? category = null,
        string? group = null,
        bool expandTags = true) =>
        Search(new RecipeSearchQuery
        {
            Query = query,
            Result = result,
            Ingredient = ingredient,
            Type = type,
            Category = category,
            Group = group,
            ExpandTags = expandTags
        });

    /// <summary>
    /// Searches recipes matching all specified criteria in <paramref name="query"/>.
    /// </summary>
    public IReadOnlyList<MinecraftRecipe> Search(RecipeSearchQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);

        IEnumerable<MinecraftRecipe> candidates = GetInitialCandidates(query);

        string? normalizedResult = string.IsNullOrWhiteSpace(query.Result)
            ? null
            : NormalizeItemOrTagIdentifier(query.Result);
        string? normalizedIngredient = string.IsNullOrWhiteSpace(query.Ingredient)
            ? null
            : NormalizeItemOrTagIdentifier(query.Ingredient);
        string? trimmedQuery = string.IsNullOrWhiteSpace(query.Query) ? null : query.Query.Trim();
        string? normalizedQueryId = trimmedQuery != null && !trimmedQuery.Contains(' ')
            ? NormalizeItemOrTagIdentifier(trimmedQuery)
            : null;

        var matched = new List<(MinecraftRecipe Recipe, int Rank)>();
        foreach (MinecraftRecipe recipe in candidates)
        {
            if (query.Type.HasValue && recipe.Type != query.Type.Value)
            {
                continue;
            }

            if (query.Types != null && query.Types.Count > 0 && !query.Types.Contains(recipe.Type))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(query.Category) &&
                !string.Equals(recipe.Category, query.Category.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(query.Group) &&
                !string.Equals(recipe.Group, query.Group.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (normalizedResult != null &&
                !string.Equals(recipe.ResultItemId, normalizedResult, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (normalizedIngredient != null && !MatchesIngredientIndex(recipe, normalizedIngredient, query.ExpandTags))
            {
                continue;
            }

            int rank = 0;
            if (trimmedQuery != null)
            {
                rank = ScoreQueryMatch(recipe, trimmedQuery, normalizedQueryId, query.ExpandTags);
                if (rank < 0)
                {
                    continue;
                }
            }

            matched.Add((recipe, rank));
        }

        if (trimmedQuery != null)
        {
            matched.Sort((a, b) =>
            {
                int cmp = a.Rank.CompareTo(b.Rank);
                return cmp != 0 ? cmp : string.Compare(a.Recipe.Id, b.Recipe.Id, StringComparison.Ordinal);
            });
        }

        return matched.Select(m => m.Recipe).ToArray();
    }

    private IEnumerable<MinecraftRecipe> GetInitialCandidates(RecipeSearchQuery query)
    {
        if (!string.IsNullOrWhiteSpace(query.Result))
        {
            return FindByResult(query.Result);
        }

        if (!string.IsNullOrWhiteSpace(query.Ingredient))
        {
            return FindByIngredient(query.Ingredient, query.ExpandTags);
        }

        if (query.Type.HasValue)
        {
            return FindByType(query.Type.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Category))
        {
            return FindByCategory(query.Category);
        }

        if (!string.IsNullOrWhiteSpace(query.Group))
        {
            return FindByGroup(query.Group);
        }

        return All;
    }

    private bool MatchesIngredientIndex(MinecraftRecipe recipe, string normalizedIngredient, bool expandTags)
    {
        if (expandTags)
        {
            return _expandedItemsByRecipe.TryGetValue(recipe, out var expanded) && expanded.Contains(normalizedIngredient);
        }

        return recipe.UsesIngredient(normalizedIngredient, tagResolver: null);
    }

    private int ScoreQueryMatch(MinecraftRecipe recipe, string rawQuery, string? normalizedQueryId, bool expandTags)
    {
        if (normalizedQueryId != null)
        {
            if (string.Equals(recipe.Id, normalizedQueryId, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(recipe.ResultItemId, normalizedQueryId, StringComparison.OrdinalIgnoreCase))
            {
                return 0;
            }
        }

        if (recipe.ResultItemId != null && recipe.ResultItemId.Contains(rawQuery, StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        if (recipe.Id.Contains(rawQuery, StringComparison.OrdinalIgnoreCase))
        {
            return 2;
        }

        if ((recipe.Group != null && recipe.Group.Contains(rawQuery, StringComparison.OrdinalIgnoreCase)) ||
            (recipe.Category != null && recipe.Category.Contains(rawQuery, StringComparison.OrdinalIgnoreCase)) ||
            recipe.RawType.Contains(rawQuery, StringComparison.OrdinalIgnoreCase))
        {
            return 3;
        }

        if (expandTags && _expandedItemsByRecipe.TryGetValue(recipe, out var expanded))
        {
            foreach (string item in expanded)
            {
                if (item.Contains(rawQuery, StringComparison.OrdinalIgnoreCase))
                {
                    return 4;
                }
            }
        }
        else
        {
            foreach (RecipeIngredient ingredient in recipe.Ingredients)
            {
                foreach (string value in ingredient.Values)
                {
                    if (value.Contains(rawQuery, StringComparison.OrdinalIgnoreCase))
                    {
                        return 4;
                    }
                }
            }
        }

        return -1;
    }

    private static MinecraftRecipe ParseRecipe(string id, JsonElement obj)
    {
        string rawType = obj.TryGetProperty("type", out var typeProp) && typeProp.ValueKind == JsonValueKind.String
            ? NormalizeRecipeId(typeProp.GetString() ?? "other")
            : "minecraft:other";
        RecipeType recipeType = MapRecipeType(rawType);

        string? category = obj.TryGetProperty("category", out var catProp) && catProp.ValueKind == JsonValueKind.String
            ? catProp.GetString()
            : null;
        string? group = obj.TryGetProperty("group", out var groupProp) && groupProp.ValueKind == JsonValueKind.String
            ? groupProp.GetString()
            : null;

        float? experience = obj.TryGetProperty("experience", out var expProp) && expProp.ValueKind == JsonValueKind.Number
            ? expProp.GetSingle()
            : null;
        int? cookingTime = obj.TryGetProperty("cookingtime", out var timeProp) && timeProp.ValueKind == JsonValueKind.Number
            ? timeProp.GetInt32()
            : null;

        (string? resultItemId, int resultCount) = ParseResult(obj);

        IReadOnlyList<string>? pattern = null;
        IReadOnlyDictionary<char, RecipeIngredient>? keyMap = null;
        var ingredients = new List<RecipeIngredient>();

        if (recipeType == RecipeType.CraftingShaped &&
            obj.TryGetProperty("pattern", out var patternProp) &&
            patternProp.ValueKind == JsonValueKind.Array &&
            obj.TryGetProperty("key", out var keyProp) &&
            keyProp.ValueKind == JsonValueKind.Object)
        {
            var rows = new List<string>();
            foreach (JsonElement row in patternProp.EnumerateArray())
            {
                if (row.ValueKind == JsonValueKind.String)
                {
                    rows.Add(row.GetString() ?? string.Empty);
                }
            }

            var keys = new Dictionary<char, RecipeIngredient>();
            foreach (JsonProperty entry in keyProp.EnumerateObject())
            {
                if (entry.Name.Length > 0 && TryParseIngredient(entry.Value, out RecipeIngredient? ing) && ing != null)
                {
                    keys[entry.Name[0]] = ing;
                }
            }

            foreach (string row in rows)
            {
                foreach (char ch in row)
                {
                    if (ch != ' ' && keys.TryGetValue(ch, out RecipeIngredient? slotIng))
                    {
                        ingredients.Add(slotIng);
                    }
                }
            }

            pattern = rows.AsReadOnly();
            keyMap = keys;
        }
        else if (obj.TryGetProperty("ingredients", out var ingsProp) && ingsProp.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement item in ingsProp.EnumerateArray())
            {
                if (TryParseIngredient(item, out RecipeIngredient? ing) && ing != null)
                {
                    ingredients.Add(ing);
                }
            }
        }
        else
        {
            foreach (string field in AdditionalIngredientKeys)
            {
                if (obj.TryGetProperty(field, out var ingElem) &&
                    TryParseIngredient(ingElem, out RecipeIngredient? ing) &&
                    ing != null)
                {
                    ingredients.Add(ing);
                }
            }

            if (obj.TryGetProperty("shapes", out var shapesProp) && shapesProp.ValueKind == JsonValueKind.Object)
            {
                foreach (JsonProperty shapeEntry in shapesProp.EnumerateObject())
                {
                    if (TryParseIngredient(shapeEntry.Value, out RecipeIngredient? ing) && ing != null)
                    {
                        ingredients.Add(ing);
                    }
                }
            }
        }

        return new MinecraftRecipe(
            id,
            recipeType,
            rawType,
            category,
            group,
            resultItemId,
            resultCount,
            pattern,
            keyMap,
            ingredients.AsReadOnly(),
            experience,
            cookingTime);
    }

    private static (string? ItemId, int Count) ParseResult(JsonElement obj)
    {
        JsonElement target;
        if (obj.TryGetProperty("result", out var resultProp) && resultProp.ValueKind == JsonValueKind.Object)
        {
            target = resultProp;
        }
        else if (obj.TryGetProperty("output", out var outputProp) && outputProp.ValueKind == JsonValueKind.Object)
        {
            target = outputProp;
        }
        else
        {
            return (null, 1);
        }

        string? itemId = target.TryGetProperty("id", out var idProp) && idProp.ValueKind == JsonValueKind.String
            ? idProp.GetString()
            : null;
        if (!string.IsNullOrWhiteSpace(itemId))
        {
            itemId = NormalizeItemOrTagIdentifier(itemId);
        }
        else
        {
            itemId = null;
        }

        int count = target.TryGetProperty("count", out var countProp) && countProp.ValueKind == JsonValueKind.Number
            ? Math.Max(1, countProp.GetInt32())
            : 1;

        return (itemId, count);
    }

    private static bool TryParseIngredient(JsonElement element, out RecipeIngredient? ingredient)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                {
                    string? value = element.GetString();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        ingredient = new RecipeIngredient([NormalizeItemOrTagIdentifier(value)]);
                        return true;
                    }

                    break;
                }
            case JsonValueKind.Array:
                {
                    var values = new List<string>();
                    foreach (JsonElement item in element.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.String)
                        {
                            string? value = item.GetString();
                            if (!string.IsNullOrWhiteSpace(value))
                            {
                                values.Add(NormalizeItemOrTagIdentifier(value));
                            }
                        }
                        else if (item.ValueKind == JsonValueKind.Object && TryExtractObjectIngredient(item, out string? objVal) && objVal != null)
                        {
                            values.Add(objVal);
                        }
                    }

                    if (values.Count > 0)
                    {
                        ingredient = new RecipeIngredient(values.AsReadOnly());
                        return true;
                    }

                    break;
                }
            case JsonValueKind.Object:
                {
                    if (TryExtractObjectIngredient(element, out string? objVal) && objVal != null)
                    {
                        ingredient = new RecipeIngredient([objVal]);
                        return true;
                    }

                    break;
                }
        }

        ingredient = null;
        return false;
    }

    private static bool TryExtractObjectIngredient(JsonElement element, out string? value)
    {
        if (element.TryGetProperty("item", out var itemProp) && itemProp.ValueKind == JsonValueKind.String)
        {
            string? str = itemProp.GetString();
            if (!string.IsNullOrWhiteSpace(str))
            {
                value = NormalizeItemOrTagIdentifier(str);
                return true;
            }
        }

        if (element.TryGetProperty("tag", out var tagProp) && tagProp.ValueKind == JsonValueKind.String)
        {
            string? str = tagProp.GetString();
            if (!string.IsNullOrWhiteSpace(str))
            {
                string prefixed = str.StartsWith('#') ? str : "#" + str;
                value = NormalizeItemOrTagIdentifier(prefixed);
                return true;
            }
        }

        value = null;
        return false;
    }

    private static RecipeType MapRecipeType(string rawType) => rawType switch
    {
        "minecraft:crafting_shaped" => RecipeType.CraftingShaped,
        "minecraft:crafting_shapeless" => RecipeType.CraftingShapeless,
        "minecraft:crafting_transmute" => RecipeType.CraftingTransmute,
        "minecraft:crafting_dye" => RecipeType.CraftingDye,
        "minecraft:crafting_decorated_pot" => RecipeType.CraftingDecoratedPot,
        "minecraft:crafting_imbue" => RecipeType.CraftingImbue,
        "minecraft:smelting" => RecipeType.Smelting,
        "minecraft:blasting" => RecipeType.Blasting,
        "minecraft:smoking" => RecipeType.Smoking,
        "minecraft:campfire_cooking" => RecipeType.CampfireCooking,
        "minecraft:stonecutting" => RecipeType.Stonecutting,
        "minecraft:smithing_transform" => RecipeType.SmithingTransform,
        "minecraft:smithing_trim" => RecipeType.SmithingTrim,
        "minecraft:brewing" => RecipeType.Brewing,
        _ when rawType.StartsWith("minecraft:crafting_special_", StringComparison.OrdinalIgnoreCase) => RecipeType.CraftingSpecial,
        _ => RecipeType.Other
    };

    private static IReadOnlyDictionary<string, IReadOnlySet<string>> ResolveAllTags(Dictionary<string, List<string>> rawTags)
    {
        var resolved = new Dictionary<string, IReadOnlySet<string>>(rawTags.Count, StringComparer.OrdinalIgnoreCase);

        foreach (string tagKey in rawTags.Keys)
        {
            var visiting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            resolved[tagKey] = ResolveTagRecursive(tagKey, rawTags, resolved, visiting);
        }

        return resolved;
    }

    private static IReadOnlySet<string> ResolveTagRecursive(
        string tagKey,
        Dictionary<string, List<string>> rawTags,
        Dictionary<string, IReadOnlySet<string>> cache,
        HashSet<string> visiting)
    {
        if (cache.TryGetValue(tagKey, out var cached))
        {
            return cached;
        }

        if (!visiting.Add(tagKey) || !rawTags.TryGetValue(tagKey, out var entries))
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        var items = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string entry in entries)
        {
            if (entry.StartsWith('#'))
            {
                foreach (string nested in ResolveTagRecursive(entry, rawTags, cache, visiting))
                {
                    items.Add(nested);
                }
            }
            else
            {
                items.Add(entry);
            }
        }

        visiting.Remove(tagKey);
        cache[tagKey] = items;
        return items;
    }

    private static void AddIndex(Dictionary<string, List<MinecraftRecipe>> index, string key, MinecraftRecipe recipe)
    {
        if (!index.TryGetValue(key, out var list))
        {
            list = [];
            index[key] = list;
        }

        list.Add(recipe);
    }

    private static Dictionary<string, IReadOnlyList<MinecraftRecipe>> FreezeIndex(Dictionary<string, List<MinecraftRecipe>> source) =>
        source.ToDictionary(
            kvp => kvp.Key,
            kvp => (IReadOnlyList<MinecraftRecipe>)kvp.Value.AsReadOnly(),
            StringComparer.OrdinalIgnoreCase);
}
