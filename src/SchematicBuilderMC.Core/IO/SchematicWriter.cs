using fNbt;
using SchematicBuilderMC.Enums;
using SchematicBuilderMC.IO.Internal;
using SchematicBuilderMC.Models;

namespace SchematicBuilderMC.IO;

/// <summary>
/// Default implementation of <see cref="ISchematicWriter"/> supporting Sponge (.schem), Classic (.schematic), and Litematica (.litematic) formats.
/// </summary>
public sealed class SchematicWriter : ISchematicWriter
{
    public static SchematicWriter Instance { get; } = new();

    public void Write(Schematic schematic, string filePath, SchematicFormat format = SchematicFormat.SpongeV2, SchematicCompression compression = SchematicCompression.GZip)
    {
        ArgumentNullException.ThrowIfNull(schematic);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        if (format == SchematicFormat.Auto)
        {
            format = DetectFormatFromExtension(filePath);
        }

        string? dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        using var stream = File.Create(filePath);
        Write(schematic, stream, format, compression);
    }

    public void Write(Schematic schematic, Stream stream, SchematicFormat format = SchematicFormat.SpongeV2, SchematicCompression compression = SchematicCompression.GZip)
    {
        ArgumentNullException.ThrowIfNull(schematic);
        ArgumentNullException.ThrowIfNull(stream);

        if (format == SchematicFormat.Auto)
        {
            format = SchematicFormat.SpongeV2;
        }

        NbtFile file = format switch
        {
            SchematicFormat.Classic => ClassicSchematicHandler.Write(schematic),
            SchematicFormat.Litematica => LitematicaSchematicHandler.Write(schematic),
            _ => SpongeSchematicHandler.Write(schematic, format)
        };

        var nbtCompression = ToNbtCompression(compression);
        file.SaveToStream(stream, nbtCompression);
    }

    public byte[] WriteToBytes(Schematic schematic, SchematicFormat format = SchematicFormat.SpongeV2, SchematicCompression compression = SchematicCompression.GZip)
    {
        using var ms = new MemoryStream();
        Write(schematic, ms, format, compression);
        return ms.ToArray();
    }

    public async Task WriteAsync(Schematic schematic, string filePath, SchematicFormat format = SchematicFormat.SpongeV2, SchematicCompression compression = SchematicCompression.GZip, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(schematic);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        byte[] bytes = WriteToBytes(schematic, format, compression);
        string? dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        await File.WriteAllBytesAsync(filePath, bytes, cancellationToken).ConfigureAwait(false);
    }

    public async Task WriteAsync(Schematic schematic, Stream stream, SchematicFormat format = SchematicFormat.SpongeV2, SchematicCompression compression = SchematicCompression.GZip, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(schematic);
        ArgumentNullException.ThrowIfNull(stream);

        byte[] bytes = WriteToBytes(schematic, format, compression);
        await stream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
    }

    private static SchematicFormat DetectFormatFromExtension(string path)
    {
        string ext = Path.GetExtension(path);
        if (ext.Equals(".schematic", StringComparison.OrdinalIgnoreCase))
            return SchematicFormat.Classic;

        if (ext.Equals(".litematic", StringComparison.OrdinalIgnoreCase))
            return SchematicFormat.Litematica;

        return SchematicFormat.SpongeV2;
    }

    private static NbtCompression ToNbtCompression(SchematicCompression compression) => compression switch
    {
        SchematicCompression.ZLib => NbtCompression.ZLib,
        SchematicCompression.None => NbtCompression.None,
        _ => NbtCompression.GZip
    };
}
