namespace SchematicBuilderMC.Core;

/// <summary>
/// Supported Minecraft schematic file formats.
/// </summary>
public enum SchematicFormat
{
    /// <summary>
    /// Automatically detects the schematic format from the file extension or NBT contents.
    /// </summary>
    Auto = 0,

    /// <summary>
    /// Sponge Schematic Version 1 (Minecraft 1.13+).
    /// </summary>
    SpongeV1 = 1,

    /// <summary>
    /// Sponge Schematic Version 2 (WorldEdit 7.1+, FAWE, modern Minecraft).
    /// </summary>
    SpongeV2 = 2,

    /// <summary>
    /// Sponge Schematic Version 3 (Latest Sponge/WorldEdit specification with nested Blocks compound).
    /// </summary>
    SpongeV3 = 3,

    /// <summary>
    /// Classic MCEdit / WorldEdit legacy format (.schematic, Minecraft &lt;= 1.12.2).
    /// </summary>
    Classic = 4
}
