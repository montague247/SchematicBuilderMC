# SchematicBuilderMC

A modern .NET 10 library and CLI for creating, manipulating, reading, and writing Minecraft schematic files.

## Features

- **NuGet Integration**: Built on `fNbt` 2.1.0, the standard, actively-maintained, high-performance NBT library for modern .NET.
- **Multi-Format Support**:
  - **Sponge Schematics (`.schem`)**: Full support for Sponge versions 1, 2, and 3 (used by WorldEdit 7+, FAWE, Axiom, etc.), including VarInt block data packing, block state palettes, block entities, entities, and metadata.
  - **Litematica Schematics (`.litematic`)**: Full read and write support for modern Litematica schematics, including arbitrary bit-width bit-packing, multi-region consolidation, negative region coordinate normalization, block entities, and entities.
  - **Classic MCEdit Schematics (`.schematic`)**: Support for legacy Minecraft <= 1.12.2 schematics, including 8-bit blocks/data, extended `AddBlocks` (4-bit nibbles for IDs > 255), TileEntities, and WorldEdit offsets.
- **Format Auto-Detection**: Automatically detects format from file extension or by inspecting NBT structure.
- **Compression**: Transparent handling of GZip, ZLib, or uncompressed NBT streams.
- **Domain Model**:
  - `Minecraft`: Strongly-typed registry constants for Minecraft 26.3 (Blocks, Items, BlockEntities, Entities, Biomes, Fluids) with compile-time safety (e.g. `Minecraft.Blocks.Cobblestone` replacing hardcoded `"minecraft:cobblestone"`).
  - `MinecraftRegistryGenerator`: Built-in code generator for creating or updating strongly-typed registry constants from Minecraft registry JSON data or mcmeta dumps.
  - `BlockState`: Namespaced block identifier (e.g. `minecraft:oak_stairs`) with canonical, sorted properties (e.g. `facing=north,half=bottom,waterlogged=false`).
  - `BlockPosition`: 3D integer coordinate record.
  - `BlockEntity`: Tile entities with position and custom NBT data, with dedicated helpers for Signs, Containers, Decorated Pots, Crafters, etc.
  - `Entity`: Non-block entities with double coordinates and custom NBT data.
  - `Schematic`: 3D block container with bounds checking, cuboid fill, and metadata.
- **Redstone & Additional Block States**:
  - Strongly-typed enums: `Direction`, `BlockFace`, `ComparatorMode`, `BlockHalf`, `DoorHinge`, `SlabType`, `ChestType`, `StairsShape`, `Axis`.
  - Semantic fluent block state extensions: `.WithDelay(4)`, `.WithSubtractMode()`, `.WithFace(BlockFace.Floor)`, `.Powered()`, `.WithPower(15)`, `.WithHalf(BlockHalf.Top)`, `.WithOpen(true)`, `.WithHinge(DoorHinge.Left)`, etc.
  - Dedicated builder methods on `Schematic` and `SchematicBuilder`: `SetRepeater`, `SetComparator`, `SetLever`, `SetRedstoneWire`, `SetPiston`, `SetChest`, `SetDoubleChest`, `SetSign`, `SetDecoratedPot`, `SetCrafter`, `AddItemFrame`.
- **Fluent Builder**: `SchematicBuilder` for quick programmatic structure generation.
- **Fluent Builder Code Generation**: Automatically generate C# source code using `SchematicBuilder` from any schematic file (`.schem`, `.schematic`, `.litematic`) with greedy 3D cuboid fill optimization, strongly-typed block constants, semantic property call emission (`WithDelay`, `WithFacing`, `WithSubtractMode`), dedicated container/sign/pot/crafter/item frame calls, and multiple output modes (`Statement`, `File`, `Method`, `Expression`).
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
    // Roof stairs using semantic fluent extensions
    .Fill(0, 3, 0, 4, 3, 4, Minecraft.Blocks.OakStairs.WithFacing(Direction.North).BottomHalf())
    // Redstone wiring & mechanisms
    .SetLever(1, 1, 1, powered: true, face: BlockFace.Floor, facing: Direction.South)
    .SetRepeater(2, 1, 1, delay: 2, facing: Direction.South)
    .SetComparator(3, 1, 1, subtractMode: true, facing: Direction.South)
    .SetRedstoneWire(1, 1, 2, power: 15)
    // Double chest with 54-slot inventory items across both halves
    .SetDoubleChest(2, 1, 3, Direction.North,
        new ItemStack(Minecraft.Items.Diamond, count: 64, slot: 0),
        new ItemStack(Minecraft.Items.GoldenApple, count: 16, slot: 1),
        new ItemStack(Minecraft.Items.IronSword, count: 1, slot: 4).WithDisplayName("Watchtower Blade"),
        new ItemStack(Minecraft.Items.Bow, count: 1, slot: 27),
        new ItemStack(Minecraft.Items.Arrow, count: 64, slot: 28))
    .Build();
```

### 2. Chests & Double Chests

SchematicBuilderMC provides first-class support for single chests and double chests with inventory items:

```csharp
// 1. Single Chest
builder.SetBlock(2, 1, 3, Minecraft.Blocks.Chest.WithFacing(Direction.North))
       .SetChest(2, 1, 3,
           new ItemStack(Minecraft.Items.Diamond, count: 64, slot: 0),
           new ItemStack(Minecraft.Items.GoldenApple, count: 16, slot: 1),
           new ItemStack(Minecraft.Items.IronSword, count: 1, slot: 4).WithDisplayName("Watchtower Blade"));

// 2. Double Chest (automatically connects right and left halves; slots 0..26 in right half, slots 27..53 in left half)
builder.SetDoubleChest(2, 1, 3, Direction.North,
    new ItemStack(Minecraft.Items.Diamond, count: 64, slot: 0),
    new ItemStack(Minecraft.Items.IronSword, count: 1, slot: 4).WithDisplayName("Watchtower Blade"),
    new ItemStack(Minecraft.Items.Bow, count: 1, slot: 27),
    new ItemStack(Minecraft.Items.Arrow, count: 64, slot: 28));

// 3. Or configure double chest block states explicitly with ChestType.Right and ChestType.Left
builder.SetBlock(2, 1, 3, Minecraft.Blocks.Chest.WithFacing(Direction.North).WithChestType(ChestType.Right))
       .SetChest(2, 1, 3, new ItemStack(Minecraft.Items.Diamond, count: 64, slot: 0))
       .SetBlock(3, 1, 3, Minecraft.Blocks.Chest.WithFacing(Direction.North).WithChestType(ChestType.Left))
       .SetChest(3, 1, 3, new ItemStack(Minecraft.Items.Bow, count: 1, slot: 0));
```

### 3. Item Frames

```csharp
// Regular item frame with item and facing
builder.AddItemFrame(24.5, 11.5, 29.03125, Direction.South, new ItemStack(Minecraft.Items.Dispenser));

// Glow item frame with rotation and invisible flag
builder.AddItemFrame(1.5, 2.0, 3.5, Direction.North, new ItemStack(Minecraft.Items.Diamond), rotation: 2, isGlow: true, isInvisible: true);

// Empty item frame
builder.AddItemFrame(5.5, 2.0, 5.5, Direction.East);
```

### 4. Writing a Schematic

```csharp
// Auto-detects format from extension:
// .schem -> Sponge V2
// .litematic -> Litematica
// .schematic -> Classic
SchematicFile.Write(schematic, "tower.schem");
SchematicFile.Write(schematic, "tower.litematic");
SchematicFile.Write(schematic, "tower.schematic");
```

### 5. Reading a Schematic

```csharp
// Format is auto-detected from NBT structure and/or extension
Schematic loaded = SchematicFile.Read("tower.schem");

Console.WriteLine($"Dimensions: {loaded.Width}x{loaded.Height}x{loaded.Length}");
BlockState block = loaded[2, 1, 2];
Console.WriteLine($"Block at (2, 1, 2): {block}");
```

### 6. Async I/O

```csharp
Schematic loaded = await SchematicFile.ReadAsync("tower.schem");
await SchematicFile.WriteAsync(loaded, "tower_copy.schem");
```

### 7. Generating Fluent Builder Source Code from a Schematic

Convert any `.schem`, `.schematic`, or `.litematic` file into clean, strongly-typed C# fluent builder code:

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
    OptimizeFills = true,               // Smart 3D greedy cuboid optimization using .Fill(...)
    UseSemanticPropertyMethods = true,  // Emits .WithFacing(...), .WithDelay(...) instead of raw .WithProperty(...)
    UseStronglyTypedBlocks = true       // Emits Minecraft.Blocks constants
};
string fileCode = schematic.ToFluentBuilderCode(options);
```

#### CLI Usage
```bash
# Print generated code to stdout
dotnet run --project src/SchematicBuilderMC -- generate-code tower.schem

# Export to a C# source file
dotnet run --project src/SchematicBuilderMC -- generate-code tower.schem -o TowerStructure.cs

# Generate factory method with raw property strings
dotnet run --project src/SchematicBuilderMC -- generate-code tower.schem -m method --method CreateTower --raw-properties
```

## Running Tests

```bash
dotnet test
```
