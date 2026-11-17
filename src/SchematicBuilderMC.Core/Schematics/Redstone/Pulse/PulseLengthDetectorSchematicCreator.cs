using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;
using SchematicBuilderMC.Extensions;
using SchematicBuilderMC.Models;
using SchematicBuilderMC.Registries;
using SchematicBuilderMC.Schematics.Redstone.Internal;

namespace SchematicBuilderMC.Schematics.Redstone.Pulse;

/// <summary>
/// Builds a configurable pulse-length detector (on-delay gate) that outputs ONLY if an input signal stays high
/// for at least <see cref="MinimumDuration"/> or <see cref="MinimumTicks"/> game ticks.
/// </summary>
public class PulseLengthDetectorSchematicCreator : SchematicCreator
{
    /// <summary>
    /// Optional minimum sustained input duration required to trigger output.
    /// </summary>
    public TimeSpan? MinimumDuration { get; set; }

    /// <summary>
    /// Optional explicit minimum sustained input duration in game ticks (overrides <see cref="MinimumDuration"/>).
    /// </summary>
    public int? MinimumTicks { get; set; }

    public Direction Facing { get; set; } = Direction.East;

    public string CircuitBlock { get; set; } = Minecraft.Blocks.SmoothStone;

    public bool IncludeInputLever { get; set; }

    public bool IncludeOutputLamp { get; set; }

    public int GetActualMinimumTicks() =>
        CircuitLayout.ResolveGameTicks(MinimumDuration, MinimumTicks, defaultGameTicks: 8, minGameTicks: 2, stepGameTicks: 2);

    public override SchematicBuilder Create()
    {
        CircuitLayout.ValidateHorizontalFacing(Facing);
        CircuitLayout.ValidateBlockId(CircuitBlock, nameof(CircuitBlock));

        int minTicks = GetActualMinimumTicks();
        int totalRt = minTicks / 2;
        int repeaterCount = Math.Max(1, (int)Math.Ceiling(totalRt / 4.0));
        int delaySpan = Math.Max(2, repeaterCount);

        int inputLead = IncludeInputLever ? 1 : 0;
        int coreLengthU = 3 + delaySpan;
        const int widthV = 2;
        int totalLengthU = inputLead + coreLengthU + (IncludeOutputLamp ? 1 : 0);
        int height = IncludeInputButtonOrLeverHeight();
        int maxU = totalLengthU - 1;
        int maxV = widthV - 1;

        var builder = CircuitLayout.CreateBuilder(
            this,
            CircuitLayout.GetWidth(totalLengthU, widthV, Facing),
            height,
            CircuitLayout.GetLength(totalLengthU, widthV, Facing),
            "Pulse Length Detector",
            $"On-delay pulse length detector requiring >= {minTicks} gt continuous input facing {Facing}.");

        for (int u = 0; u < totalLengthU; u++)
        {
            for (int v = 0; v < widthV; v++)
            {
                builder.SetBlock(CircuitLayout.Map(0, 0, 0, u, 0, v, maxU, maxV, Facing), CircuitBlock);
            }
        }

        if (IncludeInputLever)
        {
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, 0, 1, 0, maxU, maxV, Facing), CircuitBlock);
            builder.SetLever(CircuitLayout.Map(0, 0, 0, 0, 2, 0, maxU, maxV, Facing), BlockFace.Floor, Facing, powered: false);
        }

        Direction torchFacing = CircuitLayout.RotateDirection(Direction.East, Facing);

        builder.SetBlock(CircuitLayout.Map(0, 0, 0, inputLead, 1, 0, maxU, maxV, Facing), CircuitLayout.CreateWire(Facing, north: false, east: true, south: true, west: true));
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, inputLead, 1, 1, maxU, maxV, Facing), CircuitLayout.CreateWire(Facing, north: true, east: true, south: false, west: false));

        int remainingRt = totalRt;
        for (int r = 0; r < repeaterCount; r++)
        {
            int left = repeaterCount - 1 - r;
            int rt = Math.Clamp(remainingRt - left, 1, 4);
            remainingRt -= rt;
            CircuitLayout.SetRepeater(builder, CircuitLayout.Map(0, 0, 0, inputLead + 1 + r, 1, 0, maxU, maxV, Facing), Facing, delay: rt);
        }

        if (delaySpan > repeaterCount)
        {
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, inputLead + 1 + repeaterCount, 1, 0, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));
        }

        int gateU = inputLead + 1 + delaySpan;
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, inputLead + 1, 1, 1, maxU, maxV, Facing), CircuitBlock);
        builder.SetBlock(
            CircuitLayout.Map(0, 0, 0, inputLead + 2, 1, 1, maxU, maxV, Facing),
            Minecraft.Blocks.RedstoneWallTorch.WithFacing(torchFacing).WithLit(true));

        for (int u = inputLead + 3; u <= gateU; u++)
        {
            builder.SetBlock(
                CircuitLayout.Map(0, 0, 0, u, 1, 1, maxU, maxV, Facing),
                CircuitLayout.CreateWire(Facing, north: u == gateU, east: u < gateU, south: false, west: true));
        }

        CircuitLayout.SetComparator(builder, CircuitLayout.Map(0, 0, 0, gateU, 1, 0, maxU, maxV, Facing), Facing, subtractMode: true);
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, gateU + 1, 1, 0, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));

        if (IncludeOutputLamp)
        {
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, totalLengthU - 1, 1, 0, maxU, maxV, Facing), Minecraft.Blocks.RedstoneLamp);
        }

        return builder;
    }

    private int IncludeInputButtonOrLeverHeight() => IncludeInputLever ? 3 : 2;
}
