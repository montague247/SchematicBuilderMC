using SchematicBuilderMC.Enums;
using SchematicBuilderMC.Models;

namespace SchematicBuilderMC.IO;

/// <summary>
/// Interface for writing Minecraft schematic files.
/// </summary>
public interface ISchematicWriter
{
    /// <summary>
    /// Writes a schematic to a file on disk.
    /// </summary>
    void Write(Schematic schematic, string filePath, SchematicFormat format = SchematicFormat.SpongeV2, SchematicCompression compression = SchematicCompression.GZip);

    /// <summary>
    /// Writes a schematic to an open stream.
    /// </summary>
    void Write(Schematic schematic, Stream stream, SchematicFormat format = SchematicFormat.SpongeV2, SchematicCompression compression = SchematicCompression.GZip);

    /// <summary>
    /// Writes a schematic to an in-memory byte array.
    /// </summary>
    byte[] WriteToBytes(Schematic schematic, SchematicFormat format = SchematicFormat.SpongeV2, SchematicCompression compression = SchematicCompression.GZip);

    /// <summary>
    /// Writes a schematic asynchronously to a file on disk.
    /// </summary>
    Task WriteAsync(Schematic schematic, string filePath, SchematicFormat format = SchematicFormat.SpongeV2, SchematicCompression compression = SchematicCompression.GZip, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes a schematic asynchronously to an open stream.
    /// </summary>
    Task WriteAsync(Schematic schematic, Stream stream, SchematicFormat format = SchematicFormat.SpongeV2, SchematicCompression compression = SchematicCompression.GZip, CancellationToken cancellationToken = default);
}
