using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;
using SchematicBuilderMC.Extensions;
using SchematicBuilderMC.Models;
using SchematicBuilderMC.Registries;

namespace SchematicBuilderMC.Schematics.Contraptions;

/// <summary>
/// Generates a compact, single-pulse stationary TNT duper contraption schematic that activates once per short
/// redstone signal and drops primed TNT directly beneath a configurable <c>(X, Z)</c> target coordinate.
/// </summary>
/// <remarks>
/// <para>
/// Activation: feed a short redstone pulse (e.g. button, observer, repeater, or dust line) into the
/// <see cref="TriggerBlock"/> at <see cref="GetTriggerPosition"/> (default <c>minecraft:target</c> directly behind
/// the sticky piston). A single short pulse extends and retracts the sticky piston once, duplicating one primed TNT
/// entity that falls vertically through <c>(dropX, y &lt; dropY, dropZ)</c>.
/// </para>
/// <para>
/// Keep the column directly below the TNT block at <see cref="GetDropPosition"/> unobstructed air so duplicated TNT
/// falls straight down to the target <c>(X, Z)</c> coordinate without colliding with support blocks.
/// </para>
/// </remarks>
public sealed class TntDuperSchematicCreator : SchematicCreator
{
    private const int LocalTriggerU = 0;
    private const int LocalTriggerY = 1;
    private const int LocalTriggerV = 0;

    private const int LocalDropU = 1;
    private const int LocalDropY = 0;
    private const int LocalDropV = 3;

    /// <summary>
    /// Horizontal extension direction of the sticky piston (<see cref="Direction.North"/>, <see cref="Direction.South"/>,
    /// <see cref="Direction.East"/>, or <see cref="Direction.West"/>).
    /// </summary>
    public Direction Facing { get; set; } = Direction.South;

    /// <summary>
    /// Optional X coordinate in the generated schematic where the TNT block (and drop trajectory) should be placed.
    /// When <c>null</c>, the contraption is placed tight at <c>x = 0</c>.
    /// </summary>
    public int? TargetX { get; set; }

    /// <summary>
    /// Optional Z coordinate in the generated schematic where the TNT block (and drop trajectory) should be placed.
    /// When <c>null</c>, the contraption is placed tight at <c>z = 0</c>.
    /// </summary>
    public int? TargetZ { get; set; }

    /// <summary>
    /// Optional vertical open drop-shaft clearance below the TNT block in standalone <see cref="Create"/> builds (0 or greater).
    /// </summary>
    public int DropClearance { get; set; } = 0;

    /// <summary>
    /// Whether to include a solid redstone conductor trigger block behind the sticky piston for short external pulse inputs.
    /// </summary>
    public bool IncludeTriggerInput { get; set; } = true;

    /// <summary>
    /// Whether to place a single-pulse stone button on top of the trigger input block when <see cref="IncludeTriggerInput"/> is <c>true</c>.
    /// </summary>
    public bool IncludeTriggerButton { get; set; } = false;

    /// <summary>
    /// Block identifier used for the redstone trigger input block behind the sticky piston.
    /// </summary>
    public string TriggerBlock { get; set; } = Minecraft.Blocks.Target;

    /// <summary>
    /// Wall coral fan identifier used to trigger the TNT duplication update.
    /// </summary>
    public string CoralBlock { get; set; } = Minecraft.Blocks.DeadTubeCoralWallFan;

    /// <summary>
    /// Non-sticky spacer wall identifier placed in front of the primary slime block.
    /// </summary>
    public string WallBlock { get; set; } = Minecraft.Blocks.CobblestoneWall;

    /// <summary>
    /// Minimum bounding-box width along the X axis for the contraption itself.
    /// </summary>
    public int Width => Facing is Direction.North or Direction.South ? 2 : 4;

    /// <summary>
    /// Total height along the Y axis (<c>3 + DropClearance</c>).
    /// </summary>
    public int Height => 3 + DropClearance;

    /// <summary>
    /// Minimum bounding-box length along the Z axis for the contraption itself.
    /// </summary>
    public int Length => Facing is Direction.North or Direction.South ? 4 : 2;

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
    /// Returns the TNT block offset relative to the contraption placement origin <c>(startX, startY, startZ)</c>.
    /// </summary>
    public BlockPosition GetDropOffset() => MapLocalOffset(LocalDropU, LocalDropY, LocalDropV);

    /// <summary>
    /// Returns the world/schematic coordinate of the TNT block (above the unobstructed vertical drop column)
    /// when placed at <c>(startX, startY, startZ)</c>.
    /// </summary>
    public BlockPosition GetDropPosition(int startX = 0, int startY = 0, int startZ = 0) =>
        MapPosition(startX, startY, startZ, LocalDropU, LocalDropY, LocalDropV);

    /// <summary>
    /// Returns the world/schematic coordinate of the short-pulse trigger input block
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
        EnsurePlacementBounds(builder, startX, startY, startZ, Width, 3, Length);

        Direction opposite = GetOppositeDirection(Facing);
        string railShape = Facing is Direction.North or Direction.South ? "north_south" : "east_west";

        // Trigger input for a single short redstone pulse
        if (IncludeTriggerInput)
        {
            builder.SetBlock(MapPosition(startX, startY, startZ, LocalTriggerU, LocalTriggerY, LocalTriggerV), TriggerBlock);
            if (IncludeTriggerButton)
            {
                var button = Minecraft.Blocks.StoneButton
                    .WithFace(BlockFace.Floor)
                    .WithFacing(Facing)
                    .WithPowered(false);
                builder.SetBlock(MapPosition(startX, startY, startZ, LocalTriggerU, 2, LocalTriggerV), button);
            }
        }

        // Stationary sticky piston
        builder.SetPiston(MapPosition(startX, startY, startZ, 0, 1, 1), Facing, sticky: true, extended: false);

        // Movable L-shaped slime frame, wall spacer, detector rail, and minecart
        builder.SetBlock(MapPosition(startX, startY, startZ, 0, 1, 2), Minecraft.Blocks.SlimeBlock);
        builder.SetBlock(MapPosition(startX, startY, startZ, 1, 1, 2), Minecraft.Blocks.SlimeBlock);
        builder.SetBlock(MapPosition(startX, startY, startZ, 1, 1, 3), Minecraft.Blocks.SlimeBlock);
        builder.SetBlock(MapPosition(startX, startY, startZ, 0, 1, 3), WallBlock);

        var railPosition = MapPosition(startX, startY, startZ, 0, 2, 2);
        var detectorRail = Minecraft.Blocks.DetectorRail
            .WithPowered(true)
            .WithShape(railShape)
            .WithWaterlogged(false);
        builder.SetBlock(railPosition, detectorRail);
        builder.AddMinecart(railPosition);

        // Dead coral wall fan attached to the back face of TNT + TNT block at the drop column
        var coralFan = CoralBlock
            .WithFacing(opposite)
            .WithWaterlogged(false);
        builder.SetBlock(MapPosition(startX, startY, startZ, 1, 0, 2), coralFan);

        var tnt = Minecraft.Blocks.Tnt.WithProperty("unstable", "false");
        builder.SetBlock(MapPosition(startX, startY, startZ, LocalDropU, LocalDropY, LocalDropV), tnt);

        return builder;
    }

    /// <summary>
    /// Places the TNT duper into an existing <see cref="SchematicBuilder"/> positioned so that its TNT block sits
    /// directly above <c>(targetX, targetY, targetZ)</c> and drops primed TNT along <c>(targetX, y &lt; targetY, targetZ)</c>,
    /// extending the schematic dimensions automatically if needed.
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
        ArgumentException.ThrowIfNullOrWhiteSpace(CoralBlock);
        ArgumentException.ThrowIfNullOrWhiteSpace(WallBlock);
    }

    private static Direction GetOppositeDirection(Direction direction) => direction switch
    {
        Direction.North => Direction.South,
        Direction.South => Direction.North,
        Direction.East => Direction.West,
        Direction.West => Direction.East,
        _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, "Direction must be horizontal.")
    };

    private BlockPosition MapLocalOffset(int u, int y, int v) => MapPosition(0, 0, 0, u, y, v);

    private BlockPosition MapPosition(int startX, int startY, int startZ, int u, int y, int v)
    {
        const int maxU = 1;
        const int maxV = 3;

        return Facing switch
        {
            Direction.South => new BlockPosition(startX + u, startY + y, startZ + v),
            Direction.North => new BlockPosition(startX + (maxU - u), startY + y, startZ + (maxV - v)),
            Direction.East => new BlockPosition(startX + v, startY + y, startZ + (maxU - u)),
            Direction.West => new BlockPosition(startX + (maxV - v), startY + y, startZ + u),
            _ => throw new ArgumentOutOfRangeException(nameof(Facing), Facing, "Facing must be horizontal.")
        };
    }
}
