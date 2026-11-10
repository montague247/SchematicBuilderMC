using SchematicBuilderMC.Core;

Console.WriteLine("=== SchematicBuilderMC Demo ===");

// 1. Build a schematic using the fluent builder
var schematic = SchematicBuilder.Create(width: 5, height: 4, length: 5)
    .WithOffset(0, 0, 0)
    .WithName("Antigravity Watchtower")
    .WithAuthor("SchematicBuilderMC")
    .WithDescription("A small watchtower demo structure")
    // Stone foundation
    .Fill(0, 0, 0, 4, 0, 4, "minecraft:stone_bricks")
    // Cobblestone corners
    .Fill(0, 1, 0, 0, 3, 0, "minecraft:cobblestone")
    .Fill(4, 1, 0, 4, 3, 0, "minecraft:cobblestone")
    .Fill(0, 1, 4, 0, 3, 4, "minecraft:cobblestone")
    .Fill(4, 1, 4, 4, 3, 4, "minecraft:cobblestone")
    // Oak walls
    .Fill(1, 1, 0, 3, 3, 0, "minecraft:oak_planks")
    .Fill(1, 1, 4, 3, 3, 4, "minecraft:oak_planks")
    .Fill(0, 1, 1, 0, 3, 3, "minecraft:oak_planks")
    .Fill(4, 1, 1, 4, 3, 3, "minecraft:oak_planks")
    // Door entrance
    .SetBlock(2, 1, 0, "minecraft:air")
    .SetBlock(2, 2, 0, "minecraft:air")
    // Roof
    .Fill(0, 3, 0, 4, 3, 4, "minecraft:oak_stairs[facing=north,half=bottom]")
    // Chest with block entity
    .SetBlock(2, 1, 2, "minecraft:chest[facing=south]")
    .AddBlockEntity(new BlockEntity("minecraft:chest", new BlockPosition(2, 1, 2)))
    .Build();

Console.WriteLine($"Constructed schematic: {schematic.Metadata.Name}");
Console.WriteLine($"Dimensions: {schematic.Width}x{schematic.Height}x{schematic.Length}");
Console.WriteLine($"Total Blocks: {schematic.TotalBlocks}, Non-Air Blocks: {schematic.CountNonAirBlocks()}");
Console.WriteLine($"Block Entities: {schematic.BlockEntities.Count}");

// 2. Write and Read Sponge Schematic (.schem)
string spongePath = Path.Combine(Path.GetTempPath(), "demo_structure.schem");
SchematicFile.Write(schematic, spongePath, SchematicFormat.SpongeV2);
Console.WriteLine($"Wrote Sponge schematic to {spongePath} ({new FileInfo(spongePath).Length} bytes)");

var readSponge = SchematicFile.Read(spongePath);
Console.WriteLine($"Read Sponge schematic: {readSponge.Width}x{readSponge.Height}x{readSponge.Length}");
Console.WriteLine($"Block at (2, 1, 2): {readSponge[2, 1, 2]}");
Console.WriteLine($"Block Entity at (2, 1, 2): {readSponge.GetBlockEntity(2, 1, 2)?.Id}");

// 3. Write and Read Classic Schematic (.schematic)
string classicPath = Path.Combine(Path.GetTempPath(), "demo_structure.schematic");
SchematicFile.Write(schematic, classicPath, SchematicFormat.Classic);
Console.WriteLine($"Wrote Classic schematic to {classicPath} ({new FileInfo(classicPath).Length} bytes)");

var readClassic = SchematicFile.Read(classicPath);
Console.WriteLine($"Read Classic schematic: {readClassic.Width}x{readClassic.Height}x{readClassic.Length}");
Console.WriteLine($"Block at (2, 1, 2): {readClassic[2, 1, 2]}");
Console.WriteLine($"Block Entity at (2, 1, 2): {readClassic.GetBlockEntity(2, 1, 2)?.Id}");

Console.WriteLine("All operations completed successfully!");
