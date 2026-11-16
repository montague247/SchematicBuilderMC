using SchematicBuilderMC.Enums;
using SchematicBuilderMC.Models;

namespace SchematicBuilderMC.Schematics.Shapes;

public abstract class ShapesSchematicCreator : SchematicCreator
{
    /// <summary>
    /// Resolution and smoothness multiplier applied to procedural shapes (&gt; 0). Higher values produce finer, rounder forms.
    /// </summary>
    public double SmoothnessFactor { get; set; } = 1.0;

    /// <summary>
    /// Optional ordered list of block identifiers used to form a color/material gradient across the shape.
    /// When empty, the single <c>Block</c> property on the shape creator is used.
    /// </summary>
    public IReadOnlyList<string> Blocks { get; set; } = [];

    /// <summary>
    /// Transition mode between blocks in <see cref="Blocks"/>.
    /// </summary>
    public GradientTransition Transition { get; set; } = GradientTransition.Strict;

    /// <summary>
    /// Optional percentage/weight values for <see cref="GradientTransition.Weighted"/> or <see cref="GradientTransition.Fuzzy"/>.
    /// Supports <c>Blocks.Count - 1</c> values (the last block receives the remainder) or <c>Blocks.Count</c> values.
    /// </summary>
    public IReadOnlyList<double> TransitionWeights { get; set; } = [];

    /// <summary>
    /// Randomizes transitions at segment boundaries when using <see cref="GradientTransition.Strict"/> or <see cref="GradientTransition.Weighted"/>.
    /// </summary>
    public bool FuzzyTransition { get; set; }

    /// <summary>
    /// Optional random seed for deterministic <see cref="GradientTransition.Fuzzy"/> or <see cref="FuzzyTransition"/> output.
    /// </summary>
    public int? RandomSeed { get; set; }

    /// <summary>
    /// Sets <see cref="SmoothnessFactor"/> before creating the schematic and returns this creator instance.
    /// </summary>
    public SchematicCreator WithSmoothnessFactor(double smoothnessFactor)
    {
        ValidateSmoothnessFactorValue(smoothnessFactor);
        SmoothnessFactor = smoothnessFactor;
        return this;
    }

    /// <summary>
    /// Sets the gradient block list and returns this shape creator.
    /// </summary>
    public ShapesSchematicCreator WithBlocks(params string[] blocks)
    {
        ArgumentNullException.ThrowIfNull(blocks);
        Blocks = [.. blocks];
        return this;
    }

    /// <summary>
    /// Configures the gradient transition mode and blocks, and returns this shape creator.
    /// </summary>
    public ShapesSchematicCreator WithGradient(GradientTransition transition, params string[] blocks) =>
        WithGradient(blocks, transition);

    /// <summary>
    /// Configures gradient blocks, transition mode, weights, optional fuzziness, and optional random seed.
    /// </summary>
    public ShapesSchematicCreator WithGradient(
        IEnumerable<string> blocks,
        GradientTransition transition = GradientTransition.Strict,
        IEnumerable<double>? weights = null,
        bool fuzzy = false,
        int? seed = null)
    {
        ArgumentNullException.ThrowIfNull(blocks);
        Blocks = [.. blocks];
        Transition = transition;
        TransitionWeights = weights is null ? [] : [.. weights];
        FuzzyTransition = fuzzy || transition == GradientTransition.Fuzzy;
        RandomSeed = seed;
        return this;
    }

    /// <summary>
    /// Configures a <see cref="GradientTransition.Weighted"/> gradient with percentages/weights and returns this shape creator.
    /// </summary>
    public ShapesSchematicCreator WithWeightedGradient(IEnumerable<string> blocks, params double[] weights)
    {
        ArgumentNullException.ThrowIfNull(blocks);
        ArgumentNullException.ThrowIfNull(weights);
        return WithGradient(blocks, GradientTransition.Weighted, weights, fuzzy: false);
    }

    /// <summary>
    /// Configures a <see cref="GradientTransition.Fuzzy"/> gradient with optional weights and returns this shape creator.
    /// </summary>
    public ShapesSchematicCreator WithFuzzyGradient(IEnumerable<string> blocks, params double[] weights)
    {
        ArgumentNullException.ThrowIfNull(blocks);
        ArgumentNullException.ThrowIfNull(weights);
        return WithGradient(blocks, GradientTransition.Fuzzy, weights.Length > 0 ? weights : null, fuzzy: true);
    }

    protected BlockGradient ResolveGradient(string fallbackBlock)
    {
        if (Blocks is { Count: > 0 })
        {
            return new BlockGradient(
                Blocks,
                Transition,
                TransitionWeights.Count > 0 ? TransitionWeights : null,
                FuzzyTransition || Transition == GradientTransition.Fuzzy,
                RandomSeed);
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(fallbackBlock);
        return new BlockGradient([fallbackBlock]);
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
