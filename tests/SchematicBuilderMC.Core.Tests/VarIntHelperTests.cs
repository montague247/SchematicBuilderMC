using SchematicBuilderMC.Core.Exceptions;
using SchematicBuilderMC.Core.IO.Internal;

namespace SchematicBuilderMC.Core.Tests;

public class VarIntHelperTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(127)]
    [InlineData(128)]
    [InlineData(255)]
    [InlineData(256)]
    [InlineData(65535)]
    [InlineData(2097151)]
    [InlineData(int.MaxValue)]
    public void VarInt_SingleValue_EncodesAndDecodesCorrectly(int value)
    {
        using var ms = new MemoryStream();
        VarIntHelper.WriteVarInt(ms, value);
        byte[] bytes = ms.ToArray();

        int offset = 0;
        int decoded = VarIntHelper.ReadVarInt(bytes, ref offset);

        Assert.Equal(value, decoded);
        Assert.Equal(bytes.Length, offset);
    }

    [Fact]
    public void VarInt_ArrayRoundtrip_PreservesAllElements()
    {
        int[] original = [0, 1, 2, 5, 127, 128, 300, 1000, 50000, 2000000];
        byte[] encoded = VarIntHelper.EncodeVarInts(original);
        int[] decoded = VarIntHelper.DecodeVarInts(encoded, original.Length);

        Assert.Equal(original, decoded);
    }

    [Fact]
    public void VarInt_TruncatedBuffer_ThrowsSchematicFormatException()
    {
        // 0x80 indicates more bytes follow, but buffer ends
        byte[] truncated = [0x80];
        int offset = 0;
        Assert.Throws<SchematicFormatException>(() => VarIntHelper.ReadVarInt(truncated, ref offset));
    }
}
