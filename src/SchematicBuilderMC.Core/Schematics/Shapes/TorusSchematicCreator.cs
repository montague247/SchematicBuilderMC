using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;
using SchematicBuilderMC.Registries;

namespace SchematicBuilderMC.Schematics.Shapes;

/// <summary>
/// Generates a filled or hollow 3D torus schematic oriented around <see cref="Axis"/>.
/// </summary>
public sealed class TorusSchematicCreator : ShapesSchematicCreator
{
    /// <summary>
    /// Distance from the center of the torus to the center of the tube (greater than 0).
    /// </summary>
    public int MajorRadius { get; set; } = 10;

    /// <summary>
    /// Radius of the torus tube cross-section (non-negative and less than <see cref="MajorRadius"/>).
    /// </summary>
    public int MinorRadius { get; set; } = 4;

    /// <summary>
    /// Block identifier used to build the torus.
    /// </summary>
    public string Block { get; set; } = Minecraft.Blocks.GoldBlock;

    /// <summary>
    /// Whether the torus tube interior is filled (<c>true</c>) or hollow shell only (<c>false</c>).
    /// </summary>
    public bool Filled { get; set; } = true;

    /// <summary>
    /// Axis around which the torus ring revolves.
    /// </summary>
    public Axis Axis { get; set; } = Axis.Y;

    /// <summary>
    /// Places the torus into an existing <see cref="SchematicBuilder"/> with its bounding box starting at (startX, startY, startZ),
    /// extending its dimensions automatically if needed.
    /// </summary>
    public override SchematicBuilder Place(SchematicBuilder builder, int startX = 0, int startY = 0, int startZ = 0)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ValidateConfiguration();

        var (effectiveMajorRadius, effectiveMinorRadius) = GetEffectiveRadii();
        var (width, height, length) = GetDimensions(effectiveMajorRadius, effectiveMinorRadius);
        EnsurePlacementBounds(builder, startX, startY, startZ, width, height, length);

        int outerExtent = effectiveMajorRadius + effectiveMinorRadius;
        var (cx, cy, cz) = Axis switch
        {
            Axis.X => (startX + effectiveMinorRadius, startY + outerExtent, startZ + outerExtent),
            Axis.Y => (startX + outerExtent, startY + effectiveMinorRadius, startZ + outerExtent),
            Axis.Z => (startX + outerExtent, startY + outerExtent, startZ + effectiveMinorRadius),
            _ => throw new ArgumentOutOfRangeException(nameof(Axis), Axis, "Unsupported axis value.")
        };

        return builder.SetTorus(cx, cy, cz, effectiveMajorRadius, effectiveMinorRadius, Block, Filled, Axis, SmoothnessFactor);
    }

    /// <summary>
    /// Creates a new schematic containing this torus.
    /// </summary>
    public override SchematicBuilder Create()
    {
        ValidateConfiguration();

        var (effectiveMajorRadius, effectiveMinorRadius) = GetEffectiveRadii();
        var (width, height, length) = GetDimensions(effectiveMajorRadius, effectiveMinorRadius);
        var builder = SchematicBuilder.Create(width, height, length)
            .WithRotation(Rotation)
            .WithOffset(0, 0, 0)
            .WithName("Torus")
            .WithAuthor("SchematicBuilderMC")
            .WithDescription($"A {(Filled ? "filled" : "hollow")} torus (major radius {effectiveMajorRadius}, minor radius {effectiveMinorRadius}) around axis {Axis}.");

        return Place(builder, 0, 0, 0);
    }

    private (int MajorRadius, int MinorRadius) GetEffectiveRadii() =>
        (ScaleDimension(MajorRadius, minimum: 1), ScaleDimension(MinorRadius, minimum: 0));

    private (int Width, int Height, int Length) GetDimensions(int effectiveMajorRadius, int effectiveMinorRadius)
    {
        int outerSize = 2 * (effectiveMajorRadius + effectiveMinorRadius) + 1;
        int thickness = 2 * effectiveMinorRadius + 1;
        return Axis switch
        {
            Axis.X => (thickness, outerSize, outerSize),
            Axis.Y => (outerSize, thickness, outerSize),
            Axis.Z => (outerSize, outerSize, thickness),
            _ => throw new ArgumentOutOfRangeException(nameof(Axis), Axis, "Unsupported axis value.")
        };
    }

    private void ValidateConfiguration()
    {
        if (MajorRadius <= 0)
            throw new ArgumentOutOfRangeException(nameof(MajorRadius), MajorRadius, "MajorRadius must be greater than zero.");
        if (MinorRadius < 0 || MinorRadius >= MajorRadius)
            throw new ArgumentOutOfRangeException(nameof(MinorRadius), MinorRadius, "MinorRadius must be non-negative and less than MajorRadius.");
        ValidateSmoothnessFactor();
        var (effectiveMajorRadius, effectiveMinorRadius) = GetEffectiveRadii();
        if (effectiveMinorRadius >= effectiveMajorRadius)
            throw new ArgumentOutOfRangeException(nameof(SmoothnessFactor), SmoothnessFactor, "Scaled MinorRadius must remain less than scaled MajorRadius.");
        if (!Enum.IsDefined(Axis))
            throw new ArgumentOutOfRangeException(nameof(Axis), Axis, "Unsupported axis value.");
        ArgumentException.ThrowIfNullOrWhiteSpace(Block);
    }
}
