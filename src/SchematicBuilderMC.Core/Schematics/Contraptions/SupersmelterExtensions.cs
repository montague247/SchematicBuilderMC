using SchematicBuilderMC.Builder;

namespace SchematicBuilderMC.Schematics.Contraptions;

/// <summary>
/// Fluent <see cref="SchematicBuilder"/> extensions for placing <see cref="SupersmelterSchematicCreator"/> contraptions.
/// </summary>
public static class SupersmelterExtensions
{
    /// <summary>
    /// Places a <see cref="SupersmelterSchematicCreator"/> into the schematic starting at (startX, startY, startZ).
    /// </summary>
    public static SchematicBuilder Create(this SchematicBuilder builder, SupersmelterSchematicCreator supersmelter, int startX = 0, int startY = 0, int startZ = 0)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(supersmelter);
        return supersmelter.Place(builder, startX, startY, startZ);
    }
}
