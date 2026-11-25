using SchematicBuilderMC.Models;

namespace SchematicBuilderMC.Registries;

/// <summary>
/// Convenience extension methods for querying recipes directly from block/item identifiers,
/// <see cref="BlockState"/> instances, and <see cref="ItemStack"/> instances.
/// </summary>
public static class RecipeExtensions
{
    /// <summary>
    /// Returns all recipes that require this block, item, or tag identifier as an ingredient.
    /// </summary>
    /// <param name="blockOrItemOrTagId">
    /// Block ID (e.g. <see cref="Minecraft.Blocks.RedstoneBlock"/>), block state string, item ID, or item tag ID.
    /// </param>
    /// <param name="expandTags">When <c>true</c> (default), includes recipes whose ingredient tags resolve to this item.</param>
    /// <param name="type">Optional recipe category filter.</param>
    public static IReadOnlyList<MinecraftRecipe> RequiredForRecipes(
        this string? blockOrItemOrTagId,
        bool expandTags = true,
        RecipeType? type = null)
    {
        if (string.IsNullOrWhiteSpace(blockOrItemOrTagId))
        {
            return [];
        }

        string identifier = ExtractIdentifier(blockOrItemOrTagId);
        return FilterByType(Minecraft.Recipes.FindByIngredient(identifier, expandTags), type);
    }

    /// <summary>
    /// Returns all recipes that require this <see cref="BlockState"/>'s block as an ingredient.
    /// </summary>
    public static IReadOnlyList<MinecraftRecipe> RequiredForRecipes(
        this BlockState blockState,
        bool expandTags = true,
        RecipeType? type = null)
    {
        ArgumentNullException.ThrowIfNull(blockState);
        return blockState.Identifier.RequiredForRecipes(expandTags, type);
    }

    /// <summary>
    /// Returns all recipes that require this <see cref="ItemStack"/>'s item as an ingredient.
    /// </summary>
    public static IReadOnlyList<MinecraftRecipe> RequiredForRecipes(
        this ItemStack itemStack,
        bool expandTags = true,
        RecipeType? type = null)
    {
        ArgumentNullException.ThrowIfNull(itemStack);
        return itemStack.Id.RequiredForRecipes(expandTags, type);
    }

    /// <summary>
    /// Returns all recipes that produce this block or item identifier as their result.
    /// </summary>
    /// <param name="blockOrItemId">Block ID (e.g. <see cref="Minecraft.Blocks.RedstoneBlock"/>), block state string, or item ID.</param>
    /// <param name="type">Optional recipe category filter.</param>
    public static IReadOnlyList<MinecraftRecipe> GetRecipes(
        this string? blockOrItemId,
        RecipeType? type = null)
    {
        if (string.IsNullOrWhiteSpace(blockOrItemId))
        {
            return [];
        }

        string identifier = ExtractIdentifier(blockOrItemId);
        return FilterByType(Minecraft.Recipes.FindByResult(identifier), type);
    }

    /// <summary>
    /// Returns all recipes that produce this <see cref="BlockState"/>'s block as their result.
    /// </summary>
    public static IReadOnlyList<MinecraftRecipe> GetRecipes(
        this BlockState blockState,
        RecipeType? type = null)
    {
        ArgumentNullException.ThrowIfNull(blockState);
        return blockState.Identifier.GetRecipes(type);
    }

    /// <summary>
    /// Returns all recipes that produce this <see cref="ItemStack"/>'s item as their result.
    /// </summary>
    public static IReadOnlyList<MinecraftRecipe> GetRecipes(
        this ItemStack itemStack,
        RecipeType? type = null)
    {
        ArgumentNullException.ThrowIfNull(itemStack);
        return itemStack.Id.GetRecipes(type);
    }

    private static string ExtractIdentifier(string blockOrItemOrTagId)
    {
        int bracketIndex = blockOrItemOrTagId.IndexOf('[');
        return bracketIndex >= 0 ? blockOrItemOrTagId[..bracketIndex] : blockOrItemOrTagId;
    }

    private static IReadOnlyList<MinecraftRecipe> FilterByType(IReadOnlyList<MinecraftRecipe> recipes, RecipeType? type)
    {
        if (type is null || recipes.Count == 0)
        {
            return recipes;
        }

        return recipes.Where(recipe => recipe.Type == type.Value).ToArray();
    }
}
