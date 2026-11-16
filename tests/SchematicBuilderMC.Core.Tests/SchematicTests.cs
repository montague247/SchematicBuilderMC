using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;
using SchematicBuilderMC.IO;
using SchematicBuilderMC.Models;
using SchematicBuilderMC.Registries;

namespace SchematicBuilderMC.Core.Tests;

public class SchematicTests
{
    [Theory]
    [InlineData(0, 5, 5)]
    [InlineData(5, 0, 5)]
    [InlineData(5, 5, -1)]
    public void Constructor_InvalidDimensions_ThrowsArgumentOutOfRangeException(int w, int h, int l)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Schematic(w, h, l));
    }

    [Fact]
    public void IndexerAndGetSetBlock_WorkCorrectly()
    {
        var schematic = new Schematic(3, 4, 5);
        Assert.Equal(BlockState.Air, schematic[1, 2, 3]);

        schematic[1, 2, 3] = "minecraft:gold_block";
        Assert.Equal("minecraft:gold_block", schematic[1, 2, 3].Identifier);
        Assert.Equal("minecraft:gold_block", schematic.GetBlock(1, 2, 3).Identifier);

        schematic[new BlockPosition(0, 0, 0)] = "minecraft:bedrock";
        Assert.Equal("minecraft:bedrock", schematic[0, 0, 0].Identifier);
    }

    [Fact]
    public void BoundsChecking_ThrowsWhenOutOfRange()
    {
        var schematic = new Schematic(3, 3, 3);
        Assert.Throws<ArgumentOutOfRangeException>(() => schematic[-1, 0, 0]);
        Assert.Throws<ArgumentOutOfRangeException>(() => schematic[3, 0, 0]);
        Assert.Throws<ArgumentOutOfRangeException>(() => schematic[0, 3, 0]);
        Assert.Throws<ArgumentOutOfRangeException>(() => schematic[0, 0, 3]);
    }

    [Fact]
    public void Fill_FillsEntireOrSubRegion()
    {
        var schematic = new Schematic(4, 4, 4);
        schematic.Fill("minecraft:dirt");
        Assert.Equal(64, schematic.CountNonAirBlocks());

        schematic.Fill(1, 1, 1, 2, 2, 2, "minecraft:stone");
        Assert.Equal("minecraft:stone", schematic[1, 1, 1].Identifier);
        Assert.Equal("minecraft:stone", schematic[2, 2, 2].Identifier);
        Assert.Equal("minecraft:dirt", schematic[0, 0, 0].Identifier);
    }

    [Fact]
    public void BlockEntities_AddGetRemove_WorkCorrectly()
    {
        var schematic = new Schematic(5, 5, 5);
        var be = new BlockEntity("minecraft:chest", new BlockPosition(1, 1, 1));
        schematic.SetBlockEntity(be);

        Assert.Single(schematic.BlockEntities);
        Assert.Equal(be, schematic.GetBlockEntity(1, 1, 1));
        Assert.Equal(be, schematic.GetBlockEntity(new BlockPosition(1, 1, 1)));

        Assert.True(schematic.RemoveBlockEntity(1, 1, 1));
        Assert.Empty(schematic.BlockEntities);
        Assert.Null(schematic.GetBlockEntity(1, 1, 1));
    }

    [Fact]
    public void Clone_CreatesDeepCopy()
    {
        var schematic = new Schematic(3, 3, 3);
        schematic.SetBlock(1, 1, 1, "minecraft:diamond_block");
        schematic.Offset = new BlockPosition(10, 20, 30);
        schematic.Metadata.Author = "Tester";
        schematic.SetBlockEntity(new BlockEntity("minecraft:chest", new BlockPosition(1, 1, 1)));

        var clone = schematic.Clone();
        Assert.Equal(schematic.Width, clone.Width);
        Assert.Equal(schematic.Height, clone.Height);
        Assert.Equal(schematic.Length, clone.Length);
        Assert.Equal(schematic.Offset, clone.Offset);
        Assert.Equal(schematic.Metadata.Author, clone.Metadata.Author);
        Assert.Equal(schematic[1, 1, 1], clone[1, 1, 1]);
        Assert.Single(clone.BlockEntities);

        // Modifying clone does not affect original
        clone.SetBlock(1, 1, 1, "minecraft:iron_block");
        Assert.Equal("minecraft:diamond_block", schematic[1, 1, 1].Identifier);
        Assert.Equal("minecraft:iron_block", clone[1, 1, 1].Identifier);
    }

    [Theory]
    [InlineData(int.MaxValue, 2, 1)]
    [InlineData(2000, 2000, 2000)]
    [InlineData(65536, 65536, 1)]
    [InlineData(50000, 50000, 1)]
    [InlineData(int.MaxValue, 1, 1)]
    public void Constructor_OverflowingOrExcessiveDimensions_ThrowsArgumentOutOfRangeException(int w, int h, int l)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Schematic(w, h, l));
        Assert.Throws<ArgumentOutOfRangeException>(() => SchematicBuilder.Create(w, h, l));
    }

    [Theory]
    [InlineData(1, 1, 1, 1)]
    [InlineData(3, 4, 5, 60)]
    [InlineData(10, 20, 30, 6000)]
    public void TotalBlocks_OrdinaryDimensions_ReturnsValidatedVolume(int w, int h, int l, int expectedTotal)
    {
        var schematic = new Schematic(w, h, l);
        Assert.Equal(expectedTotal, schematic.TotalBlocks);
    }

    [Fact]
    public void SetBlock_ReplacingBlockEntityBlockWithIncompatibleBlock_RemovesBlockEntity()
    {
        var schematic = new Schematic(4, 4, 4);
        schematic.SetChest(1, 1, 1, new ItemStack(Minecraft.Items.Diamond, 16));
        schematic.SetSign(2, 1, 1, "Hello", "World");

        Assert.Equal(2, schematic.BlockEntities.Count);

        schematic.SetBlock(1, 1, 1, "minecraft:stone");
        schematic[2, 1, 1] = BlockState.Air;

        Assert.Null(schematic.GetBlockEntity(1, 1, 1));
        Assert.Null(schematic.GetBlockEntity(2, 1, 1));
        Assert.Empty(schematic.BlockEntities);
    }

    [Fact]
    public void SetBlock_UpdatingBlockStateWithCompatibleBlock_PreservesBlockEntity()
    {
        var schematic = new Schematic(4, 4, 4);
        schematic.SetChest(1, 1, 1, new ItemStack(Minecraft.Items.Emerald, 8, slot: 0));
        schematic.SetSign(2, 1, 1, "Line 1", "Line 2");

        schematic.SetBlock(1, 1, 1, "minecraft:chest[facing=south,waterlogged=true]");
        schematic.SetBlock(2, 1, 1, "minecraft:spruce_wall_sign[facing=east]");

        var chestBe = schematic.GetBlockEntity(1, 1, 1);
        Assert.NotNull(chestBe);
        Assert.Single(chestBe.GetItems());
        Assert.Equal("minecraft:emerald", chestBe.GetItem(0)?.Id);
        Assert.Equal(8, chestBe.GetItem(0)?.Count);

        var signBe = schematic.GetBlockEntity(2, 1, 1);
        Assert.NotNull(signBe);
        var (front, _, _) = signBe.GetSignText();
        Assert.Equal(["Line 1", "Line 2"], front);
    }

    [Fact]
    public void Fill_CuboidAndFullFill_ReconcilesBlockEntities()
    {
        var schematic = new Schematic(5, 5, 5);
        schematic.SetChest(1, 1, 1, new ItemStack(Minecraft.Items.Diamond, 1));
        schematic.SetChest(2, 1, 1, new ItemStack(Minecraft.Items.GoldIngot, 2));
        schematic.SetChest(4, 4, 4, new ItemStack(Minecraft.Items.IronIngot, 3));

        schematic.Fill(0, 0, 0, 1, 2, 2, BlockState.Air);
        Assert.Null(schematic.GetBlockEntity(1, 1, 1));
        Assert.NotNull(schematic.GetBlockEntity(2, 1, 1));
        Assert.NotNull(schematic.GetBlockEntity(4, 4, 4));

        schematic.Fill(2, 0, 0, 3, 2, 2, "minecraft:stone");
        Assert.Null(schematic.GetBlockEntity(2, 1, 1));
        Assert.NotNull(schematic.GetBlockEntity(4, 4, 4));

        schematic.Fill("minecraft:stone");
        Assert.Null(schematic.GetBlockEntity(4, 4, 4));
        Assert.Empty(schematic.BlockEntities);
    }

    [Fact]
    public void Serialization_AfterReplacingOrFillingBlockEntityPositions_DoesNotRetainStaleEntities()
    {
        var schematic = new Schematic(5, 5, 5);
        schematic.SetChest(1, 1, 1, new ItemStack(Minecraft.Items.Diamond, 10));
        schematic.SetChest(2, 2, 2, new ItemStack(Minecraft.Items.Emerald, 20));
        schematic.SetSign(3, 1, 1, "Stale Sign");

        schematic.SetBlock(1, 1, 1, "minecraft:stone");
        schematic.Fill(3, 0, 0, 3, 2, 2, "minecraft:air");

        Assert.Single(schematic.BlockEntities);
        Assert.NotNull(schematic.GetBlockEntity(2, 2, 2));

        foreach (var format in new[] { SchematicFormat.SpongeV2, SchematicFormat.Classic, SchematicFormat.Litematica })
        {
            byte[] bytes = SchematicFile.WriteToBytes(schematic, format);
            var loaded = SchematicFile.Read(bytes, format);

            Assert.Single(loaded.BlockEntities);
            Assert.Null(loaded.GetBlockEntity(1, 1, 1));
            Assert.Null(loaded.GetBlockEntity(3, 1, 1));

            var remaining = loaded.GetBlockEntity(2, 2, 2);
            Assert.NotNull(remaining);
            Assert.Equal("minecraft:chest", remaining.Id);
            Assert.Equal("minecraft:emerald", remaining.GetItem(0)?.Id);
            Assert.Equal(20, remaining.GetItem(0)?.Count);
        }

        string code = schematic.ToFluentBuilderCode();
        Assert.DoesNotContain("Stale Sign", code);
        Assert.DoesNotContain("Minecraft.Items.Diamond", code);
        Assert.Contains("Minecraft.Items.Emerald", code);
    }
}

