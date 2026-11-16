using SchematicBuilderMC.Builder;

namespace SchematicBuilderMC.Schematics.Contraptions;

/// <summary>
/// Fluent <see cref="SchematicBuilder"/> extensions for placing <see cref="ItemSortingModuleSchematicCreator"/> contraptions.
/// </summary>
public static class ItemSortingModuleExtensions
{
    /// <summary>
    /// Places an <see cref="ItemSortingModuleSchematicCreator"/> into the schematic starting at (startX, startY, startZ).
    /// </summary>
    public static SchematicBuilder Create(this SchematicBuilder builder, ItemSortingModuleSchematicCreator sorter, int startX = 0, int startY = 0, int startZ = 0)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(sorter);
        return sorter.Place(builder, startX, startY, startZ);
    }
}
