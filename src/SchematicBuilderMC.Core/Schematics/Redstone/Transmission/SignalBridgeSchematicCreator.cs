using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;
using SchematicBuilderMC.Models;
using SchematicBuilderMC.Registries;
using SchematicBuilderMC.Schematics.Redstone.Internal;

namespace SchematicBuilderMC.Schematics.Redstone.Transmission;

/// <summary>
/// Supported crossing styles for <see cref="SignalBridgeSchematicCreator"/>.
/// </summary>
public enum SignalBridgeMode
{
    /// <summary>Overpass bridge with a 1-block vertical air gap to prevent vertical crosstalk between crossing redstone wires.</summary>
    RedstoneBridge,

    /// <summary>Compact crossing using perpendicular repeaters so the two lines never touch or cross-power.</summary>
    RepeaterBridge
}

/// <summary>
/// Builds a two-line insulated Redstone signal crossing (signal bridge) where primary and secondary lines cross orthogonally without interfering.
/// </summary>
public class SignalBridgeSchematicCreator : SchematicCreator
{
    /// <summary>
    /// Crossing implementation mode. Defaults to <see cref="SignalBridgeMode.RedstoneBridge"/>.
    /// </summary>
    public SignalBridgeMode Mode { get; set; } = SignalBridgeMode.RedstoneBridge;

    /// <summary>
    /// Span length of both crossing lines in blocks (must be an odd or integer value &gt;= 3; even values are centered).
    /// </summary>
    public int SpanLength { get; set; } = 5;

    /// <summary>
    /// Facing direction of the primary (lower) line. Must be horizontal.
    /// </summary>
    public Direction Facing { get; set; } = Direction.East;

    /// <summary>
    /// Optional delay in redstone repeater modes.
    /// </summary>
    public TimeSpan? Delay { get; set; }

    /// <summary>
    /// Optional explicit delay in game ticks for repeater-bridge mode (overrides <see cref="Delay"/>).
    /// </summary>
    public int? DelayTicks { get; set; }

    /// <summary>
    /// Solid opaque block used for signal supports and bridge deck.
    /// </summary>
    public string CircuitBlock { get; set; } = Minecraft.Blocks.SmoothStone;

    /// <summary>
    /// Adds input levers at both line entrances.
    /// </summary>
    public bool IncludeInputLevers { get; set; }

    /// <summary>
    /// Adds output lamps at both line exits.
    /// </summary>
    public bool IncludeOutputLamps { get; set; }

    public override SchematicBuilder Create()
    {
        if (!Enum.IsDefined(Mode))
        {
            throw new ArgumentOutOfRangeException(nameof(Mode), Mode, "Unsupported signal bridge mode.");
        }

        CircuitLayout.ValidateHorizontalFacing(Facing);
        CircuitLayout.ValidatePositive(SpanLength, nameof(SpanLength), 3);
        CircuitLayout.ValidateBlockId(CircuitBlock, nameof(CircuitBlock));

        return Mode == SignalBridgeMode.RepeaterBridge
            ? BuildRepeaterBridge()
            : BuildRedstoneBridge();
    }

    private SchematicBuilder BuildRedstoneBridge()
    {
        int mid = SpanLength / 2;
        int max = SpanLength - 1;
        // y=0: lower floor, y=1: lower wire (air gap at y=2), y=3: upper solid bridge, y=4: upper wire
        int height = 5;

        var builder = CircuitLayout.CreateBuilder(
            this,
            CircuitLayout.GetWidth(SpanLength, SpanLength, Facing),
            height,
            CircuitLayout.GetLength(SpanLength, SpanLength, Facing),
            "Redstone Signal Bridge",
            $"Insulated air-gap redstone crossing ({SpanLength}x{SpanLength}) facing {Facing}.");

        // Primary lower line along U at v = mid, y = 0..1.
        for (int u = 0; u < SpanLength; u++)
        {
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, u, 0, mid, max, max, Facing), CircuitBlock);
            var wirePos = CircuitLayout.Map(0, 0, 0, u, 1, mid, max, max, Facing);
            if (IncludeInputLevers && u == 0)
            {
                builder.SetLever(wirePos, BlockFace.Floor, Facing, powered: false);
            }
            else if (IncludeOutputLamps && u == max)
            {
                builder.SetBlock(wirePos, Minecraft.Blocks.RedstoneLamp);
            }
            else
            {
                builder.SetBlock(wirePos, CircuitLayout.CreateAxisWire(Facing, alignedWithForwardAxis: true));
            }
        }

        // Secondary overpass line along V at u = mid, climbing via y=1 -> y=2..3 over air gap above (mid, 1, mid) -> descending back to y=1.
        Direction secondaryDir = CircuitLayout.RotateDirection(Direction.South, Facing);
        for (int v = 0; v < SpanLength; v++)
        {
            int deckY = Math.Abs(v - mid) <= 1 ? 3 : 0;
            if (Math.Abs(v - mid) == 1)
            {
                // Side step ramps from y=1 up to y=3 deck
                builder.SetBlock(CircuitLayout.Map(0, 0, 0, mid, 1, v, max, max, Facing), CircuitBlock);
                builder.SetBlock(CircuitLayout.Map(0, 0, 0, mid, 2, v, max, max, Facing), CircuitLayout.CreateAxisWire(Facing, alignedWithForwardAxis: false));
            }
            else
            {
                builder.SetBlock(CircuitLayout.Map(0, 0, 0, mid, deckY, v, max, max, Facing), CircuitBlock);
                var pos = CircuitLayout.Map(0, 0, 0, mid, deckY + 1, v, max, max, Facing);
                if (IncludeInputLevers && v == 0)
                {
                    builder.SetLever(pos, BlockFace.Floor, secondaryDir, powered: false);
                }
                else if (IncludeOutputLamps && v == max)
                {
                    builder.SetBlock(pos, Minecraft.Blocks.RedstoneLamp);
                }
                else
                {
                    builder.SetBlock(pos, CircuitLayout.CreateAxisWire(Facing, alignedWithForwardAxis: false));
                }
            }
        }

        return builder;
    }

    private SchematicBuilder BuildRepeaterBridge()
    {
        int mid = SpanLength / 2;
        int max = SpanLength - 1;
        int delayGameTicks = CircuitLayout.ResolveGameTicks(Delay, DelayTicks, defaultGameTicks: 2, minGameTicks: 2, stepGameTicks: 2);
        int repeaterDelay = Math.Clamp(delayGameTicks / 2, 1, 4);
        int height = 4;

        var builder = CircuitLayout.CreateBuilder(
            this,
            CircuitLayout.GetWidth(SpanLength, SpanLength, Facing),
            height,
            CircuitLayout.GetLength(SpanLength, SpanLength, Facing),
            "Repeater Signal Bridge",
            $"Repeater-isolated perpendicular crossing ({SpanLength}x{SpanLength}) facing {Facing}.");

        // Primary lower line runs straight along U at v = mid using a repeater under the bridge block at u = mid.
        for (int u = 0; u < SpanLength; u++)
        {
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, u, 0, mid, max, max, Facing), CircuitBlock);
            var pos = CircuitLayout.Map(0, 0, 0, u, 1, mid, max, max, Facing);
            if (u == mid)
            {
                builder.SetRepeater(pos, Facing, delay: repeaterDelay);
            }
            else if (IncludeInputLevers && u == 0)
            {
                builder.SetLever(pos, BlockFace.Floor, Facing, powered: false);
            }
            else if (IncludeOutputLamps && u == max)
            {
                builder.SetBlock(pos, Minecraft.Blocks.RedstoneLamp);
            }
            else
            {
                builder.SetBlock(pos, CircuitLayout.CreateAxisWire(Facing, alignedWithForwardAxis: true));
            }
        }

        // Secondary overpass line runs along V at u = mid using a repeater on top of bridge block (mid, 2, mid).
        Direction secondaryDir = CircuitLayout.RotateDirection(Direction.South, Facing);
        for (int v = 0; v < SpanLength; v++)
        {
            if (v == mid)
            {
                builder.SetBlock(CircuitLayout.Map(0, 0, 0, mid, 2, v, max, max, Facing), CircuitBlock);
                builder.SetRepeater(CircuitLayout.Map(0, 0, 0, mid, 3, v, max, max, Facing), secondaryDir, delay: repeaterDelay);
            }
            else if (Math.Abs(v - mid) == 1)
            {
                builder.SetBlock(CircuitLayout.Map(0, 0, 0, mid, 1, v, max, max, Facing), CircuitBlock);
                builder.SetBlock(CircuitLayout.Map(0, 0, 0, mid, 2, v, max, max, Facing), CircuitLayout.CreateAxisWire(Facing, alignedWithForwardAxis: false));
            }
            else
            {
                builder.SetBlock(CircuitLayout.Map(0, 0, 0, mid, 0, v, max, max, Facing), CircuitBlock);
                var pos = CircuitLayout.Map(0, 0, 0, mid, 1, v, max, max, Facing);
                if (IncludeInputLevers && v == 0)
                {
                    builder.SetLever(pos, BlockFace.Floor, secondaryDir, powered: false);
                }
                else if (IncludeOutputLamps && v == max)
                {
                    builder.SetBlock(pos, Minecraft.Blocks.RedstoneLamp);
                }
                else
                {
                    builder.SetBlock(pos, CircuitLayout.CreateAxisWire(Facing, alignedWithForwardAxis: false));
                }
            }
        }

        return builder;
    }
}
