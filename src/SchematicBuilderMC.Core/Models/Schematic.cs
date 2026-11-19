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
    private BlockState[] _blocks;
    private readonly Dictionary<BlockPosition, BlockEntity> _blockEntities = [];
    private readonly List<Entity> _entities = [];

    /// <summary>
    /// Size along the X axis.
    /// </summary>
    public int Width { get; private set; }

    /// <summary>
    /// Size along the Y axis.
    /// </summary>
    public int Height { get; private set; }

    /// <summary>
    /// Size along the Z axis.
    /// </summary>
    public int Length { get; private set; }

    /// <summary>
    /// Total block count (Width * Height * Length).
    /// </summary>
    public int TotalBlocks { get; private set; }

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
        int totalBlocks = ValidateDimensionsAndVolume(width, height, length);

        Width = width;
        Height = height;
        Length = length;
        TotalBlocks = totalBlocks;

        _blocks = new BlockState[totalBlocks];
        Array.Fill(_blocks, BlockState.Air);
    }

    /// <summary>
    /// Ensures this schematic is at least <paramref name="minWidth"/> x <paramref name="minHeight"/> x <paramref name="minLength"/>,
    /// expanding its dimensions while preserving existing blocks, block entities, and entities.
    /// </summary>
    public void EnsureDimensions(int minWidth, int minHeight, int minLength)
    {
        if (minWidth <= 0) throw new ArgumentOutOfRangeException(nameof(minWidth), minWidth, "Minimum width must be greater than zero.");
        if (minHeight <= 0) throw new ArgumentOutOfRangeException(nameof(minHeight), minHeight, "Minimum height must be greater than zero.");
        if (minLength <= 0) throw new ArgumentOutOfRangeException(nameof(minLength), minLength, "Minimum length must be greater than zero.");

        int newWidth = Math.Max(Width, minWidth);
        int newHeight = Math.Max(Height, minHeight);
        int newLength = Math.Max(Length, minLength);

        if (newWidth == Width && newHeight == Height && newLength == Length)
        {
            return;
        }

        int newTotalBlocks = ValidateDimensionsAndVolume(newWidth, newHeight, newLength);
        var newBlocks = new BlockState[newTotalBlocks];
        Array.Fill(newBlocks, BlockState.Air);

        for (int y = 0; y < Height; y++)
        {
            for (int z = 0; z < Length; z++)
            {
                int oldRowStart = (y * Length + z) * Width;
                int newRowStart = (y * newLength + z) * newWidth;
                Array.Copy(_blocks, oldRowStart, newBlocks, newRowStart, Width);
            }
        }

        _blocks = newBlocks;
        Width = newWidth;
        Height = newHeight;
        Length = newLength;
        TotalBlocks = newTotalBlocks;
    }

    /// <summary>
    /// Places an existing <paramref name="schematic"/> into this schematic starting at (startX, startY, startZ),
    /// automatically extending dimensions if the placed schematic exceeds the current bounds.
    /// </summary>
    public void Place(Schematic schematic, int startX = 0, int startY = 0, int startZ = 0, bool includeAir = false)
    {
        ArgumentNullException.ThrowIfNull(schematic);
        if (startX < 0) throw new ArgumentOutOfRangeException(nameof(startX), startX, "StartX must be zero or greater.");
        if (startY < 0) throw new ArgumentOutOfRangeException(nameof(startY), startY, "StartY must be zero or greater.");
        if (startZ < 0) throw new ArgumentOutOfRangeException(nameof(startZ), startZ, "StartZ must be zero or greater.");

        var source = ReferenceEquals(this, schematic) ? schematic.Clone() : schematic;
        EnsureDimensions(checked(startX + source.Width), checked(startY + source.Height), checked(startZ + source.Length));

        for (int y = 0; y < source.Height; y++)
        {
            for (int z = 0; z < source.Length; z++)
            {
                for (int x = 0; x < source.Width; x++)
                {
                    var state = source.GetBlock(x, y, z);
                    if (!state.IsAir || includeAir)
                    {
                        SetBlock(startX + x, startY + y, startZ + z, state);
                    }
                }
            }
        }

        foreach (var be in source.BlockEntities)
        {
            var beClone = be.Clone();
            beClone.Position = new BlockPosition(startX + be.Position.X, startY + be.Position.Y, startZ + be.Position.Z);
            SetBlockEntity(beClone);
        }

        bool targetUsesOffsetSpace = Offset != BlockPosition.Zero;
        foreach (var ent in source.Entities)
        {
            var entClone = ent.Clone();
            entClone.Data.Remove("Pos");

            bool sourceUsesOffsetSpace = source.Offset != BlockPosition.Zero &&
                                         (entClone.X < 0 || entClone.X > source.Width ||
                                          entClone.Y < 0 || entClone.Y > source.Height ||
                                          entClone.Z < 0 || entClone.Z > source.Length);

            double localX = sourceUsesOffsetSpace ? entClone.X - source.Offset.X : entClone.X;
            double localY = sourceUsesOffsetSpace ? entClone.Y - source.Offset.Y : entClone.Y;
            double localZ = sourceUsesOffsetSpace ? entClone.Z - source.Offset.Z : entClone.Z;

            entClone.X = localX + startX + (targetUsesOffsetSpace ? Offset.X : 0);
            entClone.Y = localY + startY + (targetUsesOffsetSpace ? Offset.Y : 0);
            entClone.Z = localZ + startZ + (targetUsesOffsetSpace ? Offset.Z : 0);
            _entities.Add(entClone);
        }

        foreach (string modId in source.Metadata.RequiredMods)
        {
            if (!string.IsNullOrWhiteSpace(modId) &&
                !Metadata.RequiredMods.Contains(modId, StringComparer.Ordinal))
            {
                Metadata.RequiredMods.Add(modId);
            }
        }
    }

    /// <summary>
    /// Places an existing <paramref name="schematic"/> into this schematic starting at <paramref name="start"/>,
    /// automatically extending dimensions if the placed schematic exceeds the current bounds.
    /// </summary>
    public void Place(Schematic schematic, BlockPosition start, bool includeAir = false) =>
        Place(schematic, start.X, start.Y, start.Z, includeAir);

    private static int ValidateDimensionsAndVolume(int width, int height, int length)
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

        return totalBlocks;
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
    /// Sets a trapdoor at (x, y, z) with specified horizontal facing, vertical placement (<paramref name="bottom"/>),
    /// open state (<paramref name="open"/>), powered state, and waterlogged state.
    /// </summary>
    public BlockState SetTrapdoor(
        int x,
        int y,
        int z,
        Direction facing = Direction.North,
        bool bottom = true,
        bool open = true,
        string trapdoorBlock = Minecraft.Blocks.OakTrapdoor,
        bool powered = false,
        bool waterlogged = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(trapdoorBlock);
        return SetTrapdoor(x, y, z, BlockState.Parse(trapdoorBlock), facing, bottom, open, powered, waterlogged);
    }

    /// <summary>
    /// Sets a trapdoor at <paramref name="position"/> with specified horizontal facing, vertical placement, and open state.
    /// </summary>
    public BlockState SetTrapdoor(
        BlockPosition position,
        Direction facing = Direction.North,
        bool bottom = true,
        bool open = true,
        string trapdoorBlock = Minecraft.Blocks.OakTrapdoor,
        bool powered = false,
        bool waterlogged = false) =>
        SetTrapdoor(position.X, position.Y, position.Z, facing, bottom, open, trapdoorBlock, powered, waterlogged);

    /// <summary>
    /// Sets a trapdoor using <paramref name="trapdoorState"/> at (x, y, z) with specified horizontal facing,
    /// vertical placement (<paramref name="bottom"/>), open state (<paramref name="open"/>), powered state, and waterlogged state.
    /// </summary>
    public BlockState SetTrapdoor(
        int x,
        int y,
        int z,
        BlockState trapdoorState,
        Direction facing = Direction.North,
        bool bottom = true,
        bool open = true,
        bool powered = false,
        bool waterlogged = false)
    {
        ArgumentNullException.ThrowIfNull(trapdoorState);
        var state = CreateTrapdoorState(trapdoorState, facing, bottom, open, powered, waterlogged);
        SetBlock(x, y, z, state);
        return state;
    }

    /// <summary>
    /// Sets a trapdoor using <paramref name="trapdoorState"/> at <paramref name="position"/>.
    /// </summary>
    public BlockState SetTrapdoor(
        BlockPosition position,
        BlockState trapdoorState,
        Direction facing = Direction.North,
        bool bottom = true,
        bool open = true,
        bool powered = false,
        bool waterlogged = false) =>
        SetTrapdoor(position.X, position.Y, position.Z, trapdoorState, facing, bottom, open, powered, waterlogged);

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
    public void SetCircle(int centerX, int centerY, int centerZ, int radius, BlockState blockState, bool filled = true, Axis normalAxis = Axis.Y, double smoothnessFactor = 1.0)
    {
        ArgumentNullException.ThrowIfNull(blockState);
        SetCircle(centerX, centerY, centerZ, radius, new BlockGradient([blockState]), filled, normalAxis, smoothnessFactor);
    }

    /// <summary>
    /// Places a filled or hollow circle centered at (centerX, centerY, centerZ) from a blockstate string.
    /// </summary>
    public void SetCircle(int centerX, int centerY, int centerZ, int radius, string blockStateString, bool filled = true, Axis normalAxis = Axis.Y, double smoothnessFactor = 1.0) =>
        SetCircle(centerX, centerY, centerZ, radius, BlockState.Parse(blockStateString), filled, normalAxis, smoothnessFactor);

    /// <summary>
    /// Places a filled or hollow circle centered at <paramref name="center"/>.
    /// </summary>
    public void SetCircle(BlockPosition center, int radius, BlockState blockState, bool filled = true, Axis normalAxis = Axis.Y, double smoothnessFactor = 1.0) =>
        SetCircle(center.X, center.Y, center.Z, radius, blockState, filled, normalAxis, smoothnessFactor);

    /// <summary>
    /// Places a filled or hollow circle centered at <paramref name="center"/> from a blockstate string.
    /// </summary>
    public void SetCircle(BlockPosition center, int radius, string blockStateString, bool filled = true, Axis normalAxis = Axis.Y, double smoothnessFactor = 1.0) =>
        SetCircle(center.X, center.Y, center.Z, radius, BlockState.Parse(blockStateString), filled, normalAxis, smoothnessFactor);

    /// <summary>
    /// Places a filled or hollow circle centered at (centerX, centerY, centerZ) using a multi-block <paramref name="gradient"/>.
    /// </summary>
    public void SetCircle(int centerX, int centerY, int centerZ, int radius, BlockGradient gradient, bool filled = true, Axis normalAxis = Axis.Y, double smoothnessFactor = 1.0)
    {
        ArgumentNullException.ThrowIfNull(gradient);
        if (radius < 0)
            throw new ArgumentOutOfRangeException(nameof(radius), radius, "Radius must be zero or greater.");
        ValidateAxis(normalAxis, nameof(normalAxis));
        double radialPadding = CalculateRadialPadding(smoothnessFactor);

        EnsureAxisBoxInBounds(centerX, centerY, centerZ, -radius, radius, -radius, radius, 0, 0, normalAxis);
        SetEllipticalSlice(centerX, centerY, centerZ, radius, radius, gradient, filled, normalAxis, radialPadding);
    }

    /// <summary>
    /// Places a filled or hollow circle centered at <paramref name="center"/> using a multi-block <paramref name="gradient"/>.
    /// </summary>
    public void SetCircle(BlockPosition center, int radius, BlockGradient gradient, bool filled = true, Axis normalAxis = Axis.Y, double smoothnessFactor = 1.0) =>
        SetCircle(center.X, center.Y, center.Z, radius, gradient, filled, normalAxis, smoothnessFactor);

    /// <summary>
    /// Places a filled or hollow sphere centered at (centerX, centerY, centerZ).
    /// </summary>
    public void SetSphere(int centerX, int centerY, int centerZ, int radius, BlockState blockState, bool filled = true, double smoothnessFactor = 1.0)
    {
        ArgumentNullException.ThrowIfNull(blockState);
        SetSphere(centerX, centerY, centerZ, radius, new BlockGradient([blockState]), filled, smoothnessFactor);
    }

    /// <summary>
    /// Places a filled or hollow sphere centered at (centerX, centerY, centerZ) from a blockstate string.
    /// </summary>
    public void SetSphere(int centerX, int centerY, int centerZ, int radius, string blockStateString, bool filled = true, double smoothnessFactor = 1.0) =>
        SetSphere(centerX, centerY, centerZ, radius, BlockState.Parse(blockStateString), filled, smoothnessFactor);

    /// <summary>
    /// Places a filled or hollow sphere centered at <paramref name="center"/>.
    /// </summary>
    public void SetSphere(BlockPosition center, int radius, BlockState blockState, bool filled = true, double smoothnessFactor = 1.0) =>
        SetSphere(center.X, center.Y, center.Z, radius, blockState, filled, smoothnessFactor);

    /// <summary>
    /// Places a filled or hollow sphere centered at <paramref name="center"/> from a blockstate string.
    /// </summary>
    public void SetSphere(BlockPosition center, int radius, string blockStateString, bool filled = true, double smoothnessFactor = 1.0) =>
        SetSphere(center.X, center.Y, center.Z, radius, BlockState.Parse(blockStateString), filled, smoothnessFactor);

    /// <summary>
    /// Places a filled or hollow sphere centered at (centerX, centerY, centerZ) using a multi-block <paramref name="gradient"/>.
    /// </summary>
    public void SetSphere(int centerX, int centerY, int centerZ, int radius, BlockGradient gradient, bool filled = true, double smoothnessFactor = 1.0)
    {
        ArgumentNullException.ThrowIfNull(gradient);
        if (radius < 0)
            throw new ArgumentOutOfRangeException(nameof(radius), radius, "Radius must be zero or greater.");
        double radialPadding = CalculateRadialPadding(smoothnessFactor);

        EnsureInBounds(centerX - radius, centerY - radius, centerZ - radius);
        EnsureInBounds(centerX + radius, centerY + radius, centerZ + radius);

        double outerRadiusSq = (radius + radialPadding) * (radius + radialPadding);
        bool IsInside(int dx, int dy, int dz) => dx * dx + dy * dy + dz * dz <= outerRadiusSq;

        int totalSteps = 2 * radius + 1;
        Random? random = gradient.CreateRandom();

        for (int dy = -radius; dy <= radius; dy++)
        {
            int stepIndex = dy + radius;
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

                    SetBlock(centerX + dx, centerY + dy, centerZ + dz, gradient.GetBlock(stepIndex, totalSteps, random));
                }
            }
        }
    }

    /// <summary>
    /// Places a filled or hollow sphere centered at <paramref name="center"/> using a multi-block <paramref name="gradient"/>.
    /// </summary>
    public void SetSphere(BlockPosition center, int radius, BlockGradient gradient, bool filled = true, double smoothnessFactor = 1.0) =>
        SetSphere(center.X, center.Y, center.Z, radius, gradient, filled, smoothnessFactor);

    /// <summary>
    /// Places a filled or hollow rectangle starting at (startX, startY, startZ) in the plane perpendicular to <paramref name="normalAxis"/>.
    /// </summary>
    public void SetRectangle(int startX, int startY, int startZ, int width, int length, BlockState blockState, bool filled = true, Axis normalAxis = Axis.Y)
    {
        ArgumentNullException.ThrowIfNull(blockState);
        SetRectangle(startX, startY, startZ, width, length, new BlockGradient([blockState]), filled, normalAxis);
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
    /// Places a filled or hollow rectangle starting at (startX, startY, startZ) using a multi-block <paramref name="gradient"/>.
    /// </summary>
    public void SetRectangle(int startX, int startY, int startZ, int width, int length, BlockGradient gradient, bool filled = true, Axis normalAxis = Axis.Y)
    {
        ArgumentNullException.ThrowIfNull(gradient);
        if (width <= 0)
            throw new ArgumentOutOfRangeException(nameof(width), width, "Width must be greater than zero.");
        if (length <= 0)
            throw new ArgumentOutOfRangeException(nameof(length), length, "Length must be greater than zero.");
        ValidateAxis(normalAxis, nameof(normalAxis));

        EnsureAxisBoxInBounds(startX, startY, startZ, 0, width - 1, 0, length - 1, 0, 0, normalAxis);

        bool useU = length == 1 && width > 1;
        int totalSteps = useU ? width : length;
        Random? random = gradient.CreateRandom();

        for (int v = 0; v < length; v++)
        {
            for (int u = 0; u < width; u++)
            {
                if (filled || u == 0 || u == width - 1 || v == 0 || v == length - 1)
                {
                    int stepIndex = useU ? u : v;
                    var pos = MapAxisOffset(startX, startY, startZ, u, v, 0, normalAxis);
                    SetBlock(pos.X, pos.Y, pos.Z, gradient.GetBlock(stepIndex, totalSteps, random));
                }
            }
        }
    }

    /// <summary>
    /// Places a filled or hollow rectangle starting at <paramref name="start"/> using a multi-block <paramref name="gradient"/>.
    /// </summary>
    public void SetRectangle(BlockPosition start, int width, int length, BlockGradient gradient, bool filled = true, Axis normalAxis = Axis.Y) =>
        SetRectangle(start.X, start.Y, start.Z, width, length, gradient, filled, normalAxis);

    /// <summary>
    /// Places a filled or hollow cube starting at (startX, startY, startZ) with edge length <paramref name="size"/>.
    /// </summary>
    public void SetCube(int startX, int startY, int startZ, int size, BlockState blockState, bool filled = true)
    {
        ArgumentNullException.ThrowIfNull(blockState);
        SetCube(startX, startY, startZ, size, new BlockGradient([blockState]), filled);
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
    /// Places a filled or hollow cube starting at (startX, startY, startZ) using a multi-block <paramref name="gradient"/>.
    /// </summary>
    public void SetCube(int startX, int startY, int startZ, int size, BlockGradient gradient, bool filled = true)
    {
        ArgumentNullException.ThrowIfNull(gradient);
        if (size <= 0)
            throw new ArgumentOutOfRangeException(nameof(size), size, "Cube size must be greater than zero.");

        EnsureInBounds(startX, startY, startZ);
        EnsureInBounds(startX + size - 1, startY + size - 1, startZ + size - 1);

        if (filled && gradient.Blocks.Count == 1)
        {
            Fill(startX, startY, startZ, startX + size - 1, startY + size - 1, startZ + size - 1, gradient.Blocks[0]);
            return;
        }

        Random? random = gradient.CreateRandom();
        for (int dy = 0; dy < size; dy++)
        {
            for (int dz = 0; dz < size; dz++)
            {
                for (int dx = 0; dx < size; dx++)
                {
                    if (filled ||
                        dx == 0 || dx == size - 1 ||
                        dy == 0 || dy == size - 1 ||
                        dz == 0 || dz == size - 1)
                    {
                        SetBlock(startX + dx, startY + dy, startZ + dz, gradient.GetBlock(dy, size, random));
                    }
                }
            }
        }
    }

    /// <summary>
    /// Places a filled or hollow cube starting at <paramref name="start"/> using a multi-block <paramref name="gradient"/>.
    /// </summary>
    public void SetCube(BlockPosition start, int size, BlockGradient gradient, bool filled = true) =>
        SetCube(start.X, start.Y, start.Z, size, gradient, filled);

    /// <summary>
    /// Places a filled or hollow oval (ellipse) centered at (centerX, centerY, centerZ) in the plane perpendicular to <paramref name="normalAxis"/>.
    /// </summary>
    public void SetOval(int centerX, int centerY, int centerZ, int radiusU, int radiusV, BlockState blockState, bool filled = true, Axis normalAxis = Axis.Y, double smoothnessFactor = 1.0)
    {
        ArgumentNullException.ThrowIfNull(blockState);
        SetOval(centerX, centerY, centerZ, radiusU, radiusV, new BlockGradient([blockState]), filled, normalAxis, smoothnessFactor);
    }

    /// <summary>
    /// Places a filled or hollow oval centered at (centerX, centerY, centerZ) from a blockstate string.
    /// </summary>
    public void SetOval(int centerX, int centerY, int centerZ, int radiusU, int radiusV, string blockStateString, bool filled = true, Axis normalAxis = Axis.Y, double smoothnessFactor = 1.0) =>
        SetOval(centerX, centerY, centerZ, radiusU, radiusV, BlockState.Parse(blockStateString), filled, normalAxis, smoothnessFactor);

    /// <summary>
    /// Places a filled or hollow oval centered at <paramref name="center"/>.
    /// </summary>
    public void SetOval(BlockPosition center, int radiusU, int radiusV, BlockState blockState, bool filled = true, Axis normalAxis = Axis.Y, double smoothnessFactor = 1.0) =>
        SetOval(center.X, center.Y, center.Z, radiusU, radiusV, blockState, filled, normalAxis, smoothnessFactor);

    /// <summary>
    /// Places a filled or hollow oval centered at <paramref name="center"/> from a blockstate string.
    /// </summary>
    public void SetOval(BlockPosition center, int radiusU, int radiusV, string blockStateString, bool filled = true, Axis normalAxis = Axis.Y, double smoothnessFactor = 1.0) =>
        SetOval(center.X, center.Y, center.Z, radiusU, radiusV, BlockState.Parse(blockStateString), filled, normalAxis, smoothnessFactor);

    /// <summary>
    /// Places a filled or hollow oval centered at (centerX, centerY, centerZ) using a multi-block <paramref name="gradient"/>.
    /// </summary>
    public void SetOval(int centerX, int centerY, int centerZ, int radiusU, int radiusV, BlockGradient gradient, bool filled = true, Axis normalAxis = Axis.Y, double smoothnessFactor = 1.0)
    {
        ArgumentNullException.ThrowIfNull(gradient);
        if (radiusU < 0)
            throw new ArgumentOutOfRangeException(nameof(radiusU), radiusU, "RadiusU must be zero or greater.");
        if (radiusV < 0)
            throw new ArgumentOutOfRangeException(nameof(radiusV), radiusV, "RadiusV must be zero or greater.");
        ValidateAxis(normalAxis, nameof(normalAxis));
        double radialPadding = CalculateRadialPadding(smoothnessFactor);

        EnsureAxisBoxInBounds(centerX, centerY, centerZ, -radiusU, radiusU, -radiusV, radiusV, 0, 0, normalAxis);
        SetEllipticalSlice(centerX, centerY, centerZ, radiusU, radiusV, gradient, filled, normalAxis, radialPadding);
    }

    /// <summary>
    /// Places a filled or hollow oval centered at <paramref name="center"/> using a multi-block <paramref name="gradient"/>.
    /// </summary>
    public void SetOval(BlockPosition center, int radiusU, int radiusV, BlockGradient gradient, bool filled = true, Axis normalAxis = Axis.Y, double smoothnessFactor = 1.0) =>
        SetOval(center.X, center.Y, center.Z, radiusU, radiusV, gradient, filled, normalAxis, smoothnessFactor);

    /// <summary>
    /// Places a filled or hollow cylinder starting at base center (centerX, startY, centerZ) extending along <paramref name="axis"/>,
    /// optionally surrounded by a stepped base of width <paramref name="baseWidth"/> and height <paramref name="baseHeight"/>,
    /// an optional stepped top flare of width <paramref name="topWidth"/> and height <paramref name="topHeight"/>,
    /// and optionally filled with inward-flowing water at the bottom.
    /// </summary>
    public void SetCylinder(
        int centerX,
        int startY,
        int centerZ,
        int radius,
        int height,
        BlockState blockState,
        bool filled = true,
        Axis axis = Axis.Y,
        double smoothnessFactor = 1.0,
        int baseWidth = 0,
        int baseHeight = 0,
        BlockGradient? baseGradient = null,
        bool baseUseStairs = true,
        string? baseStairBlockId = null,
        bool includeWater = false,
        BlockState? waterFloorBlock = null,
        int topWidth = 0,
        int topHeight = 0,
        BlockGradient? topGradient = null,
        bool topUseStairs = true,
        string? topStairBlockId = null)
    {
        ArgumentNullException.ThrowIfNull(blockState);
        SetCylinder(centerX, startY, centerZ, radius, height, new BlockGradient([blockState]), filled, axis, smoothnessFactor, baseWidth, baseHeight, baseGradient, baseUseStairs, baseStairBlockId, includeWater, waterFloorBlock, topWidth, topHeight, topGradient, topUseStairs, topStairBlockId);
    }

    /// <summary>
    /// Places a filled or hollow cylinder starting at base center (centerX, startY, centerZ) from a blockstate string,
    /// optionally surrounded by a stepped base, stepped top flare, and inward-flowing water.
    /// </summary>
    public void SetCylinder(
        int centerX,
        int startY,
        int centerZ,
        int radius,
        int height,
        string blockStateString,
        bool filled = true,
        Axis axis = Axis.Y,
        double smoothnessFactor = 1.0,
        int baseWidth = 0,
        int baseHeight = 0,
        BlockGradient? baseGradient = null,
        bool baseUseStairs = true,
        string? baseStairBlockId = null,
        bool includeWater = false,
        BlockState? waterFloorBlock = null,
        int topWidth = 0,
        int topHeight = 0,
        BlockGradient? topGradient = null,
        bool topUseStairs = true,
        string? topStairBlockId = null) =>
        SetCylinder(centerX, startY, centerZ, radius, height, BlockState.Parse(blockStateString), filled, axis, smoothnessFactor, baseWidth, baseHeight, baseGradient, baseUseStairs, baseStairBlockId, includeWater, waterFloorBlock, topWidth, topHeight, topGradient, topUseStairs, topStairBlockId);

    /// <summary>
    /// Places a filled or hollow cylinder starting at <paramref name="baseCenter"/>,
    /// optionally surrounded by a stepped base, stepped top flare, and inward-flowing water.
    /// </summary>
    public void SetCylinder(
        BlockPosition baseCenter,
        int radius,
        int height,
        BlockState blockState,
        bool filled = true,
        Axis axis = Axis.Y,
        double smoothnessFactor = 1.0,
        int baseWidth = 0,
        int baseHeight = 0,
        BlockGradient? baseGradient = null,
        bool baseUseStairs = true,
        string? baseStairBlockId = null,
        bool includeWater = false,
        BlockState? waterFloorBlock = null,
        int topWidth = 0,
        int topHeight = 0,
        BlockGradient? topGradient = null,
        bool topUseStairs = true,
        string? topStairBlockId = null) =>
        SetCylinder(baseCenter.X, baseCenter.Y, baseCenter.Z, radius, height, blockState, filled, axis, smoothnessFactor, baseWidth, baseHeight, baseGradient, baseUseStairs, baseStairBlockId, includeWater, waterFloorBlock, topWidth, topHeight, topGradient, topUseStairs, topStairBlockId);

    /// <summary>
    /// Places a filled or hollow cylinder starting at <paramref name="baseCenter"/> from a blockstate string,
    /// optionally surrounded by a stepped base, stepped top flare, and inward-flowing water.
    /// </summary>
    public void SetCylinder(
        BlockPosition baseCenter,
        int radius,
        int height,
        string blockStateString,
        bool filled = true,
        Axis axis = Axis.Y,
        double smoothnessFactor = 1.0,
        int baseWidth = 0,
        int baseHeight = 0,
        BlockGradient? baseGradient = null,
        bool baseUseStairs = true,
        string? baseStairBlockId = null,
        bool includeWater = false,
        BlockState? waterFloorBlock = null,
        int topWidth = 0,
        int topHeight = 0,
        BlockGradient? topGradient = null,
        bool topUseStairs = true,
        string? topStairBlockId = null) =>
        SetCylinder(baseCenter.X, baseCenter.Y, baseCenter.Z, radius, height, BlockState.Parse(blockStateString), filled, axis, smoothnessFactor, baseWidth, baseHeight, baseGradient, baseUseStairs, baseStairBlockId, includeWater, waterFloorBlock, topWidth, topHeight, topGradient, topUseStairs, topStairBlockId);

    /// <summary>
    /// Places a filled or hollow cylinder starting at base center (centerX, startY, centerZ) using a multi-block <paramref name="gradient"/>,
    /// optionally surrounded by a stepped base of width <paramref name="baseWidth"/> and height <paramref name="baseHeight"/>,
    /// an optional stepped top flare of width <paramref name="topWidth"/> and height <paramref name="topHeight"/>,
    /// and optionally filled with inward-flowing water at the bottom.
    /// </summary>
    public void SetCylinder(
        int centerX,
        int startY,
        int centerZ,
        int radius,
        int height,
        BlockGradient gradient,
        bool filled = true,
        Axis axis = Axis.Y,
        double smoothnessFactor = 1.0,
        int baseWidth = 0,
        int baseHeight = 0,
        BlockGradient? baseGradient = null,
        bool baseUseStairs = true,
        string? baseStairBlockId = null,
        bool includeWater = false,
        BlockState? waterFloorBlock = null,
        int topWidth = 0,
        int topHeight = 0,
        BlockGradient? topGradient = null,
        bool topUseStairs = true,
        string? topStairBlockId = null)
    {
        ArgumentNullException.ThrowIfNull(gradient);
        if (radius < 0)
            throw new ArgumentOutOfRangeException(nameof(radius), radius, "Radius must be zero or greater.");
        if (height <= 0)
            throw new ArgumentOutOfRangeException(nameof(height), height, "Height must be greater than zero.");
        ValidateAxis(axis, nameof(axis));
        ValidateCylinderCollarDimensions(baseWidth, baseHeight, topWidth, topHeight, height);
        if (includeWater)
        {
            if (filled)
                throw new ArgumentException("Water flow can only be placed inside a hollow cylinder (filled must be false).", nameof(includeWater));
            if (radius < 1)
                throw new ArgumentOutOfRangeException(nameof(radius), radius, "Cylinder radius must be at least 1 when placing water inside.");
            if (axis != Axis.Y)
                throw new ArgumentOutOfRangeException(nameof(axis), axis, "Cylinder water flow is only supported for vertical cylinders (Axis.Y).");
        }
        double radialPadding = CalculateRadialPadding(smoothnessFactor);

        int maxOuterRadius = radius + Math.Max(baseWidth, topWidth);
        EnsureAxisBoxInBounds(centerX, startY, centerZ, -maxOuterRadius, maxOuterRadius, -maxOuterRadius, maxOuterRadius, 0, height - 1, axis);

        Random? random = gradient.CreateRandom();
        for (int h = 0; h < height; h++)
        {
            var sliceCenter = MapAxisOffset(centerX, startY, centerZ, 0, 0, h, axis);
            SetEllipticalSlice(sliceCenter.X, sliceCenter.Y, sliceCenter.Z, radius, radius, gradient, filled, axis, radialPadding, h, height, random);
        }

        if (baseWidth > 0)
        {
            SetCylinderBase(centerX, startY, centerZ, radius, baseWidth, baseHeight, baseGradient ?? gradient, axis, smoothnessFactor, baseUseStairs, baseStairBlockId);
        }

        if (topWidth > 0)
        {
            var topStart = MapAxisOffset(centerX, startY, centerZ, 0, 0, height - topHeight, axis);
            SetCylinderTop(topStart.X, topStart.Y, topStart.Z, radius, topWidth, topHeight, topGradient ?? gradient, axis, smoothnessFactor, topUseStairs, topStairBlockId);
        }

        if (includeWater)
        {
            SetCylinderWater(centerX, startY, centerZ, radius, axis, smoothnessFactor, waterFloorBlock ?? ResolveWaterTierFloorBlock(gradient), height);
        }
    }

    /// <summary>
    /// Places a filled or hollow cylinder starting at <paramref name="baseCenter"/> using a multi-block <paramref name="gradient"/>,
    /// optionally surrounded by a stepped base, stepped top flare, and inward-flowing water.
    /// </summary>
    public void SetCylinder(
        BlockPosition baseCenter,
        int radius,
        int height,
        BlockGradient gradient,
        bool filled = true,
        Axis axis = Axis.Y,
        double smoothnessFactor = 1.0,
        int baseWidth = 0,
        int baseHeight = 0,
        BlockGradient? baseGradient = null,
        bool baseUseStairs = true,
        string? baseStairBlockId = null,
        bool includeWater = false,
        BlockState? waterFloorBlock = null,
        int topWidth = 0,
        int topHeight = 0,
        BlockGradient? topGradient = null,
        bool topUseStairs = true,
        string? topStairBlockId = null) =>
        SetCylinder(baseCenter.X, baseCenter.Y, baseCenter.Z, radius, height, gradient, filled, axis, smoothnessFactor, baseWidth, baseHeight, baseGradient, baseUseStairs, baseStairBlockId, includeWater, waterFloorBlock, topWidth, topHeight, topGradient, topUseStairs, topStairBlockId);

    /// <summary>
    /// Surrounds a cylinder at base center (centerX, startY, centerZ) with a stepped base extending outward by <paramref name="baseWidth"/>
    /// blocks and rising along <paramref name="axis"/> for <paramref name="baseHeight"/> blocks using oriented stairs for smooth transitions.
    /// </summary>
    public void SetCylinderBase(
        int centerX,
        int startY,
        int centerZ,
        int cylinderRadius,
        int baseWidth,
        int baseHeight,
        BlockState blockState,
        Axis axis = Axis.Y,
        double smoothnessFactor = 1.0,
        bool useStairs = true,
        string? stairBlockId = null)
    {
        ArgumentNullException.ThrowIfNull(blockState);
        SetCylinderBase(centerX, startY, centerZ, cylinderRadius, baseWidth, baseHeight, new BlockGradient([blockState]), axis, smoothnessFactor, useStairs, stairBlockId);
    }

    /// <summary>
    /// Surrounds a cylinder at base center (centerX, startY, centerZ) with a stepped stair base from a blockstate string.
    /// </summary>
    public void SetCylinderBase(
        int centerX,
        int startY,
        int centerZ,
        int cylinderRadius,
        int baseWidth,
        int baseHeight,
        string blockStateString,
        Axis axis = Axis.Y,
        double smoothnessFactor = 1.0,
        bool useStairs = true,
        string? stairBlockId = null) =>
        SetCylinderBase(centerX, startY, centerZ, cylinderRadius, baseWidth, baseHeight, BlockState.Parse(blockStateString), axis, smoothnessFactor, useStairs, stairBlockId);

    /// <summary>
    /// Surrounds a cylinder at <paramref name="baseCenter"/> with a stepped stair base.
    /// </summary>
    public void SetCylinderBase(
        BlockPosition baseCenter,
        int cylinderRadius,
        int baseWidth,
        int baseHeight,
        BlockState blockState,
        Axis axis = Axis.Y,
        double smoothnessFactor = 1.0,
        bool useStairs = true,
        string? stairBlockId = null) =>
        SetCylinderBase(baseCenter.X, baseCenter.Y, baseCenter.Z, cylinderRadius, baseWidth, baseHeight, blockState, axis, smoothnessFactor, useStairs, stairBlockId);

    /// <summary>
    /// Surrounds a cylinder at <paramref name="baseCenter"/> with a stepped stair base from a blockstate string.
    /// </summary>
    public void SetCylinderBase(
        BlockPosition baseCenter,
        int cylinderRadius,
        int baseWidth,
        int baseHeight,
        string blockStateString,
        Axis axis = Axis.Y,
        double smoothnessFactor = 1.0,
        bool useStairs = true,
        string? stairBlockId = null) =>
        SetCylinderBase(baseCenter.X, baseCenter.Y, baseCenter.Z, cylinderRadius, baseWidth, baseHeight, BlockState.Parse(blockStateString), axis, smoothnessFactor, useStairs, stairBlockId);

    /// <summary>
    /// Surrounds a cylinder at base center (centerX, startY, centerZ) with a stepped base extending outward by <paramref name="baseWidth"/>
    /// blocks and rising along <paramref name="axis"/> for <paramref name="baseHeight"/> blocks using <paramref name="gradient"/>.
    /// </summary>
    public void SetCylinderBase(
        int centerX,
        int startY,
        int centerZ,
        int cylinderRadius,
        int baseWidth,
        int baseHeight,
        BlockGradient gradient,
        Axis axis = Axis.Y,
        double smoothnessFactor = 1.0,
        bool useStairs = true,
        string? stairBlockId = null) =>
        SetCylinderCollar(centerX, startY, centerZ, cylinderRadius, baseWidth, baseHeight, gradient, axis, smoothnessFactor, useStairs, stairBlockId, isTop: false);

    /// <summary>
    /// Surrounds a cylinder at <paramref name="baseCenter"/> with a stepped stair base using <paramref name="gradient"/>.
    /// </summary>
    public void SetCylinderBase(
        BlockPosition baseCenter,
        int cylinderRadius,
        int baseWidth,
        int baseHeight,
        BlockGradient gradient,
        Axis axis = Axis.Y,
        double smoothnessFactor = 1.0,
        bool useStairs = true,
        string? stairBlockId = null) =>
        SetCylinderBase(baseCenter.X, baseCenter.Y, baseCenter.Z, cylinderRadius, baseWidth, baseHeight, gradient, axis, smoothnessFactor, useStairs, stairBlockId);

    /// <summary>
    /// Surrounds the upper section of a cylinder starting at (centerX, startY, centerZ) with a stepped outward-flaring top collar
    /// extending outward by <paramref name="topWidth"/> blocks and rising along <paramref name="axis"/> for <paramref name="topHeight"/> blocks
    /// using inverted stairs (<c>half=top</c>) for smooth transitions.
    /// </summary>
    public void SetCylinderTop(
        int centerX,
        int startY,
        int centerZ,
        int cylinderRadius,
        int topWidth,
        int topHeight,
        BlockState blockState,
        Axis axis = Axis.Y,
        double smoothnessFactor = 1.0,
        bool useStairs = true,
        string? stairBlockId = null)
    {
        ArgumentNullException.ThrowIfNull(blockState);
        SetCylinderTop(centerX, startY, centerZ, cylinderRadius, topWidth, topHeight, new BlockGradient([blockState]), axis, smoothnessFactor, useStairs, stairBlockId);
    }

    /// <summary>
    /// Surrounds the upper section of a cylinder at (centerX, startY, centerZ) with a stepped stair top collar from a blockstate string.
    /// </summary>
    public void SetCylinderTop(
        int centerX,
        int startY,
        int centerZ,
        int cylinderRadius,
        int topWidth,
        int topHeight,
        string blockStateString,
        Axis axis = Axis.Y,
        double smoothnessFactor = 1.0,
        bool useStairs = true,
        string? stairBlockId = null) =>
        SetCylinderTop(centerX, startY, centerZ, cylinderRadius, topWidth, topHeight, BlockState.Parse(blockStateString), axis, smoothnessFactor, useStairs, stairBlockId);

    /// <summary>
    /// Surrounds the upper section of a cylinder starting at <paramref name="topStart"/> with a stepped stair top collar.
    /// </summary>
    public void SetCylinderTop(
        BlockPosition topStart,
        int cylinderRadius,
        int topWidth,
        int topHeight,
        BlockState blockState,
        Axis axis = Axis.Y,
        double smoothnessFactor = 1.0,
        bool useStairs = true,
        string? stairBlockId = null) =>
        SetCylinderTop(topStart.X, topStart.Y, topStart.Z, cylinderRadius, topWidth, topHeight, blockState, axis, smoothnessFactor, useStairs, stairBlockId);

    /// <summary>
    /// Surrounds the upper section of a cylinder starting at <paramref name="topStart"/> with a stepped stair top collar from a blockstate string.
    /// </summary>
    public void SetCylinderTop(
        BlockPosition topStart,
        int cylinderRadius,
        int topWidth,
        int topHeight,
        string blockStateString,
        Axis axis = Axis.Y,
        double smoothnessFactor = 1.0,
        bool useStairs = true,
        string? stairBlockId = null) =>
        SetCylinderTop(topStart.X, topStart.Y, topStart.Z, cylinderRadius, topWidth, topHeight, BlockState.Parse(blockStateString), axis, smoothnessFactor, useStairs, stairBlockId);

    /// <summary>
    /// Surrounds the upper section of a cylinder starting at (centerX, startY, centerZ) with a stepped outward-flaring top collar
    /// extending outward by <paramref name="topWidth"/> blocks and rising along <paramref name="axis"/> for <paramref name="topHeight"/> blocks using <paramref name="gradient"/>.
    /// </summary>
    public void SetCylinderTop(
        int centerX,
        int startY,
        int centerZ,
        int cylinderRadius,
        int topWidth,
        int topHeight,
        BlockGradient gradient,
        Axis axis = Axis.Y,
        double smoothnessFactor = 1.0,
        bool useStairs = true,
        string? stairBlockId = null) =>
        SetCylinderCollar(centerX, startY, centerZ, cylinderRadius, topWidth, topHeight, gradient, axis, smoothnessFactor, useStairs, stairBlockId, isTop: true);

    /// <summary>
    /// Surrounds the upper section of a cylinder starting at <paramref name="topStart"/> with a stepped stair top collar using <paramref name="gradient"/>.
    /// </summary>
    public void SetCylinderTop(
        BlockPosition topStart,
        int cylinderRadius,
        int topWidth,
        int topHeight,
        BlockGradient gradient,
        Axis axis = Axis.Y,
        double smoothnessFactor = 1.0,
        bool useStairs = true,
        string? stairBlockId = null) =>
        SetCylinderTop(topStart.X, topStart.Y, topStart.Z, cylinderRadius, topWidth, topHeight, gradient, axis, smoothnessFactor, useStairs, stairBlockId);

    /// <summary>
    /// Places non-chaining water source blocks along the interior wall perimeter and pre-computes flowing water streams
    /// inward to the center of the cylinder at (centerX, startY, centerZ). For radii whose horizontal flow distance exceeds
    /// Minecraft's 7-block limit, stepped floor tiers are constructed automatically so water cascades cleanly to the center.
    /// </summary>
    public void SetCylinderWater(
        int centerX,
        int startY,
        int centerZ,
        int cylinderRadius,
        Axis axis = Axis.Y,
        double smoothnessFactor = 1.0,
        BlockState? tierFloorBlock = null,
        int? maxHeight = null)
    {
        if (cylinderRadius < 1)
            throw new ArgumentOutOfRangeException(nameof(cylinderRadius), cylinderRadius, "Cylinder radius must be at least 1 to contain water.");
        ValidateAxis(axis, nameof(axis));
        if (axis != Axis.Y)
            throw new ArgumentOutOfRangeException(nameof(axis), axis, "Cylinder water flow is only supported for vertical cylinders (Axis.Y).");

        double radialPadding = CalculateRadialPadding(smoothnessFactor);
        double circleLimitSq = (cylinderRadius + radialPadding) * (cylinderRadius + radialPadding);

        bool IsInterior(int du, int dv) =>
            IsInsideRadiusSq(du, dv, circleLimitSq) &&
            IsInsideRadiusSq(du + 1, dv, circleLimitSq) &&
            IsInsideRadiusSq(du - 1, dv, circleLimitSq) &&
            IsInsideRadiusSq(du, dv + 1, circleLimitSq) &&
            IsInsideRadiusSq(du, dv - 1, circleLimitSq);

        var interiorCells = new HashSet<(int Du, int Dv)>();
        int maxInteriorRadius = 0;
        for (int dv = -cylinderRadius; dv <= cylinderRadius; dv++)
        {
            for (int du = -cylinderRadius; du <= cylinderRadius; du++)
            {
                if (!IsInterior(du, dv))
                {
                    continue;
                }

                interiorCells.Add((du, dv));
                maxInteriorRadius = Math.Max(maxInteriorRadius, Math.Max(Math.Abs(du), Math.Abs(dv)));
            }
        }

        if (interiorCells.Count == 0)
            return;

        if (interiorCells.Count == 1 && interiorCells.Contains((0, 0)))
        {
            EnsureAxisBoxInBounds(centerX, startY, centerZ, 0, 0, 0, 0, 0, 0, axis);
            SetBlock(centerX, startY, centerZ, Minecraft.Blocks.Water.WithLevel(0));
            return;
        }

        int tierCount = maxInteriorRadius <= 7
            ? 1
            : 1 + (int)Math.Ceiling((maxInteriorRadius - 6.0) / 5.0);

        if (maxHeight.HasValue && tierCount > maxHeight.Value)
        {
            throw new ArgumentOutOfRangeException(nameof(maxHeight), maxHeight.Value, $"Cylinder height ({maxHeight.Value}) must be at least {tierCount} to support stepped water flow for radius {cylinderRadius}.");
        }

        EnsureAxisBoxInBounds(centerX, startY, centerZ, -maxInteriorRadius, maxInteriorRadius, -maxInteriorRadius, maxInteriorRadius, 0, tierCount - 1, axis);

        BlockState floorBlock = tierFloorBlock ?? new BlockState(Minecraft.Blocks.StoneBricks);
        Dictionary<(int Du, int Dv), int> cellTier = new(interiorCells.Count);
        foreach (var cell in interiorCells)
        {
            cellTier[cell] = CalculateWaterTier(cell.Du, cell.Dv, tierCount, radialPadding);
        }

        if (tierCount > 1)
        {
            foreach (var (cell, tier) in cellTier)
            {
                for (int y = 0; y < tier; y++)
                {
                    SetBlock(centerX + cell.Du, startY + y, centerZ + cell.Dv, floorBlock);
                }
            }
        }

        BuildCylinderWaterFlow(centerX, startY, centerZ, maxInteriorRadius, tierCount, cellTier);
    }

    /// <summary>
    /// Places inward-flowing water inside a cylinder centered at <paramref name="baseCenter"/>.
    /// </summary>
    public void SetCylinderWater(
        BlockPosition baseCenter,
        int cylinderRadius,
        Axis axis = Axis.Y,
        double smoothnessFactor = 1.0,
        BlockState? tierFloorBlock = null,
        int? maxHeight = null) =>
        SetCylinderWater(baseCenter.X, baseCenter.Y, baseCenter.Z, cylinderRadius, axis, smoothnessFactor, tierFloorBlock, maxHeight);

    /// <summary>
    /// Places inward-flowing water inside a cylinder centered at (centerX, startY, centerZ) using a string floor block identifier for stepped tiers.
    /// </summary>
    public void SetCylinderWater(
        int centerX,
        int startY,
        int centerZ,
        int cylinderRadius,
        string tierFloorBlockString,
        Axis axis = Axis.Y,
        double smoothnessFactor = 1.0,
        int? maxHeight = null) =>
        SetCylinderWater(centerX, startY, centerZ, cylinderRadius, axis, smoothnessFactor, BlockState.Parse(tierFloorBlockString), maxHeight);

    /// <summary>
    /// Places inward-flowing water inside a cylinder centered at <paramref name="baseCenter"/> using a string floor block identifier for stepped tiers.
    /// </summary>
    public void SetCylinderWater(
        BlockPosition baseCenter,
        int cylinderRadius,
        string tierFloorBlockString,
        Axis axis = Axis.Y,
        double smoothnessFactor = 1.0,
        int? maxHeight = null) =>
        SetCylinderWater(baseCenter.X, baseCenter.Y, baseCenter.Z, cylinderRadius, axis, smoothnessFactor, BlockState.Parse(tierFloorBlockString), maxHeight);

    /// <summary>
    /// Surrounds a single block at (x, y, z) on all four horizontal sides with trapdoors facing outward from the block.
    /// </summary>
    public void SurroundWithTrapdoors(
        int x,
        int y,
        int z,
        string trapdoorBlock = Minecraft.Blocks.OakTrapdoor,
        bool bottom = true,
        bool open = true,
        bool powered = false,
        bool waterlogged = false,
        bool overwriteExisting = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(trapdoorBlock);
        SurroundWithTrapdoors([new BlockPosition(x, y, z)], BlockState.Parse(trapdoorBlock), bottom, open, powered, waterlogged, overwriteExisting);
    }

    /// <summary>
    /// Surrounds a single block at <paramref name="position"/> on all four horizontal sides with trapdoors facing outward.
    /// </summary>
    public void SurroundWithTrapdoors(
        BlockPosition position,
        string trapdoorBlock = Minecraft.Blocks.OakTrapdoor,
        bool bottom = true,
        bool open = true,
        bool powered = false,
        bool waterlogged = false,
        bool overwriteExisting = false) =>
        SurroundWithTrapdoors(position.X, position.Y, position.Z, trapdoorBlock, bottom, open, powered, waterlogged, overwriteExisting);

    /// <summary>
    /// Surrounds a single block at (x, y, z) on all four horizontal sides with trapdoors using <paramref name="trapdoorState"/>.
    /// </summary>
    public void SurroundWithTrapdoors(
        int x,
        int y,
        int z,
        BlockState trapdoorState,
        bool bottom = true,
        bool open = true,
        bool powered = false,
        bool waterlogged = false,
        bool overwriteExisting = false)
    {
        ArgumentNullException.ThrowIfNull(trapdoorState);
        SurroundWithTrapdoors([new BlockPosition(x, y, z)], trapdoorState, bottom, open, powered, waterlogged, overwriteExisting);
    }

    /// <summary>
    /// Surrounds a single block at <paramref name="position"/> on all four horizontal sides with trapdoors using <paramref name="trapdoorState"/>.
    /// </summary>
    public void SurroundWithTrapdoors(
        BlockPosition position,
        BlockState trapdoorState,
        bool bottom = true,
        bool open = true,
        bool powered = false,
        bool waterlogged = false,
        bool overwriteExisting = false) =>
        SurroundWithTrapdoors(position.X, position.Y, position.Z, trapdoorState, bottom, open, powered, waterlogged, overwriteExisting);

    /// <summary>
    /// Surrounds a rectangular region of blocks starting at (startX, startY, startZ) with outward-facing trapdoors on all exposed horizontal perimeter faces.
    /// </summary>
    public void SurroundWithTrapdoors(
        int startX,
        int startY,
        int startZ,
        int width,
        int height,
        int length,
        string trapdoorBlock = Minecraft.Blocks.OakTrapdoor,
        bool bottom = true,
        bool open = true,
        bool powered = false,
        bool waterlogged = false,
        bool overwriteExisting = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(trapdoorBlock);
        SurroundWithTrapdoors(startX, startY, startZ, width, height, length, BlockState.Parse(trapdoorBlock), bottom, open, powered, waterlogged, overwriteExisting);
    }

    /// <summary>
    /// Surrounds a rectangular region of blocks starting at <paramref name="start"/> with outward-facing trapdoors on all exposed horizontal perimeter faces.
    /// </summary>
    public void SurroundWithTrapdoors(
        BlockPosition start,
        int width,
        int height,
        int length,
        string trapdoorBlock = Minecraft.Blocks.OakTrapdoor,
        bool bottom = true,
        bool open = true,
        bool powered = false,
        bool waterlogged = false,
        bool overwriteExisting = false) =>
        SurroundWithTrapdoors(start.X, start.Y, start.Z, width, height, length, trapdoorBlock, bottom, open, powered, waterlogged, overwriteExisting);

    /// <summary>
    /// Surrounds a rectangular region of blocks starting at (startX, startY, startZ) with outward-facing trapdoors using <paramref name="trapdoorState"/>.
    /// </summary>
    public void SurroundWithTrapdoors(
        int startX,
        int startY,
        int startZ,
        int width,
        int height,
        int length,
        BlockState trapdoorState,
        bool bottom = true,
        bool open = true,
        bool powered = false,
        bool waterlogged = false,
        bool overwriteExisting = false)
    {
        ArgumentNullException.ThrowIfNull(trapdoorState);
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width), width, "Width must be greater than zero.");
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height), height, "Height must be greater than zero.");
        if (length <= 0) throw new ArgumentOutOfRangeException(nameof(length), length, "Length must be greater than zero.");

        var positions = new List<BlockPosition>(checked(width * height * length));
        for (int dy = 0; dy < height; dy++)
        {
            for (int dz = 0; dz < length; dz++)
            {
                for (int dx = 0; dx < width; dx++)
                {
                    positions.Add(new BlockPosition(startX + dx, startY + dy, startZ + dz));
                }
            }
        }

        SurroundWithTrapdoors(positions, trapdoorState, bottom, open, powered, waterlogged, overwriteExisting);
    }

    /// <summary>
    /// Surrounds a rectangular region of blocks starting at <paramref name="start"/> with outward-facing trapdoors using <paramref name="trapdoorState"/>.
    /// </summary>
    public void SurroundWithTrapdoors(
        BlockPosition start,
        int width,
        int height,
        int length,
        BlockState trapdoorState,
        bool bottom = true,
        bool open = true,
        bool powered = false,
        bool waterlogged = false,
        bool overwriteExisting = false) =>
        SurroundWithTrapdoors(start.X, start.Y, start.Z, width, height, length, trapdoorState, bottom, open, powered, waterlogged, overwriteExisting);

    /// <summary>
    /// Surrounds one or more block positions (<paramref name="blockPositions"/>) with outward-facing trapdoors on all exposed horizontal perimeter faces.
    /// </summary>
    public void SurroundWithTrapdoors(
        IEnumerable<BlockPosition> blockPositions,
        string trapdoorBlock = Minecraft.Blocks.OakTrapdoor,
        bool bottom = true,
        bool open = true,
        bool powered = false,
        bool waterlogged = false,
        bool overwriteExisting = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(trapdoorBlock);
        SurroundWithTrapdoors(blockPositions, BlockState.Parse(trapdoorBlock), bottom, open, powered, waterlogged, overwriteExisting);
    }

    /// <summary>
    /// Surrounds one or more block positions (<paramref name="blockPositions"/>) with outward-facing trapdoors using <paramref name="trapdoorState"/>.
    /// </summary>
    public void SurroundWithTrapdoors(
        IEnumerable<BlockPosition> blockPositions,
        BlockState trapdoorState,
        bool bottom = true,
        bool open = true,
        bool powered = false,
        bool waterlogged = false,
        bool overwriteExisting = false)
    {
        ArgumentNullException.ThrowIfNull(blockPositions);
        ArgumentNullException.ThrowIfNull(trapdoorState);

        var targets = new HashSet<BlockPosition>();
        foreach (var pos in blockPositions)
        {
            targets.Add(pos);
        }

        if (targets.Count == 0)
        {
            throw new ArgumentException("At least one block position must be provided.", nameof(blockPositions));
        }

        // Deterministic ordering ensures shared inside-corner cells receive a predictable facing.
        var orderedTargets = targets
            .OrderBy(p => p.Y)
            .ThenBy(p => p.Z)
            .ThenBy(p => p.X)
            .ToArray();

        ReadOnlySpan<(Direction Facing, int Dx, int Dz)> horizontalFaces =
        [
            (Direction.North, 0, -1),
            (Direction.South, 0, 1),
            (Direction.East, 1, 0),
            (Direction.West, -1, 0)
        ];

        var candidates = new List<(BlockPosition Neighbor, Direction Facing)>();
        var seenNeighbors = new HashSet<BlockPosition>();

        foreach (var pos in orderedTargets)
        {
            EnsureInBounds(pos.X, pos.Y, pos.Z);
            foreach (var (facing, dx, dz) in horizontalFaces)
            {
                var neighbor = new BlockPosition(pos.X + dx, pos.Y, pos.Z + dz);
                if (targets.Contains(neighbor))
                {
                    continue;
                }

                EnsureInBounds(neighbor.X, neighbor.Y, neighbor.Z);
                if (seenNeighbors.Add(neighbor))
                {
                    candidates.Add((neighbor, facing));
                }
            }
        }

        foreach (var (neighbor, facing) in candidates)
        {
            if (!overwriteExisting && !GetBlock(neighbor.X, neighbor.Y, neighbor.Z).IsAir)
            {
                continue;
            }

            SetBlock(neighbor, CreateTrapdoorState(trapdoorState, facing, bottom, open, powered, waterlogged));
        }
    }

    private static BlockState CreateTrapdoorState(
        BlockState baseState,
        Direction facing,
        bool bottom,
        bool open,
        bool powered,
        bool waterlogged)
    {
        if (!Enum.IsDefined(facing) ||
            facing is not (Direction.North or Direction.South or Direction.East or Direction.West))
        {
            throw new ArgumentOutOfRangeException(nameof(facing), facing, "Trapdoor facing must be North, South, East, or West.");
        }

        return baseState
            .WithFacing(facing)
            .WithHalf(bottom ? BlockHalf.Bottom : BlockHalf.Top)
            .WithOpen(open)
            .WithPowered(powered)
            .WithWaterlogged(waterlogged);
    }

    /// <summary>
    /// Places a filled or hollow torus centered at (centerX, centerY, centerZ) oriented around <paramref name="axis"/>.
    /// </summary>
    public void SetTorus(int centerX, int centerY, int centerZ, int majorRadius, int minorRadius, BlockState blockState, bool filled = true, Axis axis = Axis.Y, double smoothnessFactor = 1.0)
    {
        ArgumentNullException.ThrowIfNull(blockState);
        SetTorus(centerX, centerY, centerZ, majorRadius, minorRadius, new BlockGradient([blockState]), filled, axis, smoothnessFactor);
    }

    /// <summary>
    /// Places a filled or hollow torus centered at (centerX, centerY, centerZ) from a blockstate string.
    /// </summary>
    public void SetTorus(int centerX, int centerY, int centerZ, int majorRadius, int minorRadius, string blockStateString, bool filled = true, Axis axis = Axis.Y, double smoothnessFactor = 1.0) =>
        SetTorus(centerX, centerY, centerZ, majorRadius, minorRadius, BlockState.Parse(blockStateString), filled, axis, smoothnessFactor);

    /// <summary>
    /// Places a filled or hollow torus centered at <paramref name="center"/>.
    /// </summary>
    public void SetTorus(BlockPosition center, int majorRadius, int minorRadius, BlockState blockState, bool filled = true, Axis axis = Axis.Y, double smoothnessFactor = 1.0) =>
        SetTorus(center.X, center.Y, center.Z, majorRadius, minorRadius, blockState, filled, axis, smoothnessFactor);

    /// <summary>
    /// Places a filled or hollow torus centered at <paramref name="center"/> from a blockstate string.
    /// </summary>
    public void SetTorus(BlockPosition center, int majorRadius, int minorRadius, string blockStateString, bool filled = true, Axis axis = Axis.Y, double smoothnessFactor = 1.0) =>
        SetTorus(center.X, center.Y, center.Z, majorRadius, minorRadius, BlockState.Parse(blockStateString), filled, axis, smoothnessFactor);

    /// <summary>
    /// Places a filled or hollow torus centered at (centerX, centerY, centerZ) using a multi-block <paramref name="gradient"/>.
    /// </summary>
    public void SetTorus(int centerX, int centerY, int centerZ, int majorRadius, int minorRadius, BlockGradient gradient, bool filled = true, Axis axis = Axis.Y, double smoothnessFactor = 1.0)
    {
        ArgumentNullException.ThrowIfNull(gradient);
        if (majorRadius <= 0)
            throw new ArgumentOutOfRangeException(nameof(majorRadius), majorRadius, "MajorRadius must be greater than zero.");
        if (minorRadius < 0 || minorRadius >= majorRadius)
            throw new ArgumentOutOfRangeException(nameof(minorRadius), minorRadius, "MinorRadius must be non-negative and less than MajorRadius.");
        ValidateAxis(axis, nameof(axis));
        double radialPadding = CalculateRadialPadding(smoothnessFactor);

        int outerExtent = majorRadius + minorRadius;
        EnsureAxisBoxInBounds(centerX, centerY, centerZ, -outerExtent, outerExtent, -outerExtent, outerExtent, -minorRadius, minorRadius, axis);

        double minorSq = (minorRadius + radialPadding) * (minorRadius + radialPadding);
        bool IsInside(int du, int dv, int dh)
        {
            double ringDelta = Math.Sqrt(du * du + dv * dv) - majorRadius;
            return ringDelta * ringDelta + dh * dh <= minorSq;
        }

        bool useAxialSteps = minorRadius > 0;
        int totalSteps = useAxialSteps ? 2 * minorRadius + 1 : 2 * outerExtent + 1;
        Random? random = gradient.CreateRandom();

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

                    int stepIndex = useAxialSteps ? dh + minorRadius : du + outerExtent;
                    var pos = MapAxisOffset(centerX, centerY, centerZ, du, dv, dh, axis);
                    SetBlock(pos.X, pos.Y, pos.Z, gradient.GetBlock(stepIndex, totalSteps, random));
                }
            }
        }
    }

    /// <summary>
    /// Places a filled or hollow torus centered at <paramref name="center"/> using a multi-block <paramref name="gradient"/>.
    /// </summary>
    public void SetTorus(BlockPosition center, int majorRadius, int minorRadius, BlockGradient gradient, bool filled = true, Axis axis = Axis.Y, double smoothnessFactor = 1.0) =>
        SetTorus(center.X, center.Y, center.Z, majorRadius, minorRadius, gradient, filled, axis, smoothnessFactor);

    /// <summary>
    /// Places a filled or hollow square-base pyramid starting at (startX, startY, startZ) with base edge <paramref name="baseSize"/>.
    /// </summary>
    public void SetPyramid(int startX, int startY, int startZ, int baseSize, BlockState blockState, bool filled = true, bool useStairs = false, string? stairBlockId = null)
    {
        ArgumentNullException.ThrowIfNull(blockState);
        SetPyramid(startX, startY, startZ, baseSize, new BlockGradient([blockState]), filled, useStairs, stairBlockId);
    }

    /// <summary>
    /// Places a filled or hollow square-base pyramid starting at (startX, startY, startZ) from a blockstate string.
    /// </summary>
    public void SetPyramid(int startX, int startY, int startZ, int baseSize, string blockStateString, bool filled = true, bool useStairs = false, string? stairBlockId = null) =>
        SetPyramid(startX, startY, startZ, baseSize, BlockState.Parse(blockStateString), filled, useStairs, stairBlockId);

    /// <summary>
    /// Places a filled or hollow square-base pyramid starting at <paramref name="start"/>.
    /// </summary>
    public void SetPyramid(BlockPosition start, int baseSize, BlockState blockState, bool filled = true, bool useStairs = false, string? stairBlockId = null) =>
        SetPyramid(start.X, start.Y, start.Z, baseSize, blockState, filled, useStairs, stairBlockId);

    /// <summary>
    /// Places a filled or hollow square-base pyramid starting at <paramref name="start"/> from a blockstate string.
    /// </summary>
    public void SetPyramid(BlockPosition start, int baseSize, string blockStateString, bool filled = true, bool useStairs = false, string? stairBlockId = null) =>
        SetPyramid(start.X, start.Y, start.Z, baseSize, BlockState.Parse(blockStateString), filled, useStairs, stairBlockId);

    /// <summary>
    /// Places a filled or hollow square-base pyramid starting at (startX, startY, startZ) using a multi-block <paramref name="gradient"/>.
    /// </summary>
    public void SetPyramid(int startX, int startY, int startZ, int baseSize, BlockGradient gradient, bool filled = true, bool useStairs = false, string? stairBlockId = null)
    {
        ArgumentNullException.ThrowIfNull(gradient);
        if (baseSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(baseSize), baseSize, "BaseSize must be greater than zero.");

        int pyramidHeight = (baseSize + 1) / 2;
        EnsureInBounds(startX, startY, startZ);
        EnsureInBounds(startX + baseSize - 1, startY + pyramidHeight - 1, startZ + baseSize - 1);

        Random? random = gradient.CreateRandom();
        Dictionary<string, BlockState>? stairCache = useStairs ? [] : null;

        for (int level = 0; level < pyramidHeight; level++)
        {
            int layerWidth = baseSize - 2 * level;
            int layerStartX = startX + level;
            int layerY = startY + level;
            int layerStartZ = startZ + level;

            if (!useStairs || layerWidth == 1)
            {
                if (!gradient.IsFuzzy)
                {
                    var layerBlock = gradient.GetBlock(level, pyramidHeight);
                    SetRectangle(layerStartX, layerY, layerStartZ, layerWidth, layerWidth, layerBlock, filled: filled, normalAxis: Axis.Y);
                }
                else
                {
                    int max = layerWidth - 1;
                    for (int dz = 0; dz < layerWidth; dz++)
                    {
                        for (int dx = 0; dx < layerWidth; dx++)
                        {
                            if (filled || dx == 0 || dx == max || dz == 0 || dz == max)
                            {
                                SetBlock(layerStartX + dx, layerY, layerStartZ + dz, gradient.GetBlock(level, pyramidHeight, random));
                            }
                        }
                    }
                }
                continue;
            }

            SetPyramidStairLayer(layerStartX, layerY, layerStartZ, layerWidth, gradient, level, pyramidHeight, stairBlockId, stairCache!, filled, random);
        }
    }

    /// <summary>
    /// Places a filled or hollow square-base pyramid starting at <paramref name="start"/> using a multi-block <paramref name="gradient"/>.
    /// </summary>
    public void SetPyramid(BlockPosition start, int baseSize, BlockGradient gradient, bool filled = true, bool useStairs = false, string? stairBlockId = null) =>
        SetPyramid(start.X, start.Y, start.Z, baseSize, gradient, filled, useStairs, stairBlockId);

    private void SetPyramidStairLayer(
        int layerStartX,
        int layerY,
        int layerStartZ,
        int layerWidth,
        BlockGradient gradient,
        int level,
        int pyramidHeight,
        string? stairBlockId,
        Dictionary<string, BlockState> stairCache,
        bool filled,
        Random? random)
    {
        int max = layerWidth - 1;
        bool useExplicitSingleStair = gradient.Blocks.Count == 1 && !string.IsNullOrWhiteSpace(stairBlockId);

        if (!gradient.IsFuzzy)
        {
            BlockState fillBlock = gradient.GetBlock(level, pyramidHeight);
            BlockState stairBase = ResolveStairBaseBlock(fillBlock, stairBlockId, useExplicitSingleStair, stairCache);
            BlockState nwCorner = stairBase.BottomHalf().WithFacing(Direction.South).WithStairsShape(StairsShape.OuterLeft);
            BlockState neCorner = stairBase.BottomHalf().WithFacing(Direction.South).WithStairsShape(StairsShape.OuterRight);
            BlockState swCorner = stairBase.BottomHalf().WithFacing(Direction.North).WithStairsShape(StairsShape.OuterRight);
            BlockState seCorner = stairBase.BottomHalf().WithFacing(Direction.North).WithStairsShape(StairsShape.OuterLeft);
            BlockState northEdge = stairBase.BottomHalf().WithFacing(Direction.South).WithStairsShape(StairsShape.Straight);
            BlockState southEdge = stairBase.BottomHalf().WithFacing(Direction.North).WithStairsShape(StairsShape.Straight);
            BlockState westEdge = stairBase.BottomHalf().WithFacing(Direction.East).WithStairsShape(StairsShape.Straight);
            BlockState eastEdge = stairBase.BottomHalf().WithFacing(Direction.West).WithStairsShape(StairsShape.Straight);

            for (int dz = 0; dz < layerWidth; dz++)
            {
                for (int dx = 0; dx < layerWidth; dx++)
                {
                    BlockState? placed = (dx == 0, dx == max, dz == 0, dz == max) switch
                    {
                        (true, false, true, false) => nwCorner,
                        (false, true, true, false) => neCorner,
                        (true, false, false, true) => swCorner,
                        (false, true, false, true) => seCorner,
                        (_, _, true, false) => northEdge,
                        (_, _, false, true) => southEdge,
                        (true, false, _, _) => westEdge,
                        (false, true, _, _) => eastEdge,
                        _ => filled ? fillBlock : null
                    };

                    if (placed is not null)
                    {
                        SetBlock(layerStartX + dx, layerY, layerStartZ + dz, placed);
                    }
                }
            }
            return;
        }

        for (int dz = 0; dz < layerWidth; dz++)
        {
            for (int dx = 0; dx < layerWidth; dx++)
            {
                bool isMinX = dx == 0;
                bool isMaxX = dx == max;
                bool isMinZ = dz == 0;
                bool isMaxZ = dz == max;
                bool isPerimeter = isMinX || isMaxX || isMinZ || isMaxZ;

                if (!isPerimeter)
                {
                    if (filled)
                    {
                        SetBlock(layerStartX + dx, layerY, layerStartZ + dz, gradient.GetBlock(level, pyramidHeight, random));
                    }
                    continue;
                }

                (Direction facing, StairsShape shape) = (isMinX, isMaxX, isMinZ, isMaxZ) switch
                {
                    (true, false, true, false) => (Direction.South, StairsShape.OuterLeft),
                    (false, true, true, false) => (Direction.South, StairsShape.OuterRight),
                    (true, false, false, true) => (Direction.North, StairsShape.OuterRight),
                    (false, true, false, true) => (Direction.North, StairsShape.OuterLeft),
                    (_, _, true, false) => (Direction.South, StairsShape.Straight),
                    (_, _, false, true) => (Direction.North, StairsShape.Straight),
                    (true, false, _, _) => (Direction.East, StairsShape.Straight),
                    _ => (Direction.West, StairsShape.Straight)
                };

                BlockState chosen = gradient.GetBlock(level, pyramidHeight, random);
                BlockState stair = ResolveStairBaseBlock(chosen, stairBlockId, useExplicitSingleStair, stairCache)
                    .BottomHalf()
                    .WithFacing(facing)
                    .WithStairsShape(shape);
                SetBlock(layerStartX + dx, layerY, layerStartZ + dz, stair);
            }
        }
    }

    private static string ResolveStairBlockId(string blockIdentifier, string? fallbackStairBlockId = null)
    {
        string fallback = !string.IsNullOrWhiteSpace(fallbackStairBlockId)
            ? (fallbackStairBlockId.Contains(':', StringComparison.Ordinal) ? fallbackStairBlockId : $"minecraft:{fallbackStairBlockId}")
            : Minecraft.Blocks.StoneStairs;

        if (string.IsNullOrWhiteSpace(blockIdentifier))
        {
            return fallback;
        }

        string normalized = blockIdentifier.Contains(':', StringComparison.Ordinal)
            ? blockIdentifier
            : $"minecraft:{blockIdentifier}";

        int colonIndex = normalized.IndexOf(':', StringComparison.Ordinal);
        string prefix = normalized[..(colonIndex + 1)];
        string name = normalized[(colonIndex + 1)..];

        if (name.EndsWith("_stairs", StringComparison.OrdinalIgnoreCase))
        {
            return normalized;
        }

        string directCandidate = $"{prefix}{name}_stairs";
        if (Minecraft.Blocks.IsValid(directCandidate))
        {
            return directCandidate;
        }

        if (name.EndsWith("s", StringComparison.OrdinalIgnoreCase))
        {
            string singularCandidate = $"{prefix}{name[..^1]}_stairs";
            if (Minecraft.Blocks.IsValid(singularCandidate))
            {
                return singularCandidate;
            }
        }

        if (name.EndsWith("_planks", StringComparison.OrdinalIgnoreCase))
        {
            string woodCandidate = $"{prefix}{name[..^"_planks".Length]}_stairs";
            if (Minecraft.Blocks.IsValid(woodCandidate))
            {
                return woodCandidate;
            }
        }

        if (name.EndsWith("_block", StringComparison.OrdinalIgnoreCase))
        {
            string blockCandidate = $"{prefix}{name[..^"_block".Length]}_stairs";
            if (Minecraft.Blocks.IsValid(blockCandidate))
            {
                return blockCandidate;
            }
        }

        return fallback;
    }

    private void SetCylinderCollar(
        int centerX,
        int startY,
        int centerZ,
        int cylinderRadius,
        int collarWidth,
        int collarHeight,
        BlockGradient gradient,
        Axis axis,
        double smoothnessFactor,
        bool useStairs,
        string? stairBlockId,
        bool isTop)
    {
        ArgumentNullException.ThrowIfNull(gradient);
        if (cylinderRadius < 0)
            throw new ArgumentOutOfRangeException(nameof(cylinderRadius), cylinderRadius, "Cylinder radius must be zero or greater.");
        if (collarWidth <= 0)
            throw new ArgumentOutOfRangeException(isTop ? "topWidth" : "baseWidth", collarWidth, "Collar width must be greater than zero.");
        if (collarHeight <= 0)
            throw new ArgumentOutOfRangeException(isTop ? "topHeight" : "baseHeight", collarHeight, "Collar height must be greater than zero.");
        ValidateAxis(axis, nameof(axis));
        double radialPadding = CalculateRadialPadding(smoothnessFactor);

        int maxOuterRadius = cylinderRadius + collarWidth;
        EnsureAxisBoxInBounds(centerX, startY, centerZ, -maxOuterRadius, maxOuterRadius, -maxOuterRadius, maxOuterRadius, 0, collarHeight - 1, axis);

        double cylinderLimitSq = (cylinderRadius + radialPadding) * (cylinderRadius + radialPadding);
        bool useExplicitSingleStair = gradient.Blocks.Count == 1 && !string.IsNullOrWhiteSpace(stairBlockId);
        Dictionary<string, BlockState> stairCache = [];
        Dictionary<string, BlockState> fullBlockCache = [];
        Random? random = gradient.CreateRandom();

        for (int h = 0; h < collarHeight; h++)
        {
            int layerFromRim = isTop ? collarHeight - 1 - h : h;
            int extraRadius = CalculateBaseLayerExtraRadius(collarWidth, collarHeight, layerFromRim);
            int outerRadius = cylinderRadius + extraRadius;

            int adjacentLayer = isTop ? h - 1 : h + 1;
            int adjacentRadius = adjacentLayer >= 0 && adjacentLayer < collarHeight
                ? cylinderRadius + CalculateBaseLayerExtraRadius(collarWidth, collarHeight, isTop ? collarHeight - 1 - adjacentLayer : adjacentLayer)
                : cylinderRadius;

            double outerLimitSq = (outerRadius + radialPadding) * (outerRadius + radialPadding);
            double adjacentLimitSq = (adjacentRadius + radialPadding) * (adjacentRadius + radialPadding);
            var sliceCenter = MapAxisOffset(centerX, startY, centerZ, 0, 0, h, axis);

            for (int dv = -outerRadius; dv <= outerRadius; dv++)
            {
                for (int du = -outerRadius; du <= outerRadius; du++)
                {
                    if (!IsInsideRadiusSq(du, dv, outerLimitSq) || IsInsideRadiusSq(du, dv, cylinderLimitSq))
                    {
                        continue;
                    }

                    BlockState chosen = gradient.GetBlock(h, collarHeight, random);
                    bool placeStair = useStairs &&
                                      axis == Axis.Y &&
                                      !IsInsideRadiusSq(du, dv, adjacentLimitSq) &&
                                      IsOnCircleOuterRim(du, dv, outerLimitSq);

                    BlockState placed;
                    if (placeStair)
                    {
                        BlockState stairBase = ResolveStairBaseBlock(chosen, stairBlockId, useExplicitSingleStair, stairCache);
                        var (facing, shape) = ResolveCylinderBaseStairOrientation(du, dv, outerLimitSq);
                        BlockState oriented = stairBase.WithFacing(facing).WithStairsShape(shape);
                        placed = isTop ? oriented.TopHalf() : oriented.BottomHalf();
                    }
                    else
                    {
                        placed = ResolveBaseFullBlock(chosen, fullBlockCache);
                    }

                    var pos = MapAxisOffset(sliceCenter.X, sliceCenter.Y, sliceCenter.Z, du, dv, 0, axis);
                    SetBlock(pos.X, pos.Y, pos.Z, placed);
                }
            }
        }
    }

    private static void ValidateCylinderCollarDimensions(int baseWidth, int baseHeight, int topWidth, int topHeight, int cylinderHeight)
    {
        if (baseWidth < 0)
            throw new ArgumentOutOfRangeException(nameof(baseWidth), baseWidth, "Base width must be zero or greater.");
        if (baseHeight < 0)
            throw new ArgumentOutOfRangeException(nameof(baseHeight), baseHeight, "Base height must be zero or greater.");
        if (baseWidth > 0 && baseHeight == 0)
            throw new ArgumentOutOfRangeException(nameof(baseHeight), baseHeight, "Base height must be greater than zero when baseWidth is greater than zero.");
        if (baseHeight > 0 && baseWidth == 0)
            throw new ArgumentOutOfRangeException(nameof(baseWidth), baseWidth, "Base width must be greater than zero when baseHeight is greater than zero.");
        if (baseHeight > cylinderHeight)
            throw new ArgumentOutOfRangeException(nameof(baseHeight), baseHeight, "Base height cannot exceed cylinder height.");

        if (topWidth < 0)
            throw new ArgumentOutOfRangeException(nameof(topWidth), topWidth, "Top width must be zero or greater.");
        if (topHeight < 0)
            throw new ArgumentOutOfRangeException(nameof(topHeight), topHeight, "Top height must be zero or greater.");
        if (topWidth > 0 && topHeight == 0)
            throw new ArgumentOutOfRangeException(nameof(topHeight), topHeight, "Top height must be greater than zero when topWidth is greater than zero.");
        if (topHeight > 0 && topWidth == 0)
            throw new ArgumentOutOfRangeException(nameof(topWidth), topWidth, "Top width must be greater than zero when topHeight is greater than zero.");
        if (topHeight > cylinderHeight)
            throw new ArgumentOutOfRangeException(nameof(topHeight), topHeight, "Top height cannot exceed cylinder height.");
        if (baseHeight + topHeight > cylinderHeight)
            throw new ArgumentOutOfRangeException(nameof(topHeight), topHeight, "Combined base height and top height cannot exceed cylinder height.");
    }

    private static int CalculateBaseLayerExtraRadius(int baseWidth, int baseHeight, int layerIndex)
    {
        double remainingFraction = (double)(baseHeight - layerIndex) / baseHeight;
        return Math.Max(1, (int)Math.Ceiling(baseWidth * remainingFraction));
    }

    private static bool IsInsideRadiusSq(int du, int dv, double limitSq) =>
        du * du + dv * dv <= limitSq;

    private static bool IsOnCircleOuterRim(int du, int dv, double outerLimitSq)
    {
        for (int oz = -1; oz <= 1; oz++)
        {
            for (int ox = -1; ox <= 1; ox++)
            {
                if (ox == 0 && oz == 0)
                    continue;

                if (!IsInsideRadiusSq(du + ox, dv + oz, outerLimitSq))
                    return true;
            }
        }

        return false;
    }

    private static (Direction Facing, StairsShape Shape) ResolveCylinderBaseStairOrientation(int du, int dv, double outerLimitSq)
    {
        bool airNorth = !IsInsideRadiusSq(du, dv - 1, outerLimitSq);
        bool airSouth = !IsInsideRadiusSq(du, dv + 1, outerLimitSq);
        bool airWest = !IsInsideRadiusSq(du - 1, dv, outerLimitSq);
        bool airEast = !IsInsideRadiusSq(du + 1, dv, outerLimitSq);

        bool onlyNorth = airNorth && !airSouth;
        bool onlySouth = airSouth && !airNorth;
        bool onlyWest = airWest && !airEast;
        bool onlyEast = airEast && !airWest;

        if (onlyNorth || onlySouth || onlyWest || onlyEast)
        {
            return (onlyNorth, onlySouth, onlyWest, onlyEast) switch
            {
                (true, false, true, false) => (Direction.South, StairsShape.OuterLeft),
                (true, false, false, true) => (Direction.South, StairsShape.OuterRight),
                (false, true, true, false) => (Direction.North, StairsShape.OuterRight),
                (false, true, false, true) => (Direction.North, StairsShape.OuterLeft),
                (true, false, false, false) => (Direction.South, StairsShape.Straight),
                (false, true, false, false) => (Direction.North, StairsShape.Straight),
                (false, false, true, false) => (Direction.East, StairsShape.Straight),
                (false, false, false, true) => (Direction.West, StairsShape.Straight),
                _ => (GetInwardDirection(du, dv), StairsShape.Straight)
            };
        }

        if (!airNorth && !airSouth && !airWest && !airEast)
        {
            if (!IsInsideRadiusSq(du - 1, dv - 1, outerLimitSq))
                return (Direction.South, StairsShape.InnerLeft);
            if (!IsInsideRadiusSq(du + 1, dv - 1, outerLimitSq))
                return (Direction.South, StairsShape.InnerRight);
            if (!IsInsideRadiusSq(du - 1, dv + 1, outerLimitSq))
                return (Direction.North, StairsShape.InnerRight);
            if (!IsInsideRadiusSq(du + 1, dv + 1, outerLimitSq))
                return (Direction.North, StairsShape.InnerLeft);
        }

        return (GetInwardDirection(du, dv), StairsShape.Straight);
    }

    private static Direction GetInwardDirection(int du, int dv)
    {
        if (Math.Abs(du) >= Math.Abs(dv))
            return du >= 0 ? Direction.West : Direction.East;

        return dv >= 0 ? Direction.North : Direction.South;
    }

    private static int CalculateWaterTier(int du, int dv, int tierCount, double radialPadding)
    {
        if (tierCount <= 1)
            return 0;

        int distSq = du * du + dv * dv;
        for (int t = 0; t < tierCount - 1; t++)
        {
            double limit = 6.0 + t * 5.0 + radialPadding;
            if (distSq <= limit * limit)
                return t;
        }

        return tierCount - 1;
    }

    private void BuildCylinderWaterFlow(
        int centerX,
        int startY,
        int centerZ,
        int maxInteriorRadius,
        int tierCount,
        Dictionary<(int Du, int Dv), int> cellTier)
    {
        var outerRimCells = new List<(int Du, int Dv)>();
        foreach (var (cell, tier) in cellTier)
        {
            if (tier != tierCount - 1)
                continue;

            bool touchesWall =
                !cellTier.ContainsKey((cell.Du + 1, cell.Dv)) ||
                !cellTier.ContainsKey((cell.Du - 1, cell.Dv)) ||
                !cellTier.ContainsKey((cell.Du, cell.Dv + 1)) ||
                !cellTier.ContainsKey((cell.Du, cell.Dv - 1));

            if (touchesWall)
            {
                outerRimCells.Add(cell);
            }
        }

        var rimSources = SelectNonChainingWaterRimSources(outerRimCells, maxInteriorRadius);
        var nextTierDropSeeds = new HashSet<(int Du, int Dv)>();

        for (int t = tierCount - 1; t >= 0; t--)
        {
            var queue = new Queue<(int Du, int Dv)>();
            var levels = new Dictionary<(int Du, int Dv), int>();
            var overhangDrops = new Dictionary<(int Du, int Dv), int>();

            if (t == tierCount - 1)
            {
                foreach (var src in rimSources)
                {
                    levels[src] = 0;
                    queue.Enqueue(src);
                }
            }
            else
            {
                foreach (var drop in nextTierDropSeeds)
                {
                    if (cellTier.TryGetValue(drop, out int dropTier) && dropTier == t)
                    {
                        levels[drop] = 0;
                        queue.Enqueue(drop);
                    }
                }
            }

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                int currentLevel = levels[current];
                if (currentLevel >= 7)
                    continue;

                (int Du, int Dv)[] neighbors =
                [
                    (current.Du + 1, current.Dv),
                    (current.Du - 1, current.Dv),
                    (current.Du, current.Dv + 1),
                    (current.Du, current.Dv - 1)
                ];

                int nextLevel = currentLevel + 1;
                foreach (var next in neighbors)
                {
                    if (!cellTier.TryGetValue(next, out int neighborTier))
                        continue;

                    if (neighborTier == t)
                    {
                        if (!levels.TryGetValue(next, out int existingLevel) || nextLevel < existingLevel)
                        {
                            levels[next] = nextLevel;
                            queue.Enqueue(next);
                        }
                    }
                    else if (neighborTier < t)
                    {
                        if (!overhangDrops.TryGetValue(next, out int existingDropLevel) || nextLevel < existingDropLevel)
                        {
                            overhangDrops[next] = nextLevel;
                        }
                    }
                }
            }

            int tierY = startY + t;
            bool isTopTier = t == tierCount - 1;
            foreach (var (cell, level) in levels)
            {
                int placedLevel = !isTopTier && level == 0 ? 8 : level;
                SetBlock(centerX + cell.Du, tierY, centerZ + cell.Dv, Minecraft.Blocks.Water.WithLevel(placedLevel));
            }

            foreach (var (overhangCell, overhangLevel) in overhangDrops)
            {
                SetBlock(centerX + overhangCell.Du, tierY, centerZ + overhangCell.Dv, Minecraft.Blocks.Water.WithLevel(overhangLevel));
            }

            nextTierDropSeeds.Clear();
            foreach (var overhangCell in overhangDrops.Keys)
            {
                nextTierDropSeeds.Add(overhangCell);
            }
        }
    }

    private static List<(int Du, int Dv)> SelectNonChainingWaterRimSources(
        IReadOnlyCollection<(int Du, int Dv)> outerRimCells,
        int maxInteriorRadius)
    {
        var rimSet = outerRimCells.ToHashSet();
        var selected = new List<(int Du, int Dv)>();

        bool CanAddGroup(IEnumerable<(int Du, int Dv)> group)
        {
            var distinctGroup = group.Distinct().ToList();
            foreach (var candidate in distinctGroup)
            {
                if (!rimSet.Contains(candidate))
                    return false;

                foreach (var existing in selected)
                {
                    if (Math.Abs(candidate.Du - existing.Du) + Math.Abs(candidate.Dv - existing.Dv) < 3)
                        return false;
                }
            }

            for (int i = 0; i < distinctGroup.Count; i++)
            {
                for (int j = i + 1; j < distinctGroup.Count; j++)
                {
                    if (Math.Abs(distinctGroup[i].Du - distinctGroup[j].Du) + Math.Abs(distinctGroup[i].Dv - distinctGroup[j].Dv) < 3)
                        return false;
                }
            }

            return true;
        }

        if (maxInteriorRadius >= 2)
        {
            (int, int)[] cardinals =
            [
                (maxInteriorRadius, 0),
                (-maxInteriorRadius, 0),
                (0, maxInteriorRadius),
                (0, -maxInteriorRadius)
            ];

            if (CanAddGroup(cardinals))
            {
                selected.AddRange(cardinals);
            }
        }
        else if (CanAddGroup([(1, 1), (-1, -1)]))
        {
            selected.Add((1, 1));
            selected.Add((-1, -1));
            return selected;
        }
        else if (outerRimCells.Count > 0)
        {
            selected.Add(outerRimCells.First());
            return selected;
        }

        var octantCandidates = outerRimCells
            .Where(c => c.Du > 0 && c.Dv > 0 && c.Dv <= c.Du)
            .OrderBy(c => Math.Abs(c.Du - c.Dv))
            .ThenByDescending(c => c.Du)
            .ToList();

        foreach (var (du, dv) in octantCandidates)
        {
            (int, int)[] symmetricMirrors =
            [
                (du, dv), (-du, dv), (du, -dv), (-du, -dv),
                (dv, du), (-dv, du), (dv, -du), (-dv, -du)
            ];

            if (CanAddGroup(symmetricMirrors))
            {
                selected.AddRange(symmetricMirrors.Distinct());
            }
        }

        return selected;
    }

    private static BlockState ResolveWaterTierFloorBlock(BlockGradient cylinderGradient)
    {
        BlockState primary = cylinderGradient.Blocks[0];
        if (!primary.Identifier.EndsWith("_glass", StringComparison.OrdinalIgnoreCase) &&
            !primary.Identifier.EndsWith("_glass_pane", StringComparison.OrdinalIgnoreCase))
        {
            Dictionary<string, BlockState> cache = [];
            return ResolveBaseFullBlock(primary, cache);
        }

        return new BlockState(Minecraft.Blocks.StoneBricks);
    }

    private static BlockState ResolveStairBaseBlock(
        BlockState block,
        string? explicitStairBlockId,
        bool useExplicitSingleStair,
        Dictionary<string, BlockState> stairCache)
    {
        if (useExplicitSingleStair)
        {
            string explicitId = explicitStairBlockId!.Contains(':', StringComparison.Ordinal)
                ? explicitStairBlockId
                : $"minecraft:{explicitStairBlockId}";
            if (!stairCache.TryGetValue(explicitId, out var explicitBase))
            {
                explicitBase = BlockState.Parse(explicitId);
                stairCache[explicitId] = explicitBase;
            }
            return explicitBase;
        }

        if (!stairCache.TryGetValue(block.Identifier, out var cachedBase))
        {
            string resolvedStairId = ResolveStairBlockId(block.Identifier, explicitStairBlockId);
            cachedBase = BlockState.Parse(resolvedStairId);
            stairCache[block.Identifier] = cachedBase;
        }
        return cachedBase;
    }

    private static BlockState ResolveBaseFullBlock(BlockState block, Dictionary<string, BlockState> fullBlockCache)
    {
        if (fullBlockCache.TryGetValue(block.Identifier, out var cached))
            return cached;

        BlockState resolved = block;
        if (block.Identifier.EndsWith("_stairs", StringComparison.OrdinalIgnoreCase))
        {
            string fullId = ResolveFullBlockIdFromStair(block.Identifier);
            if (!string.Equals(fullId, block.Identifier, StringComparison.OrdinalIgnoreCase))
            {
                resolved = BlockState.Parse(fullId);
            }
        }

        fullBlockCache[block.Identifier] = resolved;
        return resolved;
    }

    private static string ResolveFullBlockIdFromStair(string stairIdentifier)
    {
        string normalized = stairIdentifier.Contains(':', StringComparison.Ordinal)
            ? stairIdentifier
            : $"minecraft:{stairIdentifier}";

        int colonIndex = normalized.IndexOf(':', StringComparison.Ordinal);
        string prefix = normalized[..(colonIndex + 1)];
        string name = normalized[(colonIndex + 1)..];
        if (!name.EndsWith("_stairs", StringComparison.OrdinalIgnoreCase))
            return normalized;

        string stem = name[..^"_stairs".Length];
        string[] candidates =
        [
            $"{prefix}{stem}",
            $"{prefix}{stem}s",
            $"{prefix}{stem}_planks",
            $"{prefix}{stem}_block"
        ];

        foreach (string candidate in candidates)
        {
            if (Minecraft.Blocks.IsValid(candidate))
                return candidate;
        }

        return normalized;
    }

    private static double CalculateRadialPadding(double smoothnessFactor)
    {
        if (!double.IsFinite(smoothnessFactor) || smoothnessFactor < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(smoothnessFactor), smoothnessFactor, "SmoothnessFactor must be finite and zero or greater.");
        }

        return 0.5 / (1.0 + smoothnessFactor);
    }

    private void SetEllipticalSlice(
        int cx,
        int cy,
        int cz,
        int radiusU,
        int radiusV,
        BlockGradient gradient,
        bool filled,
        Axis normalAxis,
        double radialPadding,
        int? stepOverride = null,
        int? totalStepsOverride = null,
        Random? random = null)
    {
        double ruSq = (radiusU + radialPadding) * (radiusU + radialPadding);
        double rvSq = (radiusV + radialPadding) * (radiusV + radialPadding);
        bool isCircle = radiusU == radiusV;
        int maxRadius = Math.Max(radiusU, radiusV);
        Random? sliceRandom = random ?? gradient.CreateRandom();

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

                int stepIndex;
                int totalSteps;
                if (stepOverride.HasValue)
                {
                    stepIndex = stepOverride.Value;
                    totalSteps = totalStepsOverride ?? 1;
                }
                else
                {
                    totalSteps = maxRadius + 1;
                    double norm = isCircle
                        ? (radiusU == 0 ? 0.0 : Math.Sqrt(du * du + dv * dv) / Math.Max(1.0, radiusU + radialPadding))
                        : Math.Sqrt((du * du) / ruSq + (dv * dv) / rvSq);
                    stepIndex = Math.Clamp((int)Math.Round(norm * maxRadius), 0, maxRadius);
                }

                var pos = MapAxisOffset(cx, cy, cz, du, dv, 0, normalAxis);
                SetBlock(pos.X, pos.Y, pos.Z, gradient.GetBlock(stepIndex, totalSteps, sliceRandom));
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
