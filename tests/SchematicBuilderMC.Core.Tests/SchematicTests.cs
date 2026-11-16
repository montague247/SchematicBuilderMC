using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;
using SchematicBuilderMC.IO;
using SchematicBuilderMC.Models;
using SchematicBuilderMC.Registries;
using SchematicBuilderMC.Schematics.Contraptions;
using SchematicBuilderMC.Schematics.Shapes;

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

    [Fact]
    public void AddEntity_WithBlockCoordinates_CentersHorizontallyAndAppliesOffset()
    {
        var schematic = new Schematic(10, 10, 10)
        {
            Offset = new BlockPosition(-10, -5, 2)
        };

        var allay = new Entity(Minecraft.Entities.Allay).WithNoGravity();
        schematic.AddEntity(1, 6, 3, allay);

        Assert.Single(schematic.Entities);
        Assert.Equal(-8.5, allay.X);
        Assert.Equal(1.0, allay.Y);
        Assert.Equal(5.5, allay.Z);

        var sheep = schematic.AddEntity(new BlockPosition(4, 7, 5), Minecraft.Entities.Sheep, customName: "jeb_");
        Assert.Equal(2, schematic.Entities.Count);
        Assert.Equal(-5.5, sheep.X);
        Assert.Equal(2.0, sheep.Y);
        Assert.Equal(7.5, sheep.Z);
        Assert.Equal("jeb_", sheep.CustomName);
    }

    [Fact]
    public void AddEntity_WithBlockCoordinates_WithoutOffset_CentersWithinBlock()
    {
        var schematic = new Schematic(5, 5, 5);
        var pig = new Entity(Minecraft.Entities.Pig);

        schematic.AddEntity(2, 1, 3, pig);

        Assert.Equal(2.5, pig.X);
        Assert.Equal(1.0, pig.Y);
        Assert.Equal(3.5, pig.Z);

        var namedCow = schematic.AddEntity(0, 0, 0, Minecraft.Entities.Cow, customName: "Bessie");
        Assert.Equal(0.5, namedCow.X);
        Assert.Equal(0.0, namedCow.Y);
        Assert.Equal(0.5, namedCow.Z);
        Assert.Equal("Bessie", namedCow.CustomName);
    }

    [Fact]
    public void AddEntity_WithBlockCoordinates_ValidatesArgumentsAndBounds()
    {
        var schematic = new Schematic(3, 3, 3);

        Assert.Throws<ArgumentNullException>(() => schematic.AddEntity(0, 0, 0, (Entity)null!));
        Assert.Throws<ArgumentNullException>(() => schematic.AddEntity(new BlockPosition(0, 0, 0), (Entity)null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => schematic.AddEntity(-1, 0, 0, new Entity(Minecraft.Entities.Pig)));
        Assert.Throws<ArgumentOutOfRangeException>(() => schematic.AddEntity(3, 0, 0, new Entity(Minecraft.Entities.Pig)));
        Assert.Throws<ArgumentOutOfRangeException>(() => schematic.AddEntity(0, 3, 0, Minecraft.Entities.Pig));
        Assert.Throws<ArgumentOutOfRangeException>(() => schematic.AddEntity(new BlockPosition(0, 0, 3), Minecraft.Entities.Pig));
    }

    [Fact]
    public void Place_ExistingSchematicBeyondBounds_ExtendsDimensionsAndPreservesContents()
    {
        var target = new Schematic(2, 2, 2);
        target.SetBlock(0, 0, 0, Minecraft.Blocks.DiamondBlock);
        target.SetChest(1, 0, 1, new ItemStack(Minecraft.Items.Apple, count: 3, slot: 0));
        target.AddEntity(0, 1, 0, new Entity(Minecraft.Entities.Cow));

        var source = new Schematic(3, 4, 2);
        source.SetBlock(0, 0, 0, Minecraft.Blocks.Stone);
        source.SetBlock(2, 3, 1, Minecraft.Blocks.GoldBlock);
        source.SetChest(1, 1, 0, new ItemStack(Minecraft.Items.Diamond, count: 5, slot: 1));
        source.AddEntity(new Entity(Minecraft.Entities.Sheep, 1.5, 1.0, 0.5));
        source.Metadata.RequiredMods.Add("create");

        target.Place(source, startX: 4, startY: 2, startZ: 3);

        Assert.Equal(7, target.Width);
        Assert.Equal(6, target.Height);
        Assert.Equal(5, target.Length);

        Assert.Equal(Minecraft.Blocks.DiamondBlock, target[0, 0, 0].Identifier);
        Assert.Equal(Minecraft.Items.Apple, target.GetItems(1, 0, 1)[0].Id);
        Assert.Equal(Minecraft.Blocks.Stone, target[4, 2, 3].Identifier);
        Assert.Equal(Minecraft.Blocks.GoldBlock, target[6, 5, 4].Identifier);
        Assert.Equal(Minecraft.Items.Diamond, target.GetItems(5, 3, 3)[0].Id);
        Assert.Equal(5, target.GetItems(5, 3, 3)[0].Count);

        Assert.Equal(2, target.Entities.Count);
        Assert.Equal(Minecraft.Entities.Sheep, target.Entities[1].Id);
        Assert.Equal(5.5, target.Entities[1].X, precision: 5);
        Assert.Equal(3.0, target.Entities[1].Y, precision: 5);
        Assert.Equal(3.5, target.Entities[1].Z, precision: 5);
        Assert.Contains("create", target.Metadata.RequiredMods);

        var selfPlace = new Schematic(2, 1, 1);
        selfPlace.SetBlock(0, 0, 0, Minecraft.Blocks.EmeraldBlock);
        selfPlace.Place(selfPlace, 2, 0, 0);
        Assert.Equal(4, selfPlace.Width);
        Assert.Equal(Minecraft.Blocks.EmeraldBlock, selfPlace[0, 0, 0].Identifier);
        Assert.Equal(Minecraft.Blocks.EmeraldBlock, selfPlace[2, 0, 0].Identifier);

        Assert.Throws<ArgumentOutOfRangeException>(() => target.EnsureDimensions(0, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => target.Place(source, -1, 0, 0));
    }

    [Fact]
    public void Place_ExistingSchematic_IncludeAirControlsOverwriteBehavior()
    {
        var target = new Schematic(3, 1, 3);
        target.Fill(Minecraft.Blocks.Stone);

        var source = new Schematic(2, 1, 2);
        source.SetBlock(1, 0, 1, Minecraft.Blocks.GoldBlock);

        target.Place(source, 0, 0, 0, includeAir: false);
        Assert.Equal(Minecraft.Blocks.Stone, target[0, 0, 0].Identifier);
        Assert.Equal(Minecraft.Blocks.GoldBlock, target[1, 0, 1].Identifier);

        target.Place(source, 0, 0, 0, includeAir: true);
        Assert.Equal(BlockState.Air, target[0, 0, 0]);
        Assert.Equal(Minecraft.Blocks.GoldBlock, target[1, 0, 1].Identifier);
    }

    [Fact]
    public void Place_ExistingSchematicWithOffsetEntities_TranslatesIntoTargetCoordinateSpace()
    {
        var source = SchematicBuilder.Create(4, 4, 4)
            .WithOffset(-4, -1, 0)
            .AddEntity(1, 2, 1, new Entity(Minecraft.Entities.Allay))
            .Build();

        var localTarget = new Schematic(2, 2, 2);
        localTarget.Place(source, new BlockPosition(2, 3, 4));
        Assert.Equal(3.5, localTarget.Entities[0].X, precision: 5);
        Assert.Equal(5.0, localTarget.Entities[0].Y, precision: 5);
        Assert.Equal(5.5, localTarget.Entities[0].Z, precision: 5);

        var offsetTarget = new Schematic(2, 2, 2)
        {
            Offset = new BlockPosition(-10, 0, -2)
        };
        offsetTarget.Place(source, 2, 3, 4);
        Assert.Equal(-6.5, offsetTarget.Entities[0].X, precision: 5);
        Assert.Equal(5.0, offsetTarget.Entities[0].Y, precision: 5);
        Assert.Equal(3.5, offsetTarget.Entities[0].Z, precision: 5);
    }

    [Fact]
    public void SchematicBuilder_PlaceExistingSchematicAndCreators_IntoSmallerBuilder_ExtendsDimensions()
    {
        var cube = new CubeSchematicCreator { Size = 3 }.Build();
        var circle = new CircleSchematicCreator { Radius = 3 };
        var flyingMachine = new FlatFylingMachineSchematicCreator { Width = 4 };

        var builder = SchematicBuilder.Create(1, 1, 1)
            .SetBlock(0, 0, 0, Minecraft.Blocks.Bedrock)
            .Place(cube, 1, 2, 3);

        Assert.Equal(4, builder.Width);
        Assert.Equal(5, builder.Height);
        Assert.Equal(6, builder.Length);

        builder.Place(circle, 2, 0, 2);
        Assert.Equal(9, builder.Width);
        Assert.Equal(5, builder.Height);
        Assert.Equal(9, builder.Length);

        builder.Create(flyingMachine, 0, 5, 0);
        var built = builder.Build();
        Assert.Equal(9, built.Width);
        Assert.Equal(7, built.Height);
        Assert.Equal(9, built.Length);
        Assert.Equal(Minecraft.Blocks.Bedrock, built[0, 0, 0].Identifier);
        Assert.Equal(Minecraft.Blocks.StoneBricks, built[1, 2, 3].Identifier);
        Assert.Equal(Minecraft.Blocks.StoneBricks, built[5, 0, 5].Identifier);
    }
}

