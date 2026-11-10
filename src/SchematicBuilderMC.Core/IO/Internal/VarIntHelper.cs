using SchematicBuilderMC.Exceptions;

namespace SchematicBuilderMC.IO.Internal;

/// <summary>
/// Helper for encoding and decoding Minecraft variable-length integers (VarInt).
/// </summary>
internal static class VarIntHelper
{
    public static int ReadVarInt(byte[] buffer, ref int offset)
    {
        int result = 0;
        int shift = 0;

        while (true)
        {
            if (offset >= buffer.Length)
                throw new SchematicFormatException("Unexpected end of data while reading VarInt.");

            byte b = buffer[offset++];
            result |= (b & 0x7F) << shift;

            if ((b & 0x80) == 0)
                return result;

            shift += 7;
            if (shift >= 35)
                throw new SchematicFormatException("VarInt is too large (greater than 5 bytes).");
        }
    }

    public static void WriteVarInt(Stream stream, int value)
    {
        uint v = (uint)value;
        while ((v & ~0x7Fu) != 0)
        {
            stream.WriteByte((byte)((v & 0x7F) | 0x80));
            v >>= 7;
        }
        stream.WriteByte((byte)(v & 0x7F));
    }

    public static byte[] EncodeVarInts(int[] values)
    {
        // Allocate generous capacity (avg 1-2 bytes per block)
        using var ms = new MemoryStream(values.Length * 2);
        for (int i = 0; i < values.Length; i++)
        {
            WriteVarInt(ms, values[i]);
        }
        return ms.ToArray();
    }

    public static int[] DecodeVarInts(byte[] buffer, int expectedCount)
    {
        var result = new int[expectedCount];
        int offset = 0;
        for (int i = 0; i < expectedCount; i++)
        {
            result[i] = ReadVarInt(buffer, ref offset);
        }
        return result;
    }
}
