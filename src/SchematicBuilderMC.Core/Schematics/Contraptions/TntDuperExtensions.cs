using SchematicBuilderMC.Builder;

namespace SchematicBuilderMC.Schematics.Contraptions;

/// <summary>
/// Fluent <see cref="SchematicBuilder"/> extensions for placing <see cref="TntDuperSchematicCreator"/> contraptions.
/// </summary>
public static class TntDuperExtensions
{
    /// <summary>
    /// Places a <see cref="TntDuperSchematicCreator"/> into the schematic starting at <c>(startX, startY, startZ)</c>.
    /// </summary>
    public static SchematicBuilder Create(this SchematicBuilder builder, TntDuperSchematicCreator tntDuper, int startX = 0, int startY = 0, int startZ = 0)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(tntDuper);
        return tntDuper.Place(builder, startX, startY, startZ);
    }

    /// <summary>
    /// Places a <see cref="TntDuperSchematicCreator"/> into the schematic aligned so its TNT drops directly at
    /// <c>(targetX, targetY, targetZ)</c>, extending dimensions automatically if needed.
    /// </summary>
    public static SchematicBuilder CreateAtDropPosition(this SchematicBuilder builder, TntDuperSchematicCreator tntDuper, int targetX, int targetY, int targetZ)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(tntDuper);
        return tntDuper.PlaceAtDropPosition(builder, targetX, targetY, targetZ);
    }
}
