using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;
using SchematicBuilderMC.Extensions;
using SchematicBuilderMC.Generators;
using SchematicBuilderMC.Registries;

namespace SchematicBuilderMC.Models;

/// <summary>
/// Represents a Minecraft schematic in memory, with 3D block data, block entities, entities, and metadata.
/// </summary>
public sealed class Schematic
{
    private readonly BlockState[] _blocks;
    private readonly Dictionary<BlockPosition, BlockEntity> _blockEntities = [];
    private readonly List<Entity> _entities = [];

    /// <summary>
    /// Size along the X axis.
    /// </summary>
    public int Width { get; }

    /// <summary>
    /// Size along the Y axis.
    /// </summary>
    public int Height { get; }

    /// <summary>
    /// Size along the Z axis.
    /// </summary>
    public int Length { get; }

    /// <summary>
    /// Total block count (Width * Height * Length).
    /// </summary>
    public int TotalBlocks { get; }

    /// <summary>
    /// Placement offset relative to origin.
    /// </summary>
    public BlockPosition Offset { get; set; } = BlockPosition.Zero;

    /// <summary>
    /// Minecraft DataVersion (e.g., 3465 for 1.20.1, or null if unspecified).
    /// </summary>
    public int? DataVersion { get; set; }

    /// <summary>
    /// Schematic metadata such as author, name, date, and description.
    /// </summary>
    public SchematicMetadata Metadata { get; set; } = new();

    /// <summary>
    /// Collection of all block entities (tile entities) in the schematic.
    /// </summary>
    public IReadOnlyCollection<BlockEntity> BlockEntities => _blockEntities.Values;

    /// <summary>
    /// Collection of all entities in the schematic.
    /// </summary>
    public IReadOnlyList<Entity> Entities => _entities;

    public Schematic(int width, int height, int length)
    {
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width), width, "Width must be greater than zero.");
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height), height, "Height must be greater than zero.");
        if (length <= 0) throw new ArgumentOutOfRangeException(nameof(length), length, "Length must be greater than zero.");

        int totalBlocks;
        try
        {
            totalBlocks = checked(width * height * length);
        }
        catch (OverflowException ex)
        {
            throw new ArgumentOutOfRangeException(
                $"Schematic volume ({width} x {height} x {length}) overflows Int32 and exceeds the maximum supported block count ({Array.MaxLength}).",
                ex);
        }

        if (totalBlocks > Array.MaxLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(width),
                totalBlocks,
                $"Schematic volume ({totalBlocks}) exceeds the maximum supported array capacity ({Array.MaxLength}).");
        }

        Width = width;
        Height = height;
        Length = length;
        TotalBlocks = totalBlocks;

        _blocks = new BlockState[totalBlocks];
        Array.Fill(_blocks, BlockState.Air);
    }

    /// <summary>
    /// Gets or sets the block state at (x, y, z).
    /// </summary>
    public BlockState this[int x, int y, int z]
    {
        get => GetBlock(x, y, z);
        set => SetBlock(x, y, z, value);
    }

    /// <summary>
    /// Gets or sets the block state at the given position.
    /// </summary>
    public BlockState this[BlockPosition pos]
    {
        get => GetBlock(pos.X, pos.Y, pos.Z);
        set => SetBlock(pos.X, pos.Y, pos.Z, value);
    }

    /// <summary>
    /// Gets the block state at the specified coordinate.
    /// </summary>
    public BlockState GetBlock(int x, int y, int z)
    {
        EnsureInBounds(x, y, z);
        return _blocks[GetIndex(x, y, z)];
    }

    /// <summary>
    /// Sets the block state at the specified coordinate.
    /// </summary>
    public void SetBlock(int x, int y, int z, BlockState blockState)
    {
        ArgumentNullException.ThrowIfNull(blockState);
        EnsureInBounds(x, y, z);
        _blocks[GetIndex(x, y, z)] = blockState;

        if (_blockEntities.Count > 0)
        {
            var pos = new BlockPosition(x, y, z);
            if (_blockEntities.TryGetValue(pos, out var existingEntity) &&
                !CanHostBlockEntity(blockState, existingEntity))
            {
                _blockEntities.Remove(pos);
            }
        }
    }

    /// <summary>
    /// Sets the block state at the specified coordinate from a blockstate string.
    /// </summary>
    public void SetBlock(int x, int y, int z, string blockStateString)
    {
        SetBlock(x, y, z, BlockState.Parse(blockStateString));
    }

    /// <summary>
    /// Sets the block state at the specified position.
    /// </summary>
    public void SetBlock(BlockPosition position, BlockState blockState) =>
        SetBlock(position.X, position.Y, position.Z, blockState);

    /// <summary>
    /// Sets the block state at the specified position from a blockstate string.
    /// </summary>
    public void SetBlock(BlockPosition position, string blockStateString) =>
        SetBlock(position.X, position.Y, position.Z, blockStateString);

    /// <summary>
    /// Attempts to get the block state at (x, y, z), returning false if coordinates are out of bounds.
    /// </summary>
    public bool TryGetBlock(int x, int y, int z, out BlockState blockState)
    {
        if (!IsInBounds(x, y, z))
        {
            blockState = BlockState.Air;
            return false;
        }

        blockState = _blocks[GetIndex(x, y, z)];
        return true;
    }

    /// <summary>
    /// Returns true if coordinates are within the schematic bounds.
    /// </summary>
    public bool IsInBounds(int x, int y, int z)
    {
        return x >= 0 && x < Width &&
               y >= 0 && y < Height &&
               z >= 0 && z < Length;
    }

    /// <summary>
    /// Gets a block entity at the specified coordinates, if present.
    /// </summary>
    public BlockEntity? GetBlockEntity(int x, int y, int z)
    {
        return GetBlockEntity(new BlockPosition(x, y, z));
    }

    /// <summary>
    /// Gets a block entity at the specified position, if present.
    /// </summary>
    public BlockEntity? GetBlockEntity(BlockPosition position)
    {
        _blockEntities.TryGetValue(position, out var entity);
        return entity;
    }

    /// <summary>
    /// Sets or updates a block entity at its position.
    /// </summary>
    public void SetBlockEntity(BlockEntity blockEntity)
    {
        ArgumentNullException.ThrowIfNull(blockEntity);
        EnsureInBounds(blockEntity.Position.X, blockEntity.Position.Y, blockEntity.Position.Z);
        _blockEntities[blockEntity.Position] = blockEntity;
    }

    /// <summary>
    /// Removes the block entity at the specified coordinate.
    /// </summary>
    public bool RemoveBlockEntity(int x, int y, int z)
    {
        return _blockEntities.Remove(new BlockPosition(x, y, z));
    }

    /// <summary>
    /// Sets a chest block and its associated block entity with optional inventory items at (x, y, z).
    /// If the block at the position is Air, it is set to a standard Chest block.
    /// </summary>
    public BlockEntity SetChest(int x, int y, int z, params ItemStack[] items)
    {
        EnsureInBounds(x, y, z);
        var currentBlock = GetBlock(x, y, z);
        if (currentBlock.IsAir || !currentBlock.Identifier.EndsWith("chest", StringComparison.OrdinalIgnoreCase))
        {
            SetBlock(x, y, z, Minecraft.Blocks.Chest);
        }

        var pos = new BlockPosition(x, y, z);
        var be = GetBlockEntity(pos);
        if (be == null || !be.Id.EndsWith("chest", StringComparison.OrdinalIgnoreCase))
        {
            be = BlockEntity.CreateChest(pos, items);
            SetBlockEntity(be);
        }
        else if (items != null)
        {
            foreach (var item in items)
            {
                be.AddItem(item);
            }
        }

        return be;
    }

    /// <summary>
    /// Sets a chest block and its associated block entity with optional inventory items at the specified position.
    /// </summary>
    public BlockEntity SetChest(BlockPosition position, params ItemStack[] items) =>
        SetChest(position.X, position.Y, position.Z, items);

    /// <summary>
    /// Sets a chest block and its associated block entity with inventory items at (x, y, z).
    /// </summary>
    public BlockEntity SetChest(int x, int y, int z, IEnumerable<ItemStack> items) =>
        SetChest(x, y, z, items?.ToArray() ?? Array.Empty<ItemStack>());

    /// <summary>
    /// Sets a chest block and its associated block entity with inventory items at the specified position.
    /// </summary>
    public BlockEntity SetChest(BlockPosition position, IEnumerable<ItemStack> items) =>
        SetChest(position.X, position.Y, position.Z, items);

    /// <summary>
    /// Sets a double chest starting at (x, y, z) with the specified facing direction.
    /// (x, y, z) becomes the right half, and the adjacent connected block becomes the left half.
    /// Items with slot 0..26 are placed in the right half; items with slot 27..53 are placed in the left half (with adjusted slot 0..26).
    /// </summary>
    public (BlockEntity Right, BlockEntity Left) SetDoubleChest(int x, int y, int z, Direction facing = Direction.North, params ItemStack[] items)
    {
        var (leftX, leftY, leftZ) = GetDoubleChestLeftOffset(x, y, z, facing);
        return SetDoubleChest(x, y, z, leftX, leftY, leftZ, facing, (IEnumerable<ItemStack>)items);
    }

    /// <summary>
    /// Sets a double chest starting at the specified position with the specified facing direction.
    /// </summary>
    public (BlockEntity Right, BlockEntity Left) SetDoubleChest(BlockPosition position, Direction facing = Direction.North, params ItemStack[] items) =>
        SetDoubleChest(position.X, position.Y, position.Z, facing, items);

    /// <summary>
    /// Sets a double chest starting at (x, y, z) with the specified facing direction.
    /// </summary>
    public (BlockEntity Right, BlockEntity Left) SetDoubleChest(int x, int y, int z, Direction facing, IEnumerable<ItemStack> items)
    {
        var (leftX, leftY, leftZ) = GetDoubleChestLeftOffset(x, y, z, facing);
        return SetDoubleChest(x, y, z, leftX, leftY, leftZ, facing, items);
    }

    /// <summary>
    /// Sets a double chest starting at the specified position with the specified facing direction.
    /// </summary>
    public (BlockEntity Right, BlockEntity Left) SetDoubleChest(BlockPosition position, Direction facing, IEnumerable<ItemStack> items) =>
        SetDoubleChest(position.X, position.Y, position.Z, facing, items);

    /// <summary>
    /// Sets a double chest with explicitly specified coordinates for the right and left halves and facing direction.
    /// Items with slot 0..26 are placed in the right half; items with slot 27..53 are placed in the left half (with adjusted slot 0..26).
    /// </summary>
    public (BlockEntity Right, BlockEntity Left) SetDoubleChest(int rightX, int rightY, int rightZ, int leftX, int leftY, int leftZ, Direction facing = Direction.North, params ItemStack[] items) =>
        SetDoubleChest(rightX, rightY, rightZ, leftX, leftY, leftZ, facing, (IEnumerable<ItemStack>)items);

    /// <summary>
    /// Sets a double chest with explicitly specified coordinates for the right and left halves and facing direction.
    /// Items with slot 0..26 are placed in the right half; items with slot 27..53 are placed in the left half (with adjusted slot 0..26).
    /// </summary>
    public (BlockEntity Right, BlockEntity Left) SetDoubleChest(int rightX, int rightY, int rightZ, int leftX, int leftY, int leftZ, Direction facing, IEnumerable<ItemStack>? items)
    {
        EnsureInBounds(rightX, rightY, rightZ);
        EnsureInBounds(leftX, leftY, leftZ);

        string facingStr = facing.ToStateString();

        // 1. Set right half block state
        var rightState = Minecraft.Blocks.Chest
            .WithFacing(facingStr)
            .WithChestType(ChestType.Right);
        SetBlock(rightX, rightY, rightZ, rightState);

        // 2. Set left half block state
        var leftState = Minecraft.Blocks.Chest
            .WithFacing(facingStr)
            .WithChestType(ChestType.Left);
        SetBlock(leftX, leftY, leftZ, leftState);

        // 3. Partition items into right (0..26) and left (27..53 -> 0..26)
        var rightItems = new List<ItemStack>();
        var leftItems = new List<ItemStack>();

        if (items != null)
        {
            foreach (var item in items)
            {
                if (item.Slot >= 27 && item.Slot < 54)
                {
                    var cloned = item.Clone();
                    cloned.Slot = (byte)(item.Slot - 27);
                    leftItems.Add(cloned);
                }
                else
                {
                    rightItems.Add(item.Clone());
                }
            }
        }

        var rightPos = new BlockPosition(rightX, rightY, rightZ);
        var leftPos = new BlockPosition(leftX, leftY, leftZ);

        var rightBe = BlockEntity.CreateChest(rightPos, rightItems);
        var leftBe = BlockEntity.CreateChest(leftPos, leftItems);

        SetBlockEntity(rightBe);
        SetBlockEntity(leftBe);

        return (rightBe, leftBe);
    }

    /// <summary>
    /// Sets a double chest with explicitly specified positions for the right and left halves and facing direction.
    /// </summary>
    public (BlockEntity Right, BlockEntity Left) SetDoubleChest(BlockPosition rightPos, BlockPosition leftPos, Direction facing = Direction.North, params ItemStack[] items) =>
        SetDoubleChest(rightPos.X, rightPos.Y, rightPos.Z, leftPos.X, leftPos.Y, leftPos.Z, facing, (IEnumerable<ItemStack>)items);

    /// <summary>
    /// Sets a double chest with explicitly specified positions for the right and left halves and facing direction.
    /// </summary>
    public (BlockEntity Right, BlockEntity Left) SetDoubleChest(BlockPosition rightPos, BlockPosition leftPos, Direction facing, IEnumerable<ItemStack>? items) =>
        SetDoubleChest(rightPos.X, rightPos.Y, rightPos.Z, leftPos.X, leftPos.Y, leftPos.Z, facing, items);

    private static (int X, int Y, int Z) GetDoubleChestLeftOffset(int x, int y, int z, Direction facing)
    {
        return facing switch
        {
            Direction.North => (x + 1, y, z),
            Direction.South => (x - 1, y, z),
            Direction.East => (x, y, z - 1),
            Direction.West => (x, y, z + 1),
            _ => (x + 1, y, z)
        };
    }

    /// <summary>
    /// Sets a container block and its block entity with optional inventory items.
    /// </summary>
    public BlockEntity SetContainer(string blockId, string blockEntityId, int x, int y, int z, params ItemStack[] items)
    {
        EnsureInBounds(x, y, z);
        var currentBlock = GetBlock(x, y, z);
        if (currentBlock.IsAir)
        {
            SetBlock(x, y, z, blockId);
        }

        var pos = new BlockPosition(x, y, z);
        var be = BlockEntity.CreateContainer(blockEntityId, pos, items);
        SetBlockEntity(be);
        return be;
    }

    /// <summary>
    /// Sets a container block and its block entity with optional inventory items.
    /// </summary>
    public BlockEntity SetContainer(string blockId, string blockEntityId, BlockPosition position, params ItemStack[] items) =>
        SetContainer(blockId, blockEntityId, position.X, position.Y, position.Z, items);

    /// <summary>
    /// Sets a sign block entity with front messages at (x, y, z).
    /// If the block at (x, y, z) is air, places a default Oak Sign.
    /// </summary>
    public BlockEntity SetSign(int x, int y, int z, params string[] frontMessages) =>
        SetSign(new BlockPosition(x, y, z), frontMessages);

    /// <summary>
    /// Sets a sign block entity with front messages at the specified position.
    /// </summary>
    public BlockEntity SetSign(BlockPosition position, params string[] frontMessages) =>
        SetSign(position, (IEnumerable<string>)frontMessages);

    /// <summary>
    /// Sets a sign block entity with front messages, optional back messages, and waxed state at (x, y, z).
    /// </summary>
    public BlockEntity SetSign(int x, int y, int z, IEnumerable<string> frontMessages, IEnumerable<string>? backMessages = null, bool isWaxed = false) =>
        SetSign(new BlockPosition(x, y, z), frontMessages, backMessages, isWaxed);

    /// <summary>
    /// Sets a sign block entity with front messages, optional back messages, and waxed state at the specified position.
    /// </summary>
    public BlockEntity SetSign(BlockPosition position, IEnumerable<string> frontMessages, IEnumerable<string>? backMessages = null, bool isWaxed = false)
    {
        EnsureInBounds(position.X, position.Y, position.Z);
        var currentBlock = this[position];
        if (currentBlock.IsAir)
        {
            SetBlock(position, "minecraft:oak_sign");
        }

        var be = BlockEntity.CreateSign(position, frontMessages, backMessages, isWaxed);
        SetBlockEntity(be);
        return be;
    }

    /// <summary>
    /// Sets a decorated pot block and block entity at (x, y, z) with an item.
    /// </summary>
    public BlockEntity SetDecoratedPot(int x, int y, int z, ItemStack item) =>
        SetDecoratedPot(new BlockPosition(x, y, z), item);

    /// <summary>
    /// Sets a decorated pot block and block entity at the specified position with an item.
    /// </summary>
    public BlockEntity SetDecoratedPot(BlockPosition position, ItemStack item) =>
        SetDecoratedPot(position, item, null, null, null, null);

    /// <summary>
    /// Sets a decorated pot block and block entity at (x, y, z) with an optional item and sherds.
    /// </summary>
    public BlockEntity SetDecoratedPot(int x, int y, int z, ItemStack? item = null, string? frontSherd = null, string? backSherd = null, string? leftSherd = null, string? rightSherd = null) =>
        SetDecoratedPot(new BlockPosition(x, y, z), item, frontSherd, backSherd, leftSherd, rightSherd);

    /// <summary>
    /// Sets a decorated pot block and block entity at the specified position with an optional item and sherds.
    /// </summary>
    public BlockEntity SetDecoratedPot(BlockPosition position, ItemStack? item = null, string? frontSherd = null, string? backSherd = null, string? leftSherd = null, string? rightSherd = null)
    {
        EnsureInBounds(position.X, position.Y, position.Z);
        var currentBlock = this[position];
        if (currentBlock.IsAir)
        {
            SetBlock(position, "minecraft:decorated_pot");
        }

        var be = BlockEntity.CreateDecoratedPot(position, item, frontSherd, backSherd, leftSherd, rightSherd);
        SetBlockEntity(be);
        return be;
    }

    /// <summary>
    /// Sets a crafter block and its block entity with disabled slots at (x, y, z).
    /// </summary>
    public BlockEntity SetCrafter(int x, int y, int z, params int[] disabledSlots) =>
        SetCrafter(new BlockPosition(x, y, z), disabledSlots, null);

    /// <summary>
    /// Sets a crafter block and its block entity with disabled slots at the specified position.
    /// </summary>
    public BlockEntity SetCrafter(BlockPosition position, params int[] disabledSlots) =>
        SetCrafter(position, disabledSlots, null);

    /// <summary>
    /// Sets a crafter block and its block entity with disabled slots and items at (x, y, z).
    /// </summary>
    public BlockEntity SetCrafter(int x, int y, int z, IEnumerable<int>? disabledSlots, IEnumerable<ItemStack>? items) =>
        SetCrafter(new BlockPosition(x, y, z), disabledSlots, items);

    /// <summary>
    /// Sets a crafter block and its block entity with disabled slots and items at (x, y, z).
    /// </summary>
    public BlockEntity SetCrafter(int x, int y, int z, IEnumerable<int>? disabledSlots, params ItemStack[] items) =>
        SetCrafter(new BlockPosition(x, y, z), disabledSlots, (IEnumerable<ItemStack>)items);

    /// <summary>
    /// Sets a crafter block and its block entity with disabled slots and items at the specified position.
    /// </summary>
    public BlockEntity SetCrafter(BlockPosition position, IEnumerable<int>? disabledSlots, IEnumerable<ItemStack>? items)
    {
        EnsureInBounds(position.X, position.Y, position.Z);
        var currentBlock = this[position];
        if (currentBlock.IsAir)
        {
            SetBlock(position, "minecraft:crafter");
        }

        var be = BlockEntity.CreateCrafter(position, disabledSlots, items);
        SetBlockEntity(be);
        return be;
    }

    /// <summary>
    /// Adds an item to a container block entity at the specified coordinates.
    /// If no block entity exists at the position, creates a chest block entity.
    /// </summary>
    public BlockEntity AddItem(int x, int y, int z, string itemId, int count = 1, byte? slot = null, fNbt.NbtCompound? customData = null)
    {
        EnsureInBounds(x, y, z);
        var pos = new BlockPosition(x, y, z);
        var be = GetBlockEntity(pos);
        if (be == null)
        {
            if (GetBlock(x, y, z).IsAir)
            {
                SetBlock(x, y, z, Minecraft.Blocks.Chest);
            }
            be = new BlockEntity(Minecraft.BlockEntities.Chest, pos);
            SetBlockEntity(be);
        }

        be.AddItem(itemId, count, slot, customData);
        return be;
    }

    /// <summary>
    /// Adds an item to a container block entity at the specified position.
    /// </summary>
    public BlockEntity AddItem(BlockPosition position, ItemStack item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return AddItem(position.X, position.Y, position.Z, item.Id, item.Count, item.Slot, item.CustomData);
    }

    /// <summary>
    /// Gets all inventory items from the block entity at (x, y, z), or empty if none.
    /// </summary>
    public IReadOnlyList<ItemStack> GetItems(int x, int y, int z) =>
        GetBlockEntity(x, y, z)?.GetItems() ?? Array.Empty<ItemStack>();

    /// <summary>
    /// Gets all inventory items from the block entity at the specified position, or empty if none.
    /// </summary>
    public IReadOnlyList<ItemStack> GetItems(BlockPosition position) =>
        GetItems(position.X, position.Y, position.Z);

    /// <summary>
    /// Sets a redstone repeater block at (x, y, z) with specified facing, delay (1-4), locked, and powered states.
    /// </summary>
    public BlockState SetRepeater(int x, int y, int z, string facing = "north", int delay = 1, bool locked = false, bool powered = false)
    {
        var state = Minecraft.Blocks.Repeater
            .WithFacing(facing)
            .WithDelay(delay)
            .WithLocked(locked)
            .WithPowered(powered);
        SetBlock(x, y, z, state);
        return state;
    }

    /// <summary>
    /// Sets a redstone repeater block at the specified position.
    /// </summary>
    public BlockState SetRepeater(BlockPosition position, string facing = "north", int delay = 1, bool locked = false, bool powered = false) =>
        SetRepeater(position.X, position.Y, position.Z, facing, delay, locked, powered);

    /// <summary>
    /// Sets a redstone repeater block at (x, y, z) with strongly-typed facing, delay level (1 to 4), locked, and powered states.
    /// </summary>
    public BlockState SetRepeater(int x, int y, int z, Direction facing, int delay = 1, bool locked = false, bool powered = false) =>
        SetRepeater(x, y, z, facing.ToStateString(), delay, locked, powered);

    /// <summary>
    /// Sets a redstone repeater block at the specified position with strongly-typed facing.
    /// </summary>
    public BlockState SetRepeater(BlockPosition position, Direction facing, int delay = 1, bool locked = false, bool powered = false) =>
        SetRepeater(position.X, position.Y, position.Z, facing.ToStateString(), delay, locked, powered);

    /// <summary>
    /// Sets a redstone comparator block at (x, y, z) with specified facing, subtraction mode, and powered states.
    /// </summary>
    public BlockState SetComparator(int x, int y, int z, string facing = "north", bool subtractMode = false, bool powered = false)
    {
        var state = Minecraft.Blocks.Comparator
            .WithFacing(facing)
            .WithSubtractMode(subtractMode)
            .WithPowered(powered);
        SetBlock(x, y, z, state);
        return state;
    }

    /// <summary>
    /// Sets a redstone comparator block at the specified position.
    /// </summary>
    public BlockState SetComparator(BlockPosition position, string facing = "north", bool subtractMode = false, bool powered = false) =>
        SetComparator(position.X, position.Y, position.Z, facing, subtractMode, powered);

    /// <summary>
    /// Sets a redstone comparator block at (x, y, z) with strongly-typed facing, subtraction mode, and powered states.
    /// </summary>
    public BlockState SetComparator(int x, int y, int z, Direction facing, bool subtractMode = false, bool powered = false) =>
        SetComparator(x, y, z, facing.ToStateString(), subtractMode, powered);

    /// <summary>
    /// Sets a redstone comparator block at the specified position with strongly-typed facing.
    /// </summary>
    public BlockState SetComparator(BlockPosition position, Direction facing, bool subtractMode = false, bool powered = false) =>
        SetComparator(position.X, position.Y, position.Z, facing.ToStateString(), subtractMode, powered);

    /// <summary>
    /// Sets a lever at (x, y, z) with specified attachment face ("floor", "wall", "ceiling"), facing, and powered states.
    /// </summary>
    public BlockState SetLever(int x, int y, int z, string face = "floor", string facing = "north", bool powered = false)
    {
        var state = Minecraft.Blocks.Lever
            .WithFace(face)
            .WithFacing(facing)
            .WithPowered(powered);
        SetBlock(x, y, z, state);
        return state;
    }

    /// <summary>
    /// Sets a lever at the specified position.
    /// </summary>
    public BlockState SetLever(BlockPosition position, string face = "floor", string facing = "north", bool powered = false) =>
        SetLever(position.X, position.Y, position.Z, face, facing, powered);

    /// <summary>
    /// Sets a lever at (x, y, z) with strongly-typed face attachment, facing direction, and powered state.
    /// </summary>
    public BlockState SetLever(int x, int y, int z, BlockFace face, Direction facing = Direction.North, bool powered = false) =>
        SetLever(x, y, z, face.ToStateString(), facing.ToStateString(), powered);

    /// <summary>
    /// Sets a lever at the specified position with strongly-typed face attachment, facing direction, and powered state.
    /// </summary>
    public BlockState SetLever(BlockPosition position, BlockFace face, Direction facing = Direction.North, bool powered = false) =>
        SetLever(position.X, position.Y, position.Z, face.ToStateString(), facing.ToStateString(), powered);

    /// <summary>
    /// Sets redstone wire at (x, y, z) with signal power (0 to 15).
    /// </summary>
    public BlockState SetRedstoneWire(int x, int y, int z, int power = 0)
    {
        var state = Minecraft.Blocks.RedstoneWire.WithPower(power);
        SetBlock(x, y, z, state);
        return state;
    }

    /// <summary>
    /// Sets redstone wire at the specified position.
    /// </summary>
    public BlockState SetRedstoneWire(BlockPosition position, int power = 0) =>
        SetRedstoneWire(position.X, position.Y, position.Z, power);

    /// <summary>
    /// Sets a piston block at (x, y, z) with specified facing, sticky, and extended states.
    /// </summary>
    public BlockState SetPiston(int x, int y, int z, string facing = "up", bool sticky = false, bool extended = false)
    {
        string blockId = sticky ? Minecraft.Blocks.StickyPiston : Minecraft.Blocks.Piston;
        var state = blockId.WithFacing(facing).WithExtended(extended);
        SetBlock(x, y, z, state);
        return state;
    }

    /// <summary>
    /// Sets a piston block at the specified position.
    /// </summary>
    public BlockState SetPiston(BlockPosition position, string facing = "up", bool sticky = false, bool extended = false) =>
        SetPiston(position.X, position.Y, position.Z, facing, sticky, extended);

    /// <summary>
    /// Sets a piston block at (x, y, z) with strongly-typed facing direction, sticky, and extended states.
    /// </summary>
    public BlockState SetPiston(int x, int y, int z, Direction facing, bool sticky = false, bool extended = false) =>
        SetPiston(x, y, z, facing.ToStateString(), sticky, extended);

    /// <summary>
    /// Sets a piston block at the specified position with strongly-typed facing direction.
    /// </summary>
    public BlockState SetPiston(BlockPosition position, Direction facing, bool sticky = false, bool extended = false) =>
        SetPiston(position.X, position.Y, position.Z, facing.ToStateString(), sticky, extended);

    /// <summary>
    /// Adds an entity to the schematic.
    /// </summary>
    public void AddEntity(Entity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        _entities.Add(entity);
    }

    /// <summary>
    /// Adds an entity (optionally named, such as when renamed with a Name Tag) to the schematic.
    /// </summary>
    public Entity AddEntity(string entityId, double x, double y, double z, string? customName = null, bool customNameVisible = true, bool persistenceRequired = true)
    {
        var entity = new Entity(entityId, x, y, z);
        if (!string.IsNullOrWhiteSpace(customName))
        {
            entity.WithCustomName(customName, customNameVisible, persistenceRequired);
        }
        AddEntity(entity);
        return entity;
    }

    /// <summary>
    /// Adds an entity to the schematic using block coordinates (x, y, z).
    /// Applies the schematic's <see cref="Offset"/> and centers the entity horizontally within the block
    /// at (x + Offset.X + 0.5, y + Offset.Y, z + Offset.Z + 0.5).
    /// </summary>
    public Entity AddEntity(int x, int y, int z, Entity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        EnsureInBounds(x, y, z);

        entity.X = x + Offset.X + 0.5;
        entity.Y = y + Offset.Y;
        entity.Z = z + Offset.Z + 0.5;
        entity.Data.Remove("Pos");

        AddEntity(entity);
        return entity;
    }

    /// <summary>
    /// Adds an entity to the schematic at the specified block position.
    /// Applies the schematic's <see cref="Offset"/> and centers the entity horizontally within the block
    /// at (position.X + Offset.X + 0.5, position.Y + Offset.Y, position.Z + Offset.Z + 0.5).
    /// </summary>
    public Entity AddEntity(BlockPosition position, Entity entity) =>
        AddEntity(position.X, position.Y, position.Z, entity);

    /// <summary>
    /// Adds an entity (optionally named, such as when renamed with a Name Tag) to the schematic using block coordinates (x, y, z).
    /// Applies the schematic's <see cref="Offset"/> and centers the entity horizontally within the block.
    /// </summary>
    public Entity AddEntity(int x, int y, int z, string entityId, string? customName = null, bool customNameVisible = true, bool persistenceRequired = true)
    {
        var entity = new Entity(entityId);
        if (!string.IsNullOrWhiteSpace(customName))
        {
            entity.WithCustomName(customName, customNameVisible, persistenceRequired);
        }
        return AddEntity(x, y, z, entity);
    }

    /// <summary>
    /// Adds an entity (optionally named, such as when renamed with a Name Tag) to the schematic at the specified block position.
    /// Applies the schematic's <see cref="Offset"/> and centers the entity horizontally within the block.
    /// </summary>
    public Entity AddEntity(BlockPosition position, string entityId, string? customName = null, bool customNameVisible = true, bool persistenceRequired = true) =>
        AddEntity(position.X, position.Y, position.Z, entityId, customName, customNameVisible, persistenceRequired);

    /// <summary>
    /// Adds a minecart entity to the schematic with an optional passenger.
    /// </summary>
    public Entity AddMinecart(
        double x, double y, double z,
        Entity? passenger = null,
        double motionX = 0, double motionY = 0, double motionZ = 0,
        bool isFlipped = false,
        string minecartType = Minecraft.Entities.Minecart)
    {
        var entity = Entity.CreateMinecart(x, y, z, passenger, motionX, motionY, motionZ, isFlipped, minecartType);
        AddEntity(entity);
        return entity;
    }

    /// <summary>
    /// Adds a minecart entity with multiple passengers to the schematic.
    /// </summary>
    public Entity AddMinecart(
        double x, double y, double z,
        IEnumerable<Entity> passengers,
        double motionX = 0, double motionY = 0, double motionZ = 0,
        bool isFlipped = false,
        string minecartType = Minecraft.Entities.Minecart)
    {
        var entity = Entity.CreateMinecart(x, y, z, passengers, motionX, motionY, motionZ, isFlipped, minecartType);
        AddEntity(entity);
        return entity;
    }

    /// <summary>
    /// Adds an item frame entity (or glow item frame) to the schematic.
    /// </summary>
    public Entity AddItemFrame(
        double x, double y, double z,
        Direction facing = Direction.North,
        ItemStack? item = null,
        int rotation = 0,
        bool isGlow = false,
        bool isInvisible = false,
        bool isFixed = false)
    {
        var entity = Entity.CreateItemFrame(x, y, z, facing, item, rotation, isGlow, isInvisible, isFixed);
        AddEntity(entity);
        return entity;
    }

    /// <summary>
    /// Adds an item frame entity with a specific item identifier.
    /// </summary>
    public Entity AddItemFrame(
        double x, double y, double z,
        Direction facing,
        string itemId,
        int rotation = 0,
        bool isGlow = false,
        bool isInvisible = false,
        bool isFixed = false) =>
        AddItemFrame(x, y, z, facing, new ItemStack(itemId), rotation, isGlow, isInvisible, isFixed);

    /// <summary>
    /// Adds an item frame entity positioned relative to a block position.
    /// </summary>
    public Entity AddItemFrame(
        BlockPosition position,
        Direction facing = Direction.North,
        ItemStack? item = null,
        int rotation = 0,
        bool isGlow = false,
        bool isInvisible = false,
        bool isFixed = false) =>
        AddItemFrame(position.X + 0.5, position.Y + 0.5, position.Z + 0.5, facing, item, rotation, isGlow, isInvisible, isFixed);

    /// <summary>
    /// Adds a dropped item entity ("minecraft:item") to the schematic.
    /// </summary>
    public Entity AddDroppedItem(
        double x, double y, double z,
        ItemStack item,
        short age = 0,
        short pickupDelay = 0)
    {
        var entity = Entity.CreateItemEntity(x, y, z, item, age, pickupDelay);
        AddEntity(entity);
        return entity;
    }

    /// <summary>
    /// Adds a dropped item entity ("minecraft:item") with a specific item ID and count to the schematic.
    /// </summary>
    public Entity AddDroppedItem(
        double x, double y, double z,
        string itemId,
        int count = 1,
        short age = 0,
        short pickupDelay = 0) =>
        AddDroppedItem(x, y, z, new ItemStack(itemId, count), age, pickupDelay);

    /// <summary>
    /// Adds a dropped item entity ("minecraft:item") positioned relative to a block position (centered horizontally at y + 0.1).
    /// </summary>
    public Entity AddDroppedItem(
        BlockPosition position,
        ItemStack item,
        short age = 0,
        short pickupDelay = 0) =>
        AddDroppedItem(position.X + 0.5, position.Y + 0.1, position.Z + 0.5, item, age, pickupDelay);

    /// <summary>
    /// Adds a dropped item entity ("minecraft:item") with a specific item ID and count relative to a block position.
    /// </summary>
    public Entity AddDroppedItem(
        BlockPosition position,
        string itemId,
        int count = 1,
        short age = 0,
        short pickupDelay = 0) =>
        AddDroppedItem(position.X + 0.5, position.Y + 0.1, position.Z + 0.5, new ItemStack(itemId, count), age, pickupDelay);

    /// <summary>
    /// Removes an entity from the schematic.
    /// </summary>
    public bool RemoveEntity(Entity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return _entities.Remove(entity);
    }

    /// <summary>
    /// Clears all entities in the schematic.
    /// </summary>
    public void ClearEntities()
    {
        _entities.Clear();
    }

    /// <summary>
    /// Fills the entire schematic with the specified block state.
    /// </summary>
    public void Fill(BlockState blockState)
    {
        ArgumentNullException.ThrowIfNull(blockState);
        Array.Fill(_blocks, blockState);
        RemoveIncompatibleBlockEntities(blockState);
    }

    /// <summary>
    /// Fills a cuboid volume within the schematic with the specified block state.
    /// </summary>
    public void Fill(int minX, int minY, int minZ, int maxX, int maxY, int maxZ, BlockState blockState)
    {
        ArgumentNullException.ThrowIfNull(blockState);

        int startX = Math.Min(minX, maxX);
        int endX = Math.Max(minX, maxX);
        int startY = Math.Min(minY, maxY);
        int endY = Math.Max(minY, maxY);
        int startZ = Math.Min(minZ, maxZ);
        int endZ = Math.Max(minZ, maxZ);

        EnsureInBounds(startX, startY, startZ);
        EnsureInBounds(endX, endY, endZ);

        for (int y = startY; y <= endY; y++)
        {
            for (int z = startZ; z <= endZ; z++)
            {
                for (int x = startX; x <= endX; x++)
                {
                    _blocks[GetIndex(x, y, z)] = blockState;
                }
            }
        }

        RemoveIncompatibleBlockEntitiesInRegion(startX, startY, startZ, endX, endY, endZ, blockState);
    }

    /// <summary>
    /// Places a filled or hollow circle centered at (centerX, centerY, centerZ) in the plane perpendicular to <paramref name="normalAxis"/>.
    /// </summary>
    public void SetCircle(int centerX, int centerY, int centerZ, int radius, BlockState blockState, bool filled = true, Axis normalAxis = Axis.Y)
    {
        ArgumentNullException.ThrowIfNull(blockState);
        if (radius < 0)
            throw new ArgumentOutOfRangeException(nameof(radius), radius, "Radius must be zero or greater.");
        ValidateAxis(normalAxis, nameof(normalAxis));

        EnsureAxisBoxInBounds(centerX, centerY, centerZ, -radius, radius, -radius, radius, 0, 0, normalAxis);
        SetEllipticalSlice(centerX, centerY, centerZ, radius, radius, blockState, filled, normalAxis);
    }

    /// <summary>
    /// Places a filled or hollow circle centered at (centerX, centerY, centerZ) from a blockstate string.
    /// </summary>
    public void SetCircle(int centerX, int centerY, int centerZ, int radius, string blockStateString, bool filled = true, Axis normalAxis = Axis.Y) =>
        SetCircle(centerX, centerY, centerZ, radius, BlockState.Parse(blockStateString), filled, normalAxis);

    /// <summary>
    /// Places a filled or hollow circle centered at <paramref name="center"/>.
    /// </summary>
    public void SetCircle(BlockPosition center, int radius, BlockState blockState, bool filled = true, Axis normalAxis = Axis.Y) =>
        SetCircle(center.X, center.Y, center.Z, radius, blockState, filled, normalAxis);

    /// <summary>
    /// Places a filled or hollow circle centered at <paramref name="center"/> from a blockstate string.
    /// </summary>
    public void SetCircle(BlockPosition center, int radius, string blockStateString, bool filled = true, Axis normalAxis = Axis.Y) =>
        SetCircle(center.X, center.Y, center.Z, radius, BlockState.Parse(blockStateString), filled, normalAxis);

    /// <summary>
    /// Places a filled or hollow sphere centered at (centerX, centerY, centerZ).
    /// </summary>
    public void SetSphere(int centerX, int centerY, int centerZ, int radius, BlockState blockState, bool filled = true)
    {
        ArgumentNullException.ThrowIfNull(blockState);
        if (radius < 0)
            throw new ArgumentOutOfRangeException(nameof(radius), radius, "Radius must be zero or greater.");

        EnsureInBounds(centerX - radius, centerY - radius, centerZ - radius);
        EnsureInBounds(centerX + radius, centerY + radius, centerZ + radius);

        double outerRadiusSq = (radius + 0.5) * (radius + 0.5);
        bool IsInside(int dx, int dy, int dz) => dx * dx + dy * dy + dz * dz <= outerRadiusSq;

        for (int dy = -radius; dy <= radius; dy++)
        {
            for (int dz = -radius; dz <= radius; dz++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    if (!IsInside(dx, dy, dz))
                        continue;

                    if (!filled &&
                        IsInside(dx + 1, dy, dz) && IsInside(dx - 1, dy, dz) &&
                        IsInside(dx, dy + 1, dz) && IsInside(dx, dy - 1, dz) &&
                        IsInside(dx, dy, dz + 1) && IsInside(dx, dy, dz - 1))
                    {
                        continue;
                    }

                    SetBlock(centerX + dx, centerY + dy, centerZ + dz, blockState);
                }
            }
        }
    }

    /// <summary>
    /// Places a filled or hollow sphere centered at (centerX, centerY, centerZ) from a blockstate string.
    /// </summary>
    public void SetSphere(int centerX, int centerY, int centerZ, int radius, string blockStateString, bool filled = true) =>
        SetSphere(centerX, centerY, centerZ, radius, BlockState.Parse(blockStateString), filled);

    /// <summary>
    /// Places a filled or hollow sphere centered at <paramref name="center"/>.
    /// </summary>
    public void SetSphere(BlockPosition center, int radius, BlockState blockState, bool filled = true) =>
        SetSphere(center.X, center.Y, center.Z, radius, blockState, filled);

    /// <summary>
    /// Places a filled or hollow sphere centered at <paramref name="center"/> from a blockstate string.
    /// </summary>
    public void SetSphere(BlockPosition center, int radius, string blockStateString, bool filled = true) =>
        SetSphere(center.X, center.Y, center.Z, radius, BlockState.Parse(blockStateString), filled);

    /// <summary>
    /// Places a filled or hollow rectangle starting at (startX, startY, startZ) in the plane perpendicular to <paramref name="normalAxis"/>.
    /// </summary>
    public void SetRectangle(int startX, int startY, int startZ, int width, int length, BlockState blockState, bool filled = true, Axis normalAxis = Axis.Y)
    {
        ArgumentNullException.ThrowIfNull(blockState);
        if (width <= 0)
            throw new ArgumentOutOfRangeException(nameof(width), width, "Width must be greater than zero.");
        if (length <= 0)
            throw new ArgumentOutOfRangeException(nameof(length), length, "Length must be greater than zero.");
        ValidateAxis(normalAxis, nameof(normalAxis));

        EnsureAxisBoxInBounds(startX, startY, startZ, 0, width - 1, 0, length - 1, 0, 0, normalAxis);

        for (int v = 0; v < length; v++)
        {
            for (int u = 0; u < width; u++)
            {
                if (filled || u == 0 || u == width - 1 || v == 0 || v == length - 1)
                {
                    var pos = MapAxisOffset(startX, startY, startZ, u, v, 0, normalAxis);
                    SetBlock(pos.X, pos.Y, pos.Z, blockState);
                }
            }
        }
    }

    /// <summary>
    /// Places a filled or hollow rectangle starting at (startX, startY, startZ) from a blockstate string.
    /// </summary>
    public void SetRectangle(int startX, int startY, int startZ, int width, int length, string blockStateString, bool filled = true, Axis normalAxis = Axis.Y) =>
        SetRectangle(startX, startY, startZ, width, length, BlockState.Parse(blockStateString), filled, normalAxis);

    /// <summary>
    /// Places a filled or hollow rectangle starting at <paramref name="start"/>.
    /// </summary>
    public void SetRectangle(BlockPosition start, int width, int length, BlockState blockState, bool filled = true, Axis normalAxis = Axis.Y) =>
        SetRectangle(start.X, start.Y, start.Z, width, length, blockState, filled, normalAxis);

    /// <summary>
    /// Places a filled or hollow rectangle starting at <paramref name="start"/> from a blockstate string.
    /// </summary>
    public void SetRectangle(BlockPosition start, int width, int length, string blockStateString, bool filled = true, Axis normalAxis = Axis.Y) =>
        SetRectangle(start.X, start.Y, start.Z, width, length, BlockState.Parse(blockStateString), filled, normalAxis);

    /// <summary>
    /// Places a filled or hollow cube starting at (startX, startY, startZ) with edge length <paramref name="size"/>.
    /// </summary>
    public void SetCube(int startX, int startY, int startZ, int size, BlockState blockState, bool filled = true)
    {
        ArgumentNullException.ThrowIfNull(blockState);
        if (size <= 0)
            throw new ArgumentOutOfRangeException(nameof(size), size, "Cube size must be greater than zero.");

        EnsureInBounds(startX, startY, startZ);
        EnsureInBounds(startX + size - 1, startY + size - 1, startZ + size - 1);

        if (filled)
        {
            Fill(startX, startY, startZ, startX + size - 1, startY + size - 1, startZ + size - 1, blockState);
            return;
        }

        for (int dy = 0; dy < size; dy++)
        {
            for (int dz = 0; dz < size; dz++)
            {
                for (int dx = 0; dx < size; dx++)
                {
                    if (dx == 0 || dx == size - 1 ||
                        dy == 0 || dy == size - 1 ||
                        dz == 0 || dz == size - 1)
                    {
                        SetBlock(startX + dx, startY + dy, startZ + dz, blockState);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Places a filled or hollow cube starting at (startX, startY, startZ) from a blockstate string.
    /// </summary>
    public void SetCube(int startX, int startY, int startZ, int size, string blockStateString, bool filled = true) =>
        SetCube(startX, startY, startZ, size, BlockState.Parse(blockStateString), filled);

    /// <summary>
    /// Places a filled or hollow cube starting at <paramref name="start"/>.
    /// </summary>
    public void SetCube(BlockPosition start, int size, BlockState blockState, bool filled = true) =>
        SetCube(start.X, start.Y, start.Z, size, blockState, filled);

    /// <summary>
    /// Places a filled or hollow cube starting at <paramref name="start"/> from a blockstate string.
    /// </summary>
    public void SetCube(BlockPosition start, int size, string blockStateString, bool filled = true) =>
        SetCube(start.X, start.Y, start.Z, size, BlockState.Parse(blockStateString), filled);

    /// <summary>
    /// Places a filled or hollow oval (ellipse) centered at (centerX, centerY, centerZ) in the plane perpendicular to <paramref name="normalAxis"/>.
    /// </summary>
    public void SetOval(int centerX, int centerY, int centerZ, int radiusU, int radiusV, BlockState blockState, bool filled = true, Axis normalAxis = Axis.Y)
    {
        ArgumentNullException.ThrowIfNull(blockState);
        if (radiusU < 0)
            throw new ArgumentOutOfRangeException(nameof(radiusU), radiusU, "RadiusU must be zero or greater.");
        if (radiusV < 0)
            throw new ArgumentOutOfRangeException(nameof(radiusV), radiusV, "RadiusV must be zero or greater.");
        ValidateAxis(normalAxis, nameof(normalAxis));

        EnsureAxisBoxInBounds(centerX, centerY, centerZ, -radiusU, radiusU, -radiusV, radiusV, 0, 0, normalAxis);
        SetEllipticalSlice(centerX, centerY, centerZ, radiusU, radiusV, blockState, filled, normalAxis);
    }

    /// <summary>
    /// Places a filled or hollow oval centered at (centerX, centerY, centerZ) from a blockstate string.
    /// </summary>
    public void SetOval(int centerX, int centerY, int centerZ, int radiusU, int radiusV, string blockStateString, bool filled = true, Axis normalAxis = Axis.Y) =>
        SetOval(centerX, centerY, centerZ, radiusU, radiusV, BlockState.Parse(blockStateString), filled, normalAxis);

    /// <summary>
    /// Places a filled or hollow oval centered at <paramref name="center"/>.
    /// </summary>
    public void SetOval(BlockPosition center, int radiusU, int radiusV, BlockState blockState, bool filled = true, Axis normalAxis = Axis.Y) =>
        SetOval(center.X, center.Y, center.Z, radiusU, radiusV, blockState, filled, normalAxis);

    /// <summary>
    /// Places a filled or hollow oval centered at <paramref name="center"/> from a blockstate string.
    /// </summary>
    public void SetOval(BlockPosition center, int radiusU, int radiusV, string blockStateString, bool filled = true, Axis normalAxis = Axis.Y) =>
        SetOval(center.X, center.Y, center.Z, radiusU, radiusV, BlockState.Parse(blockStateString), filled, normalAxis);

    /// <summary>
    /// Places a filled or hollow cylinder starting at base center (centerX, startY, centerZ) extending along <paramref name="axis"/>.
    /// </summary>
    public void SetCylinder(int centerX, int startY, int centerZ, int radius, int height, BlockState blockState, bool filled = true, Axis axis = Axis.Y)
    {
        ArgumentNullException.ThrowIfNull(blockState);
        if (radius < 0)
            throw new ArgumentOutOfRangeException(nameof(radius), radius, "Radius must be zero or greater.");
        if (height <= 0)
            throw new ArgumentOutOfRangeException(nameof(height), height, "Height must be greater than zero.");
        ValidateAxis(axis, nameof(axis));

        EnsureAxisBoxInBounds(centerX, startY, centerZ, -radius, radius, -radius, radius, 0, height - 1, axis);

        for (int h = 0; h < height; h++)
        {
            var sliceCenter = MapAxisOffset(centerX, startY, centerZ, 0, 0, h, axis);
            SetEllipticalSlice(sliceCenter.X, sliceCenter.Y, sliceCenter.Z, radius, radius, blockState, filled, axis);
        }
    }

    /// <summary>
    /// Places a filled or hollow cylinder starting at base center (centerX, startY, centerZ) from a blockstate string.
    /// </summary>
    public void SetCylinder(int centerX, int startY, int centerZ, int radius, int height, string blockStateString, bool filled = true, Axis axis = Axis.Y) =>
        SetCylinder(centerX, startY, centerZ, radius, height, BlockState.Parse(blockStateString), filled, axis);

    /// <summary>
    /// Places a filled or hollow cylinder starting at <paramref name="baseCenter"/>.
    /// </summary>
    public void SetCylinder(BlockPosition baseCenter, int radius, int height, BlockState blockState, bool filled = true, Axis axis = Axis.Y) =>
        SetCylinder(baseCenter.X, baseCenter.Y, baseCenter.Z, radius, height, blockState, filled, axis);

    /// <summary>
    /// Places a filled or hollow cylinder starting at <paramref name="baseCenter"/> from a blockstate string.
    /// </summary>
    public void SetCylinder(BlockPosition baseCenter, int radius, int height, string blockStateString, bool filled = true, Axis axis = Axis.Y) =>
        SetCylinder(baseCenter.X, baseCenter.Y, baseCenter.Z, radius, height, BlockState.Parse(blockStateString), filled, axis);

    /// <summary>
    /// Places a filled or hollow torus centered at (centerX, centerY, centerZ) oriented around <paramref name="axis"/>.
    /// </summary>
    public void SetTorus(int centerX, int centerY, int centerZ, int majorRadius, int minorRadius, BlockState blockState, bool filled = true, Axis axis = Axis.Y)
    {
        ArgumentNullException.ThrowIfNull(blockState);
        if (majorRadius <= 0)
            throw new ArgumentOutOfRangeException(nameof(majorRadius), majorRadius, "MajorRadius must be greater than zero.");
        if (minorRadius < 0 || minorRadius >= majorRadius)
            throw new ArgumentOutOfRangeException(nameof(minorRadius), minorRadius, "MinorRadius must be non-negative and less than MajorRadius.");
        ValidateAxis(axis, nameof(axis));

        int outerExtent = majorRadius + minorRadius;
        EnsureAxisBoxInBounds(centerX, centerY, centerZ, -outerExtent, outerExtent, -outerExtent, outerExtent, -minorRadius, minorRadius, axis);

        double minorSq = (minorRadius + 0.5) * (minorRadius + 0.5);
        bool IsInside(int du, int dv, int dh)
        {
            double ringDelta = Math.Sqrt(du * du + dv * dv) - majorRadius;
            return ringDelta * ringDelta + dh * dh <= minorSq;
        }

        for (int dh = -minorRadius; dh <= minorRadius; dh++)
        {
            for (int dv = -outerExtent; dv <= outerExtent; dv++)
            {
                for (int du = -outerExtent; du <= outerExtent; du++)
                {
                    if (!IsInside(du, dv, dh))
                        continue;

                    if (!filled &&
                        IsInside(du + 1, dv, dh) && IsInside(du - 1, dv, dh) &&
                        IsInside(du, dv + 1, dh) && IsInside(du, dv - 1, dh) &&
                        IsInside(du, dv, dh + 1) && IsInside(du, dv, dh - 1))
                    {
                        continue;
                    }

                    var pos = MapAxisOffset(centerX, centerY, centerZ, du, dv, dh, axis);
                    SetBlock(pos.X, pos.Y, pos.Z, blockState);
                }
            }
        }
    }

    /// <summary>
    /// Places a filled or hollow torus centered at (centerX, centerY, centerZ) from a blockstate string.
    /// </summary>
    public void SetTorus(int centerX, int centerY, int centerZ, int majorRadius, int minorRadius, string blockStateString, bool filled = true, Axis axis = Axis.Y) =>
        SetTorus(centerX, centerY, centerZ, majorRadius, minorRadius, BlockState.Parse(blockStateString), filled, axis);

    /// <summary>
    /// Places a filled or hollow torus centered at <paramref name="center"/>.
    /// </summary>
    public void SetTorus(BlockPosition center, int majorRadius, int minorRadius, BlockState blockState, bool filled = true, Axis axis = Axis.Y) =>
        SetTorus(center.X, center.Y, center.Z, majorRadius, minorRadius, blockState, filled, axis);

    /// <summary>
    /// Places a filled or hollow torus centered at <paramref name="center"/> from a blockstate string.
    /// </summary>
    public void SetTorus(BlockPosition center, int majorRadius, int minorRadius, string blockStateString, bool filled = true, Axis axis = Axis.Y) =>
        SetTorus(center.X, center.Y, center.Z, majorRadius, minorRadius, BlockState.Parse(blockStateString), filled, axis);

    /// <summary>
    /// Places a filled or hollow square-base pyramid starting at (startX, startY, startZ) with base edge <paramref name="baseSize"/>.
    /// </summary>
    public void SetPyramid(int startX, int startY, int startZ, int baseSize, BlockState blockState, bool filled = true)
    {
        ArgumentNullException.ThrowIfNull(blockState);
        if (baseSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(baseSize), baseSize, "BaseSize must be greater than zero.");

        int pyramidHeight = (baseSize + 1) / 2;
        EnsureInBounds(startX, startY, startZ);
        EnsureInBounds(startX + baseSize - 1, startY + pyramidHeight - 1, startZ + baseSize - 1);

        for (int level = 0; level < pyramidHeight; level++)
        {
            int layerWidth = baseSize - 2 * level;
            SetRectangle(startX + level, startY + level, startZ + level, layerWidth, layerWidth, blockState, filled: filled, normalAxis: Axis.Y);
        }
    }

    /// <summary>
    /// Places a filled or hollow square-base pyramid starting at (startX, startY, startZ) from a blockstate string.
    /// </summary>
    public void SetPyramid(int startX, int startY, int startZ, int baseSize, string blockStateString, bool filled = true) =>
        SetPyramid(startX, startY, startZ, baseSize, BlockState.Parse(blockStateString), filled);

    /// <summary>
    /// Places a filled or hollow square-base pyramid starting at <paramref name="start"/>.
    /// </summary>
    public void SetPyramid(BlockPosition start, int baseSize, BlockState blockState, bool filled = true) =>
        SetPyramid(start.X, start.Y, start.Z, baseSize, blockState, filled);

    /// <summary>
    /// Places a filled or hollow square-base pyramid starting at <paramref name="start"/> from a blockstate string.
    /// </summary>
    public void SetPyramid(BlockPosition start, int baseSize, string blockStateString, bool filled = true) =>
        SetPyramid(start.X, start.Y, start.Z, baseSize, BlockState.Parse(blockStateString), filled);

    private void SetEllipticalSlice(int cx, int cy, int cz, int radiusU, int radiusV, BlockState blockState, bool filled, Axis normalAxis)
    {
        double ruSq = (radiusU + 0.5) * (radiusU + 0.5);
        double rvSq = (radiusV + 0.5) * (radiusV + 0.5);
        bool isCircle = radiusU == radiusV;

        bool IsInside(int du, int dv) => isCircle
            ? du * du + dv * dv <= ruSq
            : (du * du) / ruSq + (dv * dv) / rvSq <= 1.0;

        for (int dv = -radiusV; dv <= radiusV; dv++)
        {
            for (int du = -radiusU; du <= radiusU; du++)
            {
                if (!IsInside(du, dv))
                    continue;

                if (!filled &&
                    IsInside(du + 1, dv) && IsInside(du - 1, dv) &&
                    IsInside(du, dv + 1) && IsInside(du, dv - 1))
                {
                    continue;
                }

                var pos = MapAxisOffset(cx, cy, cz, du, dv, 0, normalAxis);
                SetBlock(pos.X, pos.Y, pos.Z, blockState);
            }
        }
    }

    private void EnsureAxisBoxInBounds(int cx, int cy, int cz, int minU, int maxU, int minV, int maxV, int minH, int maxH, Axis axis)
    {
        var minCorner = MapAxisOffset(cx, cy, cz, minU, minV, minH, axis);
        var maxCorner = MapAxisOffset(cx, cy, cz, maxU, maxV, maxH, axis);
        EnsureInBounds(minCorner.X, minCorner.Y, minCorner.Z);
        EnsureInBounds(maxCorner.X, maxCorner.Y, maxCorner.Z);
    }

    private static BlockPosition MapAxisOffset(int cx, int cy, int cz, int du, int dv, int dh, Axis axis) => axis switch
    {
        Axis.X => new BlockPosition(cx + dh, cy + du, cz + dv),
        Axis.Y => new BlockPosition(cx + du, cy + dh, cz + dv),
        Axis.Z => new BlockPosition(cx + du, cy + dv, cz + dh),
        _ => throw new ArgumentOutOfRangeException(nameof(axis), axis, "Unsupported axis value.")
    };

    private static void ValidateAxis(Axis axis, string paramName)
    {
        if (!Enum.IsDefined(axis))
        {
            throw new ArgumentOutOfRangeException(paramName, axis, "Unsupported axis value.");
        }
    }

    /// <summary>
    /// Returns all distinct block states currently present in this schematic.
    /// </summary>
    public IReadOnlyList<BlockState> GetUniqueBlockStates()
    {
        var set = new HashSet<BlockState>(_blocks);
        return set.ToList();
    }

    /// <summary>
    /// Counts how many non-air blocks are in the schematic.
    /// </summary>
    public int CountNonAirBlocks()
    {
        int count = 0;
        for (int i = 0; i < _blocks.Length; i++)
        {
            if (!_blocks[i].IsAir) count++;
        }
        return count;
    }

    /// <summary>
    /// Creates a deep copy of this schematic.
    /// </summary>
    public Schematic Clone()
    {
        var clone = new Schematic(Width, Height, Length)
        {
            Offset = Offset,
            DataVersion = DataVersion,
            Metadata = Metadata.Clone()
        };

        Array.Copy(_blocks, clone._blocks, _blocks.Length);

        foreach (var be in _blockEntities.Values)
        {
            clone._blockEntities[be.Position] = be.Clone();
        }

        foreach (var entity in _entities)
        {
            clone._entities.Add(entity.Clone());
        }

        return clone;
    }

    /// <summary>
    /// Returns a new <see cref="Schematic"/> rotated clockwise around the vertical Y axis.
    /// </summary>
    public Schematic Rotate(Rotation rotation)
    {
        if (!Enum.IsDefined(rotation))
        {
            throw new ArgumentOutOfRangeException(nameof(rotation), rotation, "Unsupported rotation value.");
        }

        if (rotation == Rotation.None)
        {
            return Clone();
        }

        int newWidth = rotation is Rotation.Clockwise90 or Rotation.Clockwise270 ? Length : Width;
        int newLength = rotation is Rotation.Clockwise90 or Rotation.Clockwise270 ? Width : Length;

        BlockPosition newOffset = Offset == BlockPosition.Zero
            ? BlockPosition.Zero
            : rotation switch
            {
                Rotation.Clockwise90 => new BlockPosition(-(Offset.Z + Length - 1), Offset.Y, Offset.X),
                Rotation.Clockwise180 => new BlockPosition(-(Offset.X + Width - 1), Offset.Y, -(Offset.Z + Length - 1)),
                Rotation.Clockwise270 => new BlockPosition(Offset.Z, Offset.Y, -(Offset.X + Width - 1)),
                _ => Offset
            };

        var rotated = new Schematic(newWidth, Height, newLength)
        {
            Offset = newOffset,
            DataVersion = DataVersion,
            Metadata = Metadata.Clone()
        };

        for (int y = 0; y < Height; y++)
        {
            for (int z = 0; z < Length; z++)
            {
                for (int x = 0; x < Width; x++)
                {
                    var state = GetBlock(x, y, z);
                    if (!state.IsAir || state != BlockState.Air)
                    {
                        var (rx, ry, rz) = RotateBlockCoordinates(x, y, z, rotation);
                        rotated._blocks[rotated.GetIndex(rx, ry, rz)] = state.Rotate(rotation);
                    }
                }
            }
        }

        foreach (var be in _blockEntities.Values)
        {
            var (rx, ry, rz) = RotateBlockCoordinates(be.Position.X, be.Position.Y, be.Position.Z, rotation);
            var beClone = be.Clone();
            beClone.Position = new BlockPosition(rx, ry, rz);
            rotated._blockEntities[beClone.Position] = beClone;
        }

        int steps = (int)rotation / 90;
        foreach (var ent in _entities)
        {
            var entClone = ent.Clone();
            entClone.Data.Remove("Pos");

            bool useOffsetSpace = Offset != BlockPosition.Zero &&
                                  (entClone.X < 0 || entClone.X > Width || entClone.Z < 0 || entClone.Z > Length);

            double localX = useOffsetSpace ? entClone.X - Offset.X : entClone.X;
            double localZ = useOffsetSpace ? entClone.Z - Offset.Z : entClone.Z;

            var (rotLocalX, rotLocalZ) = rotation switch
            {
                Rotation.Clockwise90 => (Length - localZ, localX),
                Rotation.Clockwise180 => (Width - localX, Length - localZ),
                Rotation.Clockwise270 => (localZ, Width - localX),
                _ => (localX, localZ)
            };

            entClone.X = useOffsetSpace ? rotLocalX + newOffset.X : rotLocalX;
            entClone.Z = useOffsetSpace ? rotLocalZ + newOffset.Z : rotLocalZ;

            if (entClone.Data["Rotation"] is fNbt.NbtList rotList && rotList.Count >= 2)
            {
                float yaw = rotList[0].FloatValue;
                float pitch = rotList[1].FloatValue;
                float newYaw = ((yaw + steps * 90f) % 360f + 360f) % 360f;
                entClone.WithRotation(newYaw, pitch);
            }

            if (entClone.IsItemFrame && (entClone.Data.Contains("Facing") || entClone.Data.Contains("facing")))
            {
                byte facingByte = (byte)(entClone.Data["Facing"]?.IntValue ?? entClone.Data["facing"]?.IntValue ?? 2);
                Direction rotatedFacing = Entity.DirectionFromFacingByte(facingByte).Rotate(rotation);
                entClone.Data["Facing"] = new fNbt.NbtByte("Facing", Entity.FacingByteFromDirection(rotatedFacing));
                entClone.Data.Remove("facing");
            }

            if (entClone.Data["Motion"] is fNbt.NbtList motionList && motionList.Count >= 3)
            {
                var (mx, my, mz) = entClone.GetMotion();
                var (rotMx, rotMz) = rotation switch
                {
                    Rotation.Clockwise90 => (-mz, mx),
                    Rotation.Clockwise180 => (-mx, -mz),
                    Rotation.Clockwise270 => (mz, -mx),
                    _ => (mx, mz)
                };
                entClone.SetMotion(rotMx, my, rotMz);
            }

            rotated._entities.Add(entClone);
        }

        return rotated;
    }

    private (int X, int Y, int Z) RotateBlockCoordinates(int x, int y, int z, Rotation rotation) => rotation switch
    {
        Rotation.Clockwise90 => (Length - 1 - z, y, x),
        Rotation.Clockwise180 => (Width - 1 - x, y, Length - 1 - z),
        Rotation.Clockwise270 => (z, y, Width - 1 - x),
        _ => (x, y, z)
    };

    /// <summary>
    /// Computes the flat 1D index from 3D coordinates.
    /// Sponge &amp; Classic indexing convention: (y * Length + z) * Width + x.
    /// </summary>
    public int GetIndex(int x, int y, int z) => (y * Length + z) * Width + x;

    /// <summary>
    /// Generates C# source code using <see cref="SchematicBuilder"/> to construct this schematic.
    /// </summary>
    /// <param name="options">Generator configuration options.</param>
    /// <returns>A string of C# fluent builder code.</returns>
    public string ToFluentBuilderCode(FluentCodeGeneratorOptions? options = null) =>
        FluentBuilderCodeGenerator.Generate(this, options);

    /// <summary>
    /// Generates C# fluent builder source code for this schematic and writes it to disk.
    /// </summary>
    /// <param name="filePath">Target destination path.</param>
    /// <param name="options">Generator configuration options.</param>
    public void SaveFluentBuilderCode(string filePath, FluentCodeGeneratorOptions? options = null) =>
        FluentBuilderCodeGenerator.GenerateToFile(this, filePath, options);

    private void EnsureInBounds(int x, int y, int z)
    {
        if (x < 0 || x >= Width)
            throw new ArgumentOutOfRangeException(nameof(x), x, $"X coordinate must be between 0 and {Width - 1}.");
        if (y < 0 || y >= Height)
            throw new ArgumentOutOfRangeException(nameof(y), y, $"Y coordinate must be between 0 and {Height - 1}.");
        if (z < 0 || z >= Length)
            throw new ArgumentOutOfRangeException(nameof(z), z, $"Z coordinate must be between 0 and {Length - 1}.");
    }

    private void RemoveIncompatibleBlockEntities(BlockState blockState)
    {
        if (_blockEntities.Count == 0)
            return;

        if (blockState.IsAir)
        {
            _blockEntities.Clear();
            return;
        }

        RemoveIncompatibleBlockEntitiesInRegion(0, 0, 0, Width - 1, Height - 1, Length - 1, blockState);
    }

    private void RemoveIncompatibleBlockEntitiesInRegion(
        int startX, int startY, int startZ,
        int endX, int endY, int endZ,
        BlockState blockState)
    {
        if (_blockEntities.Count == 0)
            return;

        List<BlockPosition>? toRemove = null;
        foreach (var (pos, entity) in _blockEntities)
        {
            if (pos.X >= startX && pos.X <= endX &&
                pos.Y >= startY && pos.Y <= endY &&
                pos.Z >= startZ && pos.Z <= endZ &&
                !CanHostBlockEntity(blockState, entity))
            {
                toRemove ??= [];
                toRemove.Add(pos);
            }
        }

        if (toRemove != null)
        {
            foreach (var pos in toRemove)
            {
                _blockEntities.Remove(pos);
            }
        }
    }

    internal static bool CanHostBlockEntity(BlockState blockState, BlockEntity blockEntity)
    {
        if (blockState.IsAir)
            return false;

        string blockId = blockState.Identifier;
        string entityId = NormalizeEntityId(blockEntity.Id);

        if (string.Equals(blockId, entityId, StringComparison.OrdinalIgnoreCase))
            return true;

        if (blockEntity.IsSign)
        {
            if (entityId.Equals("minecraft:hanging_sign", StringComparison.OrdinalIgnoreCase) ||
                entityId.EndsWith("_hanging_sign", StringComparison.OrdinalIgnoreCase))
            {
                return blockId.EndsWith("_hanging_sign", StringComparison.OrdinalIgnoreCase);
            }

            return blockId.EndsWith("_sign", StringComparison.OrdinalIgnoreCase);
        }

        return entityId.ToLowerInvariant() switch
        {
            "minecraft:banner" => blockId.EndsWith("_banner", StringComparison.OrdinalIgnoreCase),
            "minecraft:bed" => blockId.EndsWith("_bed", StringComparison.OrdinalIgnoreCase),
            "minecraft:beehive" => blockId.Equals("minecraft:bee_nest", StringComparison.OrdinalIgnoreCase),
            "minecraft:brushable_block" => blockId.Equals("minecraft:suspicious_sand", StringComparison.OrdinalIgnoreCase) ||
                                           blockId.Equals("minecraft:suspicious_gravel", StringComparison.OrdinalIgnoreCase),
            "minecraft:campfire" => blockId.EndsWith("_campfire", StringComparison.OrdinalIgnoreCase),
            "minecraft:chest" => blockId.Equals("minecraft:copper_chest", StringComparison.OrdinalIgnoreCase) ||
                                 blockId.EndsWith("_copper_chest", StringComparison.OrdinalIgnoreCase) ||
                                 blockId.Equals("minecraft:trapped_chest", StringComparison.OrdinalIgnoreCase),
            "minecraft:command_block" => blockId.EndsWith("_command_block", StringComparison.OrdinalIgnoreCase),
            "minecraft:comparator" => blockId.EndsWith("_comparator", StringComparison.OrdinalIgnoreCase),
            "minecraft:copper_golem_statue" => blockId.EndsWith("_copper_golem_statue", StringComparison.OrdinalIgnoreCase),
            "minecraft:daylight_detector" => blockId.Equals("minecraft:daylight_detector_inverted", StringComparison.OrdinalIgnoreCase),
            "minecraft:furnace" => blockId.Equals("minecraft:lit_furnace", StringComparison.OrdinalIgnoreCase),
            "minecraft:hanging_sign" => blockId.EndsWith("_hanging_sign", StringComparison.OrdinalIgnoreCase),
            "minecraft:mob_spawner" or "minecraft:spawner" => blockId.Equals("minecraft:spawner", StringComparison.OrdinalIgnoreCase) ||
                                                              blockId.Equals("minecraft:mob_spawner", StringComparison.OrdinalIgnoreCase),
            "minecraft:piston" => blockId.Equals("minecraft:moving_piston", StringComparison.OrdinalIgnoreCase) ||
                                  blockId.Equals("minecraft:sticky_piston", StringComparison.OrdinalIgnoreCase) ||
                                  blockId.Equals("minecraft:piston_head", StringComparison.OrdinalIgnoreCase),
            "minecraft:shelf" => blockId.EndsWith("_shelf", StringComparison.OrdinalIgnoreCase),
            "minecraft:shulker_box" => blockId.EndsWith("_shulker_box", StringComparison.OrdinalIgnoreCase),
            "minecraft:sign" => blockId.EndsWith("_sign", StringComparison.OrdinalIgnoreCase),
            "minecraft:skull" => blockId.EndsWith("_skull", StringComparison.OrdinalIgnoreCase) ||
                                 blockId.EndsWith("_head", StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }

    private static string NormalizeEntityId(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return string.Empty;

        int bracket = id.IndexOf('[');
        if (bracket >= 0)
            id = id[..bracket];

        id = id.Trim();
        return id.Contains(':') ? id : "minecraft:" + id;
    }
}
