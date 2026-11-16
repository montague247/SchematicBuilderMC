namespace SchematicBuilderMC.Enums;

/// <summary>
/// Represents a clockwise horizontal (Y-axis) rotation in 90-degree increments.
/// </summary>
public enum Rotation
{
    /// <summary>
    /// No rotation (0 degrees).
    /// </summary>
    None = 0,

    /// <summary>
    /// 90 degrees clockwise around the Y axis (North -> East -> South -> West -> North).
    /// </summary>
    Clockwise90 = 90,

    /// <summary>
    /// 180 degrees clockwise around the Y axis (North &lt;-&gt; South, East &lt;-&gt; West).
    /// </summary>
    Clockwise180 = 180,

    /// <summary>
    /// 270 degrees clockwise (90 degrees counter-clockwise) around the Y axis (North -> West -> South -> East -> North).
    /// </summary>
    Clockwise270 = 270
}
