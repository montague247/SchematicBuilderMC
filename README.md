# SchematicBuilderMC

A modern .NET 10 library and CLI for creating, manipulating, reading, and writing Minecraft schematic files.

## Features

- **NuGet Integration**: Built on `fNbt` 2.1.0, the standard, actively-maintained, high-performance NBT library for modern .NET.
- **Multi-Format Support**:
  - **Sponge Schematics (`.schem`)**: Full support for Sponge versions 1, 2, and 3 (used by WorldEdit 7+, FAWE, Axiom, etc.), including VarInt block data packing, block state palettes, block entities, entities, and metadata.
  - **Classic MCEdit Schematics (`.schematic`)**: Support for legacy Minecraft <= 1.12.2 schematics, including 8-bit blocks/data, extended `AddBlocks` (4-bit nibbles for IDs > 255), TileEntities, and WorldEdit offsets.
- **Format Auto-Detection**: Automatically detects format from file extension or by inspecting NBT structure.
- **Compression**: Transparent handling of GZip, ZLib, or uncompressed NBT streams.
- **Domain Model**:
  - `Minecraft`: Strongly-typed registry constants for Minecraft 26.3 (Blocks, Items, BlockEntities, Entities, Biomes, Fluids) with compile-time safety (e.g. `Minecraft.Blocks.Cobblestone` replacing hardcoded `"minecraft:cobblestone"`).
  - `MinecraftRegistryGenerator`: Built-in code generator for creating or updating strongly-typed registry constants from Minecraft registry JSON data or mcmeta dumps.
  - `BlockState`: Namespaced block identifier (e.g. `minecraft:oak_stairs`) with canonical, sorted properties (e.g. `facing=north,half=bottom,waterlogged=false`).
  - `BlockPosition`: 3D integer coordinate record.
  - `BlockEntity`: Tile entities with position and custom NBT data.
  - `Entity`: Non-block entities with double coordinates and custom NBT data.
  - `Schematic`: 3D block container with bounds checking, cuboid fill, and metadata.
- **Fluent Builder**: `SchematicBuilder` for quick programmatic structure generation.
- **Fluent Builder Code Generation**: Automatically generate C# source code using `SchematicBuilder` from any schematic file (`.schem`, `.schematic`) with greedy 3D cuboid fill optimization, strongly-typed block constants, and multiple output modes (`Statement`, `File`, `Method`, `Expression`).
- **Async I/O**: Sync and async `Read`/`Write` methods for files, streams, and byte arrays.

## Quick Start

### 1. Building a Schematic

```csharp
using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;
using SchematicBuilderMC.Extensions;
using SchematicBuilderMC.IO;
using SchematicBuilderMC.Models;
using SchematicBuilderMC.Registries;

var schematic = SchematicBuilder.Create(width: 5, height: 4, length: 5)
    .WithOffset(0, 0, 0)
    .WithName("Watchtower")
    .WithAuthor("Builder")
    // Foundation using strongly-typed blocks
    .Fill(0, 0, 0, 4, 0, 4, Minecraft.Blocks.StoneBricks)
    // Corners
    .Fill(0, 1, 0, 0, 3, 0, Minecraft.Blocks.Cobblestone)
    .Fill(4, 1, 0, 4, 3, 0, Minecraft.Blocks.Cobblestone)
    .Fill(0, 1, 4, 0, 3, 4, Minecraft.Blocks.Cobblestone)
    .Fill(4, 1, 4, 4, 3, 4, Minecraft.Blocks.Cobblestone)
    // Walls
    .Fill(1, 1, 0, 3, 3, 4, Minecraft.Blocks.OakPlanks)
    // Chest with Block Entity
    .SetBlock(2, 1, 2, Minecraft.Blocks.Chest.WithProperty("facing", "south"))
    .AddBlockEntity(new BlockEntity(Minecraft.BlockEntities.Chest, new BlockPosition(2, 1, 2)))
    .Build();
```

### 2. Writing a Schematic

```csharp
// Write as modern Sponge schematic (.schem)
SchematicFile.Write(schematic, "tower.schem", SchematicFormat.SpongeV2);

// Or write as legacy Classic schematic (.schematic)
SchematicFile.Write(schematic, "tower.schematic", SchematicFormat.Classic);
```

### 3. Reading a Schematic

```csharp
// Format is auto-detected from NBT structure and/or extension
Schematic loaded = SchematicFile.Read("tower.schem");

Console.WriteLine($"Dimensions: {loaded.Width}x{loaded.Height}x{loaded.Length}");
BlockState block = loaded[2, 1, 2];
Console.WriteLine($"Block at (2, 1, 2): {block}");
```

### 4. Async I/O

```csharp
Schematic loaded = await SchematicFile.ReadAsync("tower.schem");
await SchematicFile.WriteAsync(loaded, "tower_copy.schem");
```

### 5. Generating Fluent Builder Source Code from a Schematic

Convert any `.schem` or `.schematic` file into clean, strongly-typed C# fluent builder code:

#### Programmatic Usage
```csharp
// Generate fluent builder C# code string
string code = SchematicFile.ToFluentBuilderCode("tower.schem");

// Save directly to a standalone .cs file
SchematicFile.GenerateFluentCode("tower.schem", "TowerStructure.cs");

// Or customize options (e.g. Statement, File, Method, Expression modes)
var options = new FluentCodeGeneratorOptions
{
    Mode = CodeGenerationMode.File,
    ClassName = "MyTower",
    MethodName = "CreateTower",
    OptimizeFills = true // Smart 3D greedy cuboid optimization using .Fill(...)
};
string fileCode = schematic.ToFluentBuilderCode(options);
```

#### CLI Usage
```bash
# Print generated code to stdout
dotnet run --project src/SchematicBuilderMC -- generate-code tower.schem

# Export to a C# source file
dotnet run --project src/SchematicBuilderMC -- generate-code tower.schem -o TowerStructure.cs

# Generate factory method
dotnet run --project src/SchematicBuilderMC -- generate-code tower.schem -m method --method CreateTower
```

## Running Tests

```bash
dotnet test
```
