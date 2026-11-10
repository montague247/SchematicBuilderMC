using fNbt;

namespace SchematicBuilderMC.Core.Tests;

public class SpongeSchematicTests
{
    [Theory]
    [InlineData(SchematicFormat.SpongeV1)]
    [InlineData(SchematicFormat.SpongeV2)]
    [InlineData(SchematicFormat.SpongeV3)]
    public void SpongeSchematic_Roundtrip_PreservesBlocksAndDimensions(SchematicFormat format)
    {
        var schematic = new Schematic(3, 4, 5)
        {
            Offset = new BlockPosition(-1, 2, -3),
            DataVersion = 3465
        };

        schematic.SetBlock(0, 0, 0, "minecraft:stone");
        schematic.SetBlock(1, 2, 3, "minecraft:oak_stairs[facing=south,half=top,waterlogged=true]");
        schematic.SetBlock(2, 3, 4, "minecraft:diamond_block");

        byte[] bytes = SchematicFile.WriteToBytes(schematic, format);
        var loaded = SchematicFile.Read(bytes, format);

        Assert.Equal(3, loaded.Width);
        Assert.Equal(4, loaded.Height);
        Assert.Equal(5, loaded.Length);
        Assert.Equal(new BlockPosition(-1, 2, -3), loaded.Offset);
        Assert.Equal(3465, loaded.DataVersion);

        Assert.Equal("minecraft:stone", loaded[0, 0, 0].ToString());
        Assert.Equal("minecraft:oak_stairs[facing=south,half=top,waterlogged=true]", loaded[1, 2, 3].ToString());
        Assert.Equal("minecraft:diamond_block", loaded[2, 3, 4].ToString());
        Assert.Equal(BlockState.Air, loaded[1, 1, 1]);
    }

    [Fact]
    public void SpongeSchematic_Roundtrip_PreservesBlockEntitiesAndEntities()
    {
        var schematic = new Schematic(4, 4, 4);

        var chestData = new NbtCompound();
        chestData.Add(new NbtString("CustomName", "{\"text\":\"Treasure Chest\"}"));

        var blockEntity = new BlockEntity("minecraft:chest", new BlockPosition(1, 1, 1), chestData);
        schematic.SetBlock(1, 1, 1, "minecraft:chest[facing=north]");
        schematic.SetBlockEntity(blockEntity);

        var entityData = new NbtCompound();
        entityData.Add(new NbtByte("NoGravity", 1));
        var entity = new Entity("minecraft:armor_stand", 2.5, 1.0, 2.5, entityData);
        schematic.AddEntity(entity);

        byte[] bytes = SchematicFile.WriteToBytes(schematic, SchematicFormat.SpongeV2);
        var loaded = SchematicFile.Read(bytes);

        Assert.Single(loaded.BlockEntities);
        var loadedBe = loaded.GetBlockEntity(1, 1, 1);
        Assert.NotNull(loadedBe);
        Assert.Equal("minecraft:chest", loadedBe.Id);
        Assert.Equal("{\"text\":\"Treasure Chest\"}", loadedBe.Data["CustomName"]?.StringValue);

        Assert.Single(loaded.Entities);
        var loadedEnt = loaded.Entities[0];
        Assert.Equal("minecraft:armor_stand", loadedEnt.Id);
        Assert.Equal(2.5, loadedEnt.X, precision: 2);
        Assert.Equal(1.0, loadedEnt.Y, precision: 2);
        Assert.Equal(2.5, loadedEnt.Z, precision: 2);
        Assert.Equal((byte)1, loadedEnt.Data["NoGravity"]?.ByteValue);
    }

    [Fact]
    public void SpongeSchematic_Roundtrip_PreservesMetadata()
    {
        var schematic = new Schematic(2, 2, 2);
        schematic.Metadata.Name = "Castle Gate";
        schematic.Metadata.Author = "Notch";
        schematic.Metadata.Description = "An awesome castle gate.";
        var now = DateTimeOffset.UtcNow;
        // Truncate to milliseconds for unix epoch storage
        var expectedDate = DateTimeOffset.FromUnixTimeMilliseconds(now.ToUnixTimeMilliseconds());
        schematic.Metadata.DateCreated = expectedDate;
        schematic.Metadata.RequiredMods.Add("create");
        schematic.Metadata.RequiredMods.Add("jei");

        byte[] bytes = SchematicFile.WriteToBytes(schematic, SchematicFormat.SpongeV2);
        var loaded = SchematicFile.Read(bytes);

        Assert.Equal("Castle Gate", loaded.Metadata.Name);
        Assert.Equal("Notch", loaded.Metadata.Author);
        Assert.Equal("An awesome castle gate.", loaded.Metadata.Description);
        Assert.Equal(expectedDate, loaded.Metadata.DateCreated);
        Assert.Contains("create", loaded.Metadata.RequiredMods);
        Assert.Contains("jei", loaded.Metadata.RequiredMods);
    }

    [Theory]
    [InlineData(SchematicCompression.GZip)]
    [InlineData(SchematicCompression.ZLib)]
    [InlineData(SchematicCompression.None)]
    public void SpongeSchematic_SupportsAllCompressionMethods(SchematicCompression compression)
    {
        var schematic = new Schematic(3, 3, 3);
        schematic.Fill("minecraft:gold_block");

        byte[] bytes = SchematicFile.WriteToBytes(schematic, SchematicFormat.SpongeV2, compression);
        var loaded = SchematicFile.Read(bytes);

        Assert.Equal("minecraft:gold_block", loaded[1, 1, 1].Identifier);
    }

    [Fact]
    public async Task SpongeSchematic_AsyncFileReadWrite_WorksCorrectly()
    {
        var schematic = new Schematic(2, 2, 2);
        schematic.SetBlock(0, 0, 0, "minecraft:beacon");

        string tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.schem");
        try
        {
            await SchematicFile.WriteAsync(schematic, tempFile, SchematicFormat.SpongeV2);
            var loaded = await SchematicFile.ReadAsync(tempFile);

            Assert.Equal("minecraft:beacon", loaded[0, 0, 0].Identifier);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }
}
