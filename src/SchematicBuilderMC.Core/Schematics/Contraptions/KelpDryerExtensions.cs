using SchematicBuilderMC.Builder;

namespace SchematicBuilderMC.Schematics.Contraptions;

/// <summary>
/// Fluent <see cref="SchematicBuilder"/> extensions for placing <see cref="KelpDryerSchematicCreator"/> contraptions.
/// </summary>
public static class KelpDryerExtensions
{
    /// <summary>
    /// Places a <see cref="KelpDryerSchematicCreator"/> into the schematic starting at (startX, startY, startZ).
    /// </summary>
    public static SchematicBuilder Create(this SchematicBuilder builder, KelpDryerSchematicCreator kelpDryer, int startX = 0, int startY = 0, int startZ = 0)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(kelpDryer);
        return kelpDryer.Place(builder, startX, startY, startZ);
    }
}
