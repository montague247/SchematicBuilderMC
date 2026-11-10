using SchematicBuilderMC.Generators;
using SchematicBuilderMC.Models;

namespace SchematicBuilderMC.Builder;

/// <summary>
/// Fluent builder for constructing Minecraft schematics programmatically.
/// </summary>
public sealed class SchematicBuilder
{
    private readonly Schematic _schematic;

    private SchematicBuilder(int width, int height, int length)
    {
        _schematic = new Schematic(width, height, length);
    }

    /// <summary>
    /// Begins building a schematic with the specified dimensions.
    /// </summary>
    public static SchematicBuilder Create(int width, int height, int length) =>
        new(width, height, length);

    public SchematicBuilder WithOffset(int x, int y, int z)
    {
        _schematic.Offset = new BlockPosition(x, y, z);
        return this;
    }

    public SchematicBuilder WithOffset(BlockPosition offset)
    {
        _schematic.Offset = offset;
        return this;
    }

    public SchematicBuilder WithDataVersion(int dataVersion)
    {
        _schematic.DataVersion = dataVersion;
        return this;
    }

    public SchematicBuilder WithName(string name)
    {
        _schematic.Metadata.Name = name;
        return this;
    }

    public SchematicBuilder WithAuthor(string author)
    {
        _schematic.Metadata.Author = author;
        return this;
    }

    public SchematicBuilder WithDescription(string description)
    {
        _schematic.Metadata.Description = description;
        return this;
    }

    public SchematicBuilder WithRequiredMod(string modId)
    {
        if (!string.IsNullOrWhiteSpace(modId))
        {
            _schematic.Metadata.RequiredMods.Add(modId);
        }
        return this;
    }

    public SchematicBuilder SetBlock(int x, int y, int z, BlockState block)
    {
        _schematic.SetBlock(x, y, z, block);
        return this;
    }

    public SchematicBuilder SetBlock(int x, int y, int z, string blockStateString)
    {
        _schematic.SetBlock(x, y, z, blockStateString);
        return this;
    }

    public SchematicBuilder SetBlock(BlockPosition position, BlockState block)
    {
        _schematic.SetBlock(position.X, position.Y, position.Z, block);
        return this;
    }

    public SchematicBuilder Fill(int minX, int minY, int minZ, int maxX, int maxY, int maxZ, BlockState block)
    {
        _schematic.Fill(minX, minY, minZ, maxX, maxY, maxZ, block);
        return this;
    }

    public SchematicBuilder Fill(int minX, int minY, int minZ, int maxX, int maxY, int maxZ, string blockStateString)
    {
        _schematic.Fill(minX, minY, minZ, maxX, maxY, maxZ, BlockState.Parse(blockStateString));
        return this;
    }

    public SchematicBuilder FillAll(BlockState block)
    {
        _schematic.Fill(block);
        return this;
    }

    public SchematicBuilder AddBlockEntity(BlockEntity blockEntity)
    {
        _schematic.SetBlockEntity(blockEntity);
        return this;
    }

    /// <summary>
    /// Sets a chest block and adds inventory items to its block entity at (x, y, z).
    /// </summary>
    public SchematicBuilder SetChest(int x, int y, int z, params ItemStack[] items)
    {
        _schematic.SetChest(x, y, z, items);
        return this;
    }

    /// <summary>
    /// Sets a chest block and adds inventory items to its block entity at the specified position.
    /// </summary>
    public SchematicBuilder SetChest(BlockPosition position, params ItemStack[] items)
    {
        _schematic.SetChest(position, items);
        return this;
    }

    /// <summary>
    /// Sets a container block and adds inventory items to its block entity at (x, y, z).
    /// </summary>
    public SchematicBuilder SetContainer(string blockId, string blockEntityId, int x, int y, int z, params ItemStack[] items)
    {
        _schematic.SetContainer(blockId, blockEntityId, x, y, z, items);
        return this;
    }

    /// <summary>
    /// Sets a container block and adds inventory items to its block entity at the specified position.
    /// </summary>
    public SchematicBuilder SetContainer(string blockId, string blockEntityId, BlockPosition position, params ItemStack[] items)
    {
        _schematic.SetContainer(blockId, blockEntityId, position, items);
        return this;
    }

    /// <summary>
    /// Adds an item to the container block entity at (x, y, z).
    /// </summary>
    public SchematicBuilder AddItem(int x, int y, int z, string itemId, int count = 1, byte? slot = null, fNbt.NbtCompound? customData = null)
    {
        _schematic.AddItem(x, y, z, itemId, count, slot, customData);
        return this;
    }

    /// <summary>
    /// Adds an item to the container block entity at the specified position.
    /// </summary>
    public SchematicBuilder AddItem(BlockPosition position, ItemStack item)
    {
        _schematic.AddItem(position, item);
        return this;
    }

    /// <summary>
    /// Adds an item to the container block entity at (x, y, z).
    /// </summary>
    public SchematicBuilder AddItem(int x, int y, int z, ItemStack item)
    {
        _schematic.AddItem(new BlockPosition(x, y, z), item);
        return this;
    }

    public SchematicBuilder AddEntity(Entity entity)
    {
        _schematic.AddEntity(entity);
        return this;
    }

    /// <summary>
    /// Builds and returns the finished schematic.
    /// </summary>
    public Schematic Build() => _schematic;

    /// <summary>
    /// Generates C# fluent builder source code that reconstructs the schematic currently configured.
    /// </summary>
    public string ToFluentBuilderCode(FluentCodeGeneratorOptions? options = null) =>
        _schematic.ToFluentBuilderCode(options);
}
