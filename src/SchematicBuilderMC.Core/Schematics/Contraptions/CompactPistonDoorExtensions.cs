using SchematicBuilderMC.Builder;

namespace SchematicBuilderMC.Schematics.Contraptions;

/// <summary>
/// Fluent <see cref="SchematicBuilder"/> extensions for placing <see cref="CompactPistonDoorSchematicCreator"/> contraptions.
/// </summary>
public static class CompactPistonDoorExtensions
{
    /// <summary>
    /// Places a <see cref="CompactPistonDoorSchematicCreator"/> into the schematic starting at (startX, startY, startZ).
    /// </summary>
    public static SchematicBuilder Create(this SchematicBuilder builder, CompactPistonDoorSchematicCreator door, int startX = 0, int startY = 0, int startZ = 0)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(door);
        return door.Place(builder, startX, startY, startZ);
    }
}
