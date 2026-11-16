using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;
using SchematicBuilderMC.Extensions;
using SchematicBuilderMC.Models;
using SchematicBuilderMC.Registries;

namespace SchematicBuilderMC.Schematics.Contraptions;

/// <summary>
/// Generates a two-way bubble-column elevator schematic with an upward soul-sand shaft and a downward magma-block shaft.
/// </summary>
/// <remarks>
/// <para>
/// Water placement assumptions: every block in both vertical shafts (<c>y = 1 .. ShaftHeight</c>) is filled with a full
/// water source block (<c>minecraft:water[level=0]</c>) so Minecraft immediately generates upward and downward bubble columns
/// above the <c>minecraft:soul_sand</c> and <c>minecraft:magma_block</c> bases without requiring kelp conversion.
/// </para>
/// <para>
/// Player setup: doors are placed closed at the bottom entrance (<c>y = 1..2</c>) and top exit (<c>y = ShaftHeight - 1 .. ShaftHeight</c>)
/// to retain water inside the shafts while allowing players to walk in and out. When using <see cref="ShaftHeight"/> = 3,
/// the bottom and top door openings overlap at <c>y = 2</c>; heights of 4 or more keep the lower entrance and upper exit
/// completely separated.
/// </para>
/// </remarks>
public sealed class BubbleColumnElevatorSchematicCreator : SchematicCreator
{
    /// <summary>
    /// Height of the water-filled shaft columns in blocks (minimum 3).
    /// </summary>
    public int ShaftHeight { get; set; } = 10;

    /// <summary>
    /// Horizontal direction the entrance/exit openings face (<see cref="Direction.North"/>, <see cref="Direction.South"/>, <see cref="Direction.East"/>, or <see cref="Direction.West"/>).
    /// </summary>
    public Direction Facing { get; set; } = Direction.North;

    /// <summary>
    /// Block identifier used for the shaft enclosure walls.
    /// </summary>
    public string WallBlock { get; set; } = Minecraft.Blocks.Glass;

    /// <summary>
    /// Block identifier used for the base floor and top landing platform.
    /// </summary>
    public string BaseBlock { get; set; } = Minecraft.Blocks.StoneBricks;

    /// <summary>
    /// Block identifier used for the entrance and exit water-retaining doors.
    /// </summary>
    public string DoorBlock { get; set; } = Minecraft.Blocks.OakDoor;

    /// <summary>
    /// Total schematic width along the X axis.
    /// </summary>
    public int Width => Facing is Direction.North or Direction.South ? 5 : 4;

    /// <summary>
    /// Total schematic height along the Y axis (<see cref="ShaftHeight"/> + 1).
    /// </summary>
    public int Height => ShaftHeight + 1;

    /// <summary>
    /// Total schematic length along the Z axis.
    /// </summary>
    public int Length => Facing is Direction.North or Direction.South ? 4 : 5;

    /// <summary>
    /// Places the bubble-column elevator into an existing <see cref="SchematicBuilder"/> starting at (startX, startY, startZ).
    /// </summary>
    public SchematicBuilder Place(SchematicBuilder builder, int startX = 0, int startY = 0, int startZ = 0)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ValidateConfiguration();
        ArgumentException.ThrowIfNullOrWhiteSpace(WallBlock);
        ArgumentException.ThrowIfNullOrWhiteSpace(BaseBlock);
        ArgumentException.ThrowIfNullOrWhiteSpace(DoorBlock);

        var schematic = builder.Build();
        if (startX < 0 || startY < 0 || startZ < 0 ||
            startX + Width > schematic.Width ||
            startY + Height > schematic.Height ||
            startZ + Length > schematic.Length)
        {
            throw new ArgumentOutOfRangeException(
                nameof(startX),
                $"A bubble-column elevator facing {Facing} ({Width}x{Height}x{Length}) placed at ({startX}, {startY}, {startZ}) exceeds schematic bounds ({schematic.Width}x{schematic.Height}x{schematic.Length}).");
        }

        // Base layer (y = 0)
        for (int v = 0; v <= 3; v++)
        {
            for (int u = 0; u <= 4; u++)
            {
                builder.SetBlock(MapPosition(startX, startY, startZ, u, 0, v), BaseBlock);
            }
        }

        // Upward (soul sand) and downward (magma block) column bases
        builder.SetBlock(MapPosition(startX, startY, startZ, 1, 0, 2), Minecraft.Blocks.SoulSand);
        builder.SetBlock(MapPosition(startX, startY, startZ, 3, 0, 2), Minecraft.Blocks.MagmaBlock);

        var waterSource = Minecraft.Blocks.Water.WithLevel(0);

        // Shaft layers (y = 1 .. ShaftHeight)
        for (int y = 1; y <= ShaftHeight; y++)
        {
            // Water source columns
            builder.SetBlock(MapPosition(startX, startY, startZ, 1, y, 2), waterSource);
            builder.SetBlock(MapPosition(startX, startY, startZ, 3, y, 2), waterSource);

            // Back wall (v = 3)
            for (int u = 0; u <= 4; u++)
            {
                builder.SetBlock(MapPosition(startX, startY, startZ, u, y, 3), WallBlock);
            }

            // Side and divider pillars along v = 2 and v = 1
            foreach (int u in new[] { 0, 2, 4 })
            {
                builder.SetBlock(MapPosition(startX, startY, startZ, u, y, 2), WallBlock);
                builder.SetBlock(MapPosition(startX, startY, startZ, u, y, 1), WallBlock);
            }

            // Front wall segments along column openings where no door is placed
            bool isBottomDoor = y is 1 or 2;
            bool isTopDoor = y == ShaftHeight - 1 || y == ShaftHeight;
            if (!isBottomDoor && !isTopDoor)
            {
                builder.SetBlock(MapPosition(startX, startY, startZ, 1, y, 1), WallBlock);
                builder.SetBlock(MapPosition(startX, startY, startZ, 3, y, 1), WallBlock);
            }
        }

        // Entrance and exit doors at v = 1
        var lowerDoor = DoorBlock.WithFacing(Facing).LowerHalf().Closed();
        var upperDoor = DoorBlock.WithFacing(Facing).UpperHalf().Closed();

        foreach (int u in new[] { 1, 3 })
        {
            // Bottom entrance (y = 1..2)
            builder.SetBlock(MapPosition(startX, startY, startZ, u, 1, 1), lowerDoor);
            builder.SetBlock(MapPosition(startX, startY, startZ, u, 2, 1), upperDoor);

            // Top exit (y = ShaftHeight - 1 .. ShaftHeight)
            builder.SetBlock(MapPosition(startX, startY, startZ, u, ShaftHeight - 1, 1), lowerDoor);
            builder.SetBlock(MapPosition(startX, startY, startZ, u, ShaftHeight, 1), upperDoor);
        }

        // Top landing platform (y = ShaftHeight - 2, v = 0)
        int platformY = ShaftHeight - 2;
        for (int u = 0; u <= 4; u++)
        {
            builder.SetBlock(MapPosition(startX, startY, startZ, u, platformY, 0), BaseBlock);
        }

        return builder;
    }

    /// <summary>
    /// Creates a new schematic containing this two-way bubble-column elevator.
    /// </summary>
    public override SchematicBuilder Create()
    {
        ValidateConfiguration();

        var builder = SchematicBuilder.Create(Width, Height, Length)
            .WithOffset(0, 0, 0)
            .WithName("Bubble-Column Elevator")
            .WithAuthor("SchematicBuilderMC")
            .WithDescription($"A two-way bubble-column elevator (height {ShaftHeight}) facing {Facing}.");

        return Place(builder, 0, 0, 0);
    }

    private void ValidateConfiguration()
    {
        if (ShaftHeight < 3)
        {
            throw new ArgumentOutOfRangeException(nameof(ShaftHeight), ShaftHeight, "ShaftHeight must be at least 3 blocks.");
        }

        if (Facing is not (Direction.North or Direction.South or Direction.East or Direction.West))
        {
            throw new ArgumentOutOfRangeException(nameof(Facing), Facing, "Facing must be a horizontal direction (North, South, East, or West).");
        }
    }

    private BlockPosition MapPosition(int startX, int startY, int startZ, int u, int y, int v)
    {
        const int maxU = 4;
        const int maxV = 3;

        return Facing switch
        {
            Direction.North => new BlockPosition(startX + u, startY + y, startZ + v),
            Direction.South => new BlockPosition(startX + (maxU - u), startY + y, startZ + (maxV - v)),
            Direction.East => new BlockPosition(startX + (maxV - v), startY + y, startZ + u),
            Direction.West => new BlockPosition(startX + v, startY + y, startZ + (maxU - u)),
            _ => throw new ArgumentOutOfRangeException(nameof(Facing), Facing, "Facing must be horizontal.")
        };
    }
}
