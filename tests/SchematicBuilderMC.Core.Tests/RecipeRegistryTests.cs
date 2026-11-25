using SchematicBuilderMC.Registries;

namespace SchematicBuilderMC.Core.Tests;

public sealed class RecipeRegistryTests
{
    [Fact]
    public void BundledRecipes_LoadsAllMinecraft26_3RecipesAndTags()
    {
        Assert.Equal("26.3", Minecraft.Recipes.Registry.Version);
        Assert.Equal(2042, Minecraft.Recipes.Count);
        Assert.Equal(Minecraft.Recipes.Count, Minecraft.Recipes.All.Count);
        Assert.Equal(236, Minecraft.Recipes.Tags.Count);
    }

    [Fact]
    public void GetById_ReturnsShapedRecipeWithPatternKeyAndCounts()
    {
        Assert.True(Minecraft.Recipes.IsValid("hopper"));
        Assert.True(Minecraft.Recipes.IsValid(Minecraft.Items.Hopper));
        Assert.False(Minecraft.Recipes.IsValid("nonexistent_recipe_xyz"));

        var hopper = Minecraft.Recipes.GetById("hopper");
        Assert.Equal("minecraft:hopper", hopper.Id);
        Assert.Equal(RecipeType.CraftingShaped, hopper.Type);
        Assert.Equal("redstone", hopper.Category);
        Assert.Equal(Minecraft.Items.Hopper, hopper.ResultItemId);
        Assert.Equal(1, hopper.ResultCount);
        Assert.NotNull(hopper.Pattern);
        Assert.NotNull(hopper.Key);

        var counts = hopper.GetIngredientCounts();
        Assert.Equal(5, counts[new RecipeIngredient([Minecraft.Items.IronIngot])]);
        Assert.Equal(1, counts[new RecipeIngredient([Minecraft.Items.Chest])]);
    }

    [Fact]
    public void FindByResult_ReturnsMultipleCraftingAndCookingMethods()
    {
        var driedKelpRecipes = Minecraft.Recipes.FindByResult(Minecraft.Items.DriedKelp);
        Assert.Contains(driedKelpRecipes, r => r.Type == RecipeType.Smelting && r.Ingredients.Any(i => i.Matches(Minecraft.Items.Kelp)));
        Assert.Contains(driedKelpRecipes, r => r.Type == RecipeType.Smoking && r.Ingredients.Any(i => i.Matches(Minecraft.Items.Kelp)));
        Assert.Contains(driedKelpRecipes, r => r.Type == RecipeType.CampfireCooking && r.Ingredients.Any(i => i.Matches(Minecraft.Items.Kelp)));
        Assert.Contains(driedKelpRecipes, r => r.Type == RecipeType.CraftingShapeless && r.Ingredients.Any(i => i.Matches(Minecraft.Items.DriedKelpBlock)));
    }

    [Fact]
    public void FindByIngredient_ExpandsItemTagsByDefaultAndSupportsExactMode()
    {
        IReadOnlySet<string> planks = Minecraft.Recipes.ResolveTag("planks");
        Assert.Contains(Minecraft.Items.OakPlanks, planks);
        Assert.Contains(Minecraft.Items.BambooPlanks, planks);

        var expanded = Minecraft.Recipes.FindByIngredient(Minecraft.Items.OakPlanks, expandTags: true);
        var exactOnly = Minecraft.Recipes.FindByIngredient(Minecraft.Items.OakPlanks, expandTags: false);

        Assert.Contains(expanded, r => r.Id == "minecraft:chest");
        Assert.Contains(expanded, r => r.Id == "minecraft:piston");
        Assert.DoesNotContain(exactOnly, r => r.Id == "minecraft:chest");
        Assert.Contains(exactOnly, r => r.Id == "minecraft:oak_boat");
    }

    [Fact]
    public void Search_SupportsTextAndStructuredFiltersAcrossAllRecipeTypes()
    {
        var redstoneCrafts = Minecraft.Recipes.Search(
            ingredient: Minecraft.Items.Redstone,
            type: RecipeType.CraftingShaped,
            category: "redstone");

        Assert.Contains(redstoneCrafts, r => r.ResultItemId == Minecraft.Items.Repeater);
        Assert.Contains(redstoneCrafts, r => r.ResultItemId == Minecraft.Items.Dropper);
        Assert.Contains(redstoneCrafts, r => r.ResultItemId == Minecraft.Items.Observer);
        Assert.All(redstoneCrafts, r =>
        {
            Assert.Equal(RecipeType.CraftingShaped, r.Type);
            Assert.Equal("redstone", r.Category);
        });

        var queryMatches = Minecraft.Recipes.Search("smoker");
        Assert.True(queryMatches.Count > 0);
        Assert.Equal("minecraft:smoker", queryMatches[0].Id);

        var multiType = Minecraft.Recipes.Search(new RecipeSearchQuery
        {
            Result = Minecraft.Items.IronIngot,
            Types = [RecipeType.Smelting, RecipeType.Blasting]
        });
        Assert.Contains(multiType, r => r.Type == RecipeType.Smelting);
        Assert.Contains(multiType, r => r.Type == RecipeType.Blasting);
        Assert.All(multiType, r => Assert.Equal(Minecraft.Items.IronIngot, r.ResultItemId));

        Assert.NotEmpty(Minecraft.Recipes.FindByType(RecipeType.Stonecutting));
        Assert.NotEmpty(Minecraft.Recipes.FindByType(RecipeType.SmithingTransform));
        Assert.NotEmpty(Minecraft.Recipes.FindByType(RecipeType.Brewing));
        Assert.NotEmpty(Minecraft.Recipes.FindByGroup("planks"));
    }

    [Fact]
    public void FromJson_ResolvesNestedTagsCleanly()
    {
        const string json = """
        {
          "version": "26.3",
          "item_tags": {
            "#minecraft:oak_logs": ["minecraft:oak_log", "minecraft:stripped_oak_log"],
            "#minecraft:logs": ["#minecraft:oak_logs", "minecraft:birch_log"]
          },
          "recipes": {
            "minecraft:charcoal": {
              "type": "minecraft:smelting",
              "category": "misc",
              "cookingtime": 200,
              "experience": 0.15,
              "ingredient": "#minecraft:logs",
              "result": { "id": "minecraft:charcoal" }
            }
          }
        }
        """;

        var custom = RecipeRegistry.FromJson(json);
        Assert.Equal(1, custom.Count);
        Assert.Contains(Minecraft.Items.StrippedOakLog, custom.ResolveTag("#minecraft:logs"));

        var matches = custom.FindByIngredient(Minecraft.Items.StrippedOakLog);
        Assert.Single(matches);
        Assert.Equal("minecraft:charcoal", matches[0].Id);
        Assert.Equal(200, matches[0].CookingTime);
        Assert.Equal(0.15f, matches[0].Experience);
    }
}
