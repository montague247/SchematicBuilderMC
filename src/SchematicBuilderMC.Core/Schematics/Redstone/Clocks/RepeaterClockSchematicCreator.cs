using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;
using SchematicBuilderMC.Models;
using SchematicBuilderMC.Registries;
using SchematicBuilderMC.Schematics.Redstone.Internal;

namespace SchematicBuilderMC.Schematics.Redstone.Clocks;

/// <summary>
/// Builds a configurable two-lane repeater loop clock supporting arbitrary periods via <see cref="Period"/> or <see cref="PeriodTicks"/>.
/// </summary>
public class RepeaterClockSchematicCreator : SchematicCreator
{
    public TimeSpan? Period { get; set; }

    public int? PeriodTicks { get; set; }

    public Direction Facing { get; set; } = Direction.East;

    public string CircuitBlock { get; set; } = Minecraft.Blocks.SmoothStone;

    public bool IncludeToggleLever { get; set; }

    public bool IncludeOutputLamp { get; set; }

    public int GetActualPeriodTicks()
    {
        int ticks = CircuitLayout.ResolveGameTicks(Period, PeriodTicks, defaultGameTicks: 8, minGameTicks: 4, stepGameTicks: 2);
        return Math.Max(4, ticks);
    }

    public override SchematicBuilder Create()
    {
        CircuitLayout.ValidateHorizontalFacing(Facing);
        CircuitLayout.ValidateBlockId(CircuitBlock, nameof(CircuitBlock));

        int totalGameTicks = GetActualPeriodTicks();
        int totalRt = totalGameTicks / 2;
        int repeatersPerLane = Math.Max(1, (int)Math.Ceiling(totalRt / 8.0));

        // 2 corner columns (u=0 and u=repeatersPerLane+1) + optional input lever + optional output lamp
        int leverLead = IncludeInputLever ? 1 : 0;
        int coreLengthU = repeatersPerLane + 2;
        const int widthV = 2;
        int totalLengthU = leverLead + coreLengthU + (IncludeOutputLamp ? 1 : 0);
        int maxU = totalLengthU - 1;
        int maxV = widthV - 1;

        var builder = CircuitLayout.CreateBuilder(
            this,
            CircuitLayout.GetWidth(totalLengthU, widthV, Facing),
            2,
            CircuitLayout.GetLength(totalLengthU, widthV, Facing),
            "Repeater Clock",
            $"Two-lane repeater loop clock ({totalGameTicks} gt / {totalGameTicks * 0.05:0.##} s period) facing {Facing}.");

        for (int u = 0; u < totalLengthU; u++)
        {
            for (int v = 0; v < widthV; v++)
            {
                builder.SetBlock(CircuitLayout.Map(0, 0, 0, u, 0, v, maxU, maxV, Facing), CircuitBlock);
            }
        }

        if (IncludeInputLever)
        {
            builder.SetLever(CircuitLayout.Map(0, 0, 0, 0, 1, 0, maxU, maxV, Facing), BlockFace.Floor, Facing, powered: false);
        }

        int startU = leverLead;
        int endU = startU + coreLengthU - 1;
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, startU, 1, 0, maxU, maxV, Facing), CircuitLayout.CreateWire(Facing, north: false, east: true, south: true, west: IncludeInputLever));
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, startU, 1, 1, maxU, maxV, Facing), CircuitLayout.CreateWire(Facing, north: true, east: true, south: false, west: false));
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, endU, 1, 0, maxU, maxV, Facing), CircuitLayout.CreateWire(Facing, north: false, east: IncludeOutputLamp, south: true, west: true));
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, endU, 1, 1, maxU, maxV, Facing), CircuitLayout.CreateWire(Facing, north: true, east: false, south: false, west: true));

        Direction reverse = CircuitLayout.GetOpposite(Facing);
        int totalRepeaters = repeatersPerLane * 2;
        int remainingRt = Math.Max(totalRt, totalRepeaters);

        for (int r = 0; r < repeatersPerLane; r++)
        {
            int leftA = totalRepeaters - 1 - (r * 2);
            int rtA = Math.Clamp(remainingRt - leftA, 1, 4);
            remainingRt -= rtA;

            int leftB = totalRepeaters - 1 - ((r * 2) + 1);
            int rtB = Math.Clamp(remainingRt - leftB, 1, 4);
            remainingRt -= rtB;

            int u = startU + 1 + r;
            CircuitLayout.SetRepeater(builder, CircuitLayout.Map(0, 0, 0, u, 1, 0, maxU, maxV, Facing), Facing, delay: rtA);
            CircuitLayout.SetRepeater(builder, CircuitLayout.Map(0, 0, 0, u, 1, 1, maxU, maxV, Facing), reverse, delay: rtB);
        }

        if (IncludeOutputLamp)
        {
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, totalLengthU - 1, 1, 0, maxU, maxV, Facing), Minecraft.Blocks.RedstoneLamp);
        }

        return builder;
    }

    private bool IncludeInputLever => IncludeToggleLever;
}
