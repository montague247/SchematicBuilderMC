using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;
using SchematicBuilderMC.Extensions;
using SchematicBuilderMC.Models;
using SchematicBuilderMC.Registries;

namespace SchematicBuilderMC.Schematics.Contraptions;

/// <summary>
/// Generates a compact automatic furnace, blast furnace, or smoker schematic with input, fuel, and output chests connected by hoppers.
/// </summary>
public sealed class AutomaticSmelterSchematicCreator : SchematicCreator
{
    /// <summary>
    /// Processing machine block type (<see cref="SmelterType.Furnace"/>, <see cref="SmelterType.BlastFurnace"/>, or <see cref="SmelterType.Smoker"/>).
    /// </summary>
    public SmelterType MachineType { get; set; } = SmelterType.Furnace;

    /// <summary>
    /// Horizontal direction the machine and chests face (<see cref="Direction.North"/>, <see cref="Direction.South"/>, <see cref="Direction.East"/>, or <see cref="Direction.West"/>).
    /// </summary>
    public Direction Facing { get; set; } = Direction.North;

    /// <summary>
    /// Optional initial items placed in the top input chest (slots 0..26, counts 1..64).
    /// </summary>
    public IReadOnlyList<ItemStack>? InputItems { get; set; }

    /// <summary>
    /// Optional initial items placed in the side fuel chest (slots 0..26, counts 1..64).
    /// </summary>
    public IReadOnlyList<ItemStack>? FuelItems { get; set; }

    /// <summary>
    /// Optional initial items placed in the bottom output chest (slots 0..26, counts 1..64).
    /// </summary>
    public IReadOnlyList<ItemStack>? OutputItems { get; set; }

    /// <summary>
    /// Total schematic width along the X axis (2 blocks).
    /// </summary>
    public int Width => 2;

    /// <summary>
    /// Total schematic height along the Y axis (4 blocks).
    /// </summary>
    public int Height => 4;

    /// <summary>
    /// Total schematic length along the Z axis (2 blocks).
    /// </summary>
    public int Length => 2;

    /// <summary>
    /// Places the automatic smelter into an existing <see cref="SchematicBuilder"/> starting at (startX, startY, startZ).
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
                $"An automatic smelter ({Width}x{Height}x{Length}) placed at ({startX}, {startY}, {startZ}) exceeds schematic bounds ({builder.Width}x{builder.Height}x{builder.Length}).");
        }

        var (machineBlockId, machineBlockEntityId) = GetMachineIdentifiers();
        Direction fuelHopperFacing = GetFuelHopperFacing(Facing);

        // Output chest (u = 0, y = 0, v = 0) and output hopper (u = 0, y = 0, v = 1)
        PlaceChest(builder, MapPosition(startX, startY, startZ, 0, 0, 0), OutputItems);
        builder.SetBlock(MapPosition(startX, startY, startZ, 0, 0, 1), Minecraft.Blocks.Hopper.WithFacing(Facing));

        // Processing block (u = 0, y = 1, v = 1)
        var machinePos = MapPosition(startX, startY, startZ, 0, 1, 1);
        builder.SetBlock(machinePos, machineBlockId.WithFacing(Facing));
        builder.SetContainer(machineBlockId, machineBlockEntityId, machinePos);

        // Fuel hopper into side of processing block (u = 1, y = 1, v = 1) and fuel chest above it (u = 1, y = 2, v = 1)
        builder.SetBlock(MapPosition(startX, startY, startZ, 1, 1, 1), Minecraft.Blocks.Hopper.WithFacing(fuelHopperFacing));
        PlaceChest(builder, MapPosition(startX, startY, startZ, 1, 2, 1), FuelItems);

        // Input hopper down into top of processing block (u = 0, y = 2, v = 1) and input chest above it (u = 0, y = 3, v = 1)
        builder.SetBlock(MapPosition(startX, startY, startZ, 0, 2, 1), Minecraft.Blocks.Hopper.WithFacing(Direction.Down));
        PlaceChest(builder, MapPosition(startX, startY, startZ, 0, 3, 1), InputItems);

        return builder;
    }

    /// <summary>
    /// Creates a new schematic containing this automatic smelter.
    /// </summary>
    public override SchematicBuilder Create()
    {
        ValidateConfiguration();

        var builder = SchematicBuilder.Create(Width, Height, Length)
            .WithRotation(Rotation)
            .WithOffset(0, 0, 0)
            .WithName($"Automatic {MachineType}")
            .WithAuthor("SchematicBuilderMC")
            .WithDescription($"A compact automatic {MachineType} smelter facing {Facing}.");

        return Place(builder, 0, 0, 0);
    }

    private void PlaceChest(SchematicBuilder builder, BlockPosition position, IReadOnlyList<ItemStack>? items)
    {
        builder.SetBlock(position, Minecraft.Blocks.Chest.WithFacing(Facing));
        if (items is { Count: > 0 })
        {
            builder.SetChest(position, items.Select(item => item.Clone()).ToArray());
        }
        else
        {
            builder.SetChest(position);
        }
    }

    private void ValidateConfiguration()
    {
        if (!Enum.IsDefined(MachineType))
        {
            throw new ArgumentOutOfRangeException(nameof(MachineType), MachineType, "MachineType must be Furnace, BlastFurnace, or Smoker.");
        }

        if (Facing is not (Direction.North or Direction.South or Direction.East or Direction.West))
        {
            throw new ArgumentOutOfRangeException(nameof(Facing), Facing, "Facing must be a horizontal direction (North, South, East, or West).");
        }

        ValidateChestItems(InputItems, nameof(InputItems));
        ValidateChestItems(FuelItems, nameof(FuelItems));
        ValidateChestItems(OutputItems, nameof(OutputItems));
    }

    private static void ValidateChestItems(IReadOnlyList<ItemStack>? items, string paramName)
    {
        if (items == null)
            return;

        foreach (var item in items)
        {
            ArgumentNullException.ThrowIfNull(item, paramName);

            if (item.Slot is >= 27)
            {
                throw new ArgumentOutOfRangeException(paramName, item.Slot, "Chest item slot must be between 0 and 26.");
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

    private static Direction GetFuelHopperFacing(Direction facing) => facing switch
    {
        Direction.North => Direction.West,
        Direction.South => Direction.East,
        Direction.East => Direction.North,
        Direction.West => Direction.South,
        _ => throw new ArgumentOutOfRangeException(nameof(facing), facing, "Facing must be horizontal.")
    };

    private BlockPosition MapPosition(int startX, int startY, int startZ, int u, int y, int v)
    {
        return Facing switch
        {
            Direction.North => new BlockPosition(startX + u, startY + y, startZ + v),
            Direction.South => new BlockPosition(startX + (1 - u), startY + y, startZ + (1 - v)),
            Direction.East => new BlockPosition(startX + (1 - v), startY + y, startZ + u),
            Direction.West => new BlockPosition(startX + v, startY + y, startZ + (1 - u)),
            _ => throw new ArgumentOutOfRangeException(nameof(Facing), Facing, "Facing must be horizontal.")
        };
    }
}
