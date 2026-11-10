using SchematicBuilderMC.Core;
using SchematicBuilderMC.Core.Generators;

if (args.Length > 0 && (args[0] == "generate" || args[0] == "--generate" || args[0] == "generate-registries"))
{
    Console.WriteLine($"Generating Minecraft {Minecraft.Version} registry files...");
    string json = MinecraftRegistryGenerator.GetBundledRegistryJson();
    string targetDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "SchematicBuilderMC.Core", "Registries");
    targetDir = Path.GetFullPath(targetDir);
    MinecraftRegistryGenerator.GenerateToDirectory(json, targetDir);
    Console.WriteLine($"Registry files generated at: {targetDir}");
    return;
}

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
    // Roof using WithProperty fluent extensions
    .Fill(0, 3, 0, 4, 3, 4, Minecraft.Blocks.OakStairs.WithProperty("facing", "north").WithProperty("half", "bottom"))
    // Chest with block entity using strongly-typed identifiers
    .SetBlock(2, 1, 2, Minecraft.Blocks.Chest.WithProperty("facing", "south"))
    .AddBlockEntity(new BlockEntity(Minecraft.BlockEntities.Chest, new BlockPosition(2, 1, 2)))
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
Console.WriteLine($"Block at (2, 1, 2): {readSponge[2, 1, 2]}");
Console.WriteLine($"Block Entity at (2, 1, 2): {readSponge.GetBlockEntity(2, 1, 2)?.Id}");

// 3. Write and Read Classic Schematic (.schematic)
string classicPath = Path.Combine(Path.GetFullPath("."), "demo_structure.schematic");
SchematicFile.Write(schematic, classicPath, SchematicFormat.Classic);
Console.WriteLine($"Wrote Classic schematic to {classicPath} ({new FileInfo(classicPath).Length} bytes)");

var readClassic = SchematicFile.Read(classicPath);
Console.WriteLine($"Read Classic schematic: {readClassic.Width}x{readClassic.Height}x{readClassic.Length}");
Console.WriteLine($"Block at (2, 1, 2): {readClassic[2, 1, 2]}");
Console.WriteLine($"Block Entity at (2, 1, 2): {readClassic.GetBlockEntity(2, 1, 2)?.Id}");

Console.WriteLine("All operations completed successfully!");
