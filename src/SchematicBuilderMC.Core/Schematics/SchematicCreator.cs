using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;
using SchematicBuilderMC.Models;

namespace SchematicBuilderMC.Schematics;

public abstract class SchematicCreator
{
    /// <summary>
    /// Clockwise horizontal rotation applied when building the schematic.
    /// </summary>
    public Rotation Rotation { get; set; } = Rotation.None;

    /// <summary>
    /// Sets <see cref="Rotation"/> before creating the schematic and returns this creator instance.
    /// </summary>
    public SchematicCreator WithRotation(Rotation rotation)
    {
        if (!Enum.IsDefined(rotation))
        {
            throw new ArgumentOutOfRangeException(nameof(rotation), rotation, "Unsupported rotation value.");
        }

        Rotation = rotation;
        return this;
    }

    public abstract SchematicBuilder Create();

    public Schematic Build()
    {
        var builder = Create();
        if (Rotation != Rotation.None && builder.Rotation == Rotation.None)
        {
            builder.WithRotation(Rotation);
        }
        return builder.Build();
    }
}
