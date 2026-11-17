using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;
using SchematicBuilderMC.Models;
using SchematicBuilderMC.Registries;
using SchematicBuilderMC.Schematics.Redstone.Internal;

namespace SchematicBuilderMC.Schematics.Redstone.Clocks;

/// <summary>
/// Builds a compact facing-observer Redstone clock (base 4-game-tick period, optionally gated by a toggle piston or delayed).
/// </summary>
public class ObserverClockSchematicCreator : SchematicCreator
{
    public TimeSpan? Period { get; set; }

    public int? PeriodTicks { get; set; }

    public Direction Facing { get; set; } = Direction.East;

    public string CircuitBlock { get; set; } = Minecraft.Blocks.SmoothStone;

    public bool IncludeToggleLever { get; set; } = true;

    public bool IncludeOutputLamp { get; set; }

    public override SchematicBuilder Create()
    {
        CircuitLayout.ValidateHorizontalFacing(Facing);
        CircuitLayout.ValidateBlockId(CircuitBlock, nameof(CircuitBlock));

        int basePeriod = 4;
        if (Period.HasValue || PeriodTicks.HasValue)
        {
            basePeriod = CircuitLayout.ResolveGameTicks(Period, PeriodTicks, defaultGameTicks: 4, minGameTicks: 4, stepGameTicks: 2);
        }

        int extraRepeaters = basePeriod > 4 ? (int)Math.Ceiling((basePeriod - 4) / 8.0) : 0;
        int leverLead = IncludeToggleLever ? 1 : 0;
        int coreLengthU = 3 + extraRepeaters;
        const int widthV = 2;
        int totalLengthU = leverLead + coreLengthU + (IncludeOutputLamp ? 1 : 0);
        int maxU = totalLengthU - 1;
        int maxV = widthV - 1;

        var builder = CircuitLayout.CreateBuilder(
            this,
            CircuitLayout.GetWidth(totalLengthU, widthV, Facing),
            2,
            CircuitLayout.GetLength(totalLengthU, widthV, Facing),
            "Observer Clock",
            $"Facing-observer pulse clock ({basePeriod} gt target) facing {Facing}.");

        for (int u = 0; u < totalLengthU; u++)
        {
            for (int v = 0; v < widthV; v++)
            {
                builder.SetBlock(CircuitLayout.Map(0, 0, 0, u, 0, v, maxU, maxV, Facing), CircuitBlock);
            }
        }

        Direction reverse = CircuitLayout.GetOpposite(Facing);
        Direction lookNorth = CircuitLayout.RotateDirection(Direction.North, Facing);
        int startU = leverLead;

        if (IncludeToggleLever)
        {
            builder.SetLever(CircuitLayout.Map(0, 0, 0, 0, 1, 0, maxU, maxV, Facing), BlockFace.Floor, Facing, powered: true);
            builder.SetPiston(CircuitLayout.Map(0, 0, 0, startU, 1, 0, maxU, maxV, Facing), Facing, sticky: true, extended: true);
        }

        int obs1U = startU + (IncludeToggleLever ? 1 : 0);
        int obs2U = obs1U + 1;
        CircuitLayout.SetObserver(builder, CircuitLayout.Map(0, 0, 0, obs1U, 1, 0, maxU, maxV, Facing), Facing);
        CircuitLayout.SetObserver(builder, CircuitLayout.Map(0, 0, 0, obs2U, 1, 0, maxU, maxV, Facing), reverse);

        // Optional output extension repeaters or wire along U
        int outU = obs2U + 1;
        int remainingRt = Math.Max(0, (basePeriod - 4) / 2);
        for (int r = 0; r < extraRepeaters; r++)
        {
            int left = extraRepeaters - 1 - r;
            int rt = Math.Clamp(remainingRt - left, 1, 4);
            remainingRt -= rt;
            CircuitLayout.SetRepeater(builder, CircuitLayout.Map(0, 0, 0, outU + r, 1, 0, maxU, maxV, Facing), Facing, delay: rt);
        }

        if (IncludeOutputLamp)
        {
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, totalLengthU - 1, 1, 0, maxU, maxV, Facing), Minecraft.Blocks.RedstoneLamp);
        }

        // Keep unused observer row reference tidy via side observer tap if toggled
        CircuitLayout.SetObserver(builder, CircuitLayout.Map(0, 0, 0, obs2U, 1, 1, maxU, maxV, Facing), lookNorth);
        return builder;
    }
}
