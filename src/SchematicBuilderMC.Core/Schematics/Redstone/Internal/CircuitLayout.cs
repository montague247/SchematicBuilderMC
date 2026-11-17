using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;
using SchematicBuilderMC.Extensions;
using SchematicBuilderMC.Models;
using SchematicBuilderMC.Registries;

namespace SchematicBuilderMC.Schematics.Redstone.Internal;

/// <summary>
/// Shared internal geometry, directional rotation, timing conversion, and wiring helpers for Redstone circuit creators.
/// Canonical circuit coordinates assume forward signal flow toward <see cref="Direction.East"/> (<c>+u</c>),
/// lateral offset toward <see cref="Direction.South"/> (<c>+v</c>), and vertical offset upward (<c>+y</c>).
/// </summary>
internal static class CircuitLayout
{
    public static void ValidateHorizontalFacing(Direction facing, string paramName = "Facing")
    {
        if (!Enum.IsDefined(facing) ||
            facing is not (Direction.North or Direction.South or Direction.East or Direction.West))
        {
            throw new ArgumentOutOfRangeException(paramName, facing, "Facing must be North, South, East, or West.");
        }
    }

    public static void ValidatePositive(int value, string paramName, int minimum = 1)
    {
        if (value < minimum)
        {
            throw new ArgumentOutOfRangeException(paramName, value, $"{paramName} must be at least {minimum}.");
        }
    }

    public static void ValidateBlockId(string blockId, string paramName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(blockId, paramName);
    }

    /// <summary>
    /// Converts optional <see cref="TimeSpan"/> or game-tick configuration into even game ticks (&gt;= <paramref name="minGameTicks"/>).
    /// 1 Minecraft game tick = 50 ms (20 game ticks/second); 1 redstone tick = 2 game ticks.
    /// </summary>
    public static int ResolveGameTicks(TimeSpan? duration, int? durationTicks, int defaultGameTicks, int minGameTicks = 2, int stepGameTicks = 2)
    {
        if (duration.HasValue && duration.Value < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration), duration, "Duration cannot be negative.");
        }

        if (durationTicks.HasValue && durationTicks.Value < minGameTicks)
        {
            throw new ArgumentOutOfRangeException(nameof(durationTicks), durationTicks, $"DurationTicks must be at least {minGameTicks} game ticks.");
        }

        int rawTicks = durationTicks
            ?? (duration.HasValue ? (int)Math.Round(duration.Value.TotalSeconds * 20.0) : defaultGameTicks);

        if (rawTicks < minGameTicks)
        {
            rawTicks = minGameTicks;
        }

        if (stepGameTicks > 1)
        {
            int remainder = rawTicks % stepGameTicks;
            if (remainder != 0)
            {
                rawTicks += stepGameTicks - remainder;
            }
        }

        return rawTicks;
    }

    public static int GetWidth(int canonicalLengthU, int canonicalWidthV, Direction facing) =>
        facing is Direction.East or Direction.West ? canonicalLengthU : canonicalWidthV;

    public static int GetLength(int canonicalLengthU, int canonicalWidthV, Direction facing) =>
        facing is Direction.East or Direction.West ? canonicalWidthV : canonicalLengthU;

    /// <summary>
    /// Maps canonical <c>(u, y, v)</c> coordinates (where <see cref="Direction.East"/> is forward <c>+u</c>)
    /// into world/schematic space using a rigid clockwise horizontal rotation around <c>+Y</c>.
    /// </summary>
    public static BlockPosition Map(
        int startX,
        int startY,
        int startZ,
        int u,
        int y,
        int v,
        int maxU,
        int maxV,
        Direction facing) => facing switch
        {
            Direction.East => new BlockPosition(startX + u, startY + y, startZ + v),
            Direction.South => new BlockPosition(startX + (maxV - v), startY + y, startZ + u),
            Direction.West => new BlockPosition(startX + (maxU - u), startY + y, startZ + (maxV - v)),
            Direction.North => new BlockPosition(startX + v, startY + y, startZ + (maxU - u)),
            _ => throw new ArgumentOutOfRangeException(nameof(facing), facing, "Facing must be horizontal.")
        };

    /// <summary>
    /// Rotates a canonical horizontal direction clockwise from <see cref="Direction.East"/> into <paramref name="targetFacing"/>.
    /// </summary>
    public static Direction RotateDirection(Direction canonicalDirection, Direction targetFacing)
    {
        if (canonicalDirection is Direction.Up or Direction.Down)
        {
            return canonicalDirection;
        }

        int steps = targetFacing switch
        {
            Direction.East => 0,
            Direction.South => 1,
            Direction.West => 2,
            Direction.North => 3,
            _ => throw new ArgumentOutOfRangeException(nameof(targetFacing), targetFacing, "Facing must be horizontal.")
        };

        Direction result = canonicalDirection;
        for (int i = 0; i < steps; i++)
        {
            result = result switch
            {
                Direction.North => Direction.East,
                Direction.East => Direction.South,
                Direction.South => Direction.West,
                Direction.West => Direction.North,
                _ => throw new ArgumentOutOfRangeException(nameof(canonicalDirection), canonicalDirection, "Direction must be cardinal.")
            };
        }

        return result;
    }

    public static Direction GetOpposite(Direction direction) => direction switch
    {
        Direction.North => Direction.South,
        Direction.South => Direction.North,
        Direction.East => Direction.West,
        Direction.West => Direction.East,
        Direction.Up => Direction.Down,
        Direction.Down => Direction.Up,
        _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, "Unsupported direction.")
    };

    /// <summary>
    /// Creates a rotated <c>minecraft:redstone_wire</c> block state from canonical connection flags.
    /// </summary>
    public static BlockState CreateWire(
        Direction targetFacing,
        bool north = true,
        bool east = true,
        bool south = true,
        bool west = true,
        int power = 0)
    {
        var dirs = new HashSet<Direction>();
        if (north) dirs.Add(RotateDirection(Direction.North, targetFacing));
        if (east) dirs.Add(RotateDirection(Direction.East, targetFacing));
        if (south) dirs.Add(RotateDirection(Direction.South, targetFacing));
        if (west) dirs.Add(RotateDirection(Direction.West, targetFacing));

        return Minecraft.Blocks.RedstoneWire
            .WithProperty("north", dirs.Contains(Direction.North) ? "side" : "none")
            .WithProperty("east", dirs.Contains(Direction.East) ? "side" : "none")
            .WithProperty("south", dirs.Contains(Direction.South) ? "side" : "none")
            .WithProperty("west", dirs.Contains(Direction.West) ? "side" : "none")
            .WithPower(power);
    }

    public static BlockState CreateAxisWire(Direction targetFacing, bool alignedWithForwardAxis, int power = 0) =>
        alignedWithForwardAxis
            ? CreateWire(targetFacing, north: false, east: true, south: false, west: true, power: power)
            : CreateWire(targetFacing, north: true, east: false, south: true, west: false, power: power);

    public static void SetObserver(SchematicBuilder builder, BlockPosition position, Direction facing, bool powered = false)
    {
        builder.SetBlock(
            position,
            Minecraft.Blocks.Observer
                .WithFacing(facing)
                .WithPowered(powered));
    }

    /// <summary>
    /// Places a redstone repeater transmitting signal toward <paramref name="signalDirection"/>.
    /// In Minecraft Java Edition, repeater <c>facing</c> points toward its input (opposite of signal flow).
    /// </summary>
    public static void SetRepeater(
        SchematicBuilder builder,
        BlockPosition position,
        Direction signalDirection,
        int delay = 1,
        bool locked = false,
        bool powered = false) =>
        builder.SetRepeater(position, GetOpposite(signalDirection), delay, locked, powered);

    /// <summary>
    /// Places a redstone comparator transmitting signal toward <paramref name="signalDirection"/>.
    /// In Minecraft Java Edition, comparator <c>facing</c> points toward its rear input (opposite of signal flow).
    /// </summary>
    public static void SetComparator(
        SchematicBuilder builder,
        BlockPosition position,
        Direction signalDirection,
        bool subtractMode = false,
        bool powered = false) =>
        builder.SetComparator(position, GetOpposite(signalDirection), subtractMode, powered);

    public static void SetCopperBulb(SchematicBuilder builder, BlockPosition position, bool lit = false, bool powered = false)
    {
        builder.SetBlock(
            position,
            Minecraft.Blocks.WaxedCopperBulb
                .WithLit(lit)
                .WithPowered(powered));
    }

    public static void SetDropper(SchematicBuilder builder, BlockPosition position, Direction facing, params ItemStack[] items)
    {
        builder.SetBlock(
            position,
            Minecraft.Blocks.Dropper
                .WithFacing(facing)
                .WithProperty("triggered", "false"));
        builder.SetContainer(Minecraft.Blocks.Dropper, Minecraft.BlockEntities.Dropper, position, items);
    }

    public static void SetHopper(SchematicBuilder builder, BlockPosition position, Direction facing, bool enabled = true, params ItemStack[] items)
    {
        builder.SetBlock(
            position,
            Minecraft.Blocks.Hopper
                .WithFacing(facing)
                .WithProperty("enabled", enabled ? "true" : "false"));
        builder.SetContainer(Minecraft.Blocks.Hopper, Minecraft.BlockEntities.Hopper, position, items);
    }

    public static void SetButton(SchematicBuilder builder, BlockPosition position, BlockFace face, Direction facing, string buttonBlock = Minecraft.Blocks.StoneButton)
    {
        builder.SetBlock(
            position,
            buttonBlock
                .WithFace(face)
                .WithFacing(facing)
                .WithPowered(false));
    }

    /// <summary>
    /// Splits <paramref name="totalCount"/> items across sequential container slots up to 64 items per slot.
    /// </summary>
    public static ItemStack[] CreateItemStacks(string itemId, int totalCount, int maxSlots = 5, int maxStackSize = 64)
    {
        ValidateBlockId(itemId, nameof(itemId));
        ValidatePositive(totalCount, nameof(totalCount), 1);
        int maxTotal = maxSlots * maxStackSize;
        if (totalCount > maxTotal)
        {
            throw new ArgumentOutOfRangeException(nameof(totalCount), totalCount, $"Total item count cannot exceed {maxTotal} across {maxSlots} slot(s).");
        }

        var stacks = new List<ItemStack>();
        int remaining = totalCount;
        byte slot = 0;
        while (remaining > 0)
        {
            int count = Math.Min(remaining, maxStackSize);
            stacks.Add(new ItemStack(itemId, count, slot++));
            remaining -= count;
        }

        return stacks.ToArray();
    }

    public static SchematicBuilder CreateBuilder(
        SchematicCreator creator,
        int width,
        int height,
        int length,
        string name,
        string description) =>
        SchematicBuilder.Create(width, height, length)
            .WithRotation(creator.Rotation)
            .WithOffset(0, 0, 0)
            .WithName(name)
            .WithAuthor("SchematicBuilderMC")
            .WithDescription(description);
}
