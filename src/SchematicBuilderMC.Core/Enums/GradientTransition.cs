namespace SchematicBuilderMC.Enums;

/// <summary>
/// Defines how multiple blocks transition across a geometric shape's layers or rings.
/// </summary>
public enum GradientTransition
{
    /// <summary>
    /// Evenly distributes layers across all blocks with sharp transitions.
    /// </summary>
    Strict = 0,

    /// <summary>
    /// Distributes layers across blocks according to specified percentage/weight values with sharp transitions.
    /// </summary>
    Weighted = 1,

    /// <summary>
    /// Randomizes block transitions across segment boundaries (uses weighted distribution if weights are provided, otherwise even distribution).
    /// </summary>
    Fuzzy = 2
}
