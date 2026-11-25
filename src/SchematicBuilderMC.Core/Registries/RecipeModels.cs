namespace SchematicBuilderMC.Registries;

/// <summary>
/// Categories of Minecraft crafting and processing recipes.
/// </summary>
public enum RecipeType
{
    CraftingShaped,
    CraftingShapeless,
    CraftingTransmute,
    CraftingDye,
    CraftingDecoratedPot,
    CraftingImbue,
    CraftingSpecial,
    Smelting,
    Blasting,
    Smoking,
    CampfireCooking,
    Stonecutting,
    SmithingTransform,
    SmithingTrim,
    Brewing,
    Other
}

/// <summary>
/// Represents an ingredient slot in a recipe: a single item ID, an item tag (<c>#minecraft:...</c>),
/// or a list of accepted item/tag alternatives.
/// </summary>
public sealed class RecipeIngredient : IEquatable<RecipeIngredient>
{
    internal RecipeIngredient(IReadOnlyList<string> values)
    {
        Values = values;
    }

    /// <summary>
    /// Normalized item or tag identifiers accepted by this ingredient slot.
    /// </summary>
    public IReadOnlyList<string> Values { get; }

    /// <summary>
    /// Whether this ingredient is a single item tag reference (starts with <c>#</c>).
    /// </summary>
    public bool IsTag => Values.Count == 1 && Values[0].StartsWith('#');

    /// <summary>
    /// Primary identifier for single-entry ingredients (empty if empty).
    /// </summary>
    public string Primary => Values.Count > 0 ? Values[0] : string.Empty;

    /// <summary>
    /// Determines whether this ingredient matches the normalized item or tag identifier, optionally expanding tags.
    /// </summary>
    public bool Matches(string itemOrTagId, Func<string, IReadOnlySet<string>>? tagResolver = null)
    {
        if (string.IsNullOrWhiteSpace(itemOrTagId))
        {
            return false;
        }

        string normalized = RecipeRegistry.NormalizeItemOrTagIdentifier(itemOrTagId);

        foreach (string candidate in Values)
        {
            if (string.Equals(candidate, normalized, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (candidate.StartsWith('#') && tagResolver != null && !normalized.StartsWith('#'))
            {
                if (tagResolver(candidate).Contains(normalized))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public bool Equals(RecipeIngredient? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (Values.Count != other.Values.Count)
        {
            return false;
        }

        for (int i = 0; i < Values.Count; i++)
        {
            if (!string.Equals(Values[i], other.Values[i], StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }

    public override bool Equals(object? obj) => Equals(obj as RecipeIngredient);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (string value in Values)
        {
            hash.Add(value, StringComparer.OrdinalIgnoreCase);
        }

        return hash.ToHashCode();
    }

    public override string ToString() => Values.Count == 1 ? Values[0] : $"[{string.Join(", ", Values)}]";
}

/// <summary>
/// Represents a parsed Minecraft crafting, smelting, stonecutting, smithing, or brewing recipe.
/// </summary>
public sealed class MinecraftRecipe
{
    internal MinecraftRecipe(
        string id,
        RecipeType type,
        string rawType,
        string? category,
        string? group,
        string? resultItemId,
        int resultCount,
        IReadOnlyList<string>? pattern,
        IReadOnlyDictionary<char, RecipeIngredient>? key,
        IReadOnlyList<RecipeIngredient> ingredients,
        float? experience,
        int? cookingTime)
    {
        Id = id;
        Type = type;
        RawType = rawType;
        Category = category;
        Group = group;
        ResultItemId = resultItemId;
        ResultCount = resultCount;
        Pattern = pattern;
        Key = key;
        Ingredients = ingredients;
        Experience = experience;
        CookingTime = cookingTime;
    }

    /// <summary>Namespaced recipe identifier (e.g. <c>minecraft:hopper</c>).</summary>
    public string Id { get; }

    /// <summary>Parsed recipe category enum.</summary>
    public RecipeType Type { get; }

    /// <summary>Raw namespaced Minecraft recipe type string (e.g. <c>minecraft:crafting_shaped</c>).</summary>
    public string RawType { get; }

    /// <summary>Optional recipe book category (e.g. <c>redstone</c>, <c>building</c>, <c>food</c>).</summary>
    public string? Category { get; }

    /// <summary>Optional recipe book group identifier.</summary>
    public string? Group { get; }

    /// <summary>Produced item identifier, or <c>null</c> for dynamic recipes without a fixed output ID.</summary>
    public string? ResultItemId { get; }

    /// <summary>Number of result items produced per craft (defaults to <c>1</c>).</summary>
    public int ResultCount { get; }

    /// <summary>Crafting grid row patterns for <see cref="RecipeType.CraftingShaped"/> recipes.</summary>
    public IReadOnlyList<string>? Pattern { get; }

    /// <summary>Pattern symbol-to-ingredient mapping for <see cref="RecipeType.CraftingShaped"/> recipes.</summary>
    public IReadOnlyDictionary<char, RecipeIngredient>? Key { get; }

    /// <summary>
    /// Ordered input ingredients consumed by this recipe (for shaped recipes, one entry per non-space grid cell in row-major order).
    /// </summary>
    public IReadOnlyList<RecipeIngredient> Ingredients { get; }

    /// <summary>Experience awarded for furnace/cooking recipes.</summary>
    public float? Experience { get; }

    /// <summary>Cooking duration in game ticks for furnace/cooking recipes.</summary>
    public int? CookingTime { get; }

    /// <summary>
    /// Aggregates <see cref="Ingredients"/> into counts per distinct <see cref="RecipeIngredient"/>.
    /// </summary>
    public IReadOnlyDictionary<RecipeIngredient, int> GetIngredientCounts()
    {
        var counts = new Dictionary<RecipeIngredient, int>();
        foreach (RecipeIngredient ingredient in Ingredients)
        {
            counts[ingredient] = counts.TryGetValue(ingredient, out int existing) ? existing + 1 : 1;
        }

        return counts;
    }

    /// <summary>
    /// Determines whether this recipe uses the specified item or tag ID.
    /// </summary>
    public bool UsesIngredient(string itemOrTagId, Func<string, IReadOnlySet<string>>? tagResolver = null) =>
        Ingredients.Any(i => i.Matches(itemOrTagId, tagResolver));

    public override string ToString() =>
        ResultItemId != null ? $"{Id} -> {ResultCount}x {ResultItemId} ({RawType})" : $"{Id} ({RawType})";
}

/// <summary>
/// Filter criteria for searching <see cref="RecipeRegistry"/>.
/// </summary>
public sealed class RecipeSearchQuery
{
    /// <summary>
    /// Case-insensitive text or identifier query matched against recipe ID, result ID, category, group, or ingredients.
    /// </summary>
    public string? Query { get; init; }

    /// <summary>Filter to recipes producing this item identifier (exact match after normalization).</summary>
    public string? Result { get; init; }

    /// <summary>Filter to recipes using this item or tag identifier.</summary>
    public string? Ingredient { get; init; }

    /// <summary>Optional single recipe type filter.</summary>
    public RecipeType? Type { get; init; }

    /// <summary>Optional set of allowed recipe types.</summary>
    public IReadOnlyCollection<RecipeType>? Types { get; init; }

    /// <summary>Optional recipe book category filter (case-insensitive).</summary>
    public string? Category { get; init; }

    /// <summary>Optional recipe book group filter (case-insensitive).</summary>
    public string? Group { get; init; }

    /// <summary>
    /// When <c>true</c> (default), expands item tags (such as <c>#minecraft:planks</c>) when matching ingredients and text queries.
    /// </summary>
    public bool ExpandTags { get; init; } = true;
}
