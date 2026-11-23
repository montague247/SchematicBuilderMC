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

    /// <summary>Places 1 or 2 redstone wall torches outward from the beginning end.</summary>
    Beginning,

    /// <summary>Places 1 or 2 redstone wall torches outward from the ending end.</summary>
    End,

    /// <summary>Places 1 or 2 redstone wall torches outward from both beginning and ending ends.</summary>
    Both
}

/// <summary>
/// Builds a configurable Etho hopper clock (classic 2-wide piston/redstone-block or silent 1-wide pistonless RS-NOR hopper clock)
/// whose cycle period scales linearly with hopper items (1 item = 16 game ticks = 0.8 s, up to 320 items = 5120 game ticks = 256 s),
/// with optional beginning/end torches.
/// </summary>
public class HopperClockSchematicCreator : SchematicCreator
{
    public TimeSpan? Period { get; set; }

    public int? PeriodTicks { get; set; }

    public Direction Facing { get; set; } = Direction.East;

    /// <summary>
    /// When true, builds a 1-wide silent pistonless and redstone-block-free RS-NOR hopper clock.
    /// When false, builds the classic 2-wide sticky-piston Etho hopper clock.
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

    public string SlabBlock { get; set; } = Minecraft.Blocks.SmoothStoneSlab;

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
        CircuitLayout.ValidateBlockId(SlabBlock, nameof(SlabBlock));
        CircuitLayout.ValidateBlockId(ItemId, nameof(ItemId));

        return OneWide ? BuildOneWidePistonless() : BuildTwoWide();
    }

    private SchematicBuilder BuildTwoWide()
    {
        int itemCount = GetItemCount();
        int periodTicks = itemCount * 16;

        bool hasBeginningTorches = TorchPlacement is HopperClockTorchPlacement.Beginning or HopperClockTorchPlacement.Both;
        bool hasEndTorches = TorchPlacement is HopperClockTorchPlacement.End or HopperClockTorchPlacement.Both;

        int torchExtension = TorchCount == 1 ? 1 : 4;
        int beginningLeadU = hasBeginningTorches ? torchExtension : 0;
        int endTailU = hasEndTorches ? torchExtension : 0;

        const int coreLengthU = 6;
        const int widthV = 2;
        int totalLengthU = beginningLeadU + coreLengthU + endTailU + (IncludeOutputLamp ? 1 : 0);
        int height = IncludeToggleLever ? 3 : 2;
        int maxU = totalLengthU - 1;
        int maxV = widthV - 1;

        var builder = CircuitLayout.CreateBuilder(
            this,
            CircuitLayout.GetWidth(totalLengthU, widthV, Facing),
            height,
            CircuitLayout.GetLength(totalLengthU, widthV, Facing),
            "Etho Hopper Clock",
            $"2-wide Etho hopper clock with {itemCount} item(s) ({periodTicks} gt / {periodTicks * 0.05:0.##} s period) facing {Facing}.");

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

        // Comparator + hopper row along v = 0, y = 1
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, u0, 1, 0, maxU, maxV, Facing), CircuitBlock);
        CircuitLayout.SetComparator(builder, CircuitLayout.Map(0, 0, 0, u0 + 1, 1, 0, maxU, maxV, Facing), reverse, subtractMode: false, powered: true);
        CircuitLayout.SetHopper(builder, CircuitLayout.Map(0, 0, 0, u0 + 2, 1, 0, maxU, maxV, Facing), Facing, enabled: true, stacks);
        CircuitLayout.SetHopper(builder, CircuitLayout.Map(0, 0, 0, u0 + 3, 1, 0, maxU, maxV, Facing), reverse, enabled: false);
        CircuitLayout.SetComparator(builder, CircuitLayout.Map(0, 0, 0, u0 + 4, 1, 0, maxU, maxV, Facing), Facing, subtractMode: false, powered: false);
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, u0 + 5, 1, 0, maxU, maxV, Facing), CircuitBlock);

        // Pistons, cross dust, and moving redstone block side-by-side at v = 1
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, u0, 1, 1, maxU, maxV, Facing), CircuitLayout.CreateWire(Facing, north: true, east: true, south: true, west: true, power: 1));
        builder.SetPiston(CircuitLayout.Map(0, 0, 0, u0 + 1, 1, 1, maxU, maxV, Facing), Facing, sticky: true);
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, u0 + 3, 1, 1, maxU, maxV, Facing), Minecraft.Blocks.RedstoneBlock);
        builder.SetPiston(CircuitLayout.Map(0, 0, 0, u0 + 4, 1, 1, maxU, maxV, Facing), reverse, sticky: true);
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, u0 + 5, 1, 1, maxU, maxV, Facing), CircuitLayout.CreateWire(Facing, north: true, east: true, south: true, west: true));

        if (hasBeginningTorches)
        {
            PlaceTorchChain(builder, u0, y: 1, stepU: -1, reverse, TorchCount, firstTorchLit: false, maxU, maxV);
        }

        if (hasEndTorches)
        {
            PlaceTorchChain(builder, u0 + 5, y: 1, stepU: 1, Facing, TorchCount, firstTorchLit: true, maxU, maxV);
        }

        if (IncludeToggleLever)
        {
            builder.SetLever(CircuitLayout.Map(0, 0, 0, u0, 2, 0, maxU, maxV, Facing), BlockFace.Floor, Facing, powered: false);
        }

        if (IncludeOutputLamp)
        {
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, totalLengthU - 1, 1, 0, maxU, maxV, Facing), Minecraft.Blocks.RedstoneLamp);
        }

        return builder;
    }

    private SchematicBuilder BuildOneWidePistonless()
    {
        int itemCount = GetItemCount();
        int periodTicks = itemCount * 16;

        bool hasBeginningTorches = TorchPlacement is HopperClockTorchPlacement.Beginning or HopperClockTorchPlacement.Both;
        bool hasEndTorches = TorchPlacement is HopperClockTorchPlacement.End or HopperClockTorchPlacement.Both;

        int torchExtension = TorchCount == 1 ? 1 : 4;
        int beginningLeadU = hasBeginningTorches ? torchExtension + 1 : 0; // +1 anchor block for beginning torch chain
        int endTailU = hasEndTorches ? torchExtension : 0;

        const int coreLengthU = 12; // u = 0..11
        const int widthV = 1;
        int totalLengthU = beginningLeadU + coreLengthU + endTailU + (IncludeOutputLamp ? 1 : 0);
        const int height = 8; // y = 0..7
        int maxU = totalLengthU - 1;
        const int maxV = 0;

        var builder = CircuitLayout.CreateBuilder(
            this,
            CircuitLayout.GetWidth(totalLengthU, widthV, Facing),
            height,
            CircuitLayout.GetLength(totalLengthU, widthV, Facing),
            "1-Wide Silent Hopper Clock",
            $"1-wide pistonless RS-NOR hopper clock with {itemCount} item(s) ({periodTicks} gt / {periodTicks * 0.05:0.##} s period) facing {Facing}.");

        for (int u = 0; u < totalLengthU; u++)
        {
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, u, 0, 0, maxU, maxV, Facing), CircuitBlock);
        }

        Direction reverse = CircuitLayout.GetOpposite(Facing);
        ItemStack[] stacks = CircuitLayout.CreateItemStacks(ItemId, itemCount, maxSlots: 5);
        int c = beginningLeadU;

        // Bottom bus y = 1 (carries A_empty || Lock_A to Lock_B at c+7)
        BlockState topSlab = SlabBlock.WithSlabType("top");
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, c + 1, 1, 0, maxU, maxV, Facing), topSlab);
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, c + 2, 1, 0, maxU, maxV, Facing), topSlab);
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, c + 3, 1, 0, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, c + 4, 1, 0, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, c + 5, 1, 0, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));
        CircuitLayout.SetRepeater(builder, CircuitLayout.Map(0, 0, 0, c + 6, 1, 0, maxU, maxV, Facing), Facing, delay: 1);
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, c + 7, 1, 0, maxU, maxV, Facing), CircuitBlock);

        // Lock_B output & lift (u = c+7..c+11, y = 1..6)
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, c + 7, 2, 0, maxU, maxV, Facing), Minecraft.Blocks.RedstoneTorch.WithLit(true));
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, c + 7, 3, 0, maxU, maxV, Facing), CircuitBlock); // locks Hopper_B at c+7, y=4
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, c + 8, 1, 0, maxU, maxV, Facing), CircuitBlock);
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, c + 8, 2, 0, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, c + 9, 1, 0, maxU, maxV, Facing), CircuitBlock);
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, c + 9, 2, 0, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, c + 10, 1, 0, maxU, maxV, Facing), CircuitBlock);
        CircuitLayout.SetRepeater(builder, CircuitLayout.Map(0, 0, 0, c + 10, 2, 0, maxU, maxV, Facing), Facing, delay: 1, powered: true);

        builder.SetBlock(CircuitLayout.Map(0, 0, 0, c + 11, 2, 0, maxU, maxV, Facing), CircuitBlock);
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, c + 11, 3, 0, maxU, maxV, Facing), Minecraft.Blocks.RedstoneTorch.WithLit(false));
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, c + 11, 4, 0, maxU, maxV, Facing), CircuitBlock);
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, c + 11, 5, 0, maxU, maxV, Facing), Minecraft.Blocks.RedstoneTorch.WithLit(true));
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, c + 11, 6, 0, maxU, maxV, Facing), CircuitBlock); // Lock_B at y=6

        // Hopper & Comparator core plane at y = 4
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, c + 5, 3, 0, maxU, maxV, Facing), CircuitBlock); // Comparator_A support
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, c + 8, 3, 0, maxU, maxV, Facing), CircuitBlock); // Comparator_B support
        builder.SetBlock(
            CircuitLayout.Map(0, 0, 0, c + 3, 4, 0, maxU, maxV, Facing),
            Minecraft.Blocks.RedstoneWallTorch.WithFacing(reverse).WithLit(false)); // T_A = A_empty
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, c + 4, 4, 0, maxU, maxV, Facing), CircuitBlock); // A_has_items
        CircuitLayout.SetComparator(builder, CircuitLayout.Map(0, 0, 0, c + 5, 4, 0, maxU, maxV, Facing), reverse, subtractMode: false, powered: true);
        CircuitLayout.SetHopper(builder, CircuitLayout.Map(0, 0, 0, c + 6, 4, 0, maxU, maxV, Facing), Facing, enabled: true, stacks);
        CircuitLayout.SetHopper(builder, CircuitLayout.Map(0, 0, 0, c + 7, 4, 0, maxU, maxV, Facing), reverse, enabled: false);
        CircuitLayout.SetComparator(builder, CircuitLayout.Map(0, 0, 0, c + 8, 4, 0, maxU, maxV, Facing), Facing, subtractMode: false, powered: false);
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, c + 9, 4, 0, maxU, maxV, Facing), CircuitBlock); // B_has_items
        builder.SetBlock(
            CircuitLayout.Map(0, 0, 0, c + 10, 4, 0, maxU, maxV, Facing),
            Minecraft.Blocks.RedstoneWallTorch.WithFacing(Facing).WithLit(true)); // T_B = B_empty

        // B_empty || Lock_B top bus at y = 6..7 -> Lock_A inverter at (c+1, 7, 0)
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, c + 10, 5, 0, maxU, maxV, Facing), CircuitBlock);
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, c + 10, 6, 0, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true, power: 15));
        for (int u = c + 3; u <= c + 9; u++)
        {
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, u, 6, 0, maxU, maxV, Facing), CircuitBlock);
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, u, 7, 0, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true, power: 15 - ((c + 10) - u)));
        }

        builder.SetBlock(CircuitLayout.Map(0, 0, 0, c + 2, 7, 0, maxU, maxV, Facing), CircuitBlock);
        builder.SetBlock(
            CircuitLayout.Map(0, 0, 0, c + 1, 7, 0, maxU, maxV, Facing),
            Minecraft.Blocks.RedstoneWallTorch.WithFacing(reverse).WithLit(false)); // Lock_A

        // Lock_A output (c+1, 6, 0) -> feeds east to Hopper_A (c+6, 5, 0) AND drops via top-slab ladder to (c+2, 2, 0)
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, c + 1, 5, 0, maxU, maxV, Facing), topSlab);
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, c + 1, 6, 0, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));

        // East branch locking Hopper_A at (c+6, 5, 0)
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, c + 2, 4, 0, maxU, maxV, Facing), CircuitBlock);
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, c + 2, 5, 0, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, c + 3, 5, 0, maxU, maxV, Facing), CircuitBlock);
        CircuitLayout.SetRepeater(builder, CircuitLayout.Map(0, 0, 0, c + 4, 5, 0, maxU, maxV, Facing), Facing, delay: 1);
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, c + 5, 5, 0, maxU, maxV, Facing), CircuitBlock);
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, c + 6, 5, 0, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));

        // Downward top-slab ladder (Lock_A -> bottom bus y=1) + T_A merge at (c+2, 2, 0)
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, c, 4, 0, maxU, maxV, Facing), topSlab);
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, c, 5, 0, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, c + 1, 3, 0, maxU, maxV, Facing), topSlab);
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, c + 1, 4, 0, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, c, 2, 0, maxU, maxV, Facing), topSlab);
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, c, 3, 0, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, c + 1, 2, 0, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, c + 2, 2, 0, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, c + 3, 2, 0, maxU, maxV, Facing), topSlab);
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, c + 3, 3, 0, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));

        if (IncludeToggleLever)
        {
            // Wall lever under Hopper_A (c+6, 3, 0) locks Hopper_A when turned ON without adding height or width.
            builder.SetLever(CircuitLayout.Map(0, 0, 0, c + 6, 3, 0, maxU, maxV, Facing), BlockFace.Wall, Facing, powered: false);
        }

        if (hasBeginningTorches)
        {
            // (c, 5, 0) wire feeds into anchor block (c-1, 5, 0).
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, c - 1, 5, 0, maxU, maxV, Facing), CircuitBlock);
            PlaceTorchChain(builder, c - 1, y: 5, stepU: -1, reverse, TorchCount, firstTorchLit: true, maxU, maxV);
        }

        if (hasEndTorches)
        {
            PlaceTorchChain(builder, c + 11, y: 6, stepU: 1, Facing, TorchCount, firstTorchLit: false, maxU, maxV);
        }

        if (IncludeOutputLamp)
        {
            int lampY = hasEndTorches ? 6 : 6;
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, totalLengthU - 1, lampY, 0, maxU, maxV, Facing), Minecraft.Blocks.RedstoneLamp);
        }

        return builder;
    }

    private void PlaceTorchChain(
        SchematicBuilder builder,
        int anchorBlockU,
        int y,
        int stepU,
        Direction torchFacing,
        int count,
        bool firstTorchLit,
        int maxU,
        int maxV)
    {
        builder.SetBlock(
            CircuitLayout.Map(0, 0, 0, anchorBlockU + stepU, y, 0, maxU, maxV, Facing),
            Minecraft.Blocks.RedstoneWallTorch.WithFacing(torchFacing).WithLit(firstTorchLit));

        if (count == 2)
        {
            builder.SetBlock(
                CircuitLayout.Map(0, 0, 0, anchorBlockU + (stepU * 2), y - 1, 0, maxU, maxV, Facing),
                CircuitBlock);
            builder.SetBlock(
                CircuitLayout.Map(0, 0, 0, anchorBlockU + (stepU * 2), y, 0, maxU, maxV, Facing),
                CircuitLayout.CreateAxisWire(Facing, alignedWithForwardAxis: true));
            builder.SetBlock(
                CircuitLayout.Map(0, 0, 0, anchorBlockU + (stepU * 3), y, 0, maxU, maxV, Facing),
                CircuitBlock);
            builder.SetBlock(
                CircuitLayout.Map(0, 0, 0, anchorBlockU + (stepU * 4), y, 0, maxU, maxV, Facing),
                Minecraft.Blocks.RedstoneWallTorch.WithFacing(torchFacing).WithLit(!firstTorchLit));
        }
    }
}
