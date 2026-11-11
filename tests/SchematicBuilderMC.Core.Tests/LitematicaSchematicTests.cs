using fNbt;
using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;
using SchematicBuilderMC.Exceptions;
using SchematicBuilderMC.IO;
using SchematicBuilderMC.Models;
using SchematicBuilderMC.Registries;

namespace SchematicBuilderMC.Core.Tests;

public class LitematicaSchematicTests
{
    [Fact]
    public void Litematica_Roundtrip_PreservesBlocksDimensionsAndMetadata()
    {
        var schematic = new Schematic(3, 4, 5)
        {
            Offset = new BlockPosition(10, -5, 20),
            DataVersion = 3953
        };
        schematic.Metadata.Name = "Test Litematic Castle";
        schematic.Metadata.Author = "BuilderMC";
        schematic.Metadata.Description = "A castle for testing litematic files";
        schematic.Metadata.DateCreated = DateTimeOffset.UtcNow;

        schematic.SetBlock(0, 0, 0, Minecraft.Blocks.StoneBricks);
        schematic.SetBlock(1, 2, 3, Minecraft.Blocks.OakStairs.WithProperty("facing", "south").WithProperty("half", "top"));
        schematic.SetBlock(2, 3, 4, Minecraft.Blocks.DiamondBlock);

        byte[] bytes = SchematicFile.WriteToBytes(schematic, SchematicFormat.Litematica);
        var loaded = SchematicFile.Read(bytes, SchematicFormat.Litematica);

        Assert.Equal(3, loaded.Width);
        Assert.Equal(4, loaded.Height);
        Assert.Equal(5, loaded.Length);
        Assert.Equal(new BlockPosition(10, -5, 20), loaded.Offset);
        Assert.Equal(3953, loaded.DataVersion);

        Assert.Equal("Test Litematic Castle", loaded.Metadata.Name);
        Assert.Equal("BuilderMC", loaded.Metadata.Author);
        Assert.Equal("A castle for testing litematic files", loaded.Metadata.Description);

        Assert.Equal("minecraft:stone_bricks", loaded[0, 0, 0].Identifier);
        Assert.Equal("minecraft:oak_stairs[facing=south,half=top]", loaded[1, 2, 3].ToString());
        Assert.Equal("minecraft:diamond_block", loaded[2, 3, 4].Identifier);
        Assert.Equal(BlockState.Air, loaded[1, 1, 1]);
    }

    [Fact]
    public void Litematica_Roundtrip_PreservesBlockEntitiesAndEntities()
    {
        var schematic = SchematicBuilder.Create(4, 4, 4)
            .SetChest(1, 1, 1,
                new ItemStack(Minecraft.Items.Diamond, count: 64, slot: 0),
                new ItemStack(Minecraft.Items.GoldenApple, count: 16, slot: 1))
            .Build();

        schematic.AddEntity(new Entity("minecraft:item_frame", 1.5, 2.0, 1.5));

        byte[] bytes = SchematicFile.WriteToBytes(schematic, SchematicFormat.Litematica);
        var loaded = SchematicFile.Read(bytes, SchematicFormat.Litematica);

        Assert.Single(loaded.BlockEntities);
        var be = loaded.GetBlockEntity(1, 1, 1);
        Assert.NotNull(be);
        Assert.Equal("minecraft:chest", be.Id);
        Assert.Equal(2, be.GetItems().Count);
        Assert.Equal("minecraft:diamond", be.GetItem(0)?.Id);
        Assert.Equal(64, be.GetItem(0)?.Count);
        Assert.Equal("minecraft:golden_apple", be.GetItem(1)?.Id);
        Assert.Equal(16, be.GetItem(1)?.Count);

        Assert.Single(loaded.Entities);
        var ent = loaded.Entities[0];
        Assert.Equal("minecraft:item_frame", ent.Id);
        Assert.Equal(1.5, ent.X, 0.01);
        Assert.Equal(2.0, ent.Y, 0.01);
        Assert.Equal(1.5, ent.Z, 0.01);
    }

    [Fact]
    public void AutoDetection_IdentifiesLitematicaFromNbt()
    {
        var schematic = new Schematic(2, 2, 2);
        schematic.SetBlock(0, 0, 0, Minecraft.Blocks.Stone);

        byte[] litematicBytes = SchematicFile.WriteToBytes(schematic, SchematicFormat.Litematica);
        var loaded = SchematicFile.Read(litematicBytes, SchematicFormat.Auto);

        Assert.Equal(2, loaded.Width);
        Assert.Equal("minecraft:stone", loaded[0, 0, 0].Identifier);
    }

    [Fact]
    public void AutoDetection_IdentifiesLitematicaFromExtension()
    {
        var schematic = new Schematic(2, 2, 2);
        schematic.SetBlock(0, 0, 0, Minecraft.Blocks.IronBlock);

        string tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.litematic");
        try
        {
            SchematicFile.Write(schematic, tempPath);
            var loaded = SchematicFile.Read(tempPath, SchematicFormat.Auto);

            Assert.Equal(2, loaded.Width);
            Assert.Equal("minecraft:iron_block", loaded[0, 0, 0].Identifier);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    [Fact]
    public void Litematica_NegativeSizeAndOffset_HandledCorrectly()
    {
        // Build a raw NBT compound matching Litematica with negative Y size
        var root = new NbtCompound("");
        root.Add(new NbtInt("MinecraftDataVersion", 3953));
        root.Add(new NbtInt("Version", 6));

        var regions = new NbtCompound("Regions");
        var region = new NbtCompound("NegativeYRegion")
        {
            new NbtCompound("Position")
            {
                new NbtInt("x", 0),
                new NbtInt("y", 9), // Extends downwards to y = 0
                new NbtInt("z", 0)
            },
            new NbtCompound("Size")
            {
                new NbtInt("x", 2),
                new NbtInt("y", -10), // 10 blocks: y from 0 to 9
                new NbtInt("z", 2)
            }
        };

        var palette = new NbtList("BlockStatePalette", NbtTagType.Compound)
        {
            new NbtCompound { new NbtString("Name", "minecraft:air") },
            new NbtCompound { new NbtString("Name", "minecraft:gold_block") }
        };
        region.Add(palette);

        // 2x10x2 = 40 blocks. 2 bits per block -> 80 bits -> 2 longs.
        // Place gold_block (index 1) at (x=1, dy=9 (world y=9), z=1)
        // i = (dy * sizeZ + dz) * sizeX + dx = (9 * 2 + 1) * 2 + 1 = (18 + 1) * 2 + 1 = 39.
        int targetIndex = 39;
        long[] longs = new long[2];
        int startBit = targetIndex * 2; // 78
        longs[1] |= (1L << (startBit - 64)); // bit 14 in second long

        region.Add(new NbtLongArray("BlockStates", longs));

        // Add a tile entity at world coordinates (1, 9, 1)
        var tileEntities = new NbtList("TileEntities", NbtTagType.Compound)
        {
            new NbtCompound
            {
                new NbtString("id", "minecraft:chest"),
                new NbtInt("x", 1),
                new NbtInt("y", 9),
                new NbtInt("z", 1)
            }
        };
        region.Add(tileEntities);

        regions.Add(region);
        root.Add(regions);

        var loaded = SchematicFile.Read(new NbtFile(root).SaveToBuffer(NbtCompression.None), SchematicFormat.Litematica);

        Assert.Equal(2, loaded.Width);
        Assert.Equal(10, loaded.Height);
        Assert.Equal(2, loaded.Length);

        // World y = 9 maps to schematic y = 9 (since min y is 0)
        Assert.Equal("minecraft:gold_block", loaded[1, 9, 1].Identifier);
        Assert.Equal(BlockState.Air, loaded[0, 0, 0]);

        var be = loaded.GetBlockEntity(1, 9, 1);
        Assert.NotNull(be);
        Assert.Equal("minecraft:chest", be.Id);
    }

    [Fact]
    public void Litematica_MultiRegion_CalculatesGlobalBoundingBox()
    {
        var root = new NbtCompound("");
        root.Add(new NbtInt("MinecraftDataVersion", 3953));
        root.Add(new NbtInt("Version", 6));

        var regions = new NbtCompound("Regions");

        // Region 1: Pos (0, 0, 0), Size (2, 2, 2) -> spans x: 0..1, y: 0..1, z: 0..1
        var reg1 = new NbtCompound("Reg1")
        {
            new NbtCompound("Position") { new NbtInt("x", 0), new NbtInt("y", 0), new NbtInt("z", 0) },
            new NbtCompound("Size") { new NbtInt("x", 2), new NbtInt("y", 2), new NbtInt("z", 2) },
            new NbtList("BlockStatePalette", NbtTagType.Compound)
            {
                new NbtCompound { new NbtString("Name", "minecraft:stone") }
            },
            new NbtLongArray("BlockStates", new long[1]) // all index 0
        };

        // Region 2: Pos (4, 1, 3), Size (2, 2, 2) -> spans x: 4..5, y: 1..2, z: 3..4
        var reg2 = new NbtCompound("Reg2")
        {
            new NbtCompound("Position") { new NbtInt("x", 4), new NbtInt("y", 1), new NbtInt("z", 3) },
            new NbtCompound("Size") { new NbtInt("x", 2), new NbtInt("y", 2), new NbtInt("z", 2) },
            new NbtList("BlockStatePalette", NbtTagType.Compound)
            {
                new NbtCompound { new NbtString("Name", "minecraft:obsidian") }
            },
            new NbtLongArray("BlockStates", new long[1]) // all index 0
        };

        regions.Add(reg1);
        regions.Add(reg2);
        root.Add(regions);

        var loaded = SchematicFile.Read(new NbtFile(root).SaveToBuffer(NbtCompression.None), SchematicFormat.Litematica);

        // Global bounds:
        // X: 0 to 5 -> width = 6
        // Y: 0 to 2 -> height = 3
        // Z: 0 to 4 -> length = 5
        Assert.Equal(6, loaded.Width);
        Assert.Equal(3, loaded.Height);
        Assert.Equal(5, loaded.Length);

        // Reg1 at world (0, 0, 0) -> schem (0, 0, 0)
        Assert.Equal("minecraft:stone", loaded[0, 0, 0].Identifier);

        // Reg2 at world (4, 1, 3) -> schem (4, 1, 3)
        Assert.Equal("minecraft:obsidian", loaded[4, 1, 3].Identifier);

        // Gap in between should be air
        Assert.Equal(BlockState.Air, loaded[2, 0, 2]);
    }

    [Fact]
    public void Litematica_MissingRegions_ThrowsSchematicFormatException()
    {
        var root = new NbtCompound("")
        {
            new NbtInt("Version", 6)
        };

        Assert.Throws<SchematicFormatException>(() =>
            SchematicFile.Read(new NbtFile(root).SaveToBuffer(NbtCompression.None), SchematicFormat.Litematica));
    }

    [Fact]
    public void Litematica_FluentBuilderCodeGeneration_ProducesValidCode()
    {
        var schematic = SchematicBuilder.Create(3, 3, 3)
            .Fill(0, 0, 0, 2, 0, 2, Minecraft.Blocks.SmoothStone)
            .SetBlock(1, 1, 1, Minecraft.Blocks.RedstoneLamp.WithProperty("lit", "true"))
            .Build();

        byte[] bytes = SchematicFile.WriteToBytes(schematic, SchematicFormat.Litematica);
        string tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.litematic");

        try
        {
            File.WriteAllBytes(tempPath, bytes);
            string code = SchematicFile.ToFluentBuilderCode(tempPath);

            Assert.Contains("SchematicBuilder.Create(3, 3, 3)", code);
            Assert.Contains("Minecraft.Blocks.SmoothStone", code);
            Assert.Contains("Minecraft.Blocks.RedstoneLamp", code);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }
}
