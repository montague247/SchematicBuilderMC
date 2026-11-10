using SchematicBuilderMC.Models;

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
}
