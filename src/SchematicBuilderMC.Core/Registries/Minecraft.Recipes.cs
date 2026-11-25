namespace SchematicBuilderMC.Registries;

public static partial class Minecraft
{
    /// <summary>
    /// Searchable registry of Minecraft 26.3 crafting, smelting, stonecutting, smithing, and brewing recipes.
    /// </summary>
    public static partial class Recipes
    {
        /// <summary>Underlying <see cref="RecipeRegistry"/> instance.</summary>
        public static RecipeRegistry Registry => RecipeRegistry.Default;

        /// <summary>All registered Minecraft 26.3 recipes.</summary>
        public static IReadOnlyList<MinecraftRecipe> All => Registry.All;

        /// <summary>Total number of registered recipes.</summary>
        public static int Count => Registry.Count;

        /// <summary>Flattened item tag map (<c>#minecraft:tag</c> -> concrete item identifiers).</summary>
        public static IReadOnlyDictionary<string, IReadOnlySet<string>> Tags => Registry.Tags;

        /// <summary>Determines whether the specified recipe identifier exists.</summary>
        public static bool IsValid(string? recipeId) => Registry.IsValid(recipeId);

        /// <summary>Tries to retrieve a recipe by identifier (e.g. <c>minecraft:hopper</c> or <c>hopper</c>).</summary>
        public static bool TryGetById(string recipeId, out MinecraftRecipe? recipe) => Registry.TryGetById(recipeId, out recipe);

        /// <summary>Retrieves a recipe by identifier or throws <see cref="KeyNotFoundException"/>.</summary>
        public static MinecraftRecipe GetById(string recipeId) => Registry.GetById(recipeId);

        /// <summary>Resolves an item tag (e.g. <c>#minecraft:planks</c>) to all concrete item IDs it contains.</summary>
        public static IReadOnlySet<string> ResolveTag(string tagId) => Registry.ResolveTag(tagId);

        /// <summary>Finds all recipes producing the given item identifier.</summary>
        public static IReadOnlyList<MinecraftRecipe> FindByResult(string itemId) => Registry.FindByResult(itemId);

        /// <summary>Finds all recipes using the specified item or tag identifier.</summary>
        public static IReadOnlyList<MinecraftRecipe> FindByIngredient(string itemOrTagId, bool expandTags = true) =>
            Registry.FindByIngredient(itemOrTagId, expandTags);

        /// <summary>Finds all recipes of the specified <see cref="RecipeType"/>.</summary>
        public static IReadOnlyList<MinecraftRecipe> FindByType(RecipeType type) => Registry.FindByType(type);

        /// <summary>Finds all recipes belonging to the specified recipe book category.</summary>
        public static IReadOnlyList<MinecraftRecipe> FindByCategory(string category) => Registry.FindByCategory(category);

        /// <summary>Finds all recipes belonging to the specified recipe book group.</summary>
        public static IReadOnlyList<MinecraftRecipe> FindByGroup(string group) => Registry.FindByGroup(group);

        /// <summary>
        /// Searches recipes using optional query text, result item, ingredient (with optional tag expansion), type, category, and group filters.
        /// </summary>
        public static IReadOnlyList<MinecraftRecipe> Search(
            string? query = null,
            string? result = null,
            string? ingredient = null,
            RecipeType? type = null,
            string? category = null,
            string? group = null,
            bool expandTags = true) =>
            Registry.Search(query, result, ingredient, type, category, group, expandTags);

        /// <summary>Searches recipes matching all criteria in <paramref name="query"/>.</summary>
        public static IReadOnlyList<MinecraftRecipe> Search(RecipeSearchQuery query) => Registry.Search(query);
    }
}
