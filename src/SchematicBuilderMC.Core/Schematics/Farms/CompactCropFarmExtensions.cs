using SchematicBuilderMC.Builder;

namespace SchematicBuilderMC.Schematics.Farms;

/// <summary>
/// Fluent <see cref="SchematicBuilder"/> extensions for placing <see cref="CompactCropFarmSchematicCreator"/> farms.
/// </summary>
public static class CompactCropFarmExtensions
{
    /// <summary>
    /// Places a <see cref="CompactCropFarmSchematicCreator"/> into the schematic starting at (startX, startY, startZ).
    /// </summary>
    public static SchematicBuilder Create(this SchematicBuilder builder, CompactCropFarmSchematicCreator farm, int startX = 0, int startY = 0, int startZ = 0)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(farm);
        return farm.Place(builder, startX, startY, startZ);
    }
}
