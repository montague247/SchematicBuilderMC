using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;
using SchematicBuilderMC.Models;
using SchematicBuilderMC.Registries;
using SchematicBuilderMC.Schematics.Redstone.Internal;

namespace SchematicBuilderMC.Schematics.Redstone.Clocks;

/// <summary>
/// Builds a sticky-piston Redstone block clock with optional configurable cycle period via <see cref="Period"/> or <see cref="PeriodTicks"/>.
/// </summary>
public class PistonClockSchematicCreator : SchematicCreator
{
    public TimeSpan? Period { get; set; }

    public int? PeriodTicks { get; set; }

    public Direction Facing { get; set; } = Direction.East;

    public string CircuitBlock { get; set; } = Minecraft.Blocks.SmoothStone;

    public bool IncludeToggleLever { get; set; }

    public bool IncludeOutputLamp { get; set; }

    public int GetActualPeriodTicks()
    {
        int requested = CircuitLayout.ResolveGameTicks(Period, PeriodTicks, defaultGameTicks: 8, minGameTicks: 6, stepGameTicks: 2);
        int feedbackRt = Math.Max(1, (requested - 4) / 2);
        return 4 + (feedbackRt * 2);
    }

    public override SchematicBuilder Create()
    {
        CircuitLayout.ValidateHorizontalFacing(Facing);
        CircuitLayout.ValidateBlockId(CircuitBlock, nameof(CircuitBlock));

        int periodTicks = GetActualPeriodTicks();
        int feedbackRt = Math.Max(1, (periodTicks - 4) / 2);
        int repeaterCount = Math.Max(1, (int)Math.Ceiling(feedbackRt / 4.0));

        int leverLead = IncludeToggleLever ? 1 : 0;
        int coreLengthU = Math.Max(5, 3 + repeaterCount + 1);
        const int widthV = 2;
        int totalLengthU = leverLead + coreLengthU + (IncludeOutputLamp ? 1 : 0);
        int maxU = totalLengthU - 1;
        int maxV = widthV - 1;

        var builder = CircuitLayout.CreateBuilder(
            this,
            CircuitLayout.GetWidth(totalLengthU, widthV, Facing),
            2,
            CircuitLayout.GetLength(totalLengthU, widthV, Facing),
            "Piston Clock",
            $"Sticky-piston redstone block clock ({periodTicks} gt period) facing {Facing}.");

        for (int u = 0; u < totalLengthU; u++)
        {
            for (int v = 0; v < widthV; v++)
            {
                builder.SetBlock(CircuitLayout.Map(0, 0, 0, u, 0, v, maxU, maxV, Facing), CircuitBlock);
            }
        }

        Direction reverse = CircuitLayout.GetOpposite(Facing);
        int startU = leverLead;

        if (IncludeToggleLever)
        {
            builder.SetLever(CircuitLayout.Map(0, 0, 0, 0, 1, 0, maxU, maxV, Facing), BlockFace.Floor, Facing, powered: false);
        }

        // Piston at startU+1 pushes RedstoneBlock from startU+2 to startU+3.
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, startU, 1, 0, maxU, maxV, Facing), CircuitLayout.CreateWire(Facing, north: false, east: true, south: true, west: IncludeToggleLever));
        builder.SetPiston(CircuitLayout.Map(0, 0, 0, startU + 1, 1, 0, maxU, maxV, Facing), Facing, sticky: true);
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, startU + 2, 1, 0, maxU, maxV, Facing), Minecraft.Blocks.RedstoneBlock);

        // Feedback loop along v=1 from startU+2 back to startU,1,0
        int loopStartU = startU + 2;
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, startU, 1, 1, maxU, maxV, Facing), CircuitLayout.CreateWire(Facing, north: true, east: true, south: false, west: false));

        int remainingRt = feedbackRt;
        int returnEndU = startU + repeaterCount;
        for (int r = 0; r < repeaterCount; r++)
        {
            int left = repeaterCount - 1 - r;
            int rt = Math.Clamp(remainingRt - left, 1, 4);
            remainingRt -= rt;
            builder.SetRepeater(CircuitLayout.Map(0, 0, 0, startU + 1 + r, 1, 1, maxU, maxV, Facing), reverse, delay: rt);
        }

        for (int u = returnEndU + 1; u <= loopStartU + 1; u++)
        {
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, u, 1, 1, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));
        }

        if (IncludeOutputLamp)
        {
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, totalLengthU - 1, 1, 0, maxU, maxV, Facing), Minecraft.Blocks.RedstoneLamp);
        }

        return builder;
    }
}
