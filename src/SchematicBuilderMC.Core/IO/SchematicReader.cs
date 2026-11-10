using fNbt;
using SchematicBuilderMC.Core.Exceptions;
using SchematicBuilderMC.Core.IO.Internal;

namespace SchematicBuilderMC.Core;

/// <summary>
/// Default implementation of <see cref="ISchematicReader"/> supporting Sponge (.schem) and Classic (.schematic) formats.
/// </summary>
public sealed class SchematicReader : ISchematicReader
{
    public static SchematicReader Instance { get; } = new();

    public Schematic Read(string filePath, SchematicFormat format = SchematicFormat.Auto)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Schematic file not found: {filePath}", filePath);

        using var stream = File.OpenRead(filePath);
        if (format == SchematicFormat.Auto)
        {
            format = DetectFormatFromExtension(filePath);
        }

        return Read(stream, format);
    }

    public Schematic Read(Stream stream, SchematicFormat format = SchematicFormat.Auto)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var nbtFile = new NbtFile();
        nbtFile.LoadFromStream(stream, NbtCompression.AutoDetect);

        var root = nbtFile.RootTag;
        if (root == null)
            throw new SchematicFormatException("NBT file contains no root compound tag.");

        var resolvedFormat = format == SchematicFormat.Auto ? DetectFormatFromNbt(root) : format;

        return resolvedFormat switch
        {
            SchematicFormat.Classic => ClassicSchematicHandler.Read(root),
            SchematicFormat.SpongeV1 or SchematicFormat.SpongeV2 or SchematicFormat.SpongeV3 => SpongeSchematicHandler.Read(root),
            _ => SpongeSchematicHandler.Read(root)
        };
    }

    public Schematic Read(byte[] data, SchematicFormat format = SchematicFormat.Auto)
    {
        ArgumentNullException.ThrowIfNull(data);
        using var ms = new MemoryStream(data, writable: false);
        return Read(ms, format);
    }

    public async Task<Schematic> ReadAsync(string filePath, SchematicFormat format = SchematicFormat.Auto, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Schematic file not found: {filePath}", filePath);

        byte[] bytes = await File.ReadAllBytesAsync(filePath, cancellationToken).ConfigureAwait(false);
        if (format == SchematicFormat.Auto)
        {
            format = DetectFormatFromExtension(filePath);
        }
        return Read(bytes, format);
    }

    public async Task<Schematic> ReadAsync(Stream stream, SchematicFormat format = SchematicFormat.Auto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, cancellationToken).ConfigureAwait(false);
        ms.Position = 0;
        return Read(ms, format);
    }

    private static SchematicFormat DetectFormatFromExtension(string path)
    {
        string ext = Path.GetExtension(path);
        if (ext.Equals(".schematic", StringComparison.OrdinalIgnoreCase))
            return SchematicFormat.Classic;

        if (ext.Equals(".schem", StringComparison.OrdinalIgnoreCase))
            return SchematicFormat.Auto;

        return SchematicFormat.Auto;
    }

    private static SchematicFormat DetectFormatFromNbt(NbtCompound root)
    {
        var tag = root;
        if (root.Contains("Schematic") && root["Schematic"] is NbtCompound nested)
        {
            tag = nested;
        }

        // Classic MCEdit format check
        if (tag.Contains("Materials") || (tag["Blocks"] is NbtByteArray && !tag.Contains("Palette")))
        {
            return SchematicFormat.Classic;
        }

        // Sponge format check
        if (tag["Version"] is NbtInt versionTag)
        {
            return versionTag.Value switch
            {
                1 => SchematicFormat.SpongeV1,
                3 => SchematicFormat.SpongeV3,
                _ => SchematicFormat.SpongeV2
            };
        }

        if (tag.Contains("Palette") || tag.Contains("BlockData") || tag.Contains("Blocks"))
        {
            return SchematicFormat.SpongeV2;
        }

        // Default fallback: SpongeV2
        return SchematicFormat.SpongeV2;
    }
}
