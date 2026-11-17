using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;
using SchematicBuilderMC.Models;
using SchematicBuilderMC.Registries;
using SchematicBuilderMC.Schematics.Redstone.Internal;

namespace SchematicBuilderMC.Schematics.Redstone.Pulse;

/// <summary>
/// Supported Redstone pulse-extender architectures.
/// </summary>
public enum PulseExtenderMode
{
    /// <summary>Multi-tap repeater chain extending any short input pulse to an arbitrary configured duration.</summary>
    Repeater,

    /// <summary>Compact fading comparator loop (15-signal decay, scalable with loop stages).</summary>
    ComparatorDecay,

    /// <summary>One-shot Etho hopper pulse extender supporting durations up to 128 seconds (8 game ticks per item).</summary>
    HopperTimer
}

/// <summary>
/// Builds a configurable Redstone pulse extender that stretches a brief pulse into a sustained signal
/// of target <see cref="Duration"/> or <see cref="DurationTicks"/>.
/// </summary>
public class PulseExtenderSchematicCreator : SchematicCreator
{
    public PulseExtenderMode Mode { get; set; } = PulseExtenderMode.ComparatorDecay;

    /// <summary>
    /// Optional target extended pulse duration.
    /// </summary>
    public TimeSpan? Duration { get; set; }

    /// <summary>
    /// Optional explicit target duration in game ticks (overrides <see cref="Duration"/>).
    /// </summary>
    public int? DurationTicks { get; set; }

    public Direction Facing { get; set; } = Direction.East;

    public string CircuitBlock { get; set; } = Minecraft.Blocks.SmoothStone;

    /// <summary>
    /// Item placed inside the hopper in <see cref="PulseExtenderMode.HopperTimer"/> mode.
    /// </summary>
    public string ItemId { get; set; } = Minecraft.Items.Cobblestone;

    public bool IncludeInputButton { get; set; }

    public bool IncludeOutputLamp { get; set; }

    /// <summary>
    /// Gets the item count needed in <see cref="PulseExtenderMode.HopperTimer"/> mode (1 item = 8 game ticks = 0.4 s).
    /// </summary>
    public int GetHopperItemCount()
    {
        int ticks = CircuitLayout.ResolveGameTicks(Duration, DurationTicks, defaultGameTicks: 32, minGameTicks: 8, stepGameTicks: 8);
        return Math.Clamp((int)Math.Round(ticks / 8.0), 1, 320);
    }

    public int GetActualDurationTicks() => Mode switch
    {
        PulseExtenderMode.Repeater => GetRepeaterTapCount() * 8 + 2,
        PulseExtenderMode.ComparatorDecay => GetComparatorLoopCount() * 30,
        PulseExtenderMode.HopperTimer => GetHopperItemCount() * 8,
        _ => 30
    };

    public override SchematicBuilder Create()
    {
        if (!Enum.IsDefined(Mode))
        {
            throw new ArgumentOutOfRangeException(nameof(Mode), Mode, "Unsupported pulse extender mode.");
        }

        CircuitLayout.ValidateHorizontalFacing(Facing);
        CircuitLayout.ValidateBlockId(CircuitBlock, nameof(CircuitBlock));
        CircuitLayout.ValidateBlockId(ItemId, nameof(ItemId));

        return Mode switch
        {
            PulseExtenderMode.Repeater => BuildRepeaterExtender(),
            PulseExtenderMode.ComparatorDecay => BuildComparatorDecayExtender(),
            PulseExtenderMode.HopperTimer => BuildHopperTimerExtender(),
            _ => throw new ArgumentOutOfRangeException(nameof(Mode), Mode, "Unsupported pulse extender mode.")
        };
    }

    private int GetRepeaterTapCount()
    {
        int ticks = CircuitLayout.ResolveGameTicks(Duration, DurationTicks, defaultGameTicks: 24, minGameTicks: 8, stepGameTicks: 2);
        return Math.Max(1, (int)Math.Ceiling(ticks / 8.0));
    }

    private int GetComparatorLoopCount()
    {
        int ticks = CircuitLayout.ResolveGameTicks(Duration, DurationTicks, defaultGameTicks: 30, minGameTicks: 16, stepGameTicks: 2);
        return Math.Max(1, (int)Math.Ceiling(ticks / 30.0));
    }

    private SchematicBuilder BuildRepeaterExtender()
    {
        int taps = GetRepeaterTapCount();
        int inputLead = IncludeInputButton ? 1 : 0;
        // Each tap uses 2 U columns (repeater -> solid block -> tapped South by diode repeater onto v=2 bus).
        int coreLengthU = (taps * 2) + 2;
        const int widthV = 3;
        int totalLengthU = inputLead + coreLengthU + (IncludeOutputLamp ? 1 : 0);
        int height = IncludeInputButton ? 3 : 2;
        int maxU = totalLengthU - 1;
        int maxV = widthV - 1;

        var builder = CircuitLayout.CreateBuilder(
            this,
            CircuitLayout.GetWidth(totalLengthU, widthV, Facing),
            height,
            CircuitLayout.GetLength(totalLengthU, widthV, Facing),
            "Repeater Pulse Extender",
            $"Multi-tap repeater pulse extender ({taps} stage(s), ~{GetActualDurationTicks()} gt) facing {Facing}.");

        for (int u = 0; u < totalLengthU; u++)
        {
            for (int v = 0; v < widthV; v++)
            {
                builder.SetBlock(CircuitLayout.Map(0, 0, 0, u, 0, v, maxU, maxV, Facing), CircuitBlock);
            }
        }

        if (IncludeInputButton)
        {
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, 0, 1, 0, maxU, maxV, Facing), CircuitBlock);
            CircuitLayout.SetButton(builder, CircuitLayout.Map(0, 0, 0, 0, 2, 0, maxU, maxV, Facing), BlockFace.Floor, Facing);
        }

        Direction sideSouth = CircuitLayout.RotateDirection(Direction.South, Facing);

        builder.SetBlock(CircuitLayout.Map(0, 0, 0, inputLead, 1, 0, maxU, maxV, Facing), CircuitLayout.CreateWire(Facing, north: false, east: true, south: true, west: true));
        CircuitLayout.SetRepeater(builder, CircuitLayout.Map(0, 0, 0, inputLead, 1, 1, maxU, maxV, Facing), sideSouth, delay: 1);
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, inputLead, 1, 2, maxU, maxV, Facing), CircuitLayout.CreateWire(Facing, north: true, east: true, south: false, west: false));

        for (int t = 0; t < taps; t++)
        {
            int repU = inputLead + 1 + (t * 2);
            int blkU = repU + 1;
            CircuitLayout.SetRepeater(builder, CircuitLayout.Map(0, 0, 0, repU, 1, 0, maxU, maxV, Facing), Facing, delay: 4);
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, blkU, 1, 0, maxU, maxV, Facing), CircuitBlock);
            CircuitLayout.SetRepeater(builder, CircuitLayout.Map(0, 0, 0, blkU, 1, 1, maxU, maxV, Facing), sideSouth, delay: 1);
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, repU, 1, 2, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, blkU, 1, 2, maxU, maxV, Facing), CircuitLayout.CreateWire(Facing, north: true, east: true, south: false, west: true));
        }

        int endU = inputLead + coreLengthU - 1;
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, endU, 1, 2, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));
        if (IncludeOutputLamp)
        {
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, totalLengthU - 1, 1, 2, maxU, maxV, Facing), Minecraft.Blocks.RedstoneLamp);
        }

        return builder;
    }

    private SchematicBuilder BuildComparatorDecayExtender()
    {
        int stages = GetComparatorLoopCount();
        int inputLead = IncludeInputButton ? 1 : 0;
        int coreLengthU = 3 + stages;
        const int widthV = 2;
        int totalLengthU = inputLead + coreLengthU + (IncludeOutputLamp ? 1 : 0);
        int height = IncludeInputButton ? 3 : 2;
        int maxU = totalLengthU - 1;
        int maxV = widthV - 1;

        var builder = CircuitLayout.CreateBuilder(
            this,
            CircuitLayout.GetWidth(totalLengthU, widthV, Facing),
            height,
            CircuitLayout.GetLength(totalLengthU, widthV, Facing),
            "Comparator Decay Pulse Extender",
            $"Fading comparator loop pulse extender ({stages} pair(s), ~{GetActualDurationTicks()} gt) facing {Facing}.");

        for (int u = 0; u < totalLengthU; u++)
        {
            for (int v = 0; v < widthV; v++)
            {
                builder.SetBlock(CircuitLayout.Map(0, 0, 0, u, 0, v, maxU, maxV, Facing), CircuitBlock);
            }
        }

        if (IncludeInputButton)
        {
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, 0, 1, 0, maxU, maxV, Facing), CircuitBlock);
            CircuitLayout.SetButton(builder, CircuitLayout.Map(0, 0, 0, 0, 2, 0, maxU, maxV, Facing), BlockFace.Floor, Facing);
        }

        Direction reverse = CircuitLayout.GetOpposite(Facing);

        builder.SetBlock(CircuitLayout.Map(0, 0, 0, inputLead, 1, 0, maxU, maxV, Facing), CircuitLayout.CreateWire(Facing, north: false, east: true, south: true, west: true));
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, inputLead, 1, 1, maxU, maxV, Facing), CircuitLayout.CreateWire(Facing, north: true, east: true, south: false, west: false));

        for (int s = 0; s < stages; s++)
        {
            int u = inputLead + 1 + s;
            CircuitLayout.SetComparator(builder, CircuitLayout.Map(0, 0, 0, u, 1, 0, maxU, maxV, Facing), Facing, subtractMode: false);
            CircuitLayout.SetComparator(builder, CircuitLayout.Map(0, 0, 0, u, 1, 1, maxU, maxV, Facing), reverse, subtractMode: false);
        }

        int tailU = inputLead + 1 + stages;
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, tailU, 1, 0, maxU, maxV, Facing), CircuitLayout.CreateWire(Facing, north: false, east: true, south: true, west: true));
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, tailU, 1, 1, maxU, maxV, Facing), CircuitLayout.CreateWire(Facing, north: true, east: false, south: false, west: true));
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, tailU + 1, 1, 0, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));

        if (IncludeOutputLamp)
        {
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, totalLengthU - 1, 1, 0, maxU, maxV, Facing), Minecraft.Blocks.RedstoneLamp);
        }

        return builder;
    }

    private SchematicBuilder BuildHopperTimerExtender()
    {
        int itemCount = GetHopperItemCount();
        int inputLead = IncludeInputButton ? 1 : 0;
        const int coreLengthU = 5;
        const int widthV = 2;
        int totalLengthU = inputLead + coreLengthU + (IncludeOutputLamp ? 1 : 0);
        int height = IncludeInputButton ? 3 : 2;
        int maxU = totalLengthU - 1;
        int maxV = widthV - 1;

        var builder = CircuitLayout.CreateBuilder(
            this,
            CircuitLayout.GetWidth(totalLengthU, widthV, Facing),
            height,
            CircuitLayout.GetLength(totalLengthU, widthV, Facing),
            "One-Shot Hopper Pulse Extender",
            $"Hopper pulse extender holding {itemCount} item(s) ({GetActualDurationTicks()} gt pulse) facing {Facing}.");

        for (int u = 0; u < totalLengthU; u++)
        {
            for (int v = 0; v < widthV; v++)
            {
                builder.SetBlock(CircuitLayout.Map(0, 0, 0, u, 0, v, maxU, maxV, Facing), CircuitBlock);
            }
        }

        if (IncludeInputButton)
        {
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, 0, 1, 0, maxU, maxV, Facing), CircuitBlock);
            CircuitLayout.SetButton(builder, CircuitLayout.Map(0, 0, 0, 0, 2, 0, maxU, maxV, Facing), BlockFace.Floor, Facing);
        }

        Direction reverse = CircuitLayout.GetOpposite(Facing);
        ItemStack[] stacks = CircuitLayout.CreateItemStacks(ItemId, itemCount, maxSlots: 5);

        // Input trigger drops/unpowers locking block at u0 -> releases items from Hopper A into Hopper B -> comparator stays ON until items drain.
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, inputLead, 1, 0, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, inputLead + 1, 1, 0, maxU, maxV, Facing), CircuitBlock);
        CircuitLayout.SetHopper(builder, CircuitLayout.Map(0, 0, 0, inputLead + 2, 1, 0, maxU, maxV, Facing), Facing, enabled: false, stacks);
        CircuitLayout.SetHopper(builder, CircuitLayout.Map(0, 0, 0, inputLead + 3, 1, 0, maxU, maxV, Facing), reverse, enabled: true);
        CircuitLayout.SetComparator(builder, CircuitLayout.Map(0, 0, 0, inputLead + 4, 1, 0, maxU, maxV, Facing), Facing, subtractMode: false);

        // Reset feedback wire returning items while secondary comparator on Hopper A is idle
        CircuitLayout.SetComparator(builder, CircuitLayout.Map(0, 0, 0, inputLead + 1, 1, 1, maxU, maxV, Facing), reverse, subtractMode: false);
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, inputLead + 2, 1, 1, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, inputLead + 3, 1, 1, maxU, maxV, Facing), CircuitLayout.CreateWire(Facing, north: true, east: false, south: false, west: true));

        if (IncludeOutputLamp)
        {
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, totalLengthU - 1, 1, 0, maxU, maxV, Facing), Minecraft.Blocks.RedstoneLamp);
        }

        return builder;
    }
}
