using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;
using SchematicBuilderMC.Extensions;
using SchematicBuilderMC.Models;
using SchematicBuilderMC.Registries;
using SchematicBuilderMC.Schematics.Redstone.Internal;

namespace SchematicBuilderMC.Schematics.Redstone.Transmission;

/// <summary>
/// Builds a configurable horizontal Redstone transmission line with automatic repeater boosting every 15 blocks
/// and optional timed delay via <see cref="Delay"/> or <see cref="DelayTicks"/>.
/// </summary>
public class TransmissionLineSchematicCreator : SchematicCreator
{
    private const int MaxWireSpanWithoutRepeater = 15;

    /// <summary>
    /// Signal transmission length in blocks (excluding optional input lever / output lamp).
    /// May automatically expand if <see cref="Delay"/> or <see cref="DelayTicks"/> requires more repeaters.
    /// </summary>
    public int Length { get; set; } = 16;

    /// <summary>
    /// Direction the signal travels from input to output. Must be horizontal.
    /// </summary>
    public Direction Facing { get; set; } = Direction.East;

    /// <summary>
    /// Optional target delay across the transmission line (rounded up to redstone ticks of 2 game ticks).
    /// </summary>
    public TimeSpan? Delay { get; set; }

    /// <summary>
    /// Optional explicit delay in game ticks (overrides <see cref="Delay"/>).
    /// </summary>
    public int? DelayTicks { get; set; }

    /// <summary>
    /// When true, uses consecutive repeaters instead of dust where repeaters fit.
    /// </summary>
    public bool PureRepeaterLine { get; set; }

    /// <summary>
    /// Starting redstone dust counter offset (<c>0..15</c>) before the first repeater is placed.
    /// Defaults to <c>0</c>.
    /// </summary>
    public int DustOffset { get; set; }

    /// <summary>
    /// Solid support block placed beneath the wire and repeaters.
    /// </summary>
    public string CircuitBlock { get; set; } = Minecraft.Blocks.SmoothStone;

    /// <summary>
    /// Adds a lever at the start of the line to toggle the transmission signal.
    /// </summary>
    public bool IncludeInputLever { get; set; }

    /// <summary>
    /// Adds a redstone lamp at the output end of the line.
    /// </summary>
    public bool IncludeOutputLamp { get; set; }

    /// <summary>
    /// Returns the resolved repeater delay schedule <c>(uIndex, redstoneTicks)</c> along the signal segment.
    /// </summary>
    public IReadOnlyList<(int Index, int RepeaterTicks)> GetRepeaterSchedule()
    {
        Validate();
        int[] repeaterDelays = ComputeRepeaterDelays();
        var result = new List<(int, int)>();
        for (int i = 0; i < repeaterDelays.Length; i++)
        {
            if (repeaterDelays[i] > 0)
            {
                result.Add((i, repeaterDelays[i]));
            }
        }

        return result;
    }

    /// <summary>
    /// Returns the total signal delay in game ticks produced by all repeaters along the line.
    /// </summary>
    public int GetTotalDelayTicks()
    {
        Validate();
        int total = 0;
        foreach (int redstoneTicks in ComputeRepeaterDelays())
        {
            total += redstoneTicks * 2;
        }

        return total;
    }

    public override SchematicBuilder Create()
    {
        Validate();
        int[] repeaterDelays = ComputeRepeaterDelays();
        int signalLength = repeaterDelays.Length;
        int inputOffset = IncludeInputLever ? 1 : 0;
        int outputExtra = IncludeOutputLamp ? 1 : 0;
        int canonicalLength = inputOffset + signalLength + outputExtra;
        int width = CircuitLayout.GetWidth(canonicalLength, 1, Facing);
        int length = CircuitLayout.GetLength(canonicalLength, 1, Facing);
        int maxU = canonicalLength - 1;

        var builder = CircuitLayout.CreateBuilder(
            this,
            width,
            2,
            length,
            "Redstone Transmission Line",
            $"Horizontal transmission line ({signalLength} blocks, {GetTotalDelayTicks()} gt delay) facing {Facing}.");

        Direction worldSignalDir = Facing;

        for (int u = 0; u < canonicalLength; u++)
        {
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, u, 0, 0, maxU, 0, Facing), CircuitBlock);
        }

        if (IncludeInputLever)
        {
            builder.SetLever(CircuitLayout.Map(0, 0, 0, 0, 1, 0, maxU, 0, Facing), BlockFace.Floor, worldSignalDir, powered: false);
        }

        for (int i = 0; i < signalLength; i++)
        {
            int u = inputOffset + i;
            var pos = CircuitLayout.Map(0, 0, 0, u, 1, 0, maxU, 0, Facing);
            if (repeaterDelays[i] > 0)
            {
                CircuitLayout.SetRepeater(builder, pos, worldSignalDir, delay: repeaterDelays[i]);
            }
            else
            {
                builder.SetBlock(pos, CircuitLayout.CreateAxisWire(Facing, alignedWithForwardAxis: true));
            }
        }

        if (IncludeOutputLamp)
        {
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, inputOffset + signalLength, 1, 0, maxU, 0, Facing), Minecraft.Blocks.RedstoneLamp);
        }

        return builder;
    }

    private void Validate()
    {
        CircuitLayout.ValidateHorizontalFacing(Facing);
        CircuitLayout.ValidatePositive(Length, nameof(Length), 1);
        CircuitLayout.ValidateBlockId(CircuitBlock, nameof(CircuitBlock));
        if (DustOffset is < 0 or > 15)
        {
            throw new ArgumentOutOfRangeException(
                nameof(DustOffset),
                DustOffset,
                "DustOffset must be between 0 and 15.");
        }

        if (Delay.HasValue || DelayTicks.HasValue)
        {
            CircuitLayout.ResolveGameTicks(Delay, DelayTicks, defaultGameTicks: 2, minGameTicks: 2, stepGameTicks: 2);
        }
    }

    private int[] ComputeRepeaterDelays()
    {
        bool hasDelay = Delay.HasValue || DelayTicks.HasValue;
        int requestedGameTicks = hasDelay
            ? CircuitLayout.ResolveGameTicks(Delay, DelayTicks, defaultGameTicks: 2, minGameTicks: 2, stepGameTicks: 2)
            : 0;
        int requestedRedstoneTicks = requestedGameTicks / 2;

        int minRepeatersForDelay = requestedRedstoneTicks > 0 ? (int)Math.Ceiling(requestedRedstoneTicks / 4.0) : 0;
        int effectiveLength = Math.Max(Length, minRepeatersForDelay);
        if (PureRepeaterLine && requestedRedstoneTicks > effectiveLength)
        {
            effectiveLength = minRepeatersForDelay;
        }

        int[] delays = new int[effectiveLength];

        if (PureRepeaterLine)
        {
            int remaining = Math.Max(requestedRedstoneTicks, effectiveLength);
            for (int i = 0; i < effectiveLength; i++)
            {
                int left = effectiveLength - 1 - i;
                int rt = Math.Clamp(remaining - left, 1, 4);
                delays[i] = rt;
                remaining -= rt;
            }

            return delays;
        }

        // First place mandatory boost repeaters so no wire run exceeds 15 blocks.
        int boostCount = 0;
        int wireRun = DustOffset;
        for (int i = 0; i < effectiveLength; i++)
        {
            if (wireRun >= MaxWireSpanWithoutRepeater)
            {
                delays[i] = 1;
                boostCount++;
                wireRun = 0;
            }
            else
            {
                wireRun++;
            }
        }

        if (!hasDelay)
        {
            return delays;
        }

        // Add additional repeaters if requested delay exceeds capacity of existing boost repeaters.
        int currentCapacity = boostCount * 4;
        for (int i = 0; i < effectiveLength && currentCapacity < requestedRedstoneTicks; i++)
        {
            if (delays[i] == 0)
            {
                delays[i] = 1;
                currentCapacity += 4;
            }
        }

        // Distribute exact redstone ticks across all placed repeaters.
        var repeaterIndices = new List<int>();
        for (int i = 0; i < effectiveLength; i++)
        {
            if (delays[i] > 0)
            {
                repeaterIndices.Add(i);
            }
        }

        int remainingRt = Math.Max(requestedRedstoneTicks, repeaterIndices.Count);
        for (int idx = 0; idx < repeaterIndices.Count; idx++)
        {
            int left = repeaterIndices.Count - 1 - idx;
            int rt = Math.Clamp(remainingRt - left, 1, 4);
            delays[repeaterIndices[idx]] = rt;
            remainingRt -= rt;
        }

        return delays;
    }
}
