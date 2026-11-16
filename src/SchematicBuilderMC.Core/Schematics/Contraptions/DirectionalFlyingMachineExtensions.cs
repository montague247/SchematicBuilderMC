using SchematicBuilderMC.Builder;

namespace SchematicBuilderMC.Schematics.Contraptions;

/// <summary>
/// Fluent <see cref="SchematicBuilder"/> extensions for placing <see cref="DirectionalFlyingMachineSchematicCreator"/> contraptions.
/// </summary>
public static class DirectionalFlyingMachineExtensions
{
    /// <summary>
    /// Places a <see cref="DirectionalFlyingMachineSchematicCreator"/> into the schematic starting at (startX, startY, startZ).
    /// </summary>
    public static SchematicBuilder Create(this SchematicBuilder builder, DirectionalFlyingMachineSchematicCreator machine, int startX = 0, int startY = 0, int startZ = 0)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(machine);
        return machine.Place(builder, startX, startY, startZ);
    }
}
