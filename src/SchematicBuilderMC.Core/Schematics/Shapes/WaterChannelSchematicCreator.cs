using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;
using SchematicBuilderMC.Models;
using SchematicBuilderMC.Registries;

namespace SchematicBuilderMC.Schematics.Shapes;

/// <summary>
/// Builds a configurable horizontal water channel schematic of specified <see cref="Length"/> with a configurable
/// <see cref="BaseBlock"/> (default: <see cref="Minecraft.Blocks.BlueIce"/>), automatic water source/flowing placement
/// with pass-through segment dividers beyond 8 blocks, and optional trapdoors surrounding the base, guiding the water,
/// and covering the top.
/// </summary>
public class WaterChannelSchematicCreator : SchematicCreator
{
    /// <summary>
    /// Length of the water channel in blocks (must be at least 1). Defaults to 8.
    /// </summary>
    public int Length { get; set; } = 8;

    /// <summary>
    /// Horizontal direction in which the water channel flows. Defaults to <see cref="Direction.East"/>.
    /// </summary>
    public Direction Facing { get; set; } = Direction.East;

    /// <summary>
    /// Base floor block beneath the water stream. Defaults to <see cref="Minecraft.Blocks.BlueIce"/>.
    /// </summary>
    public string BaseBlock { get; set; } = Minecraft.Blocks.BlueIce;

    /// <summary>
    /// Trapdoor block identifier used when any trapdoor option or multi-segment trapdoor divider is placed.
    /// </summary>
    public string TrapdoorBlock { get; set; } = Minecraft.Blocks.OakTrapdoor;

    /// <summary>
    /// When true, surrounds the base blocks with outward-facing open bottom trapdoors.
    /// </summary>
    public bool SurroundBaseWithTrapdoors { get; set; }

    /// <summary>
    /// When true, places outward-facing open bottom trapdoors along the water sides and back start cap to guide the stream.
    /// </summary>
    public bool GuideWaterWithTrapdoors { get; set; }

    /// <summary>
    /// When true, places closed bottom-half trapdoors directly above the water channel to cover it.
    /// </summary>
    public bool CoverTopWithTrapdoors { get; set; }

    /// <summary>
    /// When true (and <see cref="GuideWaterWithTrapdoors"/> is enabled), also closes the exit end of the water stream with a trapdoor.
    /// </summary>
    public bool CloseEndWithTrapdoor { get; set; }

    /// <summary>
    /// Optional custom pass-through divider block placed every 9th block when <see cref="Length"/> exceeds 9.
    /// Defaults to a closed top-half <see cref="TrapdoorBlock"/>.
    /// </summary>
    public BlockState? DividerBlock { get; set; }

    public override SchematicBuilder Create()
    {
        if (Length <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(Length), Length, "Channel length must be greater than zero.");
        }

        if (!Enum.IsDefined(Facing) ||
            Facing is not (Direction.North or Direction.South or Direction.East or Direction.West))
        {
            throw new ArgumentOutOfRangeException(nameof(Facing), Facing, "Channel facing must be North, South, East, or West.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(BaseBlock);
        ArgumentException.ThrowIfNullOrWhiteSpace(TrapdoorBlock);

        int backMargin = SurroundBaseWithTrapdoors || GuideWaterWithTrapdoors ? 1 : 0;
        int frontMargin = SurroundBaseWithTrapdoors || (GuideWaterWithTrapdoors && CloseEndWithTrapdoor) ? 1 : 0;
        int sideMargin = SurroundBaseWithTrapdoors || GuideWaterWithTrapdoors ? 1 : 0;

        int axialSpan = backMargin + Length + frontMargin;
        int lateralSpan = 1 + (sideMargin * 2);

        int width = Facing is Direction.East or Direction.West ? axialSpan : lateralSpan;
        int length = Facing is Direction.East or Direction.West ? lateralSpan : axialSpan;
        int height = CoverTopWithTrapdoors ? 3 : 2;

        (int startX, int startZ) = Facing switch
        {
            Direction.East => (backMargin, sideMargin),
            Direction.West => (frontMargin + Length - 1, sideMargin),
            Direction.South => (sideMargin, backMargin),
            Direction.North => (sideMargin, frontMargin + Length - 1),
            _ => (backMargin, sideMargin)
        };

        return SchematicBuilder.Create(width, height, length, Rotation)
            .WithOffset(0, 0, 0)
            .WithName("Water Channel")
            .WithAuthor("SchematicBuilderMC")
            .WithDescription($"Blue-ice water channel ({Length} blocks, facing {Facing}).")
            .SetWaterChannel(
                startX,
                0,
                startZ,
                Length,
                Facing,
                BaseBlock,
                SurroundBaseWithTrapdoors,
                GuideWaterWithTrapdoors,
                CoverTopWithTrapdoors,
                TrapdoorBlock,
                CloseEndWithTrapdoor,
                DividerBlock);
    }
}
