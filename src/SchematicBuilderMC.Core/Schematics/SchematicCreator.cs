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

    /// <summary>
    /// Places this schematic into an existing <see cref="SchematicBuilder"/> starting at (startX, startY, startZ),
    /// extending its dimensions automatically if needed.
    /// </summary>
    public virtual SchematicBuilder Place(SchematicBuilder builder, int startX = 0, int startY = 0, int startZ = 0)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.Place(Build(), startX, startY, startZ);
    }

    protected static void EnsurePlacementBounds(SchematicBuilder builder, int startX, int startY, int startZ, int width, int height, int length)
    {
        ArgumentNullException.ThrowIfNull(builder);
        if (startX < 0)
            throw new ArgumentOutOfRangeException(nameof(startX), startX, "StartX must be zero or greater.");
        if (startY < 0)
            throw new ArgumentOutOfRangeException(nameof(startY), startY, "StartY must be zero or greater.");
        if (startZ < 0)
            throw new ArgumentOutOfRangeException(nameof(startZ), startZ, "StartZ must be zero or greater.");

        builder.EnsureDimensions(checked(startX + width), checked(startY + height), checked(startZ + length));
    }

    public Schematic Build()
    {
        var builder = Create();

        if (Rotation != Rotation.None &&
            builder.Rotation == Rotation.None)
        {
            builder.WithRotation(Rotation);
        }

        return builder.Build();
    }
}
