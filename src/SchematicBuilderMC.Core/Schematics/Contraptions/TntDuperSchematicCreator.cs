using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;
using SchematicBuilderMC.Extensions;
using SchematicBuilderMC.Models;
using SchematicBuilderMC.Registries;

namespace SchematicBuilderMC.Schematics.Contraptions;

/// <summary>
/// Generates a real-game-verified, single-pulse stationary TNT duper contraption schematic that activates once per
/// redstone pulse (or button press) and drops primed TNT straight down through a configurable <c>(X, Z)</c> target column.
/// </summary>
/// <remarks>
/// <para>
/// Based on the verified <c>tnt-button-17b</c> one-shot hold engine combined with the <c>tnt-drop-16b</c> vertical
/// stopper wall: at rest a redstone wall torch strongly powers the obsidian beside the sticky piston, keeping the
/// contraption armed. Pulsing <see cref="TriggerBlock"/> at <see cref="GetTriggerPosition"/> (via the optional
/// wall button, repeater, observer, or dust line for &gt;= 2 game ticks) holds the piston directly while turning the
/// torch off; when the pulse ends, the piston retracts 2 ticks before the torch relights, pulling the slime frame
/// back and immediately pushing one duplicated TNT forward into the stopper block so it falls straight down through
/// <see cref="GetDropPosition"/> (<c>(dropX, y &lt;= dropY + 3, dropZ)</c>).
/// </para>
/// <para>
/// Keep the vertical drop column at <see cref="GetDropPosition"/> unobstructed air beneath the contraption so
/// duplicated TNT falls straight down without detonating against support blocks.
/// </para>
/// </remarks>
public sealed class TntDuperSchematicCreator : SchematicCreator
{
    private const int LocalMaxU = 3;
    private const int LocalMaxV = 5;

    private const int LocalTriggerU = 1;
    private const int LocalTriggerY = 0;
    private const int LocalTriggerV = 4;

    private const int LocalButtonU = 0;
    private const int LocalButtonY = 0;
    private const int LocalButtonV = 4;

    private const int LocalTntU = 2;
    private const int LocalTntY = 2;
    private const int LocalTntV = 2;

    private const int LocalDropU = 2;
    private const int LocalDropY = 0;
    private const int LocalDropV = 1;

    /// <summary>
    /// Horizontal extension direction of the sticky piston (<see cref="Direction.North"/>, <see cref="Direction.South"/>,
    /// <see cref="Direction.East"/>, or <see cref="Direction.West"/>).
    /// </summary>
    public Direction Facing { get; set; } = Direction.South;

    /// <summary>
    /// Optional X coordinate in the generated schematic where duplicated TNT drops vertically.
    /// When <c>null</c>, the contraption is placed tight at <c>x = 0</c>.
    /// </summary>
    public int? TargetX { get; set; }

    /// <summary>
    /// Optional Z coordinate in the generated schematic where duplicated TNT drops vertically.
    /// When <c>null</c>, the contraption is placed tight at <c>z = 0</c>.
    /// </summary>
    public int? TargetZ { get; set; }

    /// <summary>
    /// Optional vertical open drop-shaft clearance below the contraption in standalone <see cref="Create"/> builds (0 or greater).
    /// </summary>
    public int DropClearance { get; set; } = 0;

    /// <summary>
    /// Whether to place a single-pulse stone button on the side of <see cref="TriggerBlock"/>.
    /// </summary>
    public bool IncludeTriggerButton { get; set; } = false;

    /// <summary>
    /// Solid conductive block identifier placed beneath the sticky piston to hold the wall torch and accept redstone pulses.
    /// </summary>
    public string TriggerBlock { get; set; } = Minecraft.Blocks.Stone;

    /// <summary>
    /// Solid stopper block identifier positioned at the top-front of the drop column to arrest horizontal TNT momentum.
    /// </summary>
    public string StopperBlock { get; set; } = Minecraft.Blocks.Stone;

    /// <summary>
    /// Dead floor coral fan identifier used to trigger the in-push TNT update.
    /// </summary>
    public string CoralBlock { get; set; } = Minecraft.Blocks.DeadTubeCoralFan;

    /// <summary>
    /// Minimum bounding-box width along the X axis for the contraption itself.
    /// </summary>
    public int Width => Facing is Direction.North or Direction.South ? 4 : 6;

    /// <summary>
    /// Total height along the Y axis (<c>4 + DropClearance</c>).
    /// </summary>
    public int Height => 4 + DropClearance;

    /// <summary>
    /// Minimum bounding-box length along the Z axis for the contraption itself.
    /// </summary>
    public int Length => Facing is Direction.North or Direction.South ? 6 : 4;

    /// <summary>
    /// Configures the contraption to drop TNT at the specified schematic <c>(targetX, targetZ)</c> coordinate and returns this instance.
    /// </summary>
    public TntDuperSchematicCreator WithDropPosition(int targetX, int targetZ, int dropClearance = 0)
    {
        TargetX = targetX;
        TargetZ = targetZ;
        DropClearance = dropClearance;
        return this;
    }

    /// <summary>
    /// Returns the TNT vertical drop-column offset relative to the contraption placement origin <c>(startX, startY, startZ)</c>.
    /// </summary>
    public BlockPosition GetDropOffset() => MapLocalOffset(LocalDropU, LocalDropY, LocalDropV);

    /// <summary>
    /// Returns the world/schematic coordinate at the bottom of the unobstructed vertical TNT drop column
    /// when placed at <c>(startX, startY, startZ)</c>.
    /// </summary>
    public BlockPosition GetDropPosition(int startX = 0, int startY = 0, int startZ = 0) =>
        MapPosition(startX, startY, startZ, LocalDropU, LocalDropY, LocalDropV);

    /// <summary>
    /// Returns the world/schematic coordinate of the primed TNT source block
    /// when placed at <c>(startX, startY, startZ)</c>.
    /// </summary>
    public BlockPosition GetTntBlockPosition(int startX = 0, int startY = 0, int startZ = 0) =>
        MapPosition(startX, startY, startZ, LocalTntU, LocalTntY, LocalTntV);

    /// <summary>
    /// Returns the world/schematic coordinate of the trigger input block
    /// when placed at <c>(startX, startY, startZ)</c>.
    /// </summary>
    public BlockPosition GetTriggerPosition(int startX = 0, int startY = 0, int startZ = 0) =>
        MapPosition(startX, startY, startZ, LocalTriggerU, LocalTriggerY, LocalTriggerV);

    /// <summary>
    /// Places the TNT duper into an existing <see cref="SchematicBuilder"/> starting at <c>(startX, startY, startZ)</c>,
    /// extending its dimensions automatically if needed.
    /// </summary>
    public override SchematicBuilder Place(SchematicBuilder builder, int startX = 0, int startY = 0, int startZ = 0)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ValidateConfiguration();
        EnsurePlacementBounds(builder, startX, startY, startZ, Width, 4, Length);

        Direction eastDir = RotateClockwise(Direction.East, Facing);
        Direction westDir = RotateClockwise(Direction.West, Facing);

        // Layer 0: trigger base, one-shot hold wall torch, and optional wall button
        builder.SetBlock(MapPosition(startX, startY, startZ, LocalTriggerU, LocalTriggerY, LocalTriggerV), TriggerBlock);
        builder.SetBlock(
            MapPosition(startX, startY, startZ, 2, 0, 4),
            Minecraft.Blocks.RedstoneWallTorch.WithFacing(eastDir).WithLit(true));

        if (IncludeTriggerButton)
        {
            var button = Minecraft.Blocks.StoneButton
                .WithFace(BlockFace.Wall)
                .WithFacing(westDir)
                .WithPowered(false);
            builder.SetBlock(MapPosition(startX, startY, startZ, LocalButtonU, LocalButtonY, LocalButtonV), button);
        }

        // Layer 1: sticky piston, immovable obsidian hold block, and base slime frame
        builder.SetPiston(MapPosition(startX, startY, startZ, 1, 1, 4), Facing, sticky: true, extended: false);
        builder.SetBlock(MapPosition(startX, startY, startZ, 2, 1, 4), Minecraft.Blocks.Obsidian);
        builder.SetBlock(MapPosition(startX, startY, startZ, 1, 1, 3), Minecraft.Blocks.SlimeBlock);
        builder.SetBlock(MapPosition(startX, startY, startZ, 2, 1, 3), Minecraft.Blocks.SlimeBlock);
        builder.SetBlock(MapPosition(startX, startY, startZ, 3, 1, 3), Minecraft.Blocks.SlimeBlock);
        builder.SetBlock(MapPosition(startX, startY, startZ, 3, 1, 4), Minecraft.Blocks.SlimeBlock);
        builder.SetBlock(MapPosition(startX, startY, startZ, 3, 1, 5), Minecraft.Blocks.SlimeBlock);

        // Layer 2: TNT, dead coral fan update source, settled dust-redirection wire, redstone block, and slime pillar
        builder.SetBlock(
            MapPosition(startX, startY, startZ, LocalTntU, LocalTntY, LocalTntV),
            Minecraft.Blocks.Tnt.WithProperty("unstable", "false"));
        builder.SetBlock(
            MapPosition(startX, startY, startZ, 1, 2, 3),
            CoralBlock.WithWaterlogged(false));
        builder.SetBlock(
            MapPosition(startX, startY, startZ, 2, 2, 4),
            CreateSettledWireState());
        builder.SetBlock(MapPosition(startX, startY, startZ, 3, 2, 4), Minecraft.Blocks.RedstoneBlock);
        builder.SetBlock(MapPosition(startX, startY, startZ, 3, 2, 5), Minecraft.Blocks.SlimeBlock);

        // Layer 3: top launcher slime arm (flings primed TNT) and front stopper wall above the drop shaft
        builder.SetBlock(MapPosition(startX, startY, startZ, 2, 3, 0), StopperBlock);
        builder.SetBlock(MapPosition(startX, startY, startZ, 2, 3, 4), Minecraft.Blocks.SlimeBlock);
        builder.SetBlock(MapPosition(startX, startY, startZ, 2, 3, 5), Minecraft.Blocks.SlimeBlock);
        builder.SetBlock(MapPosition(startX, startY, startZ, 3, 3, 5), Minecraft.Blocks.SlimeBlock);

        return builder;
    }

    /// <summary>
    /// Places the TNT duper into an existing <see cref="SchematicBuilder"/> positioned so that its unobstructed vertical
    /// TNT drop shaft aligns at <c>(targetX, targetY, targetZ)</c>, extending the schematic dimensions automatically if needed.
    /// </summary>
    public SchematicBuilder PlaceAtDropPosition(SchematicBuilder builder, int targetX, int targetY, int targetZ)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ValidateConfiguration();

        var dropOffset = GetDropOffset();
        int startX = targetX - dropOffset.X;
        int startZ = targetZ - dropOffset.Z;

        if (startX < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(targetX), targetX, $"targetX must be at least {dropOffset.X} when facing {Facing} so the contraption fits at non-negative X.");
        }

        if (startZ < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(targetZ), targetZ, $"targetZ must be at least {dropOffset.Z} when facing {Facing} so the contraption fits at non-negative Z.");
        }

        if (targetY < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(targetY), targetY, "targetY must be zero or greater.");
        }

        return Place(builder, startX, targetY, startZ);
    }

    /// <summary>
    /// Creates a new schematic containing this TNT duper contraption. When <see cref="TargetX"/> or <see cref="TargetZ"/>
    /// are specified, the schematic dimensions and placement origin are aligned so TNT drops at that exact <c>(X, Z)</c>.
    /// </summary>
    public override SchematicBuilder Create()
    {
        ValidateConfiguration();

        var dropOffset = GetDropOffset();
        int startX = TargetX.HasValue ? TargetX.Value - dropOffset.X : 0;
        int startZ = TargetZ.HasValue ? TargetZ.Value - dropOffset.Z : 0;

        if (startX < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(TargetX), TargetX, $"TargetX must be at least {dropOffset.X} when facing {Facing}.");
        }

        if (startZ < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(TargetZ), TargetZ, $"TargetZ must be at least {dropOffset.Z} when facing {Facing}.");
        }

        int schematicWidth = Math.Max(Width, startX + Width);
        int schematicLength = Math.Max(Length, startZ + Length);
        var dropPos = GetDropPosition(startX, DropClearance, startZ);

        var builder = SchematicBuilder.Create(schematicWidth, Height, schematicLength)
            .WithRotation(Rotation)
            .WithOffset(0, 0, 0)
            .WithName("TNT Duper")
            .WithAuthor("SchematicBuilderMC")
            .WithDescription($"A single-pulse TNT duper facing {Facing} dropping at ({dropPos.X}, {dropPos.Z}).");

        return Place(builder, startX, DropClearance, startZ);
    }

    private void ValidateConfiguration()
    {
        if (!Enum.IsDefined(Facing) ||
            Facing is not (Direction.North or Direction.South or Direction.East or Direction.West))
        {
            throw new ArgumentOutOfRangeException(nameof(Facing), Facing, "Facing must be a horizontal direction (North, South, East, or West).");
        }

        if (DropClearance < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(DropClearance), DropClearance, "DropClearance must be zero or greater.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(TriggerBlock);
        ArgumentException.ThrowIfNullOrWhiteSpace(StopperBlock);
        ArgumentException.ThrowIfNullOrWhiteSpace(CoralBlock);
    }

    private BlockState CreateSettledWireState()
    {
        bool eastWestAxis = Facing is Direction.North or Direction.South;
        return Minecraft.Blocks.RedstoneWire
            .WithProperty("east", eastWestAxis ? "side" : "none")
            .WithProperty("west", eastWestAxis ? "side" : "none")
            .WithProperty("north", eastWestAxis ? "none" : "side")
            .WithProperty("south", eastWestAxis ? "none" : "side")
            .WithPower(15);
    }

    private static Direction RotateClockwise(Direction localDirection, Direction facing)
    {
        int steps = facing switch
        {
            Direction.North => 0,
            Direction.East => 1,
            Direction.South => 2,
            Direction.West => 3,
            _ => throw new ArgumentOutOfRangeException(nameof(facing), facing, "Direction must be horizontal.")
        };

        Direction result = localDirection;
        for (int i = 0; i < steps; i++)
        {
            result = result switch
            {
                Direction.North => Direction.East,
                Direction.East => Direction.South,
                Direction.South => Direction.West,
                Direction.West => Direction.North,
                _ => throw new ArgumentOutOfRangeException(nameof(localDirection), localDirection, "Direction must be horizontal.")
            };
        }

        return result;
    }

    private BlockPosition MapLocalOffset(int u, int y, int v) => MapPosition(0, 0, 0, u, y, v);

    private BlockPosition MapPosition(int startX, int startY, int startZ, int u, int y, int v) => Facing switch
    {
        Direction.North => new BlockPosition(startX + u, startY + y, startZ + v),
        Direction.East => new BlockPosition(startX + (LocalMaxV - v), startY + y, startZ + u),
        Direction.South => new BlockPosition(startX + (LocalMaxU - u), startY + y, startZ + (LocalMaxV - v)),
        Direction.West => new BlockPosition(startX + v, startY + y, startZ + (LocalMaxU - u)),
        _ => throw new ArgumentOutOfRangeException(nameof(Facing), Facing, "Facing must be horizontal.")
    };
}
