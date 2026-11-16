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
    /// Resolution and smoothness multiplier applied to procedural shapes (&gt; 0). Higher values produce finer, rounder forms.
    /// </summary>
    public double SmoothnessFactor { get; set; } = 1.0;

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

    /// <summary>
    /// Sets <see cref="SmoothnessFactor"/> before creating the schematic and returns this creator instance.
    /// </summary>
    public SchematicCreator WithSmoothnessFactor(double smoothnessFactor)
    {
        ValidateSmoothnessFactorValue(smoothnessFactor);
        SmoothnessFactor = smoothnessFactor;
        return this;
    }

    protected void ValidateSmoothnessFactor() => ValidateSmoothnessFactorValue(SmoothnessFactor);

    protected int ScaleDimension(int value, int minimum = 0)
    {
        ValidateSmoothnessFactor();
        int scaled = (int)Math.Round(value * SmoothnessFactor, MidpointRounding.AwayFromZero);
        return Math.Max(minimum, scaled);
    }

    private static void ValidateSmoothnessFactorValue(double smoothnessFactor)
    {
        if (!double.IsFinite(smoothnessFactor) || smoothnessFactor <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(smoothnessFactor), smoothnessFactor, "SmoothnessFactor must be finite and greater than zero.");
        }
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
