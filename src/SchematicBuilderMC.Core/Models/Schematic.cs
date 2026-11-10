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
    /// Adds an entity to the schematic.
    /// </summary>
    public void AddEntity(Entity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        _entities.Add(entity);
    }

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
