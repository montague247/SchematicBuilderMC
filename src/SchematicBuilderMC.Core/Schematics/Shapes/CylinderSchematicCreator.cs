using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;
using SchematicBuilderMC.Models;
using SchematicBuilderMC.Registries;

namespace SchematicBuilderMC.Schematics.Shapes;

/// <summary>
/// Generates a filled or hollow 3D cylinder schematic extruded along <see cref="Axis"/>.
/// </summary>
public sealed class CylinderSchematicCreator : ShapesSchematicCreator
{
    /// <summary>
    /// Cylinder cross-section radius in blocks (0 or greater).
    /// </summary>
    public int Radius { get; set; } = 6;

    /// <summary>
    /// Cylinder length along <see cref="Axis"/> (greater than 0).
    /// </summary>
    public int Height { get; set; } = 10;

    /// <summary>
    /// Block identifier used to build the cylinder.
    /// </summary>
    public string Block { get; set; } = Minecraft.Blocks.StoneBricks;

    /// <summary>
    /// Whether the cylinder interior is filled (<c>true</c>) or a hollow tube (<c>false</c>).
    /// </summary>
    public bool Filled { get; set; } = false;

    /// <summary>
    /// Axis along which the cylinder extends.
    /// </summary>
    public Axis Axis { get; set; } = Axis.Y;

    /// <summary>
    /// Radial width in blocks of the optional stepped stair base surrounding the cylinder (0 disables the base).
    /// </summary>
    public int BaseWidth { get; set; } = 0;

    /// <summary>
    /// Vertical height in blocks of the optional stepped stair base surrounding the cylinder (0 disables the base).
    /// </summary>
    public int BaseHeight { get; set; } = 0;

    /// <summary>
    /// Optional single block identifier used for the cylinder base when <see cref="BaseBlocks"/> is empty.
    /// When <c>null</c> or whitespace, the cylinder's primary <see cref="Block"/> or <see cref="ShapesSchematicCreator.Blocks"/> gradient is used.
    /// </summary>
    public string? BaseBlock { get; set; }

    /// <summary>
    /// Optional ordered list of block identifiers used to form a gradient across the cylinder base.
    /// </summary>
    public IReadOnlyList<string> BaseBlocks { get; set; } = [];

    /// <summary>
    /// Transition mode between blocks in <see cref="BaseBlocks"/>.
    /// </summary>
    public GradientTransition BaseTransition { get; set; } = GradientTransition.Strict;

    /// <summary>
    /// Optional percentage/weight values for <see cref="BaseBlocks"/> when using <see cref="GradientTransition.Weighted"/> or <see cref="GradientTransition.Fuzzy"/>.
    /// </summary>
    public IReadOnlyList<double> BaseTransitionWeights { get; set; } = [];

    /// <summary>
    /// Randomizes transitions at segment boundaries in the cylinder base.
    /// </summary>
    public bool BaseFuzzyTransition { get; set; }

    /// <summary>
    /// Whether exposed transition steps of the cylinder base use oriented stairs (<c>true</c>) or full blocks only (<c>false</c>).
    /// </summary>
    public bool BaseUseStairs { get; set; } = true;

    /// <summary>
    /// Optional stair block identifier override when <see cref="BaseUseStairs"/> is <c>true</c>.
    /// When <c>null</c>, matching stairs are resolved automatically for each base block.
    /// </summary>
    public string? BaseStairBlock { get; set; }

    /// <summary>
    /// Configures a stepped stair base surrounding the cylinder with the specified width, height, and blocks.
    /// </summary>
    public CylinderSchematicCreator WithBase(int width, int height, params string[] blocks)
    {
        ArgumentNullException.ThrowIfNull(blocks);
        BaseWidth = width;
        BaseHeight = height;
        if (blocks.Length == 1)
        {
            BaseBlock = blocks[0];
            BaseBlocks = [];
        }
        else if (blocks.Length > 1)
        {
            BaseBlock = null;
            BaseBlocks = [.. blocks];
        }
        return this;
    }

    /// <summary>
    /// Configures a stepped stair base surrounding the cylinder with a multi-block gradient.
    /// </summary>
    public CylinderSchematicCreator WithBaseGradient(
        int width,
        int height,
        IEnumerable<string> blocks,
        GradientTransition transition = GradientTransition.Strict,
        IEnumerable<double>? weights = null,
        bool fuzzy = false)
    {
        ArgumentNullException.ThrowIfNull(blocks);
        BaseWidth = width;
        BaseHeight = height;
        BaseBlock = null;
        BaseBlocks = [.. blocks];
        BaseTransition = transition;
        BaseTransitionWeights = weights is null ? [] : [.. weights];
        BaseFuzzyTransition = fuzzy || transition == GradientTransition.Fuzzy;
        return this;
    }

    /// <summary>
    /// Places the cylinder into an existing <see cref="SchematicBuilder"/> with its bounding box starting at (startX, startY, startZ),
    /// extending its dimensions automatically if needed.
    /// </summary>
    public override SchematicBuilder Place(SchematicBuilder builder, int startX = 0, int startY = 0, int startZ = 0)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ValidateConfiguration();

        int effectiveRadius = GetEffectiveRadius();
        int outerRadius = effectiveRadius + BaseWidth;
        var (width, height, length) = GetDimensions(effectiveRadius);
        EnsurePlacementBounds(builder, startX, startY, startZ, width, height, length);

        var (bx, by, bz) = Axis switch
        {
            Axis.X => (startX, startY + outerRadius, startZ + outerRadius),
            Axis.Y => (startX + outerRadius, startY, startZ + outerRadius),
            Axis.Z => (startX + outerRadius, startY + outerRadius, startZ),
            _ => throw new ArgumentOutOfRangeException(nameof(Axis), Axis, "Unsupported axis value.")
        };

        BlockGradient gradient = ResolveGradient(Block);
        BlockGradient? baseGradient = BaseWidth > 0 ? ResolveBaseGradient(gradient) : null;
        return builder.SetCylinder(
            bx,
            by,
            bz,
            effectiveRadius,
            Height,
            gradient,
            Filled,
            Axis,
            SmoothnessFactor,
            BaseWidth,
            BaseHeight,
            baseGradient,
            BaseUseStairs,
            BaseStairBlock);
    }

    /// <summary>
    /// Creates a new schematic containing this cylinder.
    /// </summary>
    public override SchematicBuilder Create()
    {
        ValidateConfiguration();

        int effectiveRadius = GetEffectiveRadius();
        var (width, height, length) = GetDimensions(effectiveRadius);
        string baseDescription = BaseWidth > 0 ? $" surrounded by a {BaseWidth}x{BaseHeight} base" : string.Empty;
        var builder = SchematicBuilder.Create(width, height, length)
            .WithRotation(Rotation)
            .WithOffset(0, 0, 0)
            .WithName("Cylinder")
            .WithAuthor("SchematicBuilderMC")
            .WithDescription($"A {(Filled ? "filled" : "hollow")} cylinder (radius {effectiveRadius}, height {Height}){baseDescription} along axis {Axis}.");

        return Place(builder, 0, 0, 0);
    }

    private int GetEffectiveRadius() => ScaleDimension(Radius, minimum: 0);

    private (int Width, int Height, int Length) GetDimensions(int effectiveRadius)
    {
        int diameter = 2 * (effectiveRadius + BaseWidth) + 1;
        return Axis switch
        {
            Axis.X => (Height, diameter, diameter),
            Axis.Y => (diameter, Height, diameter),
            Axis.Z => (diameter, diameter, Height),
            _ => throw new ArgumentOutOfRangeException(nameof(Axis), Axis, "Unsupported axis value.")
        };
    }

    private BlockGradient? ResolveBaseGradient(BlockGradient cylinderGradient)
    {
        if (BaseBlocks is { Count: > 0 })
        {
            return new BlockGradient(
                BaseBlocks,
                BaseTransition,
                BaseTransitionWeights.Count > 0 ? BaseTransitionWeights : null,
                BaseFuzzyTransition || BaseTransition == GradientTransition.Fuzzy,
                RandomSeed);
        }

        if (!string.IsNullOrWhiteSpace(BaseBlock))
        {
            return new BlockGradient([BaseBlock]);
        }

        return cylinderGradient;
    }

    private void ValidateConfiguration()
    {
        if (Radius < 0)
            throw new ArgumentOutOfRangeException(nameof(Radius), Radius, "Radius must be zero or greater.");
        if (Height <= 0)
            throw new ArgumentOutOfRangeException(nameof(Height), Height, "Height must be greater than zero.");
        if (BaseWidth < 0)
            throw new ArgumentOutOfRangeException(nameof(BaseWidth), BaseWidth, "BaseWidth must be zero or greater.");
        if (BaseHeight < 0)
            throw new ArgumentOutOfRangeException(nameof(BaseHeight), BaseHeight, "BaseHeight must be zero or greater.");
        if (BaseWidth > 0 && BaseHeight == 0)
            throw new ArgumentOutOfRangeException(nameof(BaseHeight), BaseHeight, "BaseHeight must be greater than zero when BaseWidth is greater than zero.");
        if (BaseHeight > 0 && BaseWidth == 0)
            throw new ArgumentOutOfRangeException(nameof(BaseWidth), BaseWidth, "BaseWidth must be greater than zero when BaseHeight is greater than zero.");
        if (BaseHeight > Height)
            throw new ArgumentOutOfRangeException(nameof(BaseHeight), BaseHeight, "BaseHeight cannot exceed Height.");
        ValidateSmoothnessFactor();
        if (!Enum.IsDefined(Axis))
            throw new ArgumentOutOfRangeException(nameof(Axis), Axis, "Unsupported axis value.");
        BlockGradient cylinderGradient = ResolveGradient(Block);
        if (BaseWidth > 0)
        {
            _ = ResolveBaseGradient(cylinderGradient);
        }
    }
}
