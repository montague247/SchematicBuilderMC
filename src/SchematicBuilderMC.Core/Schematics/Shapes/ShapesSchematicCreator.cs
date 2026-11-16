namespace SchematicBuilderMC.Schematics.Shapes;

public abstract class ShapesSchematicCreator : SchematicCreator
{
    /// <summary>
    /// Resolution and smoothness multiplier applied to procedural shapes (&gt; 0). Higher values produce finer, rounder forms.
    /// </summary>
    public double SmoothnessFactor { get; set; } = 1.0;

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
}
