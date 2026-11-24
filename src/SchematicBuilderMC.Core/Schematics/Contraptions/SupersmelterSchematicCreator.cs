using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;
using SchematicBuilderMC.Extensions;
using SchematicBuilderMC.Models;
using SchematicBuilderMC.Registries;
using SchematicBuilderMC.Schematics.Redstone.Internal;

namespace SchematicBuilderMC.Schematics.Contraptions;

/// <summary>
/// Generates a 32-furnace supersmelter array with dual dropper-line item distribution,
/// double chests for input, fuel, and output, and optional decorative hopper caps and indicator lamp.
/// </summary>
public sealed class SupersmelterSchematicCreator : SchematicCreator
{
    private const int CanonicalWidth = 6;
    private const int CanonicalHeight = 10;
    private const int CanonicalLength = 19;

    /// <summary>
    /// Processing machine block type (<see cref="SmelterType.Furnace"/>, <see cref="SmelterType.BlastFurnace"/>, or <see cref="SmelterType.Smoker"/>).
    /// </summary>
    public SmelterType MachineType { get; set; } = SmelterType.Furnace;

    /// <summary>
    /// Horizontal direction the front input, fuel, and output double chests face
    /// (<see cref="Direction.North"/>, <see cref="Direction.South"/>, <see cref="Direction.East"/>, or <see cref="Direction.West"/>).
    /// Defaults to <see cref="Direction.South"/>.
    /// </summary>
    public Direction Facing { get; set; } = Direction.South;

    /// <summary>
    /// When <c>true</c> (default), includes decorative <see cref="Minecraft.Blocks.Composter"/> hopper caps
    /// and the <see cref="Minecraft.Blocks.RedstoneLamp"/> indicator.
    /// When <c>false</c>, omits the composters and substitutes redstone wire for the lamp so all observer triggers remain functional.
    /// </summary>
    public bool IncludeDecoration { get; set; } = true;

    /// <summary>
    /// Block identifier used for solid redstone circuit blocks (default <see cref="Minecraft.Blocks.GrayWool"/>).
    /// </summary>
    public string CircuitBlock { get; set; } = Minecraft.Blocks.GrayWool;

    /// <summary>
    /// Transparent block identifier used for redstone wire and diode supports (default <see cref="Minecraft.Blocks.GrayStainedGlass"/>).
    /// </summary>
    public string GlassBlock { get; set; } = Minecraft.Blocks.GrayStainedGlass;

    /// <summary>
    /// Optional override for the distribution clock mode.
    /// When <c>null</c> (default), automatically selects the fast 4-tick subtract clock for 2x-speed smelters
    /// (<see cref="SmelterType.Smoker"/> and <see cref="SmelterType.BlastFurnace"/>) and the standard fader clock for <see cref="SmelterType.Furnace"/>.
    /// </summary>
    public bool? UseFastClock { get; set; }

    private bool ResolvedFastClock => UseFastClock ?? (MachineType is SmelterType.Smoker or SmelterType.BlastFurnace);

    /// <summary>
    /// Optional initial items placed in the top input double chest (slots 0..53, counts 1..64).
    /// </summary>
    public IReadOnlyList<ItemStack>? InputItems { get; set; }

    /// <summary>
    /// Optional initial items placed in the middle fuel double chest (slots 0..53, counts 1..64).
    /// </summary>
    public IReadOnlyList<ItemStack>? FuelItems { get; set; }

    /// <summary>
    /// Optional initial items placed in the bottom output double chest (slots 0..53, counts 1..64).
    /// </summary>
    public IReadOnlyList<ItemStack>? OutputItems { get; set; }

    /// <summary>
    /// Total schematic width along the X axis.
    /// </summary>
    public int Width => Facing is Direction.North or Direction.South ? CanonicalWidth : CanonicalLength;

    /// <summary>
    /// Total schematic height along the Y axis (10 blocks).
    /// </summary>
    public int Height => CanonicalHeight;

    /// <summary>
    /// Total schematic length along the Z axis.
    /// </summary>
    public int Length => Facing is Direction.North or Direction.South ? CanonicalLength : CanonicalWidth;

    /// <summary>
    /// Places the supersmelter into an existing <see cref="SchematicBuilder"/> starting at (startX, startY, startZ),
    /// extending its dimensions automatically if needed.
    /// </summary>
    public override SchematicBuilder Place(SchematicBuilder builder, int startX = 0, int startY = 0, int startZ = 0)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ValidateConfiguration();
        EnsurePlacementBounds(builder, startX, startY, startZ, Width, Height, Length);

        Schematic oriented = BuildCanonical().Rotate(GetFacingRotation(Facing));
        return builder.Place(oriented, startX, startY, startZ);
    }

    /// <summary>
    /// Creates a new schematic containing this supersmelter.
    /// </summary>
    public override SchematicBuilder Create()
    {
        ValidateConfiguration();

        var builder = SchematicBuilder.Create(Width, Height, Length)
            .WithRotation(Rotation)
            .WithOffset(0, 0, 0)
            .WithName("Supersmelter")
            .WithAuthor("SchematicBuilderMC")
            .WithDescription($"A 32-{MachineType.ToString().ToLowerInvariant()} supersmelter facing {Facing}.");

        return Place(builder, 0, 0, 0);
    }

    private Schematic BuildCanonical()
    {
        var canonical = SchematicBuilder.Create(CanonicalWidth, CanonicalHeight, CanonicalLength);

        PlaceSmeltersAndCollection(canonical);
        PlaceDropperDistributionAndChests(canonical);
        PlaceRedstoneLogic(canonical);

        if (IncludeDecoration)
        {
            PlaceDecoration(canonical);
        }

        return canonical.Build();
    }

    private void PlaceSmeltersAndCollection(SchematicBuilder builder)
    {
        var (machineBlockId, machineBlockEntityId) = GetMachineIdentifiers();

        // Output hopper rails under both smelter banks (z = 0..15) into front elevator dropper at (2, 2, 16).
        builder.Fill(0, 2, 0, 0, 2, 15, Minecraft.Blocks.Hopper.WithProperty("enabled", "true").WithFacing("south"));
        builder.Fill(4, 2, 0, 4, 2, 15, Minecraft.Blocks.Hopper.WithProperty("enabled", "true").WithFacing("south"));
        builder.Fill(0, 2, 16, 1, 2, 16, Minecraft.Blocks.Hopper.WithProperty("enabled", "true").WithFacing("east"));
        builder.SetBlock(2, 2, 16, Minecraft.Blocks.Dropper.WithFacing("up").WithProperty("triggered", "false"));
        builder.Fill(3, 2, 16, 4, 2, 16, Minecraft.Blocks.Hopper.WithProperty("enabled", "true").WithFacing("west"));

        // 32 smelters (16 west-facing at x = 0, 16 east-facing at x = 4) and fuel/input hoppers.
        BlockState westSmelter = machineBlockId.WithFacing("west").WithLit(false);
        BlockState eastSmelter = machineBlockId.WithFacing("east").WithLit(false);
        for (int z = 0; z < 16; z++)
        {
            builder.SetBlock(0, 3, z, westSmelter);
            builder.SetContainer(machineBlockId, machineBlockEntityId, 0, 3, z);

            builder.SetBlock(4, 3, z, eastSmelter);
            builder.SetContainer(machineBlockId, machineBlockEntityId, 4, 3, z);
        }

        builder.Fill(1, 3, 0, 1, 3, 15, Minecraft.Blocks.Hopper.WithProperty("enabled", "true").WithFacing("west"));
        builder.Fill(3, 3, 0, 3, 3, 15, Minecraft.Blocks.Hopper.WithProperty("enabled", "true").WithFacing("east"));
        builder.SetBlock(2, 3, 16, Minecraft.Blocks.Dropper.WithFacing("south").WithProperty("triggered", "false"));
        SetDoubleChest(builder, 3, OutputItems);

        builder.Fill(0, 4, 0, 0, 4, 15, Minecraft.Blocks.Hopper.WithProperty("enabled", "true").WithFacing("down"));
        builder.Fill(4, 4, 0, 4, 4, 15, Minecraft.Blocks.Hopper.WithProperty("enabled", "true").WithFacing("down"));
        builder.SetBlock(1, 4, 17, Minecraft.Blocks.Hopper.WithProperty("enabled", "true").WithFacing("north"));
        builder.SetBlock(2, 4, 17, Minecraft.Blocks.Hopper.WithProperty("enabled", "true").WithFacing("west"));
        builder.SetBlock(3, 4, 17, Minecraft.Blocks.Hopper.WithProperty("enabled", "true").WithFacing("north"));
    }

    private void PlaceDropperDistributionAndChests(SchematicBuilder builder)
    {
        BlockState northDropper = Minecraft.Blocks.Dropper.WithFacing("north").WithProperty("triggered", "false");
        builder.Fill(1, 4, 0, 1, 4, 16, northDropper);
        builder.Fill(3, 4, 0, 3, 4, 16, northDropper);
        builder.Fill(0, 5, 0, 0, 5, 16, northDropper);
        builder.Fill(4, 5, 0, 4, 5, 16, northDropper);

        BlockState noteBlockBasedrum = Minecraft.Blocks.NoteBlock
            .WithProperty("instrument", "basedrum")
            .WithProperty("note", "0")
            .WithPowered(false);
        builder.Fill(1, 5, 1, 1, 5, 16, noteBlockBasedrum);
        builder.Fill(3, 5, 1, 3, 5, 16, noteBlockBasedrum);

        SetDoubleChest(builder, 5, FuelItems);

        builder.SetBlock(0, 6, 16, Minecraft.Blocks.Hopper.WithProperty("enabled", "true").WithFacing("down"));
        builder.SetBlock(4, 6, 16, Minecraft.Blocks.Hopper.WithProperty("enabled", "true").WithFacing("down"));
        builder.SetBlock(0, 6, 17, Minecraft.Blocks.Hopper.WithProperty("enabled", "true").WithFacing("north"));
        builder.Fill(1, 6, 17, 2, 6, 17, Minecraft.Blocks.Hopper.WithProperty("enabled", "true").WithFacing("west"));
        builder.SetBlock(3, 6, 17, Minecraft.Blocks.Hopper.WithProperty("enabled", "true").WithFacing("east"));
        builder.SetBlock(4, 6, 17, Minecraft.Blocks.Hopper.WithProperty("enabled", "true").WithFacing("north"));

        SetDoubleChest(builder, 7, InputItems);
    }

    private void PlaceRedstoneLogic(SchematicBuilder builder)
    {
        BlockState wireNone = CreateWire(north: "none", east: "none", south: "none", west: "none");
        BlockState wireNorthSouth = CreateWire(north: "side", east: "none", south: "side", west: "none");
        BlockState wireUpSouth = CreateWire(north: "up", east: "none", south: "side", west: "none");
        BlockState wireEastWest = CreateWire(north: "none", east: "side", south: "none", west: "side");

        // Output elevator sensor and pulse circuit (y = 0..2, z = 14..18)
        builder.SetBlock(2, 0, 14, CircuitBlock);
        builder.SetBlock(2, 0, 15, Minecraft.Blocks.RedstoneWallTorch.WithFacing("south").WithLit(true));
        builder.SetBlock(2, 0, 16, Minecraft.Blocks.NoteBlock.WithProperty("instrument", "harp").WithProperty("note", "0").WithPowered(true));
        builder.SetBlock(2, 0, 17, CircuitBlock);

        builder.SetBlock(2, 1, 14, wireNone);
        builder.SetBlock(2, 1, 15, GlassBlock);
        builder.SetBlock(2, 1, 16, Minecraft.Blocks.Observer.WithFacing("down").WithPowered(false));
        builder.SetBlock(2, 1, 17, Minecraft.Blocks.Observer.WithFacing("up").WithPowered(false));

        builder.SetBlock(2, 2, 14, CircuitBlock);
        builder.SetBlock(2, 2, 15, Minecraft.Blocks.Comparator.WithFacing("south").WithMode("compare").WithPowered(false));
        builder.Fill(5, 2, 16, 5, 2, 18, GlassBlock);
        builder.SetBlock(2, 2, 17, Minecraft.Blocks.Observer.WithFacing("north").WithPowered(false));
        builder.SetBlock(1, 2, 18, GlassBlock);
        builder.Fill(3, 2, 18, 4, 2, 18, GlassBlock);

        // Side signal ladder & front trigger bus (y = 3..4)
        builder.SetBlock(5, 3, 15, GlassBlock);
        builder.SetBlock(5, 3, 16, wireUpSouth);
        builder.SetBlock(5, 3, 17, wireNorthSouth);
        builder.SetBlock(0, 3, 18, CircuitBlock);
        builder.SetBlock(1, 3, 18, Minecraft.Blocks.Repeater.WithDelay(1).WithFacing("west").WithLocked(false).WithPowered(false));
        builder.SetBlock(2, 3, 18, CircuitBlock);
        builder.Fill(3, 3, 18, 4, 3, 18, wireEastWest);
        builder.SetBlock(5, 3, 18, CreateWire(north: "side", east: "none", south: "none", west: "side"));

        builder.SetBlock(5, 4, 15, CreateWire(north: "side", east: "none", south: "up", west: "none"));
        builder.Fill(5, 4, 16, 5, 4, 18, GlassBlock);
        builder.SetBlock(0, 4, 17, GlassBlock);
        builder.SetBlock(4, 4, 17, GlassBlock);
        builder.SetBlock(0, 4, 18, wireNone);

        // Middle note-block repeater spine and fuel detectors (y = 5..7)
        builder.Fill(2, 5, 1, 2, 5, 16, GlassBlock);
        builder.Fill(5, 5, 13, 5, 5, 15, GlassBlock);
        builder.SetBlock(5, 5, 16, wireUpSouth);
        builder.SetBlock(0, 5, 17, Minecraft.Blocks.Comparator.WithFacing("north").WithMode("compare").WithPowered(false));
        builder.SetBlock(4, 5, 17, Minecraft.Blocks.Comparator.WithFacing("north").WithMode("compare").WithPowered(false));
        builder.SetBlock(5, 5, 17, Minecraft.Blocks.Repeater.WithDelay(1).WithFacing("south").WithLocked(false).WithPowered(false));
        builder.SetBlock(0, 5, 18, CircuitBlock);
        builder.SetBlock(4, 5, 18, CircuitBlock);
        builder.SetBlock(5, 5, 18, wireNorthSouth);

        builder.Fill(1, 6, 1, 1, 6, 16, CircuitBlock);
        builder.Fill(2, 6, 1, 2, 6, 16, Minecraft.Blocks.Repeater.WithDelay(1).WithFacing("west").WithLocked(false).WithPowered(false));
        builder.Fill(3, 6, 1, 3, 6, 16, CircuitBlock);
        builder.SetBlock(5, 6, 12, GlassBlock);
        builder.SetBlock(5, 6, 13, wireUpSouth);
        builder.Fill(5, 6, 14, 5, 6, 15, wireNorthSouth);

        builder.Fill(1, 7, 1, 1, 7, 16, Minecraft.Blocks.Observer.WithFacing("up").WithPowered(false));
        builder.SetBlock(3, 7, 10, GlassBlock);
        builder.SetBlock(5, 7, 11, GlassBlock);
        builder.SetBlock(5, 7, 12, wireUpSouth);
        builder.Fill(2, 7, 13, 3, 7, 15, GlassBlock);
        builder.SetBlock(3, 7, 16, GlassBlock);

        // Top clock and trigger rails (y = 8..9)
        builder.SetBlock(
            1,
            8,
            1,
            IncludeDecoration
                ? Minecraft.Blocks.RedstoneLamp.WithLit(false)
                : CreateWire(north: "none", east: "none", south: "side", west: "none"));
        builder.Fill(1, 8, 2, 1, 8, 12, wireNorthSouth);
        builder.SetBlock(2, 8, 10, CircuitBlock);
        builder.SetBlock(3, 8, 10, CreateWire(north: "none", east: "side", south: "none", west: "side", power: 15));
        builder.SetBlock(4, 8, 10, Minecraft.Blocks.RedstoneWallTorch.WithFacing("west").WithLit(true));
        builder.SetBlock(5, 8, 10, CircuitBlock);
        builder.Fill(2, 8, 11, 3, 8, 11, GlassBlock);
        builder.SetBlock(5, 8, 11, wireNorthSouth);
        builder.Fill(2, 8, 12, 3, 8, 12, CircuitBlock);
        builder.SetBlock(1, 8, 16, wireNorthSouth);
        builder.SetBlock(2, 8, 16, CircuitBlock);

        PlaceDistributionClock(builder, wireNorthSouth);
    }

    private void PlaceDistributionClock(SchematicBuilder builder, BlockState wireNorthSouth)
    {
        builder.SetBlock(2, 9, 10, Minecraft.Blocks.RedstoneTorch.WithLit(false));

        if (ResolvedFastClock)
        {
            // Fast 4-redstone-tick subtract clock matching 2x Smoker/BlastFurnace speed (8 gt per trigger) with zero torch burnout.
            builder.Fill(1, 8, 13, 1, 8, 15, wireNorthSouth);
            builder.SetBlock(2, 8, 13, CircuitBlock);
            builder.SetBlock(3, 8, 13, GlassBlock);
            builder.Fill(2, 8, 14, 2, 8, 15, Minecraft.Blocks.Repeater.WithDelay(1).WithFacing("north").WithLocked(false).WithPowered(false));

            builder.SetBlock(2, 9, 11, Minecraft.Blocks.Comparator.WithFacing("north").WithMode("subtract").WithPowered(false));
            builder.SetBlock(3, 9, 11, CreateWire(north: "none", east: "none", south: "side", west: "side"));
            builder.SetBlock(2, 9, 12, wireNorthSouth);
            builder.SetBlock(3, 9, 12, Minecraft.Blocks.Repeater.WithDelay(3).WithFacing("south").WithLocked(false).WithPowered(false));
            builder.SetBlock(2, 9, 13, CreateWire(north: "side", east: "side", south: "none", west: "none"));
            builder.SetBlock(3, 9, 13, CreateWire(north: "side", east: "none", south: "none", west: "side"));
            return;
        }

        // Standard 10s-tuned 6-comparator fader loop matching regular Furnace smelting speed.
        builder.Fill(1, 8, 13, 1, 8, 15, CreateWire(north: "side", east: "side", south: "side", west: "none"));
        builder.Fill(2, 8, 13, 2, 8, 15, Minecraft.Blocks.Comparator.WithFacing("north").WithMode("compare").WithPowered(false));
        builder.Fill(3, 8, 13, 3, 8, 15, Minecraft.Blocks.Comparator.WithFacing("south").WithMode("compare").WithPowered(false));
        builder.SetBlock(3, 8, 16, wireNorthSouth);

        builder.SetBlock(3, 9, 10, CircuitBlock);
        builder.SetBlock(2, 9, 11, Minecraft.Blocks.Repeater.WithDelay(1).WithFacing("north").WithLocked(false).WithPowered(false));
        builder.SetBlock(3, 9, 11, Minecraft.Blocks.Repeater.WithDelay(3).WithFacing("south").WithLocked(false).WithPowered(false));
        builder.SetBlock(1, 9, 12, CircuitBlock);
        builder.SetBlock(2, 9, 12, CreateWire(north: "side", east: "side", south: "none", west: "none"));
        builder.SetBlock(3, 9, 12, CreateWire(north: "side", east: "none", south: "none", west: "side"));
    }

    private static void PlaceDecoration(SchematicBuilder builder)
    {
        BlockState composter = Minecraft.Blocks.Composter.WithLevel(0);
        builder.Fill(0, 3, 16, 1, 3, 16, composter);
        builder.Fill(3, 3, 16, 4, 3, 16, composter);
        builder.Fill(0, 7, 16, 0, 7, 17, composter);
        builder.Fill(4, 7, 16, 4, 7, 17, composter);
        builder.SetBlock(1, 7, 17, composter);
    }

    private static void SetDoubleChest(SchematicBuilder builder, int y, IReadOnlyList<ItemStack>? items)
    {
        ItemStack[] cloned = items is { Count: > 0 }
            ? items.Select(item => item.Clone()).ToArray()
            : [];
        builder.SetDoubleChest(2, y, 17, 3, y, 17, Direction.South, cloned);
    }

    private static BlockState CreateWire(string north, string east, string south, string west, int power = 0) =>
        Minecraft.Blocks.RedstoneWire
            .WithProperty("north", north)
            .WithProperty("east", east)
            .WithProperty("south", south)
            .WithProperty("west", west)
            .WithPower(power);

    private void ValidateConfiguration()
    {
        if (!Enum.IsDefined(MachineType))
        {
            throw new ArgumentOutOfRangeException(nameof(MachineType), MachineType, "MachineType must be Furnace, BlastFurnace, or Smoker.");
        }

        CircuitLayout.ValidateHorizontalFacing(Facing);
        CircuitLayout.ValidateBlockId(CircuitBlock, nameof(CircuitBlock));
        CircuitLayout.ValidateBlockId(GlassBlock, nameof(GlassBlock));
        ValidateDoubleChestItems(InputItems, nameof(InputItems));
        ValidateDoubleChestItems(FuelItems, nameof(FuelItems));
        ValidateDoubleChestItems(OutputItems, nameof(OutputItems));
    }

    private static void ValidateDoubleChestItems(IReadOnlyList<ItemStack>? items, string paramName)
    {
        if (items == null)
        {
            return;
        }

        foreach (var item in items)
        {
            ArgumentNullException.ThrowIfNull(item, paramName);

            if (item.Slot >= 54)
            {
                throw new ArgumentOutOfRangeException(paramName, item.Slot, "Double chest item slot must be between 0 and 53.");
            }

            if (item.Count is < 1 or > 64)
            {
                throw new ArgumentOutOfRangeException(paramName, item.Count, "Chest item count must be between 1 and 64.");
            }
        }
    }

    private (string BlockId, string BlockEntityId) GetMachineIdentifiers() => MachineType switch
    {
        SmelterType.Furnace => (Minecraft.Blocks.Furnace, Minecraft.BlockEntities.Furnace),
        SmelterType.BlastFurnace => (Minecraft.Blocks.BlastFurnace, Minecraft.BlockEntities.BlastFurnace),
        SmelterType.Smoker => (Minecraft.Blocks.Smoker, Minecraft.BlockEntities.Smoker),
        _ => throw new ArgumentOutOfRangeException(nameof(MachineType), MachineType, "Unsupported smelter type.")
    };

    private static Rotation GetFacingRotation(Direction facing) => facing switch
    {
        Direction.South => Rotation.None,
        Direction.West => Rotation.Clockwise90,
        Direction.North => Rotation.Clockwise180,
        Direction.East => Rotation.Clockwise270,
        _ => throw new ArgumentOutOfRangeException(nameof(facing), facing, "Facing must be horizontal.")
    };
}
