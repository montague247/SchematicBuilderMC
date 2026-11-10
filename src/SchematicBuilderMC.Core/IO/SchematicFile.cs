namespace SchematicBuilderMC.Core;

/// <summary>
/// Convenient static facade for reading and writing Minecraft schematic files.
/// </summary>
public static class SchematicFile
{
    private static ISchematicReader Reader => SchematicReader.Instance;
    private static ISchematicWriter Writer => SchematicWriter.Instance;

    /// <summary>
    /// Reads a schematic file from disk with automatic format detection.
    /// </summary>
    public static Schematic Read(string filePath, SchematicFormat format = SchematicFormat.Auto) =>
        Reader.Read(filePath, format);

    /// <summary>
    /// Reads a schematic from a stream.
    /// </summary>
    public static Schematic Read(Stream stream, SchematicFormat format = SchematicFormat.Auto) =>
        Reader.Read(stream, format);

    /// <summary>
    /// Reads a schematic from a byte array.
    /// </summary>
    public static Schematic Read(byte[] data, SchematicFormat format = SchematicFormat.Auto) =>
        Reader.Read(data, format);

    /// <summary>
    /// Reads a schematic file asynchronously with automatic format detection.
    /// </summary>
    public static Task<Schematic> ReadAsync(string filePath, SchematicFormat format = SchematicFormat.Auto, CancellationToken cancellationToken = default) =>
        Reader.ReadAsync(filePath, format, cancellationToken);

    /// <summary>
    /// Reads a schematic asynchronously from a stream.
    /// </summary>
    public static Task<Schematic> ReadAsync(Stream stream, SchematicFormat format = SchematicFormat.Auto, CancellationToken cancellationToken = default) =>
        Reader.ReadAsync(stream, format, cancellationToken);

    /// <summary>
    /// Writes a schematic to disk.
    /// </summary>
    public static void Write(Schematic schematic, string filePath, SchematicFormat format = SchematicFormat.SpongeV2, SchematicCompression compression = SchematicCompression.GZip) =>
        Writer.Write(schematic, filePath, format, compression);

    /// <summary>
    /// Writes a schematic to an open stream.
    /// </summary>
    public static void Write(Schematic schematic, Stream stream, SchematicFormat format = SchematicFormat.SpongeV2, SchematicCompression compression = SchematicCompression.GZip) =>
        Writer.Write(schematic, stream, format, compression);

    /// <summary>
    /// Writes a schematic to an in-memory byte array.
    /// </summary>
    public static byte[] WriteToBytes(Schematic schematic, SchematicFormat format = SchematicFormat.SpongeV2, SchematicCompression compression = SchematicCompression.GZip) =>
        Writer.WriteToBytes(schematic, format, compression);

    /// <summary>
    /// Writes a schematic asynchronously to disk.
    /// </summary>
    public static Task WriteAsync(Schematic schematic, string filePath, SchematicFormat format = SchematicFormat.SpongeV2, SchematicCompression compression = SchematicCompression.GZip, CancellationToken cancellationToken = default) =>
        Writer.WriteAsync(schematic, filePath, format, compression, cancellationToken);

    /// <summary>
    /// Writes a schematic asynchronously to an open stream.
    /// </summary>
    public static Task WriteAsync(Schematic schematic, Stream stream, SchematicFormat format = SchematicFormat.SpongeV2, SchematicCompression compression = SchematicCompression.GZip, CancellationToken cancellationToken = default) =>
        Writer.WriteAsync(schematic, stream, format, compression, cancellationToken);
}
