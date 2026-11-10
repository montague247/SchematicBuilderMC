using SchematicBuilderMC.Enums;
using SchematicBuilderMC.Generators;
using SchematicBuilderMC.Models;

namespace SchematicBuilderMC.IO;

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

    /// <summary>
    /// Reads a schematic from a file and generates C# fluent builder source code that reconstructs it.
    /// </summary>
    /// <param name="filePath">Path to the schematic file (.schem, .schematic).</param>
    /// <param name="options">Generator configuration options.</param>
    /// <param name="format">Schematic format (default: <see cref="SchematicFormat.Auto"/>).</param>
    /// <returns>A string of C# fluent builder code.</returns>
    public static string ToFluentBuilderCode(string filePath, FluentCodeGeneratorOptions? options = null, SchematicFormat format = SchematicFormat.Auto) =>
        FluentBuilderCodeGenerator.GenerateFromFile(filePath, options, format);

    /// <summary>
    /// Reads a schematic from a file and writes generated C# fluent builder source code to an output file.
    /// </summary>
    /// <param name="inputFilePath">Path to the input schematic file.</param>
    /// <param name="outputFilePath">Path where the generated C# code will be saved.</param>
    /// <param name="options">Generator configuration options.</param>
    /// <param name="format">Schematic format (default: <see cref="SchematicFormat.Auto"/>).</param>
    public static void GenerateFluentCode(string inputFilePath, string outputFilePath, FluentCodeGeneratorOptions? options = null, SchematicFormat format = SchematicFormat.Auto) =>
        FluentBuilderCodeGenerator.GenerateFromFileToFile(inputFilePath, outputFilePath, options, format);
}
