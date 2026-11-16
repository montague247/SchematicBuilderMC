using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;
using SchematicBuilderMC.Extensions;
using SchematicBuilderMC.Models;
using SchematicBuilderMC.Registries;

namespace SchematicBuilderMC.Schematics.Contraptions;

/// <summary>
/// Generates a tileable single-item sorter filter module schematic, with support for composing multiple modules into a row.
/// </summary>
public sealed class ItemSortingModuleSchematicCreator : SchematicCreator
{
    /// <summary>
    /// Horizontal direction the front storage chest faces (<see cref="Direction.North"/>, <see cref="Direction.South"/>, <see cref="Direction.East"/>, or <see cref="Direction.West"/>).
    /// </summary>
    public Direction Facing { get; set; } = Direction.North;

    /// <summary>
    /// Number of adjacent sorting module slices to place side by side (minimum 1).
    /// </summary>
    public int ModuleCount { get; set; } = 1;

    /// <summary>
    /// Item identifiers filtered by each module slice (cycled if fewer items than <see cref="ModuleCount"/> are provided).
    /// </summary>
    public IReadOnlyList<string> FilterItems { get; set; } = [Minecraft.Items.Diamond];

    /// <summary>
    /// Item identifier used to fill filter hopper slots 1 through 4.
    /// </summary>
    public string FillerItemId { get; set; } = Minecraft.Items.Stick;

    /// <summary>
    /// Stack size placed in slot 0 of each filter hopper (1 to 64, default 41 for standard impulse sorters).
    /// </summary>
    public int FilterItemCount { get; set; } = 41;

    /// <summary>
    /// Optional initial inventory items stored in each module's storage chest (slots 0..26, counts 1..64).
    /// </summary>
    public IReadOnlyList<ItemStack>? ChestItems { get; set; }

    /// <summary>
    /// Block identifier used for the redstone circuit support blocks.
    /// </summary>
    public string CircuitBlock { get; set; } = Minecraft.Blocks.SmoothStone;

    /// <summary>
    /// Total schematic width along the X axis.
    /// </summary>
    public int Width => Facing is Direction.North or Direction.South ? ModuleCount : 6;

    /// <summary>
    /// Total schematic height along the Y axis (4 blocks).
    /// </summary>
    public int Height => 4;

    /// <summary>
    /// Total schematic length along the Z axis.
    /// </summary>
    public int Length => Facing is Direction.North or Direction.South ? 6 : ModuleCount;

    /// <summary>
    /// Places one or more item-sorting modules into an existing <see cref="SchematicBuilder"/> starting at (startX, startY, startZ).
    /// </summary>
    public SchematicBuilder Place(SchematicBuilder builder, int startX = 0, int startY = 0, int startZ = 0)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ValidateConfiguration();

        if (startX < 0 || startY < 0 || startZ < 0 ||
            startX + Width > builder.Width ||
            startY + Height > builder.Height ||
            startZ + Length > builder.Length)
        {
            throw new ArgumentOutOfRangeException(
                nameof(startX),
                $"An item-sorting row ({Width}x{Height}x{Length}) facing {Facing} placed at ({startX}, {startY}, {startZ}) exceeds schematic bounds ({builder.Width}x{builder.Height}x{builder.Length}).");
        }

        Direction oppositeFacing = GetOppositeFacing(Facing);
        Direction lateralFacing = GetLateralFacing(Facing);

        for (int m = 0; m < ModuleCount; m++)
        {
            string filterItem = FilterItems[m % FilterItems.Count];

            // y = 0: storage chest, output hopper into chest, and rear circuit block
            var chestPos = MapPosition(startX, startY, startZ, m, 0, 0);
            builder.SetBlock(chestPos, Minecraft.Blocks.Chest.WithFacing(Facing));
            if (ChestItems is { Count: > 0 })
            {
                var clonedChestItems = ChestItems.Select(item => item.Clone()).ToArray();
                builder.SetChest(chestPos, clonedChestItems);
            }
            else
            {
                builder.SetChest(chestPos);
            }

            var outputHopperPos = MapPosition(startX, startY, startZ, m, 0, 1);
            builder.SetBlock(outputHopperPos, Minecraft.Blocks.Hopper.WithFacing(Facing));
            builder.SetBlock(MapPosition(startX, startY, startZ, m, 0, 3), CircuitBlock);

            // y = 1: locking redstone wall torch, repeater, and support blocks
            builder.SetBlock(MapPosition(startX, startY, startZ, m, 1, 1), Minecraft.Blocks.RedstoneWallTorch.WithFacing(Facing));
            builder.SetBlock(MapPosition(startX, startY, startZ, m, 1, 2), CircuitBlock);
            builder.SetRepeater(MapPosition(startX, startY, startZ, m, 1, 3), Facing, delay: 1, powered: false);
            builder.SetBlock(MapPosition(startX, startY, startZ, m, 1, 4), CircuitBlock);

            // y = 2: filter hopper facing into comparator support + circuit blocks
            var filterHopperPos = MapPosition(startX, startY, startZ, m, 2, 1);
            builder.SetBlock(filterHopperPos, Minecraft.Blocks.Hopper.WithFacing(oppositeFacing));

            var filterInventory = new ItemStack[5];
            filterInventory[0] = new ItemStack(filterItem, FilterItemCount, slot: 0);
            for (byte slot = 1; slot <= 4; slot++)
            {
                filterInventory[slot] = new ItemStack(FillerItemId, 1, slot: slot).WithDisplayName("Filter");
            }
            builder.SetContainer(Minecraft.Blocks.Hopper, Minecraft.BlockEntities.Hopper, filterHopperPos, filterInventory);

            for (int v = 2; v <= 5; v++)
            {
                builder.SetBlock(MapPosition(startX, startY, startZ, m, 2, v), CircuitBlock);
            }

            // y = 3: input stream hopper, comparator reading filter hopper, and redstone dust
            builder.SetBlock(MapPosition(startX, startY, startZ, m, 3, 1), Minecraft.Blocks.Hopper.WithFacing(lateralFacing));
            builder.SetComparator(MapPosition(startX, startY, startZ, m, 3, 2), oppositeFacing, subtractMode: false, powered: false);
            for (int v = 3; v <= 5; v++)
            {
                builder.SetRedstoneWire(MapPosition(startX, startY, startZ, m, 3, v), power: 0);
            }
        }

        return builder;
    }

    /// <summary>
    /// Creates a new schematic containing the configured item-sorting module(s).
    /// </summary>
    public override SchematicBuilder Create()
    {
        ValidateConfiguration();

        var builder = SchematicBuilder.Create(Width, Height, Length)
            .WithRotation(Rotation)
            .WithOffset(0, 0, 0)
            .WithName("Item-Sorting Module")
            .WithAuthor("SchematicBuilderMC")
            .WithDescription($"An item-sorting module row ({ModuleCount} module(s)) facing {Facing}.");

        return Place(builder, 0, 0, 0);
    }

    private void ValidateConfiguration()
    {
        if (Facing is not (Direction.North or Direction.South or Direction.East or Direction.West))
        {
            throw new ArgumentOutOfRangeException(nameof(Facing), Facing, "Facing must be a horizontal direction (North, South, East, or West).");
        }

        if (ModuleCount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(ModuleCount), ModuleCount, "ModuleCount must be at least 1.");
        }

        if (FilterItemCount is < 1 or > 64)
        {
            throw new ArgumentOutOfRangeException(nameof(FilterItemCount), FilterItemCount, "FilterItemCount must be between 1 and 64.");
        }

        if (FilterItems == null || FilterItems.Count == 0 || FilterItems.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("FilterItems must contain at least one valid item identifier.", nameof(FilterItems));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(FillerItemId);
        ArgumentException.ThrowIfNullOrWhiteSpace(CircuitBlock);

        if (ChestItems != null)
        {
            foreach (var item in ChestItems)
            {
                ArgumentNullException.ThrowIfNull(item, nameof(ChestItems));

                if (item.Slot is >= 27)
                {
                    throw new ArgumentOutOfRangeException(nameof(ChestItems), item.Slot, "Chest item slot must be between 0 and 26.");
                }

                if (item.Count is < 1 or > 64)
                {
                    throw new ArgumentOutOfRangeException(nameof(ChestItems), item.Count, "Chest item count must be between 1 and 64.");
                }
            }
        }
    }

    private static Direction GetOppositeFacing(Direction facing) => facing switch
    {
        Direction.North => Direction.South,
        Direction.South => Direction.North,
        Direction.East => Direction.West,
        Direction.West => Direction.East,
        _ => throw new ArgumentOutOfRangeException(nameof(facing), facing, "Facing must be horizontal.")
    };

    private static Direction GetLateralFacing(Direction facing) => facing switch
    {
        Direction.North => Direction.East,
        Direction.South => Direction.West,
        Direction.East => Direction.South,
        Direction.West => Direction.North,
        _ => throw new ArgumentOutOfRangeException(nameof(facing), facing, "Facing must be horizontal.")
    };

    private BlockPosition MapPosition(int startX, int startY, int startZ, int u, int y, int v)
    {
        int maxU = ModuleCount - 1;
        const int maxV = 5;

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
