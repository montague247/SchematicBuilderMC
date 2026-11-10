using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;
using SchematicBuilderMC.Extensions;
using SchematicBuilderMC.Generators;
using SchematicBuilderMC.IO;
using SchematicBuilderMC.Models;
using SchematicBuilderMC.Registries;

if (args.Length > 0)
{
    string firstArg = args[0].ToLowerInvariant();

    if (firstArg is "--help" or "-h" or "help")
    {
        PrintHelp();
        return 0;
    }

    if (firstArg is "generate" or "--generate" or "generate-registries")
    {
        Console.WriteLine($"Generating Minecraft {Minecraft.Version} registry files...");
        string json = MinecraftRegistryGenerator.GetBundledRegistryJson();
        string targetDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "SchematicBuilderMC.Core", "Registries");
        targetDir = Path.GetFullPath(targetDir);
        MinecraftRegistryGenerator.GenerateToDirectory(json, targetDir);
        Console.WriteLine($"Registry files generated at: {targetDir}");
        return 0;
    }

    if (firstArg is "generate-code" or "to-code" or "code" or "--code" or "-c" ||
        firstArg.EndsWith(".schem", StringComparison.OrdinalIgnoreCase) ||
        firstArg.EndsWith(".schematic", StringComparison.OrdinalIgnoreCase) ||
        args.Any(a => a is "--code" or "-c" or "--input" or "-i"))
    {
        return RunGenerateCode(args);
    }
}

// Default demonstration run
Console.WriteLine($"=== SchematicBuilderMC Demo (Minecraft {Minecraft.Version}) ===");

// 1. Build a schematic using the fluent builder and strongly-typed Minecraft 26.3 registries
var schematic = SchematicBuilder.Create(width: 5, height: 4, length: 5)
    .WithOffset(0, 0, 0)
    .WithName("Antigravity Watchtower")
    .WithAuthor("SchematicBuilderMC")
    .WithDescription($"A small watchtower demo structure built for Minecraft {Minecraft.Version}")
    // Stone foundation using strongly-typed Minecraft.Blocks
    .Fill(0, 0, 0, 4, 0, 4, Minecraft.Blocks.StoneBricks)
    // Cobblestone corners using strongly-typed Minecraft.Blocks
    .Fill(0, 1, 0, 0, 3, 0, Minecraft.Blocks.Cobblestone)
    .Fill(4, 1, 0, 4, 3, 0, Minecraft.Blocks.Cobblestone)
    .Fill(0, 1, 4, 0, 3, 4, Minecraft.Blocks.Cobblestone)
    .Fill(4, 1, 4, 4, 3, 4, Minecraft.Blocks.Cobblestone)
    // Oak walls
    .Fill(1, 1, 0, 3, 3, 0, Minecraft.Blocks.OakPlanks)
    .Fill(1, 1, 4, 3, 3, 4, Minecraft.Blocks.OakPlanks)
    .Fill(0, 1, 1, 0, 3, 3, Minecraft.Blocks.OakPlanks)
    .Fill(4, 1, 1, 4, 3, 3, Minecraft.Blocks.OakPlanks)
    // Door entrance
    .SetBlock(2, 1, 0, Minecraft.Blocks.Air)
    .SetBlock(2, 2, 0, Minecraft.Blocks.Air)
    // Roof using WithProperty / semantic fluent extensions
    .Fill(0, 3, 0, 4, 3, 4, Minecraft.Blocks.OakStairs.WithFacing(Direction.North).BottomHalf())
    // Redstone control circuit (Lever, Repeater, Comparator, Redstone Wire)
    .SetLever(1, 1, 1, powered: true, face: BlockFace.Floor, facing: Direction.South)
    .SetRepeater(2, 1, 1, delay: 2, facing: Direction.South)
    .SetComparator(3, 1, 1, subtractMode: true, facing: Direction.South)
    .SetRedstoneWire(1, 1, 2, power: 15)
    // Chest with items using strongly-typed identifiers
    .SetBlock(2, 1, 3, Minecraft.Blocks.Chest.WithFacing(Direction.North))
    .SetChest(2, 1, 3,
        new ItemStack(Minecraft.Items.Diamond, count: 64, slot: 0),
        new ItemStack(Minecraft.Items.GoldenApple, count: 16, slot: 1),
        new ItemStack(Minecraft.Items.IronSword, count: 1, slot: 4).WithDisplayName("Watchtower Blade"))
    .Build();

Console.WriteLine($"Constructed schematic: {schematic.Metadata.Name}");
Console.WriteLine($"Dimensions: {schematic.Width}x{schematic.Height}x{schematic.Length}");
Console.WriteLine($"Total Blocks: {schematic.TotalBlocks}, Non-Air Blocks: {schematic.CountNonAirBlocks()}");
Console.WriteLine($"Block Entities: {schematic.BlockEntities.Count}");
Console.WriteLine($"Verified Cobblestone call: {Minecraft.Blocks.Cobblestone}");

// 2. Write and Read Sponge Schematic (.schem)
string spongePath = Path.Combine(Path.GetFullPath("."), "demo_structure.schem");
SchematicFile.Write(schematic, spongePath, SchematicFormat.SpongeV2);
Console.WriteLine($"Wrote Sponge schematic to {spongePath} ({new FileInfo(spongePath).Length} bytes)");

var readSponge = SchematicFile.Read(spongePath);
Console.WriteLine($"Read Sponge schematic: {readSponge.Width}x{readSponge.Height}x{readSponge.Length}");
Console.WriteLine($"Block at (2, 1, 1) [Repeater]: {readSponge[2, 1, 1]}");
Console.WriteLine($"Block Entity at (2, 1, 3) [Chest]: {readSponge.GetBlockEntity(2, 1, 3)?.Id}, Items: {readSponge.GetItems(2, 1, 3).Count}");

// 3. Write and Read Classic Schematic (.schematic)
string classicPath = Path.Combine(Path.GetFullPath("."), "demo_structure.schematic");
SchematicFile.Write(schematic, classicPath, SchematicFormat.Classic);
Console.WriteLine($"Wrote Classic schematic to {classicPath} ({new FileInfo(classicPath).Length} bytes)");

var readClassic = SchematicFile.Read(classicPath);
Console.WriteLine($"Read Classic schematic: {readClassic.Width}x{readClassic.Height}x{readClassic.Length}");
Console.WriteLine($"Block at (2, 1, 1) [Repeater]: {readClassic[2, 1, 1]}");
Console.WriteLine($"Block Entity at (2, 1, 3) [Chest]: {readClassic.GetBlockEntity(2, 1, 3)?.Id}, Items: {readClassic.GetItems(2, 1, 3).Count}");

// 4. Generate fluent builder source code from the schematic
Console.WriteLine("\n--- Fluent Builder Source Code Generation Demo ---");
string generatedCode = SchematicFile.ToFluentBuilderCode(spongePath);
Console.WriteLine("Generated fluent builder code preview (first 10 lines):");
var previewLines = generatedCode.Split('\n').Take(10);
foreach (var line in previewLines)
{
    Console.WriteLine("  " + line);
}
Console.WriteLine("  ... [truncated for display]");

Console.WriteLine("\nAll operations completed successfully!");
return 0;

static int RunGenerateCode(string[] args)
{
    string? inputFile = null;
    string? outputFile = null;
    var options = new FluentCodeGeneratorOptions();
    bool modeSetExplicitly = false;

    // Parse arguments
    int index = 0;
    while (index < args.Length)
    {
        string arg = args[index];

        if (arg is "generate-code" or "to-code" or "code" or "--code" or "-c")
        {
            index++;
            continue;
        }

        if (arg is "-i" or "--input" && index + 1 < args.Length)
        {
            inputFile = args[++index];
        }
        else if (arg is "-o" or "--output" && index + 1 < args.Length)
        {
            outputFile = args[++index];
        }
        else if (arg is "-m" or "--mode" && index + 1 < args.Length)
        {
            string modeStr = args[++index];
            if (Enum.TryParse<CodeGenerationMode>(modeStr, ignoreCase: true, out var parsedMode))
            {
                options.Mode = parsedMode;
                modeSetExplicitly = true;
            }
            else
            {
                Console.Error.WriteLine($"Warning: Unknown mode '{modeStr}'. Allowed values: Statement, File, Method, Expression.");
            }
        }
        else if (arg is "--variable" && index + 1 < args.Length)
        {
            options.VariableName = args[++index];
        }
        else if (arg is "--class" && index + 1 < args.Length)
        {
            options.ClassName = args[++index];
        }
        else if (arg is "--namespace" && index + 1 < args.Length)
        {
            options.TargetNamespace = args[++index];
        }
        else if (arg is "--method" && index + 1 < args.Length)
        {
            options.MethodName = args[++index];
        }
        else if (arg is "--no-optimize")
        {
            options.OptimizeFills = false;
        }
        else if (arg is "--include-air")
        {
            options.OmitAir = false;
        }
        else if (arg is "--raw-strings")
        {
            options.UseStronglyTypedBlocks = false;
            options.UseStronglyTypedEntities = false;
            options.UseStronglyTypedItems = false;
        }
        else if (arg is "--raw-properties")
        {
            options.UseSemanticPropertyMethods = false;
        }
        else if (arg is "--always-offset")
        {
            options.AlwaysIncludeOffset = true;
        }
        else if (!arg.StartsWith('-'))
        {
            if (inputFile == null)
            {
                inputFile = arg;
            }
            else if (outputFile == null)
            {
                outputFile = arg;
            }
        }

        index++;
    }

    if (string.IsNullOrWhiteSpace(inputFile))
    {
        Console.Error.WriteLine("Error: No input schematic file specified.");
        Console.Error.WriteLine("Usage: SchematicBuilderMC generate-code <schematic-file> [output-file.cs] [options]");
        return 1;
    }

    if (!File.Exists(inputFile))
    {
        Console.Error.WriteLine($"Error: Input schematic file not found: {inputFile}");
        return 1;
    }

    // Default to File mode if writing to a .cs output file unless explicitly specified
    if (!modeSetExplicitly && !string.IsNullOrEmpty(outputFile) && outputFile.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
    {
        options.Mode = CodeGenerationMode.File;
    }

    try
    {
        if (!string.IsNullOrEmpty(outputFile))
        {
            SchematicFile.GenerateFluentCode(inputFile, outputFile, options);
            Console.WriteLine($"Successfully generated fluent builder code from '{inputFile}' to '{outputFile}'.");
        }
        else
        {
            string code = SchematicFile.ToFluentBuilderCode(inputFile, options);
            Console.WriteLine(code);
        }

        return 0;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Error generating fluent builder code: {ex.Message}");
        return 1;
    }
}

static void PrintHelp()
{
    Console.WriteLine($"""
        SchematicBuilderMC (Minecraft {Minecraft.Version})

        Usage:
          SchematicBuilderMC [command] [options]

        Commands:
          generate-code <file> [output.cs]   Generate C# fluent builder code from a schematic file (.schem, .schematic).
          generate-registries                Generate Minecraft registry constants from embedded/online registries.json.
          demo                               Run the build and read/write demo (default when no args provided).

        Options for generate-code:
          -i, --input <path>      Path to input schematic file (.schem, .schematic).
          -o, --output <path>     Path to output .cs file. If omitted, writes to standard output.
          -m, --mode <mode>       Code generation mode: Statement, File, Method, Expression.
                                  (Default: File if output path ends with .cs, otherwise Statement).
          --variable <name>       Variable name for Statement mode (default: "schematic").
          --class <name>          Class name for File mode (default: "GeneratedSchematic").
          --namespace <name>      Namespace for File mode (default: "SchematicBuilderMC").
          --method <name>         Method name for Method/File mode (default: "CreateSchematic").
          --no-optimize           Disable cuboid fill optimization (emits SetBlock for every block).
          --include-air           Include air blocks in output (default omits air).
          --raw-strings           Use raw string literals instead of Minecraft.Blocks constants.
          --raw-properties        Use raw WithProperty calls instead of semantic methods (WithDelay, etc.).
          --always-offset         Always emit WithOffset even if (0, 0, 0).
          -h, --help              Display this help message.
        """);
}
