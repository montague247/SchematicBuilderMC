using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;
using SchematicBuilderMC.Extensions;
using SchematicBuilderMC.Models;
using SchematicBuilderMC.Registries;

namespace SchematicBuilderMC.Schematics.Contraptions;

/// <summary>
/// Generates a compact, directional 2-wide Minecraft flying-machine schematic recipe with optional starter and end-stop blocks.
/// </summary>
/// <remarks>
/// <para>
/// External launch/stop notes: when <see cref="IncludeStarter"/> is <c>false</c>, trigger the rear observer at
/// <c>(u = 0, y = 0, v = 0)</c> by updating an adjacent block (e.g. placing/removing a block or triggering a note block).
/// When <see cref="IncludeEndStop"/> is <c>false</c>, place an immovable block (such as <c>minecraft:obsidian</c>,
/// <c>minecraft:furnace</c>, or <c>minecraft:glazed_terracotta</c>) in front of the machine's travel path to halt it.
/// </para>
/// </remarks>
public sealed class DirectionalFlyingMachineSchematicCreator : SchematicCreator
{
    /// <summary>
    /// Horizontal travel direction (<see cref="Direction.North"/>, <see cref="Direction.South"/>, <see cref="Direction.East"/>, or <see cref="Direction.West"/>).
    /// </summary>
    public Direction TravelDirection { get; set; } = Direction.South;

    /// <summary>
    /// When <c>true</c>, uses <see cref="Minecraft.Blocks.HoneyBlock"/> instead of <see cref="Minecraft.Blocks.SlimeBlock"/>.
    /// </summary>
    public bool UseHoneyBlocks { get; set; } = false;

    /// <summary>
    /// When <c>true</c>, places a <see cref="Minecraft.Blocks.NoteBlock"/> starter directly above the rear observer.
    /// </summary>
    public bool IncludeStarter { get; set; } = true;

    /// <summary>
    /// When <c>true</c>, places an immovable <see cref="Minecraft.Blocks.Obsidian"/> end-stop block ahead of the machine.
    /// </summary>
    public bool IncludeEndStop { get; set; } = false;

    /// <summary>
    /// Travel gap in blocks between the front of the flying machine and the optional end-stop block (minimum 1).
    /// </summary>
    public int StopDistance { get; set; } = 4;

    private int TravelSpan => 4 + (IncludeEndStop ? StopDistance : 0);

    /// <summary>
    /// Total schematic width along the X axis.
    /// </summary>
    public int Width => TravelDirection is Direction.North or Direction.South ? 2 : TravelSpan;

    /// <summary>
    /// Total schematic height along the Y axis.
    /// </summary>
    public int Height => IncludeStarter ? 2 : 1;

    /// <summary>
    /// Total schematic length along the Z axis.
    /// </summary>
    public int Length => TravelDirection is Direction.North or Direction.South ? TravelSpan : 2;

    /// <summary>
    /// Places the directional flying machine into an existing <see cref="SchematicBuilder"/> starting at (startX, startY, startZ),
    /// extending its dimensions automatically if needed.
    /// </summary>
    public override SchematicBuilder Place(SchematicBuilder builder, int startX = 0, int startY = 0, int startZ = 0)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ValidateConfiguration();
        EnsurePlacementBounds(builder, startX, startY, startZ, Width, Height, Length);

        Direction oppositeDirection = GetOppositeDirection(TravelDirection);
        string slimeOrHoney = UseHoneyBlocks ? Minecraft.Blocks.HoneyBlock : Minecraft.Blocks.SlimeBlock;

        // Rear half + forward pusher (u = 0)
        builder.SetBlock(MapPosition(startX, startY, startZ, 0, 0, 0), Minecraft.Blocks.Observer.WithFacing(oppositeDirection));
        builder.SetPiston(MapPosition(startX, startY, startZ, 0, 0, 1), TravelDirection, sticky: true, extended: false);
        builder.SetBlock(MapPosition(startX, startY, startZ, 0, 0, 2), slimeOrHoney);
        builder.SetBlock(MapPosition(startX, startY, startZ, 0, 0, 3), slimeOrHoney);

        // Front half + rear puller (u = 1)
        builder.SetBlock(MapPosition(startX, startY, startZ, 1, 0, 0), slimeOrHoney);
        builder.SetBlock(MapPosition(startX, startY, startZ, 1, 0, 1), slimeOrHoney);
        builder.SetPiston(MapPosition(startX, startY, startZ, 1, 0, 2), oppositeDirection, sticky: true, extended: false);
        builder.SetBlock(MapPosition(startX, startY, startZ, 1, 0, 3), Minecraft.Blocks.Observer.WithFacing(TravelDirection));

        if (IncludeStarter)
        {
            builder.SetBlock(MapPosition(startX, startY, startZ, 0, 1, 0), Minecraft.Blocks.NoteBlock);
        }

        if (IncludeEndStop)
        {
            builder.SetBlock(MapPosition(startX, startY, startZ, 0, 0, 3 + StopDistance), Minecraft.Blocks.Obsidian);
        }

        return builder;
    }

    /// <summary>
    /// Creates a new schematic containing this directional flying machine.
    /// </summary>
    public override SchematicBuilder Create()
    {
        ValidateConfiguration();

        var builder = SchematicBuilder.Create(Width, Height, Length)
            .WithRotation(Rotation)
            .WithOffset(0, 0, 0)
            .WithName("Directional Flying Machine")
            .WithAuthor("SchematicBuilderMC")
            .WithDescription($"A compact flying machine traveling {TravelDirection}.");

        return Place(builder, 0, 0, 0);
    }

    private void ValidateConfiguration()
    {
        if (!Enum.IsDefined(TravelDirection) ||
            TravelDirection is not (Direction.North or Direction.South or Direction.East or Direction.West))
        {
            throw new ArgumentOutOfRangeException(
                nameof(TravelDirection),
                TravelDirection,
                "TravelDirection must be a horizontal direction (North, South, East, or West).");
        }

        if (StopDistance < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(StopDistance), StopDistance, "StopDistance must be at least 1.");
        }
    }

    private static Direction GetOppositeDirection(Direction direction) => direction switch
    {
        Direction.North => Direction.South,
        Direction.South => Direction.North,
        Direction.East => Direction.West,
        Direction.West => Direction.East,
        _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, "Direction must be horizontal.")
    };

    private BlockPosition MapPosition(int startX, int startY, int startZ, int u, int y, int v)
    {
        const int maxU = 1;
        int maxV = TravelSpan - 1;

        return TravelDirection switch
        {
            Direction.South => new BlockPosition(startX + u, startY + y, startZ + v),
            Direction.North => new BlockPosition(startX + (maxU - u), startY + y, startZ + (maxV - v)),
            Direction.East => new BlockPosition(startX + v, startY + y, startZ + (maxU - u)),
            Direction.West => new BlockPosition(startX + (maxV - v), startY + y, startZ + u),
            _ => throw new ArgumentOutOfRangeException(nameof(TravelDirection), TravelDirection, "TravelDirection must be horizontal.")
        };
    }
}
