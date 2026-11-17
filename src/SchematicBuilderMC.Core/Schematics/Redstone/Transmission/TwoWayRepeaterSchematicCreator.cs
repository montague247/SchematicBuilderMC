using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;
using SchematicBuilderMC.Models;
using SchematicBuilderMC.Registries;
using SchematicBuilderMC.Schematics.Redstone.Internal;

namespace SchematicBuilderMC.Schematics.Redstone.Transmission;

/// <summary>
/// Supported bidirectional repeater architectures on Minecraft Wiki.
/// </summary>
public enum TwoWayRepeaterMode
{
    /// <summary>Silent 5x2x2 comparator-based two-way repeater preserving signal strength with zero lockup.</summary>
    Comparison,

    /// <summary>5x2x3 locking-repeater bidirectional booster restoring full signal strength (15) in both directions.</summary>
    LockedRepeater
}

/// <summary>
/// Builds a bidirectional (two-way) Redstone repeater allowing signals to travel in both directions without self-locking.
/// </summary>
public class TwoWayRepeaterSchematicCreator : SchematicCreator
{
    /// <summary>
    /// Two-way repeater topology. Defaults to <see cref="TwoWayRepeaterMode.Comparison"/>.
    /// </summary>
    public TwoWayRepeaterMode Mode { get; set; } = TwoWayRepeaterMode.Comparison;

    /// <summary>
    /// Alignment axis of the bidirectional terminal ports (signals travel along <see cref="Facing"/> and its opposite).
    /// </summary>
    public Direction Facing { get; set; } = Direction.East;

    /// <summary>
    /// Optional delay in <see cref="TwoWayRepeaterMode.LockedRepeater"/> mode.
    /// </summary>
    public TimeSpan? Delay { get; set; }

    /// <summary>
    /// Optional explicit delay in game ticks in <see cref="TwoWayRepeaterMode.LockedRepeater"/> mode.
    /// </summary>
    public int? DelayTicks { get; set; }

    /// <summary>
    /// Solid support block placed beneath components and used as power-conducting intermediate blocks.
    /// </summary>
    public string CircuitBlock { get; set; } = Minecraft.Blocks.SmoothStone;

    /// <summary>
    /// Adds test levers at both bidirectional ports.
    /// </summary>
    public bool IncludeInputLevers { get; set; }

    public override SchematicBuilder Create()
    {
        if (!Enum.IsDefined(Mode))
        {
            throw new ArgumentOutOfRangeException(nameof(Mode), Mode, "Unsupported two-way repeater mode.");
        }

        CircuitLayout.ValidateHorizontalFacing(Facing);
        CircuitLayout.ValidateBlockId(CircuitBlock, nameof(CircuitBlock));

        return Mode == TwoWayRepeaterMode.LockedRepeater
            ? BuildLockedRepeater()
            : BuildComparison();
    }

    private SchematicBuilder BuildComparison()
    {
        const int lengthU = 5;
        const int widthV = 2;
        const int height = 2;
        int maxU = lengthU - 1;
        int maxV = widthV - 1;

        var builder = CircuitLayout.CreateBuilder(
            this,
            CircuitLayout.GetWidth(lengthU, widthV, Facing),
            height,
            CircuitLayout.GetLength(lengthU, widthV, Facing),
            "Comparison Two-Way Repeater",
            $"5x2x2 silent comparison two-way repeater aligned along {Facing}.");

        for (int u = 0; u < lengthU; u++)
        {
            for (int v = 0; v < widthV; v++)
            {
                builder.SetBlock(CircuitLayout.Map(0, 0, 0, u, 0, v, maxU, maxV, Facing), CircuitBlock);
            }
        }

        Direction forward = Facing;
        Direction reverse = CircuitLayout.GetOpposite(Facing);

        // v=0 lane (West -> East)
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, 0, 1, 0, maxU, maxV, Facing), CircuitLayout.CreateWire(Facing, north: false, east: true, south: true, west: true));
        builder.SetComparator(CircuitLayout.Map(0, 0, 0, 1, 1, 0, maxU, maxV, Facing), forward, subtractMode: false);
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, 2, 1, 0, maxU, maxV, Facing), CircuitBlock);
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, 3, 1, 0, maxU, maxV, Facing), CircuitLayout.CreateWire(Facing, north: false, east: true, south: true, west: true));
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, 4, 1, 0, maxU, maxV, Facing), CircuitLayout.CreateWire(Facing, north: false, east: true, south: true, west: true));

        // v=1 lane (East -> West)
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, 0, 1, 1, maxU, maxV, Facing), CircuitLayout.CreateWire(Facing, north: true, east: true, south: false, west: false));
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, 1, 1, 1, maxU, maxV, Facing), CircuitLayout.CreateWire(Facing, north: true, east: true, south: false, west: true));
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, 2, 1, 1, maxU, maxV, Facing), CircuitBlock);
        builder.SetComparator(CircuitLayout.Map(0, 0, 0, 3, 1, 1, maxU, maxV, Facing), reverse, subtractMode: false);
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, 4, 1, 1, maxU, maxV, Facing), CircuitLayout.CreateWire(Facing, north: true, east: false, south: false, west: true));

        if (IncludeInputLevers)
        {
            builder.SetLever(CircuitLayout.Map(0, 0, 0, 0, 1, 1, maxU, maxV, Facing), BlockFace.Floor, forward, powered: false);
            builder.SetLever(CircuitLayout.Map(0, 0, 0, 4, 1, 0, maxU, maxV, Facing), BlockFace.Floor, reverse, powered: false);
        }

        return builder;
    }

    private SchematicBuilder BuildLockedRepeater()
    {
        const int lengthU = 5;
        const int widthV = 3;
        const int height = 2;
        int maxU = lengthU - 1;
        int maxV = widthV - 1;

        int gameTicks = CircuitLayout.ResolveGameTicks(Delay, DelayTicks, defaultGameTicks: 2, minGameTicks: 2, stepGameTicks: 2);
        int signalDelay = Math.Clamp(gameTicks / 2, 1, 4);

        var builder = CircuitLayout.CreateBuilder(
            this,
            CircuitLayout.GetWidth(lengthU, widthV, Facing),
            height,
            CircuitLayout.GetLength(lengthU, widthV, Facing),
            "Locked-Repeater Two-Way Repeater",
            $"5x2x3 locking-repeater bidirectional booster ({gameTicks} gt delay) aligned along {Facing}.");

        for (int u = 0; u < lengthU; u++)
        {
            for (int v = 0; v < widthV; v++)
            {
                builder.SetBlock(CircuitLayout.Map(0, 0, 0, u, 0, v, maxU, maxV, Facing), CircuitBlock);
            }
        }

        Direction forward = Facing;
        Direction reverse = CircuitLayout.GetOpposite(Facing);
        Direction sideNorth = CircuitLayout.RotateDirection(Direction.North, Facing);
        Direction sideSouth = CircuitLayout.RotateDirection(Direction.South, Facing);

        // End terminal buses at u=0 and u=4
        for (int v = 0; v < widthV; v++)
        {
            bool connectNorth = v > 0;
            bool connectSouth = v < widthV - 1;
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, 0, 1, v, maxU, maxV, Facing), CircuitLayout.CreateWire(Facing, north: connectNorth, east: true, south: connectSouth, west: v == 1));
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, 4, 1, v, maxU, maxV, Facing), CircuitLayout.CreateWire(Facing, north: connectNorth, east: v == 1, south: connectSouth, west: true));
        }

        // Forward path (v=0): wire -> repeater -> wire
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, 1, 1, 0, maxU, maxV, Facing), CircuitLayout.CreateWire(Facing, north: false, east: true, south: true, west: true));
        builder.SetRepeater(CircuitLayout.Map(0, 0, 0, 2, 1, 0, maxU, maxV, Facing), forward, delay: signalDelay);
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, 3, 1, 0, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));

        // Locking middle row (v=1)
        builder.SetRepeater(CircuitLayout.Map(0, 0, 0, 1, 1, 1, maxU, maxV, Facing), sideSouth, delay: 1);
        builder.SetRepeater(CircuitLayout.Map(0, 0, 0, 2, 1, 1, maxU, maxV, Facing), sideNorth, delay: 1);

        // Reverse path (v=2): wire <- repeater <- wire
        builder.SetRepeater(CircuitLayout.Map(0, 0, 0, 1, 1, 2, maxU, maxV, Facing), reverse, delay: signalDelay);
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, 2, 1, 2, maxU, maxV, Facing), CircuitLayout.CreateWire(Facing, north: true, east: true, south: false, west: true));
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, 3, 1, 2, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));

        if (IncludeInputLevers)
        {
            builder.SetLever(CircuitLayout.Map(0, 0, 0, 0, 1, 1, maxU, maxV, Facing), BlockFace.Floor, forward, powered: false);
            builder.SetLever(CircuitLayout.Map(0, 0, 0, 4, 1, 1, maxU, maxV, Facing), BlockFace.Floor, reverse, powered: false);
        }

        return builder;
    }
}
