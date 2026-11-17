using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;
using SchematicBuilderMC.Extensions;
using SchematicBuilderMC.Models;
using SchematicBuilderMC.Registries;
using SchematicBuilderMC.Schematics.Shapes;

namespace SchematicBuilderMC.Schematics.Bases;

/// <summary>
/// Generates a configurable offshore oil rig mega base schematic with 4 outer columns,
/// 1 center column, a connecting usage platform, outer column spiral stairs starting at a configurable height,
/// and inner column spiral stairs starting from the ground.
/// </summary>
public sealed class OilRigMegaBaseSchematicCreator : ShapesSchematicCreator
{
    /// <summary>
    /// Radius of each of the 4 corner outer columns in blocks (2 or greater).
    /// </summary>
    public int OuterColumnRadius { get; set; } = 16;

    /// <summary>
    /// Radius of the central column in blocks (2 or greater).
    /// </summary>
    public int InnerColumnRadius { get; set; } = 32;

    /// <summary>
    /// Convenience alias for <see cref="InnerColumnRadius"/>.
    /// </summary>
    public int CenterColumnRadius
    {
        get => InnerColumnRadius;
        set => InnerColumnRadius = value;
    }

    /// <summary>
    /// Horizontal center-to-center distance between the outer columns along X and Z (or clear horizontal spacing
    /// between outer and center columns when less than <c>2 * OuterColumnRadius + 2</c>).
    /// </summary>
    public int ColumnSpacing { get; set; } = 32;

    /// <summary>
    /// Height above ground (Y = 0) at which the usage platform starts (2 or greater).
    /// </summary>
    public int PlatformHeight { get; set; } = 128;

    /// <summary>
    /// Vertical thickness of the usage platform in blocks (1 or greater).
    /// </summary>
    public int PlatformThickness { get; set; } = 10;

    /// <summary>
    /// Number of blocks the usage platform overhangs beyond the outer walls of the outer columns (0 or greater).
    /// </summary>
    public int PlatformOverhang { get; set; } = 15;

    /// <summary>
    /// Height above ground where the exterior spiral stairs around the 4 outer columns begin (0 to PlatformHeight - 1).
    /// </summary>
    public int OuterStairsStartHeight { get; set; } = 100;

    /// <summary>
    /// Height above ground where the interior spiral stairs inside all 5 columns begin (defaults to 0 / ground level).
    /// </summary>
    public int InnerStairsStartHeight { get; set; } = 0;

    /// <summary>
    /// Whether to generate exterior spiral stairs on each of the 4 outer columns.
    /// </summary>
    public bool IncludeOuterStairs { get; set; } = true;

    /// <summary>
    /// Whether to generate interior spiral stairs inside each of the 5 columns.
    /// </summary>
    public bool IncludeInnerStairs { get; set; } = true;

    /// <summary>
    /// Whether to place perimeter safety guardrails along the top deck of the usage platform.
    /// </summary>
    public bool IncludeGuardrails { get; set; } = true;

    /// <summary>
    /// Whether to place horizontal cross-bracing beams connecting the columns beneath the usage platform.
    /// </summary>
    public bool IncludeSupportBracing { get; set; } = true;

    /// <summary>
    /// Fallback single block identifier used for columns when no column gradient is configured.
    /// </summary>
    public string ColumnBlock { get; set; } = Minecraft.Blocks.GrayConcrete;

    /// <summary>
    /// Default industrial multi-block gradient palette used for the columns when <see cref="ShapesSchematicCreator.Blocks"/> is empty.
    /// </summary>
    public IReadOnlyList<string> ColumnGradientBlocks { get; set; } =
    [
        Minecraft.Blocks.DeepslateBricks,
        Minecraft.Blocks.GrayConcrete,
        Minecraft.Blocks.LightGrayConcrete
    ];

    /// <summary>
    /// Gradient transition mode used for <see cref="ColumnGradientBlocks"/>.
    /// </summary>
    public GradientTransition ColumnGradientTransition { get; set; } = GradientTransition.Fuzzy;

    /// <summary>
    /// Percentage/weight values used when <see cref="ColumnGradientBlocks"/> uses weighted or fuzzy transitions.
    /// </summary>
    public IReadOnlyList<double> ColumnGradientWeights { get; set; } = [30, 50];

    /// <summary>
    /// Optional deterministic random seed for <see cref="ColumnGradientBlocks"/> when using fuzzy transitions.
    /// </summary>
    public int? ColumnGradientSeed { get; set; } = 42;

    /// <summary>
    /// Main block identifier used for the usage platform deck.
    /// </summary>
    public string PlatformBlock { get; set; } = Minecraft.Blocks.SmoothStone;

    /// <summary>
    /// Block identifier used for the industrial hazard trim around the top edge of the usage platform.
    /// </summary>
    public string PlatformBorderBlock { get; set; } = Minecraft.Blocks.YellowConcrete;

    /// <summary>
    /// Block identifier used for horizontal structural bracing beams beneath the platform.
    /// </summary>
    public string SupportBeamBlock { get; set; } = Minecraft.Blocks.DeepslateTiles;

    /// <summary>
    /// Stair block identifier used for exterior spiral stairs on the 4 outer columns.
    /// </summary>
    public string OuterStairBlock { get; set; } = Minecraft.Blocks.StoneBrickStairs;

    /// <summary>
    /// Stair block identifier used for interior spiral stairs inside all 5 columns.
    /// </summary>
    public string InnerStairBlock { get; set; } = Minecraft.Blocks.StoneBrickStairs;

    /// <summary>
    /// Block identifier used for safety guardrails along the top deck perimeter.
    /// </summary>
    public string GuardrailBlock { get; set; } = Minecraft.Blocks.IronBars;

    /// <summary>
    /// Total schematic width along the X axis.
    /// </summary>
    public int Width => GetSpan();

    /// <summary>
    /// Total schematic height along the Y axis.
    /// </summary>
    public int Height => PlatformHeight + PlatformThickness + (IncludeGuardrails ? 1 : 0);

    /// <summary>
    /// Total schematic length along the Z axis.
    /// </summary>
    public int Length => GetSpan();

    private int EffectiveOuterColumnRadius => ScaleDimension(OuterColumnRadius, minimum: 2);

    private int EffectiveInnerColumnRadius => ScaleDimension(InnerColumnRadius, minimum: 2);

    private int EffectiveColumnSpacing => ScaleDimension(ColumnSpacing, minimum: 2);

    private int EffectivePlatformOverhang => PlatformOverhang == 0 ? 0 : ScaleDimension(PlatformOverhang, minimum: 0);

    private int GetEffectiveCenterToCenterSpacing()
    {
        int outerRadius = EffectiveOuterColumnRadius;
        int innerRadius = EffectiveInnerColumnRadius;
        int spacing = EffectiveColumnSpacing;
        return spacing >= 2 * outerRadius + 2
            ? spacing
            : 2 * (innerRadius + outerRadius) + spacing;
    }

    /// <summary>
    /// Returns the base center coordinate of the center column when placed at (startX, startY, startZ).
    /// </summary>
    public BlockPosition GetCenterColumnBasePosition(int startX = 0, int startY = 0, int startZ = 0)
    {
        var (minOuter, maxOuter) = GetOuterColumnOffsetRange();
        int center = (minOuter + maxOuter) / 2;
        return new BlockPosition(startX + center, startY, startZ + center);
    }

    /// <summary>
    /// Returns the base center coordinates of the 4 outer columns (NW, NE, SW, SE) when placed at (startX, startY, startZ).
    /// </summary>
    public IReadOnlyList<BlockPosition> GetOuterColumnBasePositions(int startX = 0, int startY = 0, int startZ = 0)
    {
        var (minOuter, maxOuter) = GetOuterColumnOffsetRange();
        return
        [
            new BlockPosition(startX + minOuter, startY, startZ + minOuter),
            new BlockPosition(startX + maxOuter, startY, startZ + minOuter),
            new BlockPosition(startX + minOuter, startY, startZ + maxOuter),
            new BlockPosition(startX + maxOuter, startY, startZ + maxOuter)
        ];
    }

    /// <summary>
    /// Places the oil rig mega base into an existing <see cref="SchematicBuilder"/> starting at (startX, startY, startZ),
    /// extending its dimensions automatically if needed.
    /// </summary>
    public override SchematicBuilder Place(SchematicBuilder builder, int startX = 0, int startY = 0, int startZ = 0)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ValidateConfiguration();
        EnsurePlacementBounds(builder, startX, startY, startZ, Width, Height, Length);

        int outerRadius = EffectiveOuterColumnRadius;
        int innerRadius = EffectiveInnerColumnRadius;
        int columnHeight = PlatformHeight + PlatformThickness;
        double radialPadding = 0.5 / (1.0 + SmoothnessFactor);

        var outerCenters = GetOuterColumnBasePositions(startX, startY, startZ);
        var centerColumnBase = GetCenterColumnBasePosition(startX, startY, startZ);
        BlockGradient columnGradient = ResolveColumnGradient();

        PlaceColumns(builder, outerCenters, centerColumnBase, outerRadius, innerRadius, columnHeight, columnGradient);

        if (IncludeSupportBracing && PlatformHeight >= 4)
        {
            PlaceSupportBracing(builder, startY, outerCenters, centerColumnBase, outerRadius, innerRadius, radialPadding);
        }

        var (platformMinX, platformMaxX, platformMinZ, platformMaxZ) = GetPlatformBounds(startX, startZ, outerRadius);
        PlaceUsagePlatform(
            builder,
            startY,
            platformMinX,
            platformMaxX,
            platformMinZ,
            platformMaxZ,
            outerCenters,
            centerColumnBase,
            outerRadius,
            innerRadius,
            radialPadding);

        HashSet<(int X, int Z)>? outerStairLandings = null;
        if (IncludeOuterStairs)
        {
            outerStairLandings = PlaceOuterStairs(builder, startY, outerCenters, outerRadius, radialPadding);
        }

        if (IncludeGuardrails)
        {
            PlaceGuardrails(builder, startY, platformMinX, platformMaxX, platformMinZ, platformMaxZ, outerStairLandings);
        }

        if (IncludeInnerStairs)
        {
            PlaceInnerStairs(builder, startY, outerCenters, centerColumnBase, outerRadius, innerRadius, radialPadding);
        }

        return builder;
    }

    /// <summary>
    /// Creates a new schematic containing this offshore oil rig mega base.
    /// </summary>
    public override SchematicBuilder Create()
    {
        ValidateConfiguration();

        var builder = SchematicBuilder.Create(Width, Height, Length)
            .WithRotation(Rotation)
            .WithOffset(0, 0, 0)
            .WithName("Oil Rig Mega Base")
            .WithAuthor("SchematicBuilderMC")
            .WithDescription(
                $"An offshore oil rig mega base with 4 outer columns (r={EffectiveOuterColumnRadius}), 1 center column (r={EffectiveInnerColumnRadius}), and a {PlatformThickness}-thick platform at height {PlatformHeight}.");

        return Place(builder, 0, 0, 0);
    }

    private void PlaceColumns(
        SchematicBuilder builder,
        IReadOnlyList<BlockPosition> outerCenters,
        BlockPosition centerColumnBase,
        int outerRadius,
        int innerRadius,
        int columnHeight,
        BlockGradient columnGradient)
    {
        foreach (var center in outerCenters)
        {
            builder.SetCylinder(center, outerRadius, columnHeight, columnGradient, filled: false, axis: Axis.Y, smoothnessFactor: SmoothnessFactor);
        }

        builder.SetCylinder(centerColumnBase, innerRadius, columnHeight, columnGradient, filled: false, axis: Axis.Y, smoothnessFactor: SmoothnessFactor);
    }

    private void PlaceSupportBracing(
        SchematicBuilder builder,
        int startY,
        IReadOnlyList<BlockPosition> outerCenters,
        BlockPosition centerColumnBase,
        int outerRadius,
        int innerRadius,
        double radialPadding)
    {
        var bracingHeights = new List<int> { startY + PlatformHeight - 1 };
        if (PlatformHeight >= 8)
        {
            bracingHeights.Add(startY + PlatformHeight / 2);
        }

        var nw = outerCenters[0];
        var ne = outerCenters[1];
        var sw = outerCenters[2];
        var se = outerCenters[3];

        var spans = new (BlockPosition From, BlockPosition To)[]
        {
            (nw, ne),
            (ne, se),
            (se, sw),
            (sw, nw),
            (nw, se),
            (ne, sw)
        };

        BlockState supportBeamState = BlockState.Parse(SupportBeamBlock);
        foreach (int y in bracingHeights)
        {
            foreach (var (from, to) in spans)
            {
                PlaceHorizontalBeam(builder, y, from, to, outerCenters, centerColumnBase, outerRadius, innerRadius, radialPadding, supportBeamState);
            }
        }
    }

    private static void PlaceHorizontalBeam(
        SchematicBuilder builder,
        int y,
        BlockPosition from,
        BlockPosition to,
        IReadOnlyList<BlockPosition> outerCenters,
        BlockPosition centerColumnBase,
        int outerRadius,
        int innerRadius,
        double radialPadding,
        BlockState supportBeamState)
    {
        int dx = to.X - from.X;
        int dz = to.Z - from.Z;
        int steps = Math.Max(Math.Abs(dx), Math.Abs(dz));

        for (int i = 0; i <= steps; i++)
        {
            int x = steps == 0 ? from.X : from.X + (int)Math.Round((double)dx * i / steps);
            int z = steps == 0 ? from.Z : from.Z + (int)Math.Round((double)dz * i / steps);

            if (!IsInsideAnyColumnDisk(x, z, outerCenters, centerColumnBase, outerRadius, innerRadius, radialPadding))
            {
                builder.SetBlock(x, y, z, supportBeamState);
            }
        }
    }

    private void PlaceUsagePlatform(
        SchematicBuilder builder,
        int startY,
        int platformMinX,
        int platformMaxX,
        int platformMinZ,
        int platformMaxZ,
        IReadOnlyList<BlockPosition> outerCenters,
        BlockPosition centerColumnBase,
        int outerRadius,
        int innerRadius,
        double radialPadding)
    {
        int platformStartY = startY + PlatformHeight;
        int topDeckY = platformStartY + PlatformThickness - 1;
        BlockState platformState = BlockState.Parse(PlatformBlock);
        BlockState platformBorderState = BlockState.Parse(PlatformBorderBlock);

        for (int z = platformMinZ; z <= platformMaxZ; z++)
        {
            bool isZBorder = z == platformMinZ || z == platformMaxZ;
            for (int x = platformMinX; x <= platformMaxX; x++)
            {
                if (IsOnAnyColumnWall(x, z, outerCenters, centerColumnBase, outerRadius, innerRadius, radialPadding))
                {
                    continue;
                }

                bool isStrictlyInside = IsStrictlyInsideAnyColumn(x, z, outerCenters, centerColumnBase, outerRadius, innerRadius, radialPadding);
                bool isBorderXZ = isZBorder || x == platformMinX || x == platformMaxX;

                for (int y = platformStartY; y <= topDeckY; y++)
                {
                    if (isStrictlyInside && y < topDeckY)
                    {
                        builder.SetBlock(x, y, z, BlockState.Air);
                        continue;
                    }

                    bool isBorder = y == topDeckY && isBorderXZ;
                    builder.SetBlock(x, y, z, isBorder ? platformBorderState : platformState);
                }
            }
        }
    }

    private void PlaceGuardrails(
        SchematicBuilder builder,
        int startY,
        int platformMinX,
        int platformMaxX,
        int platformMinZ,
        int platformMaxZ,
        HashSet<(int X, int Z)>? outerStairLandings)
    {
        int railY = startY + PlatformHeight + PlatformThickness;
        bool openForOuterStairs = outerStairLandings is { Count: > 0 } && EffectivePlatformOverhang == 0;
        BlockState guardrailState = BlockState.Parse(GuardrailBlock);

        for (int z = platformMinZ; z <= platformMaxZ; z++)
        {
            for (int x = platformMinX; x <= platformMaxX; x++)
            {
                if (x != platformMinX && x != platformMaxX && z != platformMinZ && z != platformMaxZ)
                {
                    continue;
                }

                if (openForOuterStairs &&
                    (outerStairLandings!.Contains((x - 1, z)) ||
                     outerStairLandings.Contains((x + 1, z)) ||
                     outerStairLandings.Contains((x, z - 1)) ||
                     outerStairLandings.Contains((x, z + 1))))
                {
                    continue;
                }

                builder.SetBlock(x, railY, z, guardrailState);
            }
        }
    }

    private void PlaceInnerStairs(
        SchematicBuilder builder,
        int startY,
        IReadOnlyList<BlockPosition> outerCenters,
        BlockPosition centerColumnBase,
        int outerRadius,
        int innerRadius,
        double radialPadding)
    {
        int topDeckY = startY + PlatformHeight + PlatformThickness - 1;
        int stairStartY = startY + InnerStairsStartHeight;
        var stairBase = BlockState.Parse(InnerStairBlock);

        var columns = new List<(BlockPosition Center, int Radius)>(outerCenters.Count + 1);
        foreach (var center in outerCenters)
        {
            columns.Add((center, outerRadius));
        }
        columns.Add((centerColumnBase, innerRadius));

        foreach (var (center, radius) in columns)
        {
            var innerRing = BuildInnerStairRing(radius, radialPadding);
            PlaceSpiralStairsOnRing(
                builder,
                center,
                innerRing,
                stairStartY,
                topDeckY,
                stairBase,
                maxHeadroomY: Math.Min(startY + Height - 1, topDeckY + 2),
                clearHeadroom: true);
        }
    }

    private HashSet<(int X, int Z)> PlaceOuterStairs(
        SchematicBuilder builder,
        int startY,
        IReadOnlyList<BlockPosition> outerCenters,
        int outerRadius,
        double radialPadding)
    {
        var stairBase = BlockState.Parse(OuterStairBlock);
        var outerRing = BuildOuterStairRing(outerRadius, radialPadding);
        int stairStartY = startY + OuterStairsStartHeight;
        int stairEndY = startY + PlatformHeight - 1;
        int maxHeadroomBelowPlatformY = startY + PlatformHeight - 1;
        var topLandings = new HashSet<(int X, int Z)>();

        foreach (var center in outerCenters)
        {
            PlaceSpiralStairsOnRing(
                builder,
                center,
                outerRing,
                stairStartY,
                stairEndY,
                stairBase,
                maxHeadroomY: maxHeadroomBelowPlatformY,
                clearHeadroom: true);

            int topStepIndex = (stairEndY - stairStartY) % outerRing.Count;
            var (du, dv) = outerRing[topStepIndex];
            topLandings.Add((center.X + du, center.Z + dv));
        }

        return topLandings;
    }

    private static void PlaceSpiralStairsOnRing(
        SchematicBuilder builder,
        BlockPosition center,
        IReadOnlyList<(int Du, int Dv)> ring,
        int startY,
        int endY,
        BlockState stairBase,
        int maxHeadroomY,
        bool clearHeadroom)
    {
        if (ring.Count == 0 || startY > endY)
        {
            return;
        }

        for (int y = startY; y <= endY; y++)
        {
            int stepIndex = (y - startY) % ring.Count;
            var current = ring[stepIndex];
            var next = ring[(stepIndex + 1) % ring.Count];
            Direction facing = GetTangentFacing(current, next);

            int x = center.X + current.Du;
            int z = center.Z + current.Dv;
            builder.SetBlock(x, y, z, stairBase.BottomHalf().WithFacing(facing));

            if (clearHeadroom)
            {
                for (int dy = 1; dy <= 3; dy++)
                {
                    int headY = y + dy;
                    if (headY <= maxHeadroomY)
                    {
                        builder.SetBlock(x, headY, z, BlockState.Air);
                    }
                }
            }
        }
    }

    private static List<(int Du, int Dv)> BuildInnerStairRing(int radius, double radialPadding)
    {
        double outerRadiusSq = (radius + radialPadding) * (radius + radialPadding);
        bool IsInside(int du, int dv) => du * du + dv * dv <= outerRadiusSq;
        bool IsStrictlyInside(int du, int dv) =>
            IsInside(du, dv) &&
            IsInside(du + 1, dv) && IsInside(du - 1, dv) &&
            IsInside(du, dv + 1) && IsInside(du, dv - 1);

        var ring = new List<(int Du, int Dv)>();
        for (int dv = -radius; dv <= radius; dv++)
        {
            for (int du = -radius; du <= radius; du++)
            {
                if (!IsStrictlyInside(du, dv))
                {
                    continue;
                }

                if (!(IsStrictlyInside(du + 1, dv) &&
                      IsStrictlyInside(du - 1, dv) &&
                      IsStrictlyInside(du, dv + 1) &&
                      IsStrictlyInside(du, dv - 1)))
                {
                    ring.Add((du, dv));
                }
            }
        }

        ring.Sort((a, b) => CompareClockwiseAngle(a, b));
        return ring;
    }

    private static List<(int Du, int Dv)> BuildOuterStairRing(int radius, double radialPadding)
    {
        double outerRadiusSq = (radius + radialPadding) * (radius + radialPadding);
        bool IsInside(int du, int dv) => du * du + dv * dv <= outerRadiusSq;
        bool IsStrictlyInside(int du, int dv) =>
            IsInside(du, dv) &&
            IsInside(du + 1, dv) && IsInside(du - 1, dv) &&
            IsInside(du, dv + 1) && IsInside(du, dv - 1);
        bool IsWall(int du, int dv) => IsInside(du, dv) && !IsStrictlyInside(du, dv);

        var ring = new List<(int Du, int Dv)>();
        int extent = radius + 1;
        for (int dv = -extent; dv <= extent; dv++)
        {
            for (int du = -extent; du <= extent; du++)
            {
                if (IsInside(du, dv))
                {
                    continue;
                }

                if (IsWall(du + 1, dv) || IsWall(du - 1, dv) || IsWall(du, dv + 1) || IsWall(du, dv - 1))
                {
                    ring.Add((du, dv));
                }
            }
        }

        ring.Sort((a, b) => CompareClockwiseAngle(a, b));
        return ring;
    }

    private static int CompareClockwiseAngle((int Du, int Dv) a, (int Du, int Dv) b)
    {
        double angleA = Math.Atan2(a.Du, -a.Dv);
        if (angleA < 0) angleA += 2 * Math.PI;
        double angleB = Math.Atan2(b.Du, -b.Dv);
        if (angleB < 0) angleB += 2 * Math.PI;
        return angleA.CompareTo(angleB);
    }

    private static Direction GetTangentFacing((int Du, int Dv) current, (int Du, int Dv) next)
    {
        int dx = next.Du - current.Du;
        int dz = next.Dv - current.Dv;
        if (Math.Abs(dx) >= Math.Abs(dz))
        {
            return dx >= 0 ? Direction.East : Direction.West;
        }

        return dz >= 0 ? Direction.South : Direction.North;
    }

    private static bool IsInsideColumnDisk(int x, int z, BlockPosition center, int radius, double radialPadding)
    {
        int du = x - center.X;
        int dv = z - center.Z;
        double outerRadiusSq = (radius + radialPadding) * (radius + radialPadding);
        return du * du + dv * dv <= outerRadiusSq;
    }

    private static bool IsStrictlyInsideColumnDisk(int x, int z, BlockPosition center, int radius, double radialPadding)
    {
        int du = x - center.X;
        int dv = z - center.Z;
        double outerRadiusSq = (radius + radialPadding) * (radius + radialPadding);
        bool IsInside(int u, int v) => u * u + v * v <= outerRadiusSq;
        return IsInside(du, dv) &&
               IsInside(du + 1, dv) && IsInside(du - 1, dv) &&
               IsInside(du, dv + 1) && IsInside(du, dv - 1);
    }

    private static bool IsInsideAnyColumnDisk(
        int x,
        int z,
        IReadOnlyList<BlockPosition> outerCenters,
        BlockPosition centerColumnBase,
        int outerRadius,
        int innerRadius,
        double radialPadding)
    {
        foreach (var center in outerCenters)
        {
            if (IsInsideColumnDisk(x, z, center, outerRadius, radialPadding))
            {
                return true;
            }
        }

        return IsInsideColumnDisk(x, z, centerColumnBase, innerRadius, radialPadding);
    }

    private static bool IsStrictlyInsideAnyColumn(
        int x,
        int z,
        IReadOnlyList<BlockPosition> outerCenters,
        BlockPosition centerColumnBase,
        int outerRadius,
        int innerRadius,
        double radialPadding)
    {
        foreach (var center in outerCenters)
        {
            if (IsStrictlyInsideColumnDisk(x, z, center, outerRadius, radialPadding))
            {
                return true;
            }
        }

        return IsStrictlyInsideColumnDisk(x, z, centerColumnBase, innerRadius, radialPadding);
    }

    private static bool IsOnAnyColumnWall(
        int x,
        int z,
        IReadOnlyList<BlockPosition> outerCenters,
        BlockPosition centerColumnBase,
        int outerRadius,
        int innerRadius,
        double radialPadding)
    {
        foreach (var center in outerCenters)
        {
            if (IsInsideColumnDisk(x, z, center, outerRadius, radialPadding) &&
                !IsStrictlyInsideColumnDisk(x, z, center, outerRadius, radialPadding))
            {
                return true;
            }
        }

        return IsInsideColumnDisk(x, z, centerColumnBase, innerRadius, radialPadding) &&
               !IsStrictlyInsideColumnDisk(x, z, centerColumnBase, innerRadius, radialPadding);
    }

    private int GetSpan()
    {
        int outerRadius = EffectiveOuterColumnRadius;
        int innerRadius = EffectiveInnerColumnRadius;
        int spacing = GetEffectiveCenterToCenterSpacing();
        int margin = Math.Max(EffectivePlatformOverhang, IncludeOuterStairs ? 1 : 0);
        int outerSpan = spacing + 2 * (outerRadius + margin) + 1;
        int requiredInnerSpan = 2 * innerRadius + 1;
        return Math.Max(outerSpan, requiredInnerSpan);
    }

    private (int MinOuter, int MaxOuter) GetOuterColumnOffsetRange()
    {
        int spacing = GetEffectiveCenterToCenterSpacing();
        int span = GetSpan();
        int minOuter = (span - spacing) / 2;
        return (minOuter, minOuter + spacing);
    }

    private (int MinX, int MaxX, int MinZ, int MaxZ) GetPlatformBounds(int startX, int startZ, int outerRadius)
    {
        var (minOuter, maxOuter) = GetOuterColumnOffsetRange();
        int overhang = EffectivePlatformOverhang;
        int minOffset = minOuter - outerRadius - overhang;
        int maxOffset = maxOuter + outerRadius + overhang;
        return (startX + minOffset, startX + maxOffset, startZ + minOffset, startZ + maxOffset);
    }

    private BlockGradient ResolveColumnGradient()
    {
        if (Blocks is { Count: > 0 })
        {
            return ResolveGradient(ColumnBlock);
        }

        if (ColumnGradientBlocks is { Count: > 0 })
        {
            return new BlockGradient(
                ColumnGradientBlocks,
                ColumnGradientTransition,
                ColumnGradientWeights.Count > 0 ? ColumnGradientWeights : null,
                FuzzyTransition || ColumnGradientTransition == GradientTransition.Fuzzy,
                RandomSeed ?? ColumnGradientSeed);
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(ColumnBlock);
        return new BlockGradient([ColumnBlock]);
    }

    private void ValidateConfiguration()
    {
        if (OuterColumnRadius < 2)
        {
            throw new ArgumentOutOfRangeException(nameof(OuterColumnRadius), OuterColumnRadius, "OuterColumnRadius must be 2 or greater so columns have an interior for stairs.");
        }

        if (InnerColumnRadius < 2)
        {
            throw new ArgumentOutOfRangeException(nameof(InnerColumnRadius), InnerColumnRadius, "InnerColumnRadius must be 2 or greater so the center column has an interior for stairs.");
        }

        if (ColumnSpacing < 2)
        {
            throw new ArgumentOutOfRangeException(nameof(ColumnSpacing), ColumnSpacing, "ColumnSpacing must be 2 or greater.");
        }

        if (PlatformHeight < 2)
        {
            throw new ArgumentOutOfRangeException(nameof(PlatformHeight), PlatformHeight, "PlatformHeight must be 2 or greater.");
        }

        if (PlatformThickness < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(PlatformThickness), PlatformThickness, "PlatformThickness must be 1 or greater.");
        }

        if (PlatformOverhang < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(PlatformOverhang), PlatformOverhang, "PlatformOverhang must be zero or greater.");
        }

        if (OuterStairsStartHeight < 0 || OuterStairsStartHeight >= PlatformHeight)
        {
            throw new ArgumentOutOfRangeException(nameof(OuterStairsStartHeight), OuterStairsStartHeight, "OuterStairsStartHeight must be between 0 and PlatformHeight - 1.");
        }

        if (InnerStairsStartHeight < 0 || InnerStairsStartHeight >= PlatformHeight + PlatformThickness)
        {
            throw new ArgumentOutOfRangeException(nameof(InnerStairsStartHeight), InnerStairsStartHeight, "InnerStairsStartHeight must be between 0 and PlatformHeight + PlatformThickness - 1.");
        }

        ValidateSmoothnessFactor();
        _ = ResolveColumnGradient();
        ArgumentException.ThrowIfNullOrWhiteSpace(PlatformBlock);
        ArgumentException.ThrowIfNullOrWhiteSpace(PlatformBorderBlock);
        ArgumentException.ThrowIfNullOrWhiteSpace(SupportBeamBlock);
        ArgumentException.ThrowIfNullOrWhiteSpace(OuterStairBlock);
        ArgumentException.ThrowIfNullOrWhiteSpace(InnerStairBlock);
        ArgumentException.ThrowIfNullOrWhiteSpace(GuardrailBlock);
    }
}
