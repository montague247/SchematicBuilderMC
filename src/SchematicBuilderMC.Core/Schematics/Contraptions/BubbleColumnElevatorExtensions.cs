using SchematicBuilderMC.Builder;

namespace SchematicBuilderMC.Schematics.Contraptions;

/// <summary>
/// Fluent <see cref="SchematicBuilder"/> extensions for placing <see cref="BubbleColumnElevatorSchematicCreator"/> contraptions.
/// </summary>
public static class BubbleColumnElevatorExtensions
{
    /// <summary>
    /// Places a <see cref="BubbleColumnElevatorSchematicCreator"/> into the schematic starting at (startX, startY, startZ).
    /// </summary>
    public static SchematicBuilder Create(this SchematicBuilder builder, BubbleColumnElevatorSchematicCreator elevator, int startX = 0, int startY = 0, int startZ = 0)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(elevator);
        return elevator.Place(builder, startX, startY, startZ);
    }
}
