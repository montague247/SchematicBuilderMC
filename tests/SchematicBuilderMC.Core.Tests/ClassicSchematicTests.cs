using fNbt;
using SchematicBuilderMC.Enums;
using SchematicBuilderMC.IO;
using SchematicBuilderMC.Models;

namespace SchematicBuilderMC.Core.Tests;

public class ClassicSchematicTests
{
    [Fact]
    public void ClassicSchematic_Roundtrip_PreservesBasicBlocksAndOffsets()
    {
        var schematic = new Schematic(3, 2, 4)
        {
            Offset = new BlockPosition(10, -5, 20)
        };

        schematic.SetBlock(0, 0, 0, "minecraft:stone");
        schematic.SetBlock(1, 0, 0, "minecraft:granite");
        schematic.SetBlock(2, 0, 0, "minecraft:diorite");
        schematic.SetBlock(0, 1, 0, "minecraft:oak_planks");
        schematic.SetBlock(1, 1, 0, "minecraft:red_wool");
        schematic.SetBlock(2, 1, 0, "minecraft:diamond_block");

        byte[] bytes = SchematicFile.WriteToBytes(schematic, SchematicFormat.Classic);
        var loaded = SchematicFile.Read(bytes, SchematicFormat.Classic);

        Assert.Equal(3, loaded.Width);
        Assert.Equal(2, loaded.Height);
        Assert.Equal(4, loaded.Length);
        Assert.Equal(new BlockPosition(10, -5, 20), loaded.Offset);

        Assert.Equal("minecraft:stone", loaded[0, 0, 0].Identifier);
        Assert.Equal("minecraft:granite", loaded[1, 0, 0].Identifier);
        Assert.Equal("minecraft:diorite", loaded[2, 0, 0].Identifier);
        Assert.Equal("minecraft:oak_planks", loaded[0, 1, 0].Identifier);
        Assert.Equal("minecraft:red_wool", loaded[1, 1, 0].Identifier);
        Assert.Equal("minecraft:diamond_block", loaded[2, 1, 0].Identifier);
        Assert.Equal(BlockState.Air, loaded[0, 0, 1]);
    }

    [Fact]
    public void ClassicSchematic_AddBlocks_HandlesExtendedBlockIds()
    {
        var schematic = new Schematic(2, 2, 2);
        // Concrete (id 251) and concrete powder (id 252) or unmapped legacy block > 255
        schematic.SetBlock(0, 0, 0, "minecraft:legacy_block_300_5");
        schematic.SetBlock(1, 1, 1, "minecraft:legacy_block_512_0");

        byte[] bytes = SchematicFile.WriteToBytes(schematic, SchematicFormat.Classic);
        var loaded = SchematicFile.Read(bytes, SchematicFormat.Classic);

        Assert.Equal("minecraft:legacy_block_300_5", loaded[0, 0, 0].Identifier);
        Assert.Equal("minecraft:legacy_block_512_0", loaded[1, 1, 1].Identifier);
    }

    [Fact]
    public void ClassicSchematic_PreservesTileEntities()
    {
        var schematic = new Schematic(3, 3, 3);
        schematic.SetBlock(1, 1, 1, "minecraft:chest");

        var tileData = new NbtCompound
        {
            new NbtString("CustomName", "OldChest")
        };
        schematic.SetBlockEntity(new BlockEntity("minecraft:chest", new BlockPosition(1, 1, 1), tileData));

        byte[] bytes = SchematicFile.WriteToBytes(schematic, SchematicFormat.Classic);
        var loaded = SchematicFile.Read(bytes, SchematicFormat.Classic);

        Assert.Single(loaded.BlockEntities);
        var be = loaded.GetBlockEntity(1, 1, 1);
        Assert.NotNull(be);
        Assert.Equal("OldChest", be.Data["CustomName"]?.StringValue);
    }

    [Fact]
    public async Task ClassicSchematic_AsyncFileReadWrite_WorksCorrectly()
    {
        var schematic = new Schematic(2, 2, 2);
        schematic.SetBlock(0, 0, 0, "minecraft:bookshelf");

        string tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.schematic");
        try
        {
            await SchematicFile.WriteAsync(schematic, tempFile, SchematicFormat.Classic);
            var loaded = await SchematicFile.ReadAsync(tempFile);

            Assert.Equal("minecraft:bookshelf", loaded[0, 0, 0].Identifier);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }
}
