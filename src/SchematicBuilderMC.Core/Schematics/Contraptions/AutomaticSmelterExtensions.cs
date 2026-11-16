using SchematicBuilderMC.Builder;

namespace SchematicBuilderMC.Schematics.Contraptions;

/// <summary>
/// Fluent <see cref="SchematicBuilder"/> extensions for placing <see cref="AutomaticSmelterSchematicCreator"/> contraptions.
/// </summary>
public static class AutomaticSmelterExtensions
{
    /// <summary>
    /// Places an <see cref="AutomaticSmelterSchematicCreator"/> into the schematic starting at (startX, startY, startZ).
    /// </summary>
    public static SchematicBuilder Create(this SchematicBuilder builder, AutomaticSmelterSchematicCreator smelter, int startX = 0, int startY = 0, int startZ = 0)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(smelter);
        return smelter.Place(builder, startX, startY, startZ);
    }
}
