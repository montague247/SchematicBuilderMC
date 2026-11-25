using System.Collections.Concurrent;
using SchematicBuilderMC.Extensions;
using SchematicBuilderMC.Models;
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

    [Fact]
    public void RequiredForRecipes_FromBlockConstantStateAndItemStack_ReturnsMatchingRecipes()
    {
        var redstoneBlockUsages = Minecraft.Blocks.RedstoneBlock.RequiredForRecipes();
        Assert.Contains(redstoneBlockUsages, r => r.Id == "minecraft:redstone" && r.ResultItemId == Minecraft.Items.Redstone && r.ResultCount == 9);

        var planksWithTags = Minecraft.Blocks.OakPlanks.RequiredForRecipes();
        var planksExactOnly = Minecraft.Blocks.OakPlanks.RequiredForRecipes(expandTags: false);
        Assert.Contains(planksWithTags, r => r.Id == "minecraft:chest");
        Assert.DoesNotContain(planksExactOnly, r => r.Id == "minecraft:chest");
        Assert.Contains(planksExactOnly, r => r.Id == "minecraft:oak_boat");

        var filtered = Minecraft.Blocks.RedstoneBlock.RequiredForRecipes(type: RecipeType.CraftingShapeless);
        Assert.Contains(filtered, r => r.Id == "minecraft:redstone");
        Assert.All(filtered, r => Assert.Equal(RecipeType.CraftingShapeless, r.Type));

        var fromBlockState = Minecraft.Blocks.Chest.WithFacing("north").RequiredForRecipes();
        Assert.Contains(fromBlockState, r => r.Id == "minecraft:hopper");

        var fromStateString = "minecraft:chest[facing=south]".RequiredForRecipes();
        Assert.Contains(fromStateString, r => r.Id == "minecraft:hopper");

        var fromStack = new ItemStack(Minecraft.Items.Chest, 1).RequiredForRecipes();
        Assert.Contains(fromStack, r => r.Id == "minecraft:hopper");

        Assert.Empty(((string?)null).RequiredForRecipes());
        Assert.Empty("   ".RequiredForRecipes());
    }

    [Fact]
    public void GetFactoryRecipes()
    {
        var blocks = new List<string>();

        FillFactoryRecipes(blocks, Minecraft.Blocks.RedstoneBlock);

        Assert.Equal(29, blocks.Count);
    }

    [Fact]
    public void GetFactoryIncredients()
    {
        var requiredBlocks = new string[]
        {
            Minecraft.Blocks.Dispenser,
            Minecraft.Blocks.Dropper,
            Minecraft.Blocks.Piston,
            Minecraft.Blocks.StickyPiston,
            Minecraft.Blocks.Repeater,
            Minecraft.Blocks.Comparator,
            Minecraft.Blocks.RedstoneTorch,
            Minecraft.Blocks.Chest,
            Minecraft.Blocks.Hopper,
            Minecraft.Blocks.Observer,
            Minecraft.Blocks.Lever,
            Minecraft.Blocks.BambooTrapdoor,
            Minecraft.Blocks.BambooButton
        };

        var blocks = new List<string>(requiredBlocks);

        foreach (var blockId in requiredBlocks)
        {
            FillFactoryIncredients(blocks, blockId, 0);
        }

        Assert.Equal(29, blocks.Count);
    }

    [Fact]
    public void GetRecipes_FromBlockConstantStateAndItemStack_ReturnsProducingRecipes()
    {
        var redstoneBlockRecipes = Minecraft.Blocks.RedstoneBlock.GetRecipes();
        Assert.Contains(redstoneBlockRecipes, r => r.Id == "minecraft:redstone_block" && r.ResultItemId == Minecraft.Items.RedstoneBlock);

        var smokerCookingRecipes = Minecraft.Blocks.DriedKelpBlock.GetRecipes(type: RecipeType.CraftingShaped);
        Assert.Contains(smokerCookingRecipes, r => r.Id == "minecraft:dried_kelp_block");

        var fromState = Minecraft.Blocks.Hopper.WithFacing("east").GetRecipes();
        Assert.Contains(fromState, r => r.Id == "minecraft:hopper");

        var fromStack = new ItemStack(Minecraft.Items.Smoker, 1).GetRecipes();
        Assert.Contains(fromStack, r => r.Id == "minecraft:smoker");

        Assert.Empty(((string?)null).GetRecipes());
        Assert.Throws<ArgumentNullException>(() => ((BlockState)null!).RequiredForRecipes());
        Assert.Throws<ArgumentNullException>(() => ((ItemStack)null!).GetRecipes());
    }

    private static void FillFactoryRecipes(List<string> blocks, string blockId)
    {
        blocks.Add(blockId);

        var recipes = blockId.RequiredForRecipes();

        foreach (var recipe in recipes)
        {
            if (recipe.ResultItemId == null ||
                recipe.Type == RecipeType.Brewing ||
                blocks.Contains(recipe.ResultItemId))
                continue;

            FillFactoryRecipes(blocks, recipe.ResultItemId);
        }
    }

    private static void FillFactoryIncredients(List<string> blocks, string blockId, int depth)
    {
        if (blockId.Contains("nugget"))
            return;

        if (!blocks.Contains(blockId))
            blocks.Add(blockId);
        else if (depth != 0)
            return;

        var recipes = blockId.GetRecipes();

        foreach (var recipe in recipes)
        {
            if (recipe.Ingredients == null ||
                recipe.Type == RecipeType.Stonecutting ||
                recipe.Type == RecipeType.Blasting ||
                recipe.Type == RecipeType.Smelting)
                continue;

            foreach (var ingredient in recipe.Ingredients)
            {
                FillFactoryIncredients(blocks, ingredient.Primary, depth + 1);
            }
        }
    }
}
