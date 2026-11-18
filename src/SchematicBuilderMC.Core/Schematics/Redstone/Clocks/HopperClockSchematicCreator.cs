using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;
using SchematicBuilderMC.Extensions;
using SchematicBuilderMC.Models;
using SchematicBuilderMC.Registries;
using SchematicBuilderMC.Schematics.Redstone.Internal;

namespace SchematicBuilderMC.Schematics.Redstone.Clocks;

/// <summary>
/// Selects where optional output/indicator redstone wall torches are placed on a hopper clock.
/// </summary>
public enum HopperClockTorchPlacement
{
    /// <summary>No external end torches are added.</summary>
    None,

    /// <summary>Places 1 or 2 redstone wall torches outward from the beginning end block.</summary>
    Beginning,

    /// <summary>Places 1 or 2 redstone wall torches outward from the ending end block.</summary>
    End,

    /// <summary>Places 1 or 2 redstone wall torches outward from both beginning and ending blocks.</summary>
    Both
}

/// <summary>
/// Builds a configurable Etho hopper clock (classic 2-wide or tileable 1-wide) whose cycle period scales linearly with hopper items
/// (1 item = 16 game ticks = 0.8 s, up to 320 items = 5120 game ticks = 256 s), with optional beginning/end torches.
/// </summary>
public class HopperClockSchematicCreator : SchematicCreator
{
    public TimeSpan? Period { get; set; }

    public int? PeriodTicks { get; set; }

    public Direction Facing { get; set; } = Direction.East;

    /// <summary>
    /// When true, stacks the pistons and moving redstone block vertically above the hoppers for a 1-block-wide footprint.
    /// </summary>
    public bool OneWide { get; set; }

    /// <summary>
    /// Optional placement for output/inverter redstone torches at the beginning and/or end of the clock.
    /// </summary>
    public HopperClockTorchPlacement TorchPlacement { get; set; } = HopperClockTorchPlacement.None;

    /// <summary>
    /// Number of cascaded torches (1 or 2) placed at each selected end when <see cref="TorchPlacement"/> is not <see cref="HopperClockTorchPlacement.None"/>.
    /// </summary>
    public int TorchCount { get; set; } = 1;

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
        if (!Enum.IsDefined(TorchPlacement))
        {
            throw new ArgumentOutOfRangeException(nameof(TorchPlacement), TorchPlacement, "Unsupported torch placement.");
        }

        if (TorchCount is < 1 or > 2)
        {
            throw new ArgumentOutOfRangeException(nameof(TorchCount), TorchCount, "TorchCount must be 1 or 2.");
        }

        CircuitLayout.ValidateHorizontalFacing(Facing);
        CircuitLayout.ValidateBlockId(CircuitBlock, nameof(CircuitBlock));
        CircuitLayout.ValidateBlockId(ItemId, nameof(ItemId));

        int itemCount = GetItemCount();
        int periodTicks = itemCount * 16;

        bool hasBeginningTorches = TorchPlacement is HopperClockTorchPlacement.Beginning or HopperClockTorchPlacement.Both;
        bool hasEndTorches = TorchPlacement is HopperClockTorchPlacement.End or HopperClockTorchPlacement.Both;

        int torchExtension = TorchCount == 1 ? 1 : 4;
        int beginningLeadU = hasBeginningTorches ? torchExtension : 0;
        int endTailU = hasEndTorches ? torchExtension : 0;

        const int coreLengthU = 6;
        int widthV = OneWide ? 1 : 2;
        int totalLengthU = beginningLeadU + coreLengthU + endTailU + (IncludeOutputLamp ? 1 : 0);
        int height = OneWide
            ? (IncludeToggleLever ? 5 : 3)
            : (IncludeToggleLever ? 3 : 2);
        int maxU = totalLengthU - 1;
        int maxV = widthV - 1;

        var builder = CircuitLayout.CreateBuilder(
            this,
            CircuitLayout.GetWidth(totalLengthU, widthV, Facing),
            height,
            CircuitLayout.GetLength(totalLengthU, widthV, Facing),
            OneWide ? "1-Wide Etho Hopper Clock" : "Etho Hopper Clock",
            $"{(OneWide ? "1-wide" : "2-wide")} Etho hopper clock with {itemCount} item(s) ({periodTicks} gt / {periodTicks * 0.05:0.##} s period) facing {Facing}.");

        for (int u = 0; u < totalLengthU; u++)
        {
            for (int v = 0; v < widthV; v++)
            {
                builder.SetBlock(CircuitLayout.Map(0, 0, 0, u, 0, v, maxU, maxV, Facing), CircuitBlock);
            }
        }

        Direction reverse = CircuitLayout.GetOpposite(Facing);
        ItemStack[] stacks = CircuitLayout.CreateItemStacks(ItemId, itemCount, maxSlots: 5);
        int u0 = beginningLeadU;

        // Shared comparator + hopper bottom row along v = 0, y = 1
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, u0, 1, 0, maxU, maxV, Facing), CircuitBlock);
        CircuitLayout.SetComparator(builder, CircuitLayout.Map(0, 0, 0, u0 + 1, 1, 0, maxU, maxV, Facing), reverse, subtractMode: false, powered: true);
        CircuitLayout.SetHopper(builder, CircuitLayout.Map(0, 0, 0, u0 + 2, 1, 0, maxU, maxV, Facing), Facing, enabled: true, stacks);
        CircuitLayout.SetHopper(builder, CircuitLayout.Map(0, 0, 0, u0 + 3, 1, 0, maxU, maxV, Facing), reverse, enabled: false);
        CircuitLayout.SetComparator(builder, CircuitLayout.Map(0, 0, 0, u0 + 4, 1, 0, maxU, maxV, Facing), Facing, subtractMode: false, powered: false);
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, u0 + 5, 1, 0, maxU, maxV, Facing), CircuitBlock);

        // Pistons, cross redstone dust, and redstone block either stacked at y=2 (1-wide) or side-by-side at v=1 (2-wide)
        int pistonY = OneWide ? 2 : 1;
        int pistonV = OneWide ? 0 : 1;
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, u0, pistonY, pistonV, maxU, maxV, Facing), CircuitLayout.CreateWire(Facing, north: true, east: true, south: true, west: true, power: 1));
        builder.SetPiston(CircuitLayout.Map(0, 0, 0, u0 + 1, pistonY, pistonV, maxU, maxV, Facing), Facing, sticky: true);
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, u0 + 3, pistonY, pistonV, maxU, maxV, Facing), Minecraft.Blocks.RedstoneBlock);
        builder.SetPiston(CircuitLayout.Map(0, 0, 0, u0 + 4, pistonY, pistonV, maxU, maxV, Facing), reverse, sticky: true);
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, u0 + 5, pistonY, pistonV, maxU, maxV, Facing), CircuitLayout.CreateWire(Facing, north: true, east: true, south: true, west: true));

        if (hasBeginningTorches)
        {
            PlaceTorchChain(builder, u0, stepU: -1, reverse, TorchCount, firstTorchLit: false, maxU, maxV);
        }

        if (hasEndTorches)
        {
            PlaceTorchChain(builder, u0 + 5, stepU: 1, Facing, TorchCount, firstTorchLit: true, maxU, maxV);
        }

        if (IncludeToggleLever)
        {
            if (OneWide)
            {
                builder.SetBlock(CircuitLayout.Map(0, 0, 0, u0, 3, 0, maxU, maxV, Facing), CircuitBlock);
                builder.SetLever(CircuitLayout.Map(0, 0, 0, u0, 4, 0, maxU, maxV, Facing), BlockFace.Floor, Facing, powered: false);
            }
            else
            {
                builder.SetLever(CircuitLayout.Map(0, 0, 0, u0, 2, 0, maxU, maxV, Facing), BlockFace.Floor, Facing, powered: false);
            }
        }

        if (IncludeOutputLamp)
        {
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, totalLengthU - 1, 1, 0, maxU, maxV, Facing), Minecraft.Blocks.RedstoneLamp);
        }

        return builder;
    }

    private void PlaceTorchChain(
        SchematicBuilder builder,
        int anchorBlockU,
        int stepU,
        Direction torchFacing,
        int count,
        bool firstTorchLit,
        int maxU,
        int maxV)
    {
        builder.SetBlock(
            CircuitLayout.Map(0, 0, 0, anchorBlockU + stepU, 1, 0, maxU, maxV, Facing),
            Minecraft.Blocks.RedstoneWallTorch.WithFacing(torchFacing).WithLit(firstTorchLit));

        if (count == 2)
        {
            builder.SetBlock(
                CircuitLayout.Map(0, 0, 0, anchorBlockU + (stepU * 2), 1, 0, maxU, maxV, Facing),
                CircuitLayout.CreateAxisWire(Facing, alignedWithForwardAxis: true));
            builder.SetBlock(
                CircuitLayout.Map(0, 0, 0, anchorBlockU + (stepU * 3), 1, 0, maxU, maxV, Facing),
                CircuitBlock);
            builder.SetBlock(
                CircuitLayout.Map(0, 0, 0, anchorBlockU + (stepU * 4), 1, 0, maxU, maxV, Facing),
                Minecraft.Blocks.RedstoneWallTorch.WithFacing(torchFacing).WithLit(!firstTorchLit));
        }
    }
}
