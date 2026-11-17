using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;
using SchematicBuilderMC.Models;
using SchematicBuilderMC.Registries;
using SchematicBuilderMC.Schematics.Redstone.Internal;

namespace SchematicBuilderMC.Schematics.Redstone.Clocks;

/// <summary>
/// Builds a configurable Etho hopper clock whose cycle period scales linearly with hopper items
/// (1 item = 16 game ticks = 0.8 s, up to 320 items = 5120 game ticks = 256 s).
/// </summary>
public class HopperClockSchematicCreator : SchematicCreator
{
    public TimeSpan? Period { get; set; }

    public int? PeriodTicks { get; set; }

    public Direction Facing { get; set; } = Direction.East;

    public string CircuitBlock { get; set; } = Minecraft.Blocks.SmoothStone;

    public string ItemId { get; set; } = Minecraft.Items.Cobblestone;

    public bool IncludeToggleLever { get; set; }

    public bool IncludeOutputLamp { get; set; }

    public int GetItemCount()
    {
        int ticks = CircuitLayout.ResolveGameTicks(Period, PeriodTicks, defaultGameTicks: 160, minGameTicks: 16, stepGameTicks: 16);
        return Math.Clamp((int)Math.Round(ticks / 16.0), 1, 320);
    }

    public int GetActualPeriodTicks() => GetItemCount() * 16;

    public override SchematicBuilder Create()
    {
        CircuitLayout.ValidateHorizontalFacing(Facing);
        CircuitLayout.ValidateBlockId(CircuitBlock, nameof(CircuitBlock));
        CircuitLayout.ValidateBlockId(ItemId, nameof(ItemId));

        int itemCount = GetItemCount();
        int periodTicks = itemCount * 16;
        const int coreLengthU = 6;
        const int widthV = 2;
        int totalLengthU = coreLengthU + (IncludeOutputLamp ? 1 : 0);
        int height = IncludeToggleLever ? 3 : 2;
        int maxU = totalLengthU - 1;
        int maxV = widthV - 1;

        var builder = CircuitLayout.CreateBuilder(
            this,
            CircuitLayout.GetWidth(totalLengthU, widthV, Facing),
            height,
            CircuitLayout.GetLength(totalLengthU, widthV, Facing),
            "Etho Hopper Clock",
            $"Etho hopper clock with {itemCount} item(s) ({periodTicks} gt / {periodTicks * 0.05:0.##} s period) facing {Facing}.");

        for (int u = 0; u < totalLengthU; u++)
        {
            for (int v = 0; v < widthV; v++)
            {
                builder.SetBlock(CircuitLayout.Map(0, 0, 0, u, 0, v, maxU, maxV, Facing), CircuitBlock);
            }
        }

        Direction reverse = CircuitLayout.GetOpposite(Facing);
        ItemStack[] stacks = CircuitLayout.CreateItemStacks(ItemId, itemCount, maxSlots: 5);

        builder.SetBlock(CircuitLayout.Map(0, 0, 0, 0, 1, 0, maxU, maxV, Facing), CircuitBlock);
        CircuitLayout.SetComparator(builder, CircuitLayout.Map(0, 0, 0, 1, 1, 0, maxU, maxV, Facing), reverse, subtractMode: false, powered: true);
        CircuitLayout.SetHopper(builder, CircuitLayout.Map(0, 0, 0, 2, 1, 0, maxU, maxV, Facing), Facing, enabled: true, stacks);
        CircuitLayout.SetHopper(builder, CircuitLayout.Map(0, 0, 0, 3, 1, 0, maxU, maxV, Facing), reverse, enabled: false);
        CircuitLayout.SetComparator(builder, CircuitLayout.Map(0, 0, 0, 4, 1, 0, maxU, maxV, Facing), Facing, subtractMode: false, powered: false);
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, 5, 1, 0, maxU, maxV, Facing), CircuitBlock);

        builder.SetBlock(CircuitLayout.Map(0, 0, 0, 0, 1, 1, maxU, maxV, Facing), CircuitLayout.CreateWire(Facing, north: true, east: true, south: true, west: true, power: 1));
        builder.SetPiston(CircuitLayout.Map(0, 0, 0, 1, 1, 1, maxU, maxV, Facing), Facing, sticky: true);
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, 3, 1, 1, maxU, maxV, Facing), Minecraft.Blocks.RedstoneBlock);
        builder.SetPiston(CircuitLayout.Map(0, 0, 0, 4, 1, 1, maxU, maxV, Facing), reverse, sticky: true);
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, 5, 1, 1, maxU, maxV, Facing), CircuitLayout.CreateWire(Facing, north: true, east: true, south: true, west: true));

        if (IncludeToggleLever)
        {
            builder.SetLever(CircuitLayout.Map(0, 0, 0, 0, 2, 0, maxU, maxV, Facing), BlockFace.Floor, Facing, powered: false);
        }

        if (IncludeOutputLamp)
        {
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, totalLengthU - 1, 1, 0, maxU, maxV, Facing), Minecraft.Blocks.RedstoneLamp);
        }

        return builder;
    }
}
