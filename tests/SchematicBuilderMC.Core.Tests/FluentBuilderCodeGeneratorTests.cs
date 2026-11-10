using fNbt;
using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;
using SchematicBuilderMC.Extensions;
using SchematicBuilderMC.Generators;
using SchematicBuilderMC.IO;
using SchematicBuilderMC.Models;
using SchematicBuilderMC.Registries;
using Xunit;

namespace SchematicBuilderMC.Core.Tests;

public class FluentBuilderCodeGeneratorTests : IDisposable
{
    private readonly List<string> _tempFiles = [];

    private string GetTempFilePath(string extension)
    {
        string path = Path.Combine(Path.GetTempPath(), $"schem_gen_test_{Guid.NewGuid():N}.{extension.TrimStart('.')}");
        _tempFiles.Add(path);
        return path;
    }

    public void Dispose()
    {
        foreach (var file in _tempFiles)
        {
            try
            {
                if (File.Exists(file)) File.Delete(file);
            }
            catch
            {
                // Best-effort cleanup
            }
        }
    }

    [Fact]
    public void Generate_NullSchematic_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => FluentBuilderCodeGenerator.Generate(null!));
    }

    [Fact]
    public void GenerateFromFile_NonExistentFile_ThrowsFileNotFoundException()
    {
        Assert.Throws<FileNotFoundException>(() =>
            FluentBuilderCodeGenerator.GenerateFromFile("non_existent_file_path.schem"));
    }

    [Fact]
    public void Generate_SimpleSchematic_ProducesExpectedStatementCode()
    {
        var schematic = SchematicBuilder.Create(3, 2, 3)
            .SetBlock(1, 0, 1, Minecraft.Blocks.Stone)
            .Build();

        string code = FluentBuilderCodeGenerator.Generate(schematic);

        Assert.Contains("var schematic = SchematicBuilder.Create(3, 2, 3)", code);
        Assert.Contains(".SetBlock(1, 0, 1, Minecraft.Blocks.Stone)", code);
        Assert.EndsWith(".Build();", code.TrimEnd());
    }

    [Fact]
    public void Generate_WithMetadataAndOffset_EmitsMetadataCalls()
    {
        var schematic = SchematicBuilder.Create(2, 2, 2)
            .WithOffset(10, -5, 20)
            .WithDataVersion(3465)
            .WithName("Castle Gate")
            .WithAuthor("BuilderBob")
            .WithDescription("A sturdy wooden gate")
            .WithRequiredMod("mod_doors")
            .Build();

        string code = FluentBuilderCodeGenerator.Generate(schematic);

        Assert.Contains(".WithOffset(10, -5, 20)", code);
        Assert.Contains(".WithDataVersion(3465)", code);
        Assert.Contains(".WithName(\"Castle Gate\")", code);
        Assert.Contains(".WithAuthor(\"BuilderBob\")", code);
        Assert.Contains(".WithDescription(\"A sturdy wooden gate\")", code);
        Assert.Contains(".WithRequiredMod(\"mod_doors\")", code);
    }

    [Fact]
    public void Generate_CuboidFloor_OptimizesIntoFillCall()
    {
        // 5x3x5 structure with 5x1x5 stone floor and air above
        var schematic = SchematicBuilder.Create(5, 3, 5)
            .Fill(0, 0, 0, 4, 0, 4, Minecraft.Blocks.Stone)
            .Build();

        string code = FluentBuilderCodeGenerator.Generate(schematic);

        Assert.Contains(".Fill(0, 0, 0, 4, 0, 4, Minecraft.Blocks.Stone)", code);
        Assert.DoesNotContain(".SetBlock(", code);
    }

    [Fact]
    public void Generate_FillAll_WhenEntireSchematicIsSingleBlock()
    {
        // 3x3x3 of pure Cobblestone
        var schematic = SchematicBuilder.Create(3, 3, 3)
            .FillAll(Minecraft.Blocks.Cobblestone)
            .Build();

        string code = FluentBuilderCodeGenerator.Generate(schematic);

        Assert.Contains(".FillAll(Minecraft.Blocks.Cobblestone)", code);
        Assert.DoesNotContain(".Fill(0, 0, 0", code);
        Assert.DoesNotContain(".SetBlock(", code);
    }

    [Fact]
    public void Generate_WithoutOptimization_EmitsIndividualSetBlocks()
    {
        var schematic = SchematicBuilder.Create(2, 1, 2)
            .Fill(0, 0, 0, 1, 0, 1, Minecraft.Blocks.Cobblestone)
            .Build();

        var options = new FluentCodeGeneratorOptions { OptimizeFills = false };
        string code = FluentBuilderCodeGenerator.Generate(schematic, options);

        Assert.DoesNotContain(".Fill(", code);
        Assert.Contains(".SetBlock(0, 0, 0, Minecraft.Blocks.Cobblestone)", code);
        Assert.Contains(".SetBlock(1, 0, 0, Minecraft.Blocks.Cobblestone)", code);
        Assert.Contains(".SetBlock(0, 0, 1, Minecraft.Blocks.Cobblestone)", code);
        Assert.Contains(".SetBlock(1, 0, 1, Minecraft.Blocks.Cobblestone)", code);
    }

    [Fact]
    public void Generate_OmitAirFalse_IncludesAirBlocks()
    {
        var schematic = SchematicBuilder.Create(2, 1, 1).Build();
        schematic[0, 0, 0] = Minecraft.Blocks.Cobblestone;
        schematic[1, 0, 0] = Minecraft.Blocks.Air;

        var optionsWithAir = new FluentCodeGeneratorOptions { OmitAir = false, OptimizeFills = false };
        string codeWithAir = FluentBuilderCodeGenerator.Generate(schematic, optionsWithAir);
        Assert.Contains(".SetBlock(1, 0, 0, Minecraft.Blocks.Air)", codeWithAir);

        var optionsDefault = new FluentCodeGeneratorOptions { OmitAir = true, OptimizeFills = false };
        string codeDefault = FluentBuilderCodeGenerator.Generate(schematic, optionsDefault);
        Assert.DoesNotContain("Minecraft.Blocks.Air", codeDefault);
    }

    [Fact]
    public void Generate_BlockWithProperties_EmitsWithPropertyChains()
    {
        var schematic = SchematicBuilder.Create(2, 2, 2)
            .SetBlock(0, 0, 0, Minecraft.Blocks.Chest.WithProperty("facing", "south"))
            .SetBlock(1, 0, 0, Minecraft.Blocks.OakStairs.WithProperty("facing", "north").WithProperty("half", "bottom"))
            .Build();

        // 1. Semantic property methods (default)
        string code = FluentBuilderCodeGenerator.Generate(schematic);
        Assert.Contains("Minecraft.Blocks.Chest.WithFacing(\"south\")", code);
        Assert.Contains("Minecraft.Blocks.OakStairs.WithFacing(\"north\").WithHalf(\"bottom\")", code);

        // 2. Generic WithProperty fallback option
        string rawCode = FluentBuilderCodeGenerator.Generate(schematic, new FluentCodeGeneratorOptions { UseSemanticPropertyMethods = false });
        Assert.Contains("Minecraft.Blocks.Chest.WithProperty(\"facing\", \"south\")", rawCode);
        Assert.Contains("Minecraft.Blocks.OakStairs.WithProperty(\"facing\", \"north\").WithProperty(\"half\", \"bottom\")", rawCode);
    }

    [Fact]
    public void Generate_ModdedBlock_EmitsStringLiteralWithProperties()
    {
        var schematic = SchematicBuilder.Create(2, 2, 2)
            .SetBlock(0, 0, 0, "create:gearbox[axis=x]")
            .SetBlock(1, 0, 0, "create:cogwheel")
            .Build();

        string code = FluentBuilderCodeGenerator.Generate(schematic);

        Assert.Contains("\"create:gearbox\".WithAxis(\"x\")", code);
        Assert.Contains("\"create:cogwheel\"", code);
    }

    [Fact]
    public void Generate_RawStringsOption_EmitsLiteralBlockStateStrings()
    {
        var schematic = SchematicBuilder.Create(2, 2, 2)
            .SetBlock(0, 0, 0, Minecraft.Blocks.Chest.WithProperty("facing", "south"))
            .Build();

        var options = new FluentCodeGeneratorOptions { UseStronglyTypedBlocks = false };
        string code = FluentBuilderCodeGenerator.Generate(schematic, options);

        Assert.Contains("\"minecraft:chest[facing=south]\"", code);
        Assert.DoesNotContain("Minecraft.Blocks.Chest", code);
    }

    [Fact]
    public void Generate_BlockEntitiesAndEntities_EmitsAddCalls()
    {
        var schematic = SchematicBuilder.Create(3, 3, 3)
            .SetBlock(1, 1, 1, Minecraft.Blocks.Chest)
            .AddBlockEntity(new BlockEntity(Minecraft.BlockEntities.Chest, new BlockPosition(1, 1, 1)))
            .AddEntity(new Entity(Minecraft.Entities.Zombie, 1.5, 2.0, 1.5))
            .Build();

        string code = FluentBuilderCodeGenerator.Generate(schematic);

        Assert.Contains(".AddBlockEntity(new BlockEntity(Minecraft.BlockEntities.Chest, new BlockPosition(1, 1, 1)))", code);
        Assert.Contains(".AddEntity(new Entity(Minecraft.Entities.Zombie, 1.5, 2.0, 1.5))", code);
    }

    [Fact]
    public void Generate_BlockEntityWithCustomNbt_EmitsNbtCompound()
    {
        var compound = new NbtCompound
        {
            new NbtString("CustomName", "Secret Loot"),
            new NbtInt("LootSeed", 42)
        };

        var schematic = SchematicBuilder.Create(2, 2, 2)
            .SetBlock(0, 0, 0, Minecraft.Blocks.Chest)
            .AddBlockEntity(new BlockEntity(Minecraft.BlockEntities.Chest, new BlockPosition(0, 0, 0), compound))
            .Build();

        string code = FluentBuilderCodeGenerator.Generate(schematic);

        Assert.Contains("new NbtCompound", code);
        Assert.Contains("new NbtString(\"CustomName\", \"Secret Loot\")", code);
        Assert.Contains("new NbtInt(\"LootSeed\", 42)", code);
    }

    [Theory]
    [InlineData(CodeGenerationMode.Statement)]
    [InlineData(CodeGenerationMode.Expression)]
    [InlineData(CodeGenerationMode.Method)]
    [InlineData(CodeGenerationMode.File)]
    public void Generate_AllModes_ProduceValidExpectedStructure(CodeGenerationMode mode)
    {
        var schematic = SchematicBuilder.Create(2, 2, 2)
            .SetBlock(0, 0, 0, Minecraft.Blocks.Stone)
            .Build();

        var options = new FluentCodeGeneratorOptions
        {
            Mode = mode,
            VariableName = "mySchem",
            ClassName = "CustomSchematic",
            MethodName = "CreateCustom",
            TargetNamespace = "MyProject.Schematics"
        };

        string code = FluentBuilderCodeGenerator.Generate(schematic, options);

        switch (mode)
        {
            case CodeGenerationMode.Statement:
                Assert.StartsWith("var mySchem = SchematicBuilder.Create(2, 2, 2)", code);
                Assert.EndsWith(".Build();", code.TrimEnd());
                break;

            case CodeGenerationMode.Expression:
                Assert.StartsWith("SchematicBuilder.Create(2, 2, 2)", code);
                Assert.EndsWith(".Build()", code.TrimEnd());
                break;

            case CodeGenerationMode.Method:
                Assert.Contains("public static Schematic CreateCustom()", code);
                Assert.Contains("return SchematicBuilder.Create(2, 2, 2)", code);
                break;

            case CodeGenerationMode.File:
                Assert.Contains("namespace MyProject.Schematics;", code);
                Assert.Contains("public static class CustomSchematic", code);
                Assert.Contains("public static Schematic CreateCustom()", code);
                Assert.Contains("using SchematicBuilderMC;", code);
                break;
        }
    }

    [Fact]
    public void GenerateFromFile_And_ToFile_RoundtripThroughDisk()
    {
        var original = SchematicBuilder.Create(4, 3, 4)
            .WithName("Disk Test Structure")
            .WithAuthor("DiskTester")
            .Fill(0, 0, 0, 3, 0, 3, Minecraft.Blocks.StoneBricks)
            .SetBlock(1, 1, 1, Minecraft.Blocks.Chest.WithProperty("facing", "north"))
            .AddBlockEntity(new BlockEntity(Minecraft.BlockEntities.Chest, new BlockPosition(1, 1, 1)))
            .Build();

        string schemPath = GetTempFilePath(".schem");
        SchematicFile.Write(original, schemPath);

        // 1. Generate code from file
        string code = SchematicFile.ToFluentBuilderCode(schemPath);
        Assert.Contains("SchematicBuilder.Create(4, 3, 4)", code);
        Assert.Contains("Minecraft.Blocks.StoneBricks", code);
        Assert.Contains("Minecraft.Blocks.Chest.WithFacing(\"north\")", code);

        // 2. Generate code to file
        string csPath = GetTempFilePath(".cs");
        SchematicFile.GenerateFluentCode(schemPath, csPath);
        Assert.True(File.Exists(csPath));

        string fileContent = File.ReadAllText(csPath);
        Assert.Contains("public static class GeneratedSchematic", fileContent);
        Assert.Contains("public static Schematic CreateSchematic()", fileContent);
    }

    [Fact]
    public void Schematic_And_Builder_InstanceExtensions_Work()
    {
        var builder = SchematicBuilder.Create(2, 2, 2)
            .SetBlock(0, 0, 0, Minecraft.Blocks.Cobblestone);

        string builderCode = builder.ToFluentBuilderCode();
        Assert.Contains("SchematicBuilder.Create(2, 2, 2)", builderCode);
        Assert.Contains("Minecraft.Blocks.Cobblestone", builderCode);

        var schematic = builder.Build();
        string schematicCode = schematic.ToFluentBuilderCode();
        Assert.Equal(builderCode, schematicCode);

        string csPath = GetTempFilePath(".cs");
        schematic.SaveFluentBuilderCode(csPath);
        Assert.True(File.Exists(csPath));
    }

    [Fact]
    public void StringEscaping_HandlesQuotesAndBackslashes()
    {
        var schematic = SchematicBuilder.Create(1, 1, 1)
            .WithName("Test \"Structure\" with \\ backslash")
            .WithAuthor("Author \"One\"")
            .WithDescription("Line1\nLine2\tTabbed")
            .Build();

        string code = FluentBuilderCodeGenerator.Generate(schematic);

        Assert.Contains("Test \\\"Structure\\\" with \\\\ backslash", code);
        Assert.Contains("Author \\\"One\\\"", code);
        Assert.Contains("Line1\\nLine2\\tTabbed", code);
    }
}
