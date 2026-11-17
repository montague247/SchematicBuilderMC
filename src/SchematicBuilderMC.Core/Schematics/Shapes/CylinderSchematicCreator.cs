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
    /// Radial width in blocks of the optional outward-flaring stair collar at the top of the cylinder (0 disables the top collar).
    /// </summary>
    public int TopWidth { get; set; } = 0;

    /// <summary>
    /// Vertical height in blocks of the optional stepped stair collar at the top of the cylinder (0 disables the top collar).
    /// </summary>
    public int TopHeight { get; set; } = 0;

    /// <summary>
    /// Optional single block identifier used for the cylinder top collar when <see cref="TopBlocks"/> is empty.
    /// When <c>null</c> or whitespace, the cylinder's primary <see cref="Block"/> or <see cref="ShapesSchematicCreator.Blocks"/> gradient is used.
    /// </summary>
    public string? TopBlock { get; set; }

    /// <summary>
    /// Optional ordered list of block identifiers used to form a gradient across the cylinder top collar.
    /// </summary>
    public IReadOnlyList<string> TopBlocks { get; set; } = [];

    /// <summary>
    /// Transition mode between blocks in <see cref="TopBlocks"/>.
    /// </summary>
    public GradientTransition TopTransition { get; set; } = GradientTransition.Strict;

    /// <summary>
    /// Optional percentage/weight values for <see cref="TopBlocks"/> when using <see cref="GradientTransition.Weighted"/> or <see cref="GradientTransition.Fuzzy"/>.
    /// </summary>
    public IReadOnlyList<double> TopTransitionWeights { get; set; } = [];

    /// <summary>
    /// Randomizes transitions at segment boundaries in the cylinder top collar.
    /// </summary>
    public bool TopFuzzyTransition { get; set; }

    /// <summary>
    /// Whether exposed underside transition steps of the cylinder top collar use inverted stairs (<c>true</c>) or full blocks only (<c>false</c>).
    /// </summary>
    public bool TopUseStairs { get; set; } = true;

    /// <summary>
    /// Optional stair block identifier override when <see cref="TopUseStairs"/> is <c>true</c>.
    /// When <c>null</c>, matching stairs are resolved automatically for each top block.
    /// </summary>
    public string? TopStairBlock { get; set; }

    /// <summary>
    /// Whether to place inward-flowing water at the bottom inside a hollow vertical cylinder so entities and items flow toward the center.
    /// </summary>
    public bool IncludeWater { get; set; } = false;

    /// <summary>
    /// Optional floor block identifier used to construct stepped inner water tiers when the cylinder radius exceeds Minecraft's 7-block flat water range.
    /// </summary>
    public string? WaterFloorBlock { get; set; }

    /// <summary>
    /// Enables or disables inward-flowing water inside the hollow vertical cylinder.
    /// </summary>
    public CylinderSchematicCreator WithWater(bool includeWater = true, string? waterFloorBlock = null)
    {
        IncludeWater = includeWater;
        WaterFloorBlock = waterFloorBlock;
        return this;
    }

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
    /// Configures a stepped stair top collar surrounding the upper end of the cylinder with the specified width, height, and blocks.
    /// </summary>
    public CylinderSchematicCreator WithTop(int width, int height, params string[] blocks)
    {
        ArgumentNullException.ThrowIfNull(blocks);
        TopWidth = width;
        TopHeight = height;
        if (blocks.Length == 1)
        {
            TopBlock = blocks[0];
            TopBlocks = [];
        }
        else if (blocks.Length > 1)
        {
            TopBlock = null;
            TopBlocks = [.. blocks];
        }
        return this;
    }

    /// <summary>
    /// Configures a stepped stair top collar surrounding the upper end of the cylinder with a multi-block gradient.
    /// </summary>
    public CylinderSchematicCreator WithTopGradient(
        int width,
        int height,
        IEnumerable<string> blocks,
        GradientTransition transition = GradientTransition.Strict,
        IEnumerable<double>? weights = null,
        bool fuzzy = false)
    {
        ArgumentNullException.ThrowIfNull(blocks);
        TopWidth = width;
        TopHeight = height;
        TopBlock = null;
        TopBlocks = [.. blocks];
        TopTransition = transition;
        TopTransitionWeights = weights is null ? [] : [.. weights];
        TopFuzzyTransition = fuzzy || transition == GradientTransition.Fuzzy;
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
        int outerRadius = effectiveRadius + Math.Max(BaseWidth, TopWidth);
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
        BlockGradient? baseGradient = BaseWidth > 0 ? ResolveCollarGradient(BaseBlocks, BaseBlock, BaseTransition, BaseTransitionWeights, BaseFuzzyTransition, gradient) : null;
        BlockGradient? topGradient = TopWidth > 0 ? ResolveCollarGradient(TopBlocks, TopBlock, TopTransition, TopTransitionWeights, TopFuzzyTransition, gradient) : null;
        BlockState? waterFloor = IncludeWater ? ResolveWaterFloorBlockState(baseGradient ?? gradient) : null;
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
            BaseStairBlock,
            IncludeWater,
            waterFloor,
            TopWidth,
            TopHeight,
            topGradient,
            TopUseStairs,
            TopStairBlock);
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
        string topDescription = TopWidth > 0 ? $" and a {TopWidth}x{TopHeight} top collar" : string.Empty;
        string waterDescription = IncludeWater ? " with inward-flowing water" : string.Empty;
        var builder = SchematicBuilder.Create(width, height, length)
            .WithRotation(Rotation)
            .WithOffset(0, 0, 0)
            .WithName("Cylinder")
            .WithAuthor("SchematicBuilderMC")
            .WithDescription($"A {(Filled ? "filled" : "hollow")} cylinder (radius {effectiveRadius}, height {Height}){baseDescription}{topDescription}{waterDescription} along axis {Axis}.");

        return Place(builder, 0, 0, 0);
    }

    private int GetEffectiveRadius() => ScaleDimension(Radius, minimum: 0);

    private (int Width, int Height, int Length) GetDimensions(int effectiveRadius)
    {
        int diameter = 2 * (effectiveRadius + Math.Max(BaseWidth, TopWidth)) + 1;
        return Axis switch
        {
            Axis.X => (Height, diameter, diameter),
            Axis.Y => (diameter, Height, diameter),
            Axis.Z => (diameter, diameter, Height),
            _ => throw new ArgumentOutOfRangeException(nameof(Axis), Axis, "Unsupported axis value.")
        };
    }

    private BlockGradient ResolveCollarGradient(
        IReadOnlyList<string> blocks,
        string? singleBlock,
        GradientTransition transition,
        IReadOnlyList<double> weights,
        bool fuzzy,
        BlockGradient cylinderGradient)
    {
        if (blocks is { Count: > 0 })
        {
            return new BlockGradient(
                blocks,
                transition,
                weights.Count > 0 ? weights : null,
                fuzzy || transition == GradientTransition.Fuzzy,
                RandomSeed);
        }

        if (!string.IsNullOrWhiteSpace(singleBlock))
        {
            return new BlockGradient([singleBlock]);
        }

        return cylinderGradient;
    }

    private BlockState? ResolveWaterFloorBlockState(BlockGradient fallbackGradient)
    {
        if (!string.IsNullOrWhiteSpace(WaterFloorBlock))
        {
            return BlockState.Parse(WaterFloorBlock);
        }

        if (BaseWidth > 0)
        {
            return fallbackGradient.Blocks[0];
        }

        return null;
    }

    private void ValidateConfiguration()
    {
        if (Radius < 0)
            throw new ArgumentOutOfRangeException(nameof(Radius), Radius, "Radius must be zero or greater.");
        if (Height <= 0)
            throw new ArgumentOutOfRangeException(nameof(Height), Height, "Height must be greater than zero.");
        ValidateCollarConfiguration(BaseWidth, BaseHeight, nameof(BaseWidth), nameof(BaseHeight));
        ValidateCollarConfiguration(TopWidth, TopHeight, nameof(TopWidth), nameof(TopHeight));
        if (BaseHeight + TopHeight > Height)
            throw new ArgumentOutOfRangeException(nameof(TopHeight), TopHeight, "Combined BaseHeight and TopHeight cannot exceed Height.");
        ValidateSmoothnessFactor();
        if (!Enum.IsDefined(Axis))
            throw new ArgumentOutOfRangeException(nameof(Axis), Axis, "Unsupported axis value.");
        if (IncludeWater)
        {
            if (Filled)
                throw new ArgumentException("Water flow can only be placed inside a hollow cylinder (Filled must be false).", nameof(IncludeWater));
            if (GetEffectiveRadius() < 1)
                throw new ArgumentOutOfRangeException(nameof(Radius), Radius, "Radius must be at least 1 when IncludeWater is true.");
            if (Axis != Axis.Y)
                throw new ArgumentOutOfRangeException(nameof(Axis), Axis, "Cylinder water flow is only supported for vertical cylinders (Axis.Y).");
            if (!string.IsNullOrWhiteSpace(WaterFloorBlock))
                _ = BlockState.Parse(WaterFloorBlock);
        }
        BlockGradient cylinderGradient = ResolveGradient(Block);
        if (BaseWidth > 0)
        {
            _ = ResolveCollarGradient(BaseBlocks, BaseBlock, BaseTransition, BaseTransitionWeights, BaseFuzzyTransition, cylinderGradient);
        }
        if (TopWidth > 0)
        {
            _ = ResolveCollarGradient(TopBlocks, TopBlock, TopTransition, TopTransitionWeights, TopFuzzyTransition, cylinderGradient);
        }
    }

    private void ValidateCollarConfiguration(int width, int height, string widthParam, string heightParam)
    {
        if (width < 0)
            throw new ArgumentOutOfRangeException(widthParam, width, $"{widthParam} must be zero or greater.");
        if (height < 0)
            throw new ArgumentOutOfRangeException(heightParam, height, $"{heightParam} must be zero or greater.");
        if (width > 0 && height == 0)
            throw new ArgumentOutOfRangeException(heightParam, height, $"{heightParam} must be greater than zero when {widthParam} is greater than zero.");
        if (height > 0 && width == 0)
            throw new ArgumentOutOfRangeException(widthParam, width, $"{widthParam} must be greater than zero when {heightParam} is greater than zero.");
        if (height > Height)
            throw new ArgumentOutOfRangeException(heightParam, height, $"{heightParam} cannot exceed Height.");
    }
}
