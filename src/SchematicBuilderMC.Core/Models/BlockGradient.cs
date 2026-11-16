using SchematicBuilderMC.Enums;

namespace SchematicBuilderMC.Models;

/// <summary>
/// Represents a multi-block color/material gradient and its transition rule across a geometric shape.
/// </summary>
public sealed class BlockGradient
{
    private readonly double[] _cumulativeBounds;

    /// <summary>
    /// Ordered list of block states used across the shape.
    /// </summary>
    public IReadOnlyList<BlockState> Blocks { get; }

    /// <summary>
    /// Normalized weights (summing to 1.0) corresponding to each entry in <see cref="Blocks"/>.
    /// </summary>
    public IReadOnlyList<double> Weights { get; }

    /// <summary>
    /// Transition mode between blocks.
    /// </summary>
    public GradientTransition Transition { get; }

    /// <summary>
    /// Whether boundary transitions are randomized.
    /// </summary>
    public bool IsFuzzy { get; }

    /// <summary>
    /// Optional deterministic seed used when <see cref="IsFuzzy"/> is <c>true</c>.
    /// </summary>
    public int? Seed { get; }

    public BlockGradient(
        IEnumerable<BlockState> blocks,
        GradientTransition transition = GradientTransition.Strict,
        IEnumerable<double>? weights = null,
        bool isFuzzy = false,
        int? seed = null)
    {
        ArgumentNullException.ThrowIfNull(blocks);
        if (!Enum.IsDefined(transition))
        {
            throw new ArgumentOutOfRangeException(nameof(transition), transition, "Unsupported gradient transition value.");
        }

        var blockList = new List<BlockState>();
        foreach (var block in blocks)
        {
            ArgumentNullException.ThrowIfNull(block, nameof(blocks));
            blockList.Add(block);
        }

        if (blockList.Count == 0)
        {
            throw new ArgumentException("At least one block must be specified.", nameof(blocks));
        }

        Blocks = blockList.AsReadOnly();
        Transition = transition;
        IsFuzzy = transition == GradientTransition.Fuzzy || isFuzzy;
        Seed = seed;

        var normalizedWeights = NormalizeWeights(blockList.Count, transition, weights);
        Weights = Array.AsReadOnly(normalizedWeights);

        _cumulativeBounds = new double[blockList.Count];
        double cumulative = 0;
        for (int i = 0; i < normalizedWeights.Length; i++)
        {
            cumulative += normalizedWeights[i];
            _cumulativeBounds[i] = cumulative;
        }
        _cumulativeBounds[^1] = 1.0;
    }

    public BlockGradient(
        IEnumerable<string> blocks,
        GradientTransition transition = GradientTransition.Strict,
        IEnumerable<double>? weights = null,
        bool isFuzzy = false,
        int? seed = null)
        : this(ParseBlocks(blocks), transition, weights, isFuzzy, seed)
    {
    }

    public static BlockGradient Strict(params string[] blocks) =>
        new(blocks, GradientTransition.Strict);

    public static BlockGradient Strict(params BlockState[] blocks) =>
        new(blocks, GradientTransition.Strict);

    public static BlockGradient Weighted(IEnumerable<string> blocks, params double[] weights) =>
        new(blocks, GradientTransition.Weighted, weights);

    public static BlockGradient Weighted(IEnumerable<BlockState> blocks, params double[] weights) =>
        new(blocks, GradientTransition.Weighted, weights);

    public static BlockGradient Fuzzy(params string[] blocks) =>
        new(blocks, GradientTransition.Fuzzy);

    public static BlockGradient Fuzzy(IEnumerable<string> blocks, params double[] weights) =>
        new(blocks, GradientTransition.Fuzzy, weights);

    public static BlockGradient Fuzzy(IEnumerable<BlockState> blocks, params double[] weights) =>
        new(blocks, GradientTransition.Fuzzy, weights);

    internal Random? CreateRandom() =>
        IsFuzzy ? (Seed.HasValue ? new Random(Seed.Value) : Random.Shared) : null;

    /// <summary>
    /// Selects a block state for the given step index out of <paramref name="totalSteps"/>.
    /// </summary>
    public BlockState GetBlock(int stepIndex, int totalSteps, Random? random = null)
    {
        if (Blocks.Count == 1)
        {
            return Blocks[0];
        }

        double t = totalSteps <= 1
            ? 0.0
            : Math.Clamp((stepIndex + 0.5) / totalSteps, 0.0, 1.0);

        int segmentIndex = Blocks.Count - 1;
        for (int i = 0; i < _cumulativeBounds.Length; i++)
        {
            if (t <= _cumulativeBounds[i] + 1e-12)
            {
                segmentIndex = i;
                break;
            }
        }

        if (!IsFuzzy)
        {
            return Blocks[segmentIndex];
        }

        random ??= CreateRandom()!;

        int boundaryIndex = 0;
        double minDistance = double.MaxValue;
        for (int i = 0; i < Blocks.Count - 1; i++)
        {
            double dist = Math.Abs(t - _cumulativeBounds[i]);
            if (dist < minDistance)
            {
                minDistance = dist;
                boundaryIndex = i;
            }
        }

        double boundary = _cumulativeBounds[boundaryIndex];
        double stepWidth = 1.0 / Math.Max(2, totalSteps);
        double adjacentMinWeight = Math.Min(Weights[boundaryIndex], Weights[boundaryIndex + 1]);
        double halfBand = Math.Max(stepWidth, 0.35 * adjacentMinWeight);

        if (Weights[boundaryIndex] > 0 &&
            Weights[boundaryIndex + 1] > 0 &&
            minDistance < halfBand &&
            halfBand > 1e-12)
        {
            double pNext = Math.Clamp((t - (boundary - halfBand)) / (2.0 * halfBand), 0.0, 1.0);
            return random.NextDouble() < pNext ? Blocks[boundaryIndex + 1] : Blocks[boundaryIndex];
        }

        return Blocks[segmentIndex];
    }

    private static IEnumerable<BlockState> ParseBlocks(IEnumerable<string> blocks)
    {
        ArgumentNullException.ThrowIfNull(blocks);

        var parsed = new List<BlockState>();
        foreach (string blockId in blocks)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(blockId, nameof(blocks));
            parsed.Add(BlockState.Parse(blockId));
        }

        return parsed;
    }

    private static double[] NormalizeWeights(int blockCount, GradientTransition transition, IEnumerable<double>? weights)
    {
        var weightList = weights?.ToList();

        if (transition == GradientTransition.Weighted && blockCount > 1 && (weightList is null || weightList.Count == 0))
        {
            throw new ArgumentException("Weights must be provided when using Weighted gradient transition with multiple blocks.", nameof(weights));
        }

        if (weightList is { Count: > 0 } && blockCount > 1)
        {
            foreach (double w in weightList)
            {
                if (!double.IsFinite(w) || w < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(weights), w, "Gradient weights must be finite and zero or greater.");
                }
            }

            double[] normalized = new double[blockCount];
            if (weightList.Count == blockCount - 1)
            {
                bool isPercentageScale = weightList.Any(w => w > 1.0);
                double maxTotal = isPercentageScale ? 100.0 : 1.0;
                double sum = weightList.Sum();
                if (sum > maxTotal + 1e-9)
                {
                    throw new ArgumentOutOfRangeException(nameof(weights), sum, "The sum of the specified weights cannot exceed 100% (or 1.0).");
                }

                for (int i = 0; i < weightList.Count; i++)
                {
                    normalized[i] = weightList[i] / maxTotal;
                }

                normalized[blockCount - 1] = Math.Max(0.0, maxTotal - sum) / maxTotal;
                return normalized;
            }

            if (weightList.Count == blockCount)
            {
                double sum = weightList.Sum();
                if (sum <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(weights), sum, "Total weight must be greater than zero.");
                }

                for (int i = 0; i < blockCount; i++)
                {
                    normalized[i] = weightList[i] / sum;
                }

                return normalized;
            }

            throw new ArgumentException(
                $"Expected either {blockCount - 1} or {blockCount} weights for {blockCount} blocks, but received {weightList.Count}.",
                nameof(weights));
        }

        double[] even = new double[blockCount];
        double equalWeight = 1.0 / blockCount;
        Array.Fill(even, equalWeight);
        return even;
    }
}
