using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;
using SchematicBuilderMC.Extensions;
using SchematicBuilderMC.Models;
using SchematicBuilderMC.Registries;
using SchematicBuilderMC.Schematics.Redstone.Internal;

namespace SchematicBuilderMC.Schematics.Redstone.Transmission;

/// <summary>
/// Builds a zero-tick rising-edge piston instant repeater using cut-wire unblocking for instantaneous signal propagation.
/// Supports chaining multiple 15-block-range stages via <see cref="StageCount"/>.
/// </summary>
public class InstantRepeaterSchematicCreator : SchematicCreator
{
    private const int StageLengthU = 5;

    /// <summary>
    /// Number of chained instant-repeater stages (each stage refreshes signal strength to 14 at the cut torch).
    /// </summary>
    public int StageCount { get; set; } = 1;

    /// <summary>
    /// Signal transmission direction. Must be horizontal.
    /// </summary>
    public Direction Facing { get; set; } = Direction.East;

    /// <summary>
    /// Solid opaque block used for wire supports, cutting block, and piston mounting.
    /// </summary>
    public string CircuitBlock { get; set; } = Minecraft.Blocks.SmoothStone;

    /// <summary>
    /// Adds a lever at the input terminal.
    /// </summary>
    public bool IncludeInputLever { get; set; }

    /// <summary>
    /// Adds a redstone lamp at the final output terminal.
    /// </summary>
    public bool IncludeOutputLamp { get; set; }

    public override SchematicBuilder Create()
    {
        CircuitLayout.ValidateHorizontalFacing(Facing);
        CircuitLayout.ValidatePositive(StageCount, nameof(StageCount), 1);
        CircuitLayout.ValidateBlockId(CircuitBlock, nameof(CircuitBlock));

        int inputLead = IncludeInputLever ? 1 : 0;
        int outputExtra = IncludeOutputLamp ? 1 : 0;
        int canonicalLengthU = inputLead + (StageCount * StageLengthU) + outputExtra;
        const int widthV = 3;
        const int height = 3;
        int maxU = canonicalLengthU - 1;
        int maxV = widthV - 1;

        var builder = CircuitLayout.CreateBuilder(
            this,
            CircuitLayout.GetWidth(canonicalLengthU, widthV, Facing),
            height,
            CircuitLayout.GetLength(canonicalLengthU, widthV, Facing),
            "Instant Repeater",
            $"0-tick cut-wire piston instant repeater ({StageCount} stage(s)) facing {Facing}.");

        for (int u = 0; u < canonicalLengthU; u++)
        {
            for (int v = 0; v < widthV; v++)
            {
                builder.SetBlock(CircuitLayout.Map(0, 0, 0, u, 0, v, maxU, maxV, Facing), CircuitBlock);
            }
        }

        if (IncludeInputLever)
        {
            builder.SetLever(CircuitLayout.Map(0, 0, 0, 0, 1, 2, maxU, maxV, Facing), BlockFace.Floor, Facing, powered: false);
        }

        Direction pistonFacing = CircuitLayout.RotateDirection(Direction.North, Facing);

        for (int stage = 0; stage < StageCount; stage++)
        {
            int sU = inputLead + (stage * StageLengthU);

            // Stage input on v=2 (bridging from previous stage's v=1 output when chained)
            if (stage > 0)
            {
                builder.SetBlock(CircuitLayout.Map(0, 0, 0, sU, 1, 1, maxU, maxV, Facing), CircuitLayout.CreateWire(Facing, north: false, east: false, south: true, west: true));
                builder.SetBlock(CircuitLayout.Map(0, 0, 0, sU, 1, 2, maxU, maxV, Facing), CircuitLayout.CreateWire(Facing, north: true, east: true, south: false, west: false));
            }
            else
            {
                builder.SetBlock(CircuitLayout.Map(0, 0, 0, sU, 1, 2, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));
            }

            builder.SetBlock(CircuitLayout.Map(0, 0, 0, sU + 1, 1, 2, maxU, maxV, Facing), CircuitBlock);
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, sU + 1, 2, 2, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));
            builder.SetPiston(CircuitLayout.Map(0, 0, 0, sU + 2, 2, 2, maxU, maxV, Facing), pistonFacing, sticky: true, extended: false);

            // Cut-wire bus on v=1 using a constant upright redstone torch at v=0
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, sU + 2, 1, 0, maxU, maxV, Facing), Minecraft.Blocks.RedstoneTorch.WithLit(true));
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, sU + 2, 1, 1, maxU, maxV, Facing), CircuitLayout.CreateWire(Facing, north: true, east: true, south: false, west: false));
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, sU + 2, 2, 1, maxU, maxV, Facing), CircuitBlock);
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, sU + 3, 1, 1, maxU, maxV, Facing), CircuitBlock);
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, sU + 3, 2, 1, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, sU + 4, 1, 1, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));
        }

        if (IncludeOutputLamp)
        {
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, canonicalLengthU - 1, 1, 1, maxU, maxV, Facing), Minecraft.Blocks.RedstoneLamp);
        }

        return builder;
    }
}
