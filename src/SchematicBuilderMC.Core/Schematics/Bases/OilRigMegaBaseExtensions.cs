using SchematicBuilderMC.Builder;

namespace SchematicBuilderMC.Schematics.Bases;

/// <summary>
/// Fluent <see cref="SchematicBuilder"/> extensions for placing <see cref="OilRigMegaBaseSchematicCreator"/> mega bases.
/// </summary>
public static class OilRigMegaBaseExtensions
{
    /// <summary>
    /// Places an <see cref="OilRigMegaBaseSchematicCreator"/> into the schematic starting at (startX, startY, startZ),
    /// extending its dimensions automatically if needed.
    /// </summary>
    public static SchematicBuilder Create(this SchematicBuilder builder, OilRigMegaBaseSchematicCreator oilRig, int startX = 0, int startY = 0, int startZ = 0)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(oilRig);
        return oilRig.Place(builder, startX, startY, startZ);
    }
}
