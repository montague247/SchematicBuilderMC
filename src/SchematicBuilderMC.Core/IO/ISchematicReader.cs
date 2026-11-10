namespace SchematicBuilderMC.Core;

/// <summary>
/// Interface for reading Minecraft schematic files.
/// </summary>
public interface ISchematicReader
{
    /// <summary>
    /// Reads a schematic from a file on disk.
    /// </summary>
    Schematic Read(string filePath, SchematicFormat format = SchematicFormat.Auto);

    /// <summary>
    /// Reads a schematic from an open stream.
    /// </summary>
    Schematic Read(Stream stream, SchematicFormat format = SchematicFormat.Auto);

    /// <summary>
    /// Reads a schematic from a byte array.
    /// </summary>
    Schematic Read(byte[] data, SchematicFormat format = SchematicFormat.Auto);

    /// <summary>
    /// Reads a schematic asynchronously from a file on disk.
    /// </summary>
    Task<Schematic> ReadAsync(string filePath, SchematicFormat format = SchematicFormat.Auto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads a schematic asynchronously from an open stream.
    /// </summary>
    Task<Schematic> ReadAsync(Stream stream, SchematicFormat format = SchematicFormat.Auto, CancellationToken cancellationToken = default);
}
