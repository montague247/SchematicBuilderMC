using fNbt;
using SchematicBuilderMC.Core.Exceptions;

namespace SchematicBuilderMC.Core.Tests;

public class FormatDetectionAndErrorTests
{
    [Fact]
    public void AutoDetection_IdentifiesSpongeFromNbt()
    {
        var schematic = new Schematic(2, 2, 2);
        schematic.SetBlock(0, 0, 0, "minecraft:stone");

        byte[] spongeBytes = SchematicFile.WriteToBytes(schematic, SchematicFormat.SpongeV2);
        var loaded = SchematicFile.Read(spongeBytes, SchematicFormat.Auto);

        Assert.Equal(2, loaded.Width);
        Assert.Equal("minecraft:stone", loaded[0, 0, 0].Identifier);
    }

    [Fact]
    public void AutoDetection_IdentifiesClassicFromNbt()
    {
        var schematic = new Schematic(2, 2, 2);
        schematic.SetBlock(0, 0, 0, "minecraft:stone");

        byte[] classicBytes = SchematicFile.WriteToBytes(schematic, SchematicFormat.Classic);
        var loaded = SchematicFile.Read(classicBytes, SchematicFormat.Auto);

        Assert.Equal(2, loaded.Width);
        Assert.Equal("minecraft:stone", loaded[0, 0, 0].Identifier);
    }

    [Fact]
    public void Read_NonExistentFile_ThrowsFileNotFoundException()
    {
        Assert.Throws<FileNotFoundException>(() => SchematicFile.Read("non_existent_file.schem"));
    }

    [Fact]
    public void Read_CorruptData_ThrowsException()
    {
        byte[] invalidBytes = [1, 2, 3, 4, 5, 6, 7, 8];
        Assert.ThrowsAny<Exception>(() => SchematicFile.Read(invalidBytes));
    }

    [Fact]
    public void Read_MissingWidthTag_ThrowsSchematicFormatException()
    {
        var nbt = new NbtFile
        {
            RootTag = new NbtCompound("Schematic")
            {
                new NbtShort("Height", 5),
                new NbtShort("Length", 5)
            }
        };

        using var ms = new MemoryStream();
        nbt.SaveToStream(ms, NbtCompression.None);
        byte[] bytes = ms.ToArray();

        Assert.Throws<SchematicFormatException>(() => SchematicFile.Read(bytes, SchematicFormat.SpongeV2));
    }
}
