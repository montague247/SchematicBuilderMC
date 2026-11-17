using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;
using SchematicBuilderMC.Models;
using SchematicBuilderMC.Registries;
using SchematicBuilderMC.Schematics.Redstone.Internal;

namespace SchematicBuilderMC.Schematics.Redstone.Clocks;

/// <summary>
/// Builds a compact Redstone comparator subtraction clock with optional toggle lever and extended loop period.
/// </summary>
public class ComparatorClockSchematicCreator : SchematicCreator
{
    public TimeSpan? Period { get; set; }

    public int? PeriodTicks { get; set; }

    public Direction Facing { get; set; } = Direction.East;

    public string CircuitBlock { get; set; } = Minecraft.Blocks.SmoothStone;

    public bool IncludeToggleLever { get; set; } = true;

    public bool IncludeOutputLamp { get; set; }

    public int GetActualPeriodTicks()
    {
        int requested = CircuitLayout.ResolveGameTicks(Period, PeriodTicks, defaultGameTicks: 4, minGameTicks: 4, stepGameTicks: 4);
        int extraRt = Math.Max(0, (requested - 4) / 4);
        return 4 + (extraRt * 4);
    }

    public override SchematicBuilder Create()
    {
        CircuitLayout.ValidateHorizontalFacing(Facing);
        CircuitLayout.ValidateBlockId(CircuitBlock, nameof(CircuitBlock));

        int periodTicks = GetActualPeriodTicks();
        int extraRt = (periodTicks - 4) / 4;
        int repeaterCount = extraRt > 0 ? (int)Math.Ceiling(extraRt / 4.0) : 0;

        int coreLengthU = 3 + repeaterCount;
        const int widthV = 2;
        int totalLengthU = coreLengthU + (IncludeOutputLamp ? 1 : 0);
        int height = 3;
        int maxU = totalLengthU - 1;
        int maxV = widthV - 1;

        var builder = CircuitLayout.CreateBuilder(
            this,
            CircuitLayout.GetWidth(totalLengthU, widthV, Facing),
            height,
            CircuitLayout.GetLength(totalLengthU, widthV, Facing),
            "Comparator Clock",
            $"Subtraction comparator clock ({periodTicks} gt period) facing {Facing}.");

        for (int u = 0; u < totalLengthU; u++)
        {
            for (int v = 0; v < widthV; v++)
            {
                builder.SetBlock(CircuitLayout.Map(0, 0, 0, u, 0, v, maxU, maxV, Facing), CircuitBlock);
            }
        }

        Direction reverse = CircuitLayout.GetOpposite(Facing);

        builder.SetBlock(CircuitLayout.Map(0, 0, 0, 0, 1, 0, maxU, maxV, Facing), CircuitBlock);
        if (IncludeToggleLever)
        {
            builder.SetLever(CircuitLayout.Map(0, 0, 0, 0, 2, 0, maxU, maxV, Facing), BlockFace.Floor, Facing, powered: true);
        }
        else
        {
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, 0, 2, 0, maxU, maxV, Facing), Minecraft.Blocks.RedstoneTorch);
        }

        CircuitLayout.SetComparator(builder, CircuitLayout.Map(0, 0, 0, 1, 1, 0, maxU, maxV, Facing), Facing, subtractMode: true);

        int tailU = 2 + repeaterCount;
        for (int u = 2; u <= tailU; u++)
        {
            builder.SetBlock(
                CircuitLayout.Map(0, 0, 0, u, 1, 0, maxU, maxV, Facing),
                CircuitLayout.CreateWire(Facing, north: false, east: u < tailU || IncludeOutputLamp, south: u == tailU, west: true));
        }

        builder.SetBlock(CircuitLayout.Map(0, 0, 0, tailU, 1, 1, maxU, maxV, Facing), CircuitLayout.CreateWire(Facing, north: true, east: false, south: false, west: true));
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, 1, 1, 1, maxU, maxV, Facing), CircuitLayout.CreateWire(Facing, north: true, east: true, south: false, west: false));

        int remainingRt = extraRt;
        for (int r = 0; r < repeaterCount; r++)
        {
            int left = repeaterCount - 1 - r;
            int rt = Math.Clamp(remainingRt - left, 1, 4);
            remainingRt -= rt;
            CircuitLayout.SetRepeater(builder, CircuitLayout.Map(0, 0, 0, tailU - 1 - r, 1, 1, maxU, maxV, Facing), reverse, delay: rt);
        }

        if (IncludeOutputLamp)
        {
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, totalLengthU - 1, 1, 0, maxU, maxV, Facing), Minecraft.Blocks.RedstoneLamp);
        }

        return builder;
    }
}
