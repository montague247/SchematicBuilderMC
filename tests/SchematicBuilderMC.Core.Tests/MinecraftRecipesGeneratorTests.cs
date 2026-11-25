using System.IO.Compression;
using SchematicBuilderMC.Generators;
using SchematicBuilderMC.Registries;

namespace SchematicBuilderMC.Core.Tests;

public sealed class MinecraftRecipesGeneratorTests
{
    [Fact]
    public void GeneratedConstants_ExistAndMatchRecipeRegistry()
    {
        Assert.Equal("minecraft:hopper", Minecraft.Recipes.Ids.Hopper);
        Assert.Equal("minecraft:smoker", Minecraft.Recipes.Ids.Smoker);
        Assert.Equal("#minecraft:planks", Minecraft.Recipes.TagIds.Planks);

        Assert.Equal(2042, Minecraft.Recipes.Ids.All.Count);
        Assert.Equal(236, Minecraft.Recipes.TagIds.All.Count);

        Assert.True(Minecraft.Recipes.Ids.IsValid("hopper"));
        Assert.True(Minecraft.Recipes.Ids.IsValid(Minecraft.Recipes.Ids.Smoker));
        Assert.False(Minecraft.Recipes.Ids.IsValid("fake_recipe_xyz"));

        Assert.True(Minecraft.Recipes.TagIds.IsValid("planks"));
        Assert.True(Minecraft.Recipes.TagIds.IsValid("#minecraft:planks"));
        Assert.False(Minecraft.Recipes.TagIds.IsValid("#minecraft:fake_tag_xyz"));
    }

    [Fact]
    public void Generator_GetBundledRecipeJson_And_GenerateSourceCode_ProducesValidCode()
    {
        string json = MinecraftRecipesGenerator.GetBundledRecipeJson();
        Assert.False(string.IsNullOrWhiteSpace(json));

        var parsed = MinecraftRecipesGenerator.ParseRecipeJson(json);
        Assert.Equal(2042, parsed.Count);

        string code = MinecraftRecipesGenerator.GenerateSourceCode(json, new RecipesGeneratorOptions
        {
            TargetNamespace = "CustomNamespace",
            RootClassName = "CustomMc",
            MinecraftVersion = "26.3"
        });

        Assert.Contains("namespace CustomNamespace;", code);
        Assert.Contains("public static partial class CustomMc", code);
        Assert.Contains("public static partial class Recipes", code);
        Assert.Contains("public static partial class Ids", code);
        Assert.Contains("public static partial class TagIds", code);
        Assert.Contains("public const string Hopper = \"minecraft:hopper\";", code);
        Assert.Contains("public const string Planks = \"#minecraft:planks\";", code);
    }

    [Fact]
    public void Generator_ExtractRecipeJsonFromJar_And_Directory_RoundTrips()
    {
        string tempRoot = Path.Combine(Path.GetTempPath(), $"mc_recipe_gen_{Guid.NewGuid():N}");
        try
        {
            string jarContentDir = Path.Combine(tempRoot, "jar-src");
            string dataMcDir = Path.Combine(jarContentDir, "data", "minecraft");
            Directory.CreateDirectory(Path.Combine(dataMcDir, "recipe"));
            Directory.CreateDirectory(Path.Combine(dataMcDir, "tags", "item"));

            File.WriteAllText(
                Path.Combine(dataMcDir, "recipe", "stick.json"),
                """{"type":"minecraft:crafting_shaped","pattern":["#","#"],"key":{"#":"#minecraft:planks"},"result":{"count":4,"id":"minecraft:stick"}}""");
            File.WriteAllText(
                Path.Combine(dataMcDir, "tags", "item", "planks.json"),
                """{"values":["minecraft:oak_planks","minecraft:bamboo_planks"]}""");

            string jarPath = Path.Combine(tempRoot, "test.jar");
            ZipFile.CreateFromDirectory(jarContentDir, jarPath);

            string fromDirJson = MinecraftRecipesGenerator.ExtractRecipeJsonFromDirectory(dataMcDir);
            string fromJarJson = MinecraftRecipesGenerator.ExtractRecipeJsonFromJar(jarPath);

            var fromDir = MinecraftRecipesGenerator.ParseRecipeJson(fromDirJson);
            var fromJar = MinecraftRecipesGenerator.ParseRecipeJson(fromJarJson);

            Assert.Equal(1, fromDir.Count);
            Assert.Equal(1, fromJar.Count);
            Assert.Equal(4, fromJar.GetById("stick").ResultCount);
            Assert.Contains(Minecraft.Items.BambooPlanks, fromJar.ResolveTag("planks"));

            var files = MinecraftRecipesGenerator.GenerateSourceFiles(fromJarJson);
            Assert.Equal(2, files.Count);
            Assert.Contains("Minecraft.Recipes.Ids.cs", files.Keys);
            Assert.Contains("Minecraft.Recipes.TagIds.cs", files.Keys);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [Fact]
    public void Generator_ToPascalCase_HandlesTagPrefixesAndPaths()
    {
        Assert.Equal("Planks", MinecraftRecipesGenerator.ToPascalCase("#minecraft:planks"));
        Assert.Equal("BrewingLingeringPotionAwkwardBlazePowder", MinecraftRecipesGenerator.ToPascalCase("minecraft:brewing/lingering_potion_awkward_blaze_powder"));
    }
}
