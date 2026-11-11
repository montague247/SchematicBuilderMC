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
    public int TotalBlocks => Width * Height * Length;

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

        Width = width;
        Height = height;
        Length = length;

        _blocks = new BlockState[width * height * length];
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
        if (blockState.IsAir)
        {
            _blockEntities.Clear();
        }
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
}
