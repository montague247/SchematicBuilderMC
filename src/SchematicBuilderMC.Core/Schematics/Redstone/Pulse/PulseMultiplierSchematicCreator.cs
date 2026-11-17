using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;
using SchematicBuilderMC.Models;
using SchematicBuilderMC.Registries;
using SchematicBuilderMC.Schematics.Redstone.Internal;

namespace SchematicBuilderMC.Schematics.Redstone.Pulse;

/// <summary>
/// Supported pulse-multiplier designs.
/// </summary>
public enum PulseMultiplierMode
{
    /// <summary>Repeater sequence tapping distinct separated pulses onto a shared output bus.</summary>
    RepeaterTaps,

    /// <summary>Dropper-hopper clock burst firing exact configured number of items/pulses per trigger.</summary>
    DropperBurst
}

/// <summary>
/// Builds a pulse multiplier that emits <see cref="PulseCount"/> separate output pulses from one input trigger.
/// </summary>
public class PulseMultiplierSchematicCreator : SchematicCreator
{
    public PulseMultiplierMode Mode { get; set; } = PulseMultiplierMode.RepeaterTaps;

    /// <summary>
    /// Number of output pulses emitted per trigger (minimum 2).
    /// </summary>
    public int PulseCount { get; set; } = 4;

    /// <summary>
    /// Optional spacing between consecutive output pulses in <see cref="PulseMultiplierMode.RepeaterTaps"/> mode.
    /// </summary>
    public TimeSpan? PulseSpacing { get; set; }

    /// <summary>
    /// Optional explicit pulse spacing in game ticks (overrides <see cref="PulseSpacing"/>).
    /// </summary>
    public int? PulseSpacingTicks { get; set; }

    public Direction Facing { get; set; } = Direction.East;

    public string CircuitBlock { get; set; } = Minecraft.Blocks.SmoothStone;

    public string ItemId { get; set; } = Minecraft.Items.Cobblestone;

    public bool IncludeInputButton { get; set; }

    public bool IncludeOutputLamp { get; set; }

    public override SchematicBuilder Create()
    {
        if (!Enum.IsDefined(Mode))
        {
            throw new ArgumentOutOfRangeException(nameof(Mode), Mode, "Unsupported pulse multiplier mode.");
        }

        CircuitLayout.ValidateHorizontalFacing(Facing);
        CircuitLayout.ValidatePositive(PulseCount, nameof(PulseCount), 2);
        CircuitLayout.ValidateBlockId(CircuitBlock, nameof(CircuitBlock));
        CircuitLayout.ValidateBlockId(ItemId, nameof(ItemId));

        return Mode == PulseMultiplierMode.DropperBurst
            ? BuildDropperBurst()
            : BuildRepeaterTaps();
    }

    private SchematicBuilder BuildRepeaterTaps()
    {
        int spacingTicks = CircuitLayout.ResolveGameTicks(PulseSpacing, PulseSpacingTicks, defaultGameTicks: 4, minGameTicks: 4, stepGameTicks: 2);
        int repeatersPerGap = (int)Math.Ceiling(spacingTicks / 8.0);
        int tapDelay = Math.Clamp(spacingTicks / (2 * repeatersPerGap), 1, 4);

        int inputLead = IncludeInputButton ? 1 : 0;
        // Stage 0 taps directly at u0 + 1; each subsequent pulse adds repeatersPerGap + 1 spacer blocks along U.
        int coreLengthU = 3 + ((PulseCount - 1) * (repeatersPerGap + 1));
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
            "Repeater-Tap Pulse Multiplier",
            $"Repeater-tap pulse multiplier emitting {PulseCount} pulse(s) spaced ~{spacingTicks} gt apart facing {Facing}.");

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

        // Input enters v=0 and feeds output bus v=2 via v=1 diode repeaters at each tap.
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, inputLead, 1, 0, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));

        int curU = inputLead + 1;
        for (int p = 0; p < PulseCount; p++)
        {
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, curU, 1, 0, maxU, maxV, Facing), CircuitBlock);
            builder.SetRepeater(CircuitLayout.Map(0, 0, 0, curU, 1, 1, maxU, maxV, Facing), sideSouth, delay: 1);
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, curU, 1, 2, maxU, maxV, Facing), CircuitLayout.CreateWire(Facing, north: true, east: true, south: false, west: p > 0));
            curU++;

            if (p < PulseCount - 1)
            {
                for (int r = 0; r < repeatersPerGap; r++)
                {
                    builder.SetRepeater(CircuitLayout.Map(0, 0, 0, curU, 1, 0, maxU, maxV, Facing), Facing, delay: tapDelay);
                    builder.SetBlock(CircuitLayout.Map(0, 0, 0, curU, 1, 2, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));
                    curU++;
                }
            }
        }

        builder.SetBlock(CircuitLayout.Map(0, 0, 0, curU, 1, 2, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));
        if (IncludeOutputLamp)
        {
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, totalLengthU - 1, 1, 2, maxU, maxV, Facing), Minecraft.Blocks.RedstoneLamp);
        }

        return builder;
    }

    private SchematicBuilder BuildDropperBurst()
    {
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
            "Dropper Burst Pulse Multiplier",
            $"Dropper-hopper burst generator emitting {PulseCount} pulse(s) per trigger facing {Facing}.");

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
        Direction observerLook = CircuitLayout.RotateDirection(Direction.South, Facing);
        ItemStack[] stacks = CircuitLayout.CreateItemStacks(ItemId, PulseCount, maxSlots: 9);

        builder.SetBlock(CircuitLayout.Map(0, 0, 0, inputLead, 1, 0, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));
        CircuitLayout.SetDropper(builder, CircuitLayout.Map(0, 0, 0, inputLead + 1, 1, 0, maxU, maxV, Facing), Facing, stacks);
        CircuitLayout.SetHopper(builder, CircuitLayout.Map(0, 0, 0, inputLead + 2, 1, 0, maxU, maxV, Facing), reverse, enabled: true);

        // Comparator clock reads hopper contents & pulses dropper back while items remain
        builder.SetComparator(CircuitLayout.Map(0, 0, 0, inputLead + 3, 1, 0, maxU, maxV, Facing), Facing, subtractMode: true);
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, inputLead + 3, 1, 1, maxU, maxV, Facing), CircuitLayout.CreateWire(Facing, north: true, east: false, south: false, west: true));
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, inputLead + 2, 1, 1, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, inputLead + 1, 1, 1, maxU, maxV, Facing), CircuitLayout.CreateWire(Facing, north: true, east: true, south: false, west: false));

        CircuitLayout.SetObserver(builder, CircuitLayout.Map(0, 0, 0, inputLead + 4, 1, 0, maxU, maxV, Facing), observerLook);

        if (IncludeOutputLamp)
        {
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, totalLengthU - 1, 1, 0, maxU, maxV, Facing), Minecraft.Blocks.RedstoneLamp);
        }

        return builder;
    }
}
