using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;
using SchematicBuilderMC.Extensions;
using SchematicBuilderMC.Models;
using SchematicBuilderMC.Registries;

namespace SchematicBuilderMC.Schematics.Farms;

/// <summary>
/// Generates a compact crop-farm schematic recipe for a selected <see cref="CropType"/>.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="CropType.Wheat"/>, <see cref="CropType.Carrots"/>, <see cref="CropType.Potatoes"/>, and
/// <see cref="CropType.Beetroots"/> use water-bucket dispensers at the back of the beds triggered by a lever to flush
/// mature crops into the front hopper and collection chest; these crops require manual replanting after harvesting.
/// </para>
/// <para>
/// <see cref="CropType.SugarCane"/> places dirt beds adjacent to the central water channel and equips both outer walls
/// with inward-facing observers, pistons, and rear redstone dust for automatic harvesting.
/// </para>
/// </remarks>
public sealed class CompactCropFarmSchematicCreator : SchematicCreator
{
    /// <summary>
    /// Crop type planted and harvested by this farm.
    /// </summary>
    public CropType Crop { get; set; } = CropType.Wheat;

    /// <summary>
    /// Length of each planting row in blocks (minimum 2).
    /// </summary>
    public int RowLength { get; set; } = 8;

    /// <summary>
    /// Horizontal direction the collection chest and crop beds face (<see cref="Direction.North"/>, <see cref="Direction.South"/>, <see cref="Direction.East"/>, or <see cref="Direction.West"/>).
    /// </summary>
    public Direction Facing { get; set; } = Direction.North;

    /// <summary>
    /// Block identifier used for the base floor, perimeter walls, and redstone support blocks.
    /// </summary>
    public string BorderBlock { get; set; } = Minecraft.Blocks.StoneBricks;

    /// <summary>
    /// Optional initial inventory items stored in the collection chest (slots 0..26, counts 1..64).
    /// </summary>
    public IReadOnlyList<ItemStack>? CollectionChestItems { get; set; }

    private int LateralSpan => Crop == CropType.SugarCane ? 7 : 5;

    private int DepthSpan => RowLength + 3;

    /// <summary>
    /// Total schematic width along the X axis.
    /// </summary>
    public int Width => Facing is Direction.North or Direction.South ? LateralSpan : DepthSpan;

    /// <summary>
    /// Total schematic height along the Y axis (5 for <see cref="CropType.SugarCane"/>, 4 for seed/root crops).
    /// </summary>
    public int Height => Crop == CropType.SugarCane ? 5 : 4;

    /// <summary>
    /// Total schematic length along the Z axis.
    /// </summary>
    public int Length => Facing is Direction.North or Direction.South ? DepthSpan : LateralSpan;

    /// <summary>
    /// Places the compact crop farm into an existing <see cref="SchematicBuilder"/> starting at (startX, startY, startZ).
    /// </summary>
    public SchematicBuilder Place(SchematicBuilder builder, int startX = 0, int startY = 0, int startZ = 0)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ValidateConfiguration();

        var schematic = builder.Build();
        if (startX < 0 || startY < 0 || startZ < 0 ||
            startX + Width > schematic.Width ||
            startY + Height > schematic.Height ||
            startZ + Length > schematic.Length)
        {
            throw new ArgumentOutOfRangeException(
                nameof(startX),
                $"A compact {Crop} farm ({Width}x{Height}x{Length}) facing {Facing} placed at ({startX}, {startY}, {startZ}) exceeds schematic bounds ({schematic.Width}x{schematic.Height}x{schematic.Length}).");
        }

        int centerU = LateralSpan / 2;
        int leftCropU = centerU - 1;
        int rightCropU = centerU + 1;
        int leftWallU = centerU - 2;
        int rightWallU = centerU + 2;
        int maxV = RowLength + 2;

        // Base floor (y = 0) across the farm bed and walls
        for (int v = 0; v <= maxV; v++)
        {
            for (int u = leftWallU; u <= rightWallU; u++)
            {
                builder.SetBlock(MapPosition(startX, startY, startZ, u, 0, v), BorderBlock);
            }
        }

        // Perimeter walls (y = 1..2)
        for (int y = 1; y <= 2; y++)
        {
            for (int v = 0; v <= maxV; v++)
            {
                builder.SetBlock(MapPosition(startX, startY, startZ, leftWallU, y, v), BorderBlock);
                builder.SetBlock(MapPosition(startX, startY, startZ, rightWallU, y, v), BorderBlock);
            }

            for (int u = leftWallU; u <= rightWallU; u++)
            {
                builder.SetBlock(MapPosition(startX, startY, startZ, u, y, maxV), BorderBlock);
            }
        }

        // Front border at v = 0, y = 1 (leaving centerU open for collection chest)
        foreach (int u in new[] { leftWallU, leftCropU, rightCropU, rightWallU })
        {
            builder.SetBlock(MapPosition(startX, startY, startZ, u, 1, 0), BorderBlock);
        }

        // Collection chest & hopper
        var chestPos = MapPosition(startX, startY, startZ, centerU, 1, 0);
        builder.SetBlock(chestPos, Minecraft.Blocks.Chest.WithFacing(Facing));
        if (CollectionChestItems is { Count: > 0 })
        {
            builder.SetChest(chestPos, CollectionChestItems.Select(item => item.Clone()).ToArray());
        }
        else
        {
            builder.SetChest(chestPos);
        }

        builder.SetBlock(MapPosition(startX, startY, startZ, centerU, 1, 1), Minecraft.Blocks.Hopper.WithFacing(Facing));

        // Central hydration water channel (v = 2 .. RowLength + 1)
        var waterSource = Minecraft.Blocks.Water.WithLevel(0);
        for (int v = 2; v <= RowLength + 1; v++)
        {
            builder.SetBlock(MapPosition(startX, startY, startZ, centerU, 1, v), waterSource);
        }

        // Planting rows (v = 1 .. RowLength)
        var soilState = Crop == CropType.SugarCane
            ? new BlockState(Minecraft.Blocks.Dirt)
            : Minecraft.Blocks.Farmland.WithProperty("moisture", "7");
        string cropBlockId = GetCropBlockId();

        for (int v = 1; v <= RowLength; v++)
        {
            foreach (int cropU in new[] { leftCropU, rightCropU })
            {
                builder.SetBlock(MapPosition(startX, startY, startZ, cropU, 1, v), soilState);
                builder.SetBlock(MapPosition(startX, startY, startZ, cropU, 2, v), cropBlockId);
            }
        }

        if (Crop == CropType.SugarCane)
        {
            var (inwardLeft, inwardRight) = GetInwardFacings();
            for (int v = 1; v <= RowLength; v++)
            {
                builder.SetPiston(MapPosition(startX, startY, startZ, leftWallU, 3, v), inwardLeft, sticky: false, extended: false);
                builder.SetPiston(MapPosition(startX, startY, startZ, rightWallU, 3, v), inwardRight, sticky: false, extended: false);

                builder.SetBlock(MapPosition(startX, startY, startZ, leftWallU, 4, v), Minecraft.Blocks.Observer.WithFacing(inwardLeft));
                builder.SetBlock(MapPosition(startX, startY, startZ, rightWallU, 4, v), Minecraft.Blocks.Observer.WithFacing(inwardRight));

                builder.SetBlock(MapPosition(startX, startY, startZ, 0, 3, v), BorderBlock);
                builder.SetBlock(MapPosition(startX, startY, startZ, 6, 3, v), BorderBlock);

                builder.SetRedstoneWire(MapPosition(startX, startY, startZ, 0, 4, v), power: 0);
                builder.SetRedstoneWire(MapPosition(startX, startY, startZ, 6, 4, v), power: 0);
            }
        }
        else
        {
            // Back ledge & water-bucket dispensers for flush harvesting
            builder.SetBlock(MapPosition(startX, startY, startZ, leftCropU, 1, RowLength + 1), BorderBlock);
            builder.SetBlock(MapPosition(startX, startY, startZ, rightCropU, 1, RowLength + 1), BorderBlock);
            builder.SetBlock(MapPosition(startX, startY, startZ, centerU, 2, RowLength + 1), BorderBlock);

            foreach (int dispenserU in new[] { leftCropU, rightCropU })
            {
                var dispenserPos = MapPosition(startX, startY, startZ, dispenserU, 2, RowLength + 1);
                builder.SetBlock(dispenserPos, Minecraft.Blocks.Dispenser.WithFacing(Facing));
                builder.SetContainer(
                    Minecraft.Blocks.Dispenser,
                    Minecraft.BlockEntities.Dispenser,
                    dispenserPos,
                    new ItemStack(Minecraft.Items.WaterBucket, count: 1, slot: 0));
            }

            for (int u = leftCropU; u <= rightCropU; u++)
            {
                builder.SetRedstoneWire(MapPosition(startX, startY, startZ, u, 3, RowLength + 1), power: 0);
            }

            builder.SetLever(MapPosition(startX, startY, startZ, centerU, 3, maxV), BlockFace.Floor, Facing, powered: false);
        }

        return builder;
    }

    /// <summary>
    /// Creates a new schematic containing this compact crop farm.
    /// </summary>
    public override SchematicBuilder Create()
    {
        ValidateConfiguration();

        var builder = SchematicBuilder.Create(Width, Height, Length)
            .WithOffset(0, 0, 0)
            .WithName($"Compact {Crop} Farm")
            .WithAuthor("SchematicBuilderMC")
            .WithDescription($"A compact {Crop} farm (row length {RowLength}) facing {Facing}.");

        return Place(builder, 0, 0, 0);
    }

    private void ValidateConfiguration()
    {
        if (!Enum.IsDefined(Crop))
        {
            throw new ArgumentOutOfRangeException(nameof(Crop), Crop, "Unsupported crop type.");
        }

        if (RowLength < 2)
        {
            throw new ArgumentOutOfRangeException(nameof(RowLength), RowLength, "RowLength must be at least 2.");
        }

        if (Facing is not (Direction.North or Direction.South or Direction.East or Direction.West))
        {
            throw new ArgumentOutOfRangeException(nameof(Facing), Facing, "Facing must be a horizontal direction (North, South, East, or West).");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(BorderBlock);

        if (CollectionChestItems != null)
        {
            foreach (var item in CollectionChestItems)
            {
                ArgumentNullException.ThrowIfNull(item, nameof(CollectionChestItems));

                if (item.Slot is >= 27)
                {
                    throw new ArgumentOutOfRangeException(nameof(CollectionChestItems), item.Slot, "Chest item slot must be between 0 and 26.");
                }

                if (item.Count is < 1 or > 64)
                {
                    throw new ArgumentOutOfRangeException(nameof(CollectionChestItems), item.Count, "Chest item count must be between 1 and 64.");
                }
            }
        }
    }

    private string GetCropBlockId() => Crop switch
    {
        CropType.Wheat => Minecraft.Blocks.Wheat,
        CropType.Carrots => Minecraft.Blocks.Carrots,
        CropType.Potatoes => Minecraft.Blocks.Potatoes,
        CropType.Beetroots => Minecraft.Blocks.Beetroots,
        CropType.SugarCane => Minecraft.Blocks.SugarCane,
        _ => throw new ArgumentOutOfRangeException(nameof(Crop), Crop, "Unsupported crop type.")
    };

    private (Direction InwardLeft, Direction InwardRight) GetInwardFacings() => Facing switch
    {
        Direction.North => (Direction.East, Direction.West),
        Direction.South => (Direction.West, Direction.East),
        Direction.East => (Direction.South, Direction.North),
        Direction.West => (Direction.North, Direction.South),
        _ => throw new ArgumentOutOfRangeException(nameof(Facing), Facing, "Facing must be horizontal.")
    };

    private BlockPosition MapPosition(int startX, int startY, int startZ, int u, int y, int v)
    {
        int maxU = LateralSpan - 1;
        int maxV = DepthSpan - 1;

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
