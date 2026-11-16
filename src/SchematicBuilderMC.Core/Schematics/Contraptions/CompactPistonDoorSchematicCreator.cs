using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;
using SchematicBuilderMC.Extensions;
using SchematicBuilderMC.Models;
using SchematicBuilderMC.Registries;

namespace SchematicBuilderMC.Schematics.Contraptions;

/// <summary>
/// Generates a compact 2x2 or 3x3 sticky-piston door contraption schematic.
/// </summary>
public sealed class CompactPistonDoorSchematicCreator : SchematicCreator
{
    /// <summary>
    /// Door opening size (<see cref="PistonDoorSize.TwoByTwo"/> or <see cref="PistonDoorSize.ThreeByThree"/>).
    /// </summary>
    public PistonDoorSize Size { get; set; } = PistonDoorSize.TwoByTwo;

    /// <summary>
    /// Horizontal direction the front face of the door faces (<see cref="Direction.North"/>, <see cref="Direction.South"/>, <see cref="Direction.East"/>, or <see cref="Direction.West"/>).
    /// </summary>
    public Direction Facing { get; set; } = Direction.North;

    /// <summary>
    /// Block identifier used for the surrounding door frame.
    /// </summary>
    public string FrameBlock { get; set; } = Minecraft.Blocks.StoneBricks;

    /// <summary>
    /// Block identifier used for the movable door blocks.
    /// </summary>
    public string DoorBlock { get; set; } = Minecraft.Blocks.IronBlock;

    /// <summary>
    /// Block identifier used for the supporting redstone circuit pillars and base.
    /// </summary>
    public string CircuitBlock { get; set; } = Minecraft.Blocks.SmoothStone;

    /// <summary>
    /// Whether the trigger lever starts in the powered state.
    /// </summary>
    public bool Powered { get; set; } = false;

    private int DoorSpan => (int)Size;

    /// <summary>
    /// Total schematic width along the X axis for the current <see cref="Size"/> and <see cref="Facing"/>.
    /// </summary>
    public int Width => Facing is Direction.North or Direction.South ? DoorSpan + 4 : 3;

    /// <summary>
    /// Total schematic height along the Y axis for the current <see cref="Size"/>.
    /// </summary>
    public int Height => DoorSpan + 2;

    /// <summary>
    /// Total schematic length along the Z axis for the current <see cref="Size"/> and <see cref="Facing"/>.
    /// </summary>
    public int Length => Facing is Direction.North or Direction.South ? 3 : DoorSpan + 4;

    /// <summary>
    /// Returns the block coordinates of the front trigger lever for the specified origin.
    /// </summary>
    public BlockPosition GetTriggerPosition(int startX = 0, int startY = 0, int startZ = 0)
    {
        ValidateConfiguration();
        return MapPosition(startX, startY, startZ, 1, 1, 0);
    }

    /// <summary>
    /// Places the piston door into an existing <see cref="SchematicBuilder"/> starting at (startX, startY, startZ).
    /// </summary>
    public SchematicBuilder Place(SchematicBuilder builder, int startX = 0, int startY = 0, int startZ = 0)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ValidateConfiguration();
        ArgumentException.ThrowIfNullOrWhiteSpace(FrameBlock);
        ArgumentException.ThrowIfNullOrWhiteSpace(DoorBlock);
        ArgumentException.ThrowIfNullOrWhiteSpace(CircuitBlock);

        var schematic = builder.Build();
        if (startX < 0 || startY < 0 || startZ < 0 ||
            startX + Width > schematic.Width ||
            startY + Height > schematic.Height ||
            startZ + Length > schematic.Length)
        {
            throw new ArgumentOutOfRangeException(
                nameof(startX),
                $"A {Size} piston door facing {Facing} ({Width}x{Height}x{Length}) placed at ({startX}, {startY}, {startZ}) exceeds schematic bounds ({schematic.Width}x{schematic.Height}x{schematic.Length}).");
        }

        int span = DoorSpan;
        var (leftPistonFacing, rightPistonFacing) = GetInwardPistonFacings();

        // Frame floor and lintel along the piston plane (v = 1)
        for (int u = 1; u <= span + 2; u++)
        {
            builder.SetBlock(MapPosition(startX, startY, startZ, u, 0, 1), FrameBlock);
            builder.SetBlock(MapPosition(startX, startY, startZ, u, span + 1, 1), FrameBlock);
        }

        // Front frame pillars (v = 0)
        for (int y = 0; y <= span + 1; y++)
        {
            builder.SetBlock(MapPosition(startX, startY, startZ, 1, y, 0), FrameBlock);
            builder.SetBlock(MapPosition(startX, startY, startZ, span + 2, y, 0), FrameBlock);
        }

        // Left & right horizontal sticky pistons + movable door blocks (v = 1)
        for (int y = 1; y <= span; y++)
        {
            builder.SetPiston(MapPosition(startX, startY, startZ, 1, y, 1), leftPistonFacing, sticky: true, extended: Powered);
            builder.SetPiston(MapPosition(startX, startY, startZ, span + 2, y, 1), rightPistonFacing, sticky: true, extended: Powered);

            for (int u = 2; u <= span + 1; u++)
            {
                builder.SetBlock(MapPosition(startX, startY, startZ, u, y, 1), DoorBlock);
            }

            // Outer supporting circuit pillars
            builder.SetBlock(MapPosition(startX, startY, startZ, 0, y, 1), CircuitBlock);
            builder.SetBlock(MapPosition(startX, startY, startZ, span + 3, y, 1), CircuitBlock);
        }

        // Center top & bottom vertical sticky pistons for 3x3 doors
        if (Size == PistonDoorSize.ThreeByThree)
        {
            builder.SetPiston(MapPosition(startX, startY, startZ, 3, 4, 1), Direction.Down, sticky: true, extended: Powered);
            builder.SetPiston(MapPosition(startX, startY, startZ, 3, 0, 1), Direction.Up, sticky: true, extended: Powered);
        }

        // Trigger lever on the front frame pillar
        builder.SetLever(GetTriggerPosition(startX, startY, startZ), BlockFace.Wall, Facing, Powered);

        // Back redstone wiring along v = 2
        for (int u = 0; u <= span + 3; u++)
        {
            builder.SetBlock(MapPosition(startX, startY, startZ, u, 0, 2), CircuitBlock);
        }

        builder.SetBlock(MapPosition(startX, startY, startZ, 0, 1, 2), Minecraft.Blocks.RedstoneTorch.WithLit(!Powered));
        builder.SetBlock(MapPosition(startX, startY, startZ, span + 3, 1, 2), Minecraft.Blocks.RedstoneTorch.WithLit(!Powered));

        builder.SetRepeater(MapPosition(startX, startY, startZ, 1, 1, 2), leftPistonFacing, delay: 1, powered: !Powered);
        builder.SetRepeater(MapPosition(startX, startY, startZ, span + 2, 1, 2), rightPistonFacing, delay: 1, powered: !Powered);

        int wirePower = Powered ? 15 : 0;
        for (int u = 2; u <= span + 1; u++)
        {
            builder.SetRedstoneWire(MapPosition(startX, startY, startZ, u, 1, 2), wirePower);
        }

        return builder;
    }

    /// <summary>
    /// Creates a new schematic containing this piston door.
    /// </summary>
    public override SchematicBuilder Create()
    {
        ValidateConfiguration();

        var builder = SchematicBuilder.Create(Width, Height, Length)
            .WithOffset(0, 0, 0)
            .WithName("Compact Piston Door")
            .WithAuthor("SchematicBuilderMC")
            .WithDescription($"A compact {(int)Size}x{(int)Size} sticky-piston door facing {Facing}.");

        return Place(builder, 0, 0, 0);
    }

    private void ValidateConfiguration()
    {
        if (!Enum.IsDefined(Size))
        {
            throw new ArgumentOutOfRangeException(nameof(Size), Size, "Piston door size must be TwoByTwo or ThreeByThree.");
        }

        if (Facing is not (Direction.North or Direction.South or Direction.East or Direction.West))
        {
            throw new ArgumentOutOfRangeException(nameof(Facing), Facing, "Facing must be a horizontal direction (North, South, East, or West).");
        }
    }

    private (Direction LeftPistonFacing, Direction RightPistonFacing) GetInwardPistonFacings() => Facing switch
    {
        Direction.North => (Direction.East, Direction.West),
        Direction.South => (Direction.West, Direction.East),
        Direction.East => (Direction.South, Direction.North),
        Direction.West => (Direction.North, Direction.South),
        _ => throw new ArgumentOutOfRangeException(nameof(Facing), Facing, "Facing must be horizontal.")
    };

    private BlockPosition MapPosition(int startX, int startY, int startZ, int u, int y, int v)
    {
        int maxU = DoorSpan + 3;
        const int maxV = 2;

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
