using SchematicBuilderMC.Enums;
using SchematicBuilderMC.IO;
using SchematicBuilderMC.IO.Internal;
using SchematicBuilderMC.Models;

namespace SchematicBuilderMC.Core.Tests;

public class ExtendedSchematicTests
{
    [Fact]
    public void SpongeSchematic_LargePaletteWithMultiByteVarInts_RoundtripsSuccessfully()
    {
        // 150 distinct blocks ensures palette indices > 127 which require 2-byte VarInts
        int distinctCount = 150;
        var schematic = new Schematic(distinctCount, 1, 1);

        for (int i = 0; i < distinctCount; i++)
        {
            schematic.SetBlock(i, 0, 0, $"custom:block_{i}[variant={i}]");
        }

        byte[] bytes = SchematicFile.WriteToBytes(schematic, SchematicFormat.SpongeV2);
        var loaded = SchematicFile.Read(bytes);

        Assert.Equal(distinctCount, loaded.Width);
        for (int i = 0; i < distinctCount; i++)
        {
            Assert.Equal($"custom:block_{i}[variant={i}]", loaded[i, 0, 0].ToString());
        }
    }

    [Fact]
    public void SpongeSchematic_LargeVolume_ProcessesQuickly()
    {
        // 30 x 15 x 30 = 13,500 blocks
        var schematic = new Schematic(30, 15, 30);
        schematic.Fill(0, 0, 0, 29, 0, 29, "minecraft:bedrock");
        schematic.Fill(0, 1, 0, 29, 10, 29, "minecraft:stone");
        schematic.Fill(5, 5, 5, 24, 9, 24, "minecraft:air");

        byte[] bytes = SchematicFile.WriteToBytes(schematic, SchematicFormat.SpongeV2);
        var loaded = SchematicFile.Read(bytes);

        Assert.Equal(13500, loaded.TotalBlocks);
        Assert.Equal("minecraft:bedrock", loaded[10, 0, 10].Identifier);
        Assert.Equal(BlockState.Air, loaded[10, 6, 10]);
        Assert.Equal("minecraft:stone", loaded[2, 5, 2].Identifier);
    }

    [Fact]
    public void ClassicSchematic_OddAndEvenExtendedBlockIds_TestAllNibbles()
    {
        var schematic = new Schematic(4, 1, 1);
        // Even index 0, Odd index 1, Even index 2, Odd index 3
        schematic.SetBlock(0, 0, 0, "minecraft:legacy_block_257_1");
        schematic.SetBlock(1, 0, 0, "minecraft:legacy_block_515_3");
        schematic.SetBlock(2, 0, 0, "minecraft:legacy_block_1024_0");
        schematic.SetBlock(3, 0, 0, "minecraft:legacy_block_4000_7");

        byte[] bytes = SchematicFile.WriteToBytes(schematic, SchematicFormat.Classic);
        var loaded = SchematicFile.Read(bytes, SchematicFormat.Classic);

        Assert.Equal("minecraft:legacy_block_257_1", loaded[0, 0, 0].Identifier);
        Assert.Equal("minecraft:legacy_block_515_3", loaded[1, 0, 0].Identifier);
        Assert.Equal("minecraft:legacy_block_1024_0", loaded[2, 0, 0].Identifier);
        Assert.Equal("minecraft:legacy_block_4000_7", loaded[3, 0, 0].Identifier);
    }

    [Fact]
    public void LegacyBlockConverter_MapsStandardBlocksBidirectionally()
    {
        // Test stone
        var stone = LegacyBlockConverter.ToBlockState(1, 0);
        Assert.Equal("minecraft:stone", stone.Identifier);
        var (stoneId, stoneData) = LegacyBlockConverter.FromBlockState(stone);
        Assert.Equal(1, stoneId);
        Assert.Equal(0, stoneData);

        // Test red wool
        var redWool = LegacyBlockConverter.ToBlockState(35, 14);
        Assert.Equal("minecraft:red_wool", redWool.Identifier);
        var (woolId, woolData) = LegacyBlockConverter.FromBlockState(redWool);
        Assert.Equal(35, woolId);
        Assert.Equal(14, woolData);

        // Test oak planks
        var planks = LegacyBlockConverter.ToBlockState(5, 0);
        Assert.Equal("minecraft:oak_planks", planks.Identifier);
        var (plankId, plankData) = LegacyBlockConverter.FromBlockState(planks);
        Assert.Equal(5, plankId);
        Assert.Equal(0, plankData);
    }

    [Fact]
    public void EmptySchematic_AllAir_RoundtripsSuccessfully()
    {
        var schematic = new Schematic(2, 2, 2);
        byte[] bytes = SchematicFile.WriteToBytes(schematic, SchematicFormat.SpongeV2);
        var loaded = SchematicFile.Read(bytes);

        Assert.Equal(0, loaded.CountNonAirBlocks());
        Assert.True(loaded[0, 0, 0].IsAir);
    }
}
