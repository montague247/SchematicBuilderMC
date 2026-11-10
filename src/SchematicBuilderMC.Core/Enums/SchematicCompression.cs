namespace SchematicBuilderMC.Core;

/// <summary>
/// Compression methods for schematic files.
/// </summary>
public enum SchematicCompression
{
    /// <summary>
    /// GZip compression (standard for Minecraft schematics).
    /// </summary>
    GZip = 0,

    /// <summary>
    /// ZLib / Deflate compression.
    /// </summary>
    ZLib = 1,

    /// <summary>
    /// Uncompressed raw NBT.
    /// </summary>
    None = 2
}
