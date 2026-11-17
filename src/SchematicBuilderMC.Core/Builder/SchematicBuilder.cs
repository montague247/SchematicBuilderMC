using SchematicBuilderMC.Enums;
using SchematicBuilderMC.Generators;
using SchematicBuilderMC.Models;
using SchematicBuilderMC.Registries;
using SchematicBuilderMC.Schematics;

namespace SchematicBuilderMC.Builder;

/// <summary>
/// Fluent builder for constructing Minecraft schematics programmatically.
/// </summary>
public sealed class SchematicBuilder
{
    private readonly Schematic _schematic;

    /// <summary>
    /// Gets the unrotated schematic width along the X axis.
    /// </summary>
    public int Width => _schematic.Width;

    /// <summary>
    /// Gets the unrotated schematic height along the Y axis.
    /// </summary>
    public int Height => _schematic.Height;

    /// <summary>
    /// Gets the unrotated schematic length along the Z axis.
    /// </summary>
    public int Length => _schematic.Length;

    /// <summary>
    /// Gets the rotation applied when <see cref="Build"/> is called.
    /// </summary>
    public Rotation Rotation { get; private set; } = Rotation.None;

    private SchematicBuilder(int width, int height, int length, Rotation rotation = Rotation.None)
    {
        if (!Enum.IsDefined(rotation))
        {
            throw new ArgumentOutOfRangeException(nameof(rotation), rotation, "Unsupported rotation value.");
        }

        _schematic = new Schematic(width, height, length);
        Rotation = rotation;
    }

    /// <summary>
    /// Begins building a schematic with the specified dimensions and optional rotation applied on <see cref="Build"/>.
    /// </summary>
    public static SchematicBuilder Create(int width, int height, int length, Rotation rotation = Rotation.None) =>
        new(width, height, length, rotation);

    /// <summary>
    /// Configures the clockwise rotation applied to the schematic when <see cref="Build"/> is called.
    /// </summary>
    public SchematicBuilder WithRotation(Rotation rotation)
    {
        if (!Enum.IsDefined(rotation))
        {
            throw new ArgumentOutOfRangeException(nameof(rotation), rotation, "Unsupported rotation value.");
        }

        Rotation = rotation;
        return this;
    }

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

    public SchematicBuilder SetCircle(int centerX, int centerY, int centerZ, int radius, BlockState block, bool filled = true, Axis normalAxis = Axis.Y, double smoothnessFactor = 1.0)
    {
        _schematic.SetCircle(centerX, centerY, centerZ, radius, block, filled, normalAxis, smoothnessFactor);
        return this;
    }

    public SchematicBuilder SetCircle(int centerX, int centerY, int centerZ, int radius, string blockStateString, bool filled = true, Axis normalAxis = Axis.Y, double smoothnessFactor = 1.0)
    {
        _schematic.SetCircle(centerX, centerY, centerZ, radius, blockStateString, filled, normalAxis, smoothnessFactor);
        return this;
    }

    public SchematicBuilder SetCircle(BlockPosition center, int radius, BlockState block, bool filled = true, Axis normalAxis = Axis.Y, double smoothnessFactor = 1.0)
    {
        _schematic.SetCircle(center, radius, block, filled, normalAxis, smoothnessFactor);
        return this;
    }

    public SchematicBuilder SetCircle(BlockPosition center, int radius, string blockStateString, bool filled = true, Axis normalAxis = Axis.Y, double smoothnessFactor = 1.0)
    {
        _schematic.SetCircle(center, radius, blockStateString, filled, normalAxis, smoothnessFactor);
        return this;
    }

    public SchematicBuilder SetCircle(int centerX, int centerY, int centerZ, int radius, BlockGradient gradient, bool filled = true, Axis normalAxis = Axis.Y, double smoothnessFactor = 1.0)
    {
        _schematic.SetCircle(centerX, centerY, centerZ, radius, gradient, filled, normalAxis, smoothnessFactor);
        return this;
    }

    public SchematicBuilder SetCircle(BlockPosition center, int radius, BlockGradient gradient, bool filled = true, Axis normalAxis = Axis.Y, double smoothnessFactor = 1.0)
    {
        _schematic.SetCircle(center, radius, gradient, filled, normalAxis, smoothnessFactor);
        return this;
    }

    public SchematicBuilder SetSphere(int centerX, int centerY, int centerZ, int radius, BlockState block, bool filled = true, double smoothnessFactor = 1.0)
    {
        _schematic.SetSphere(centerX, centerY, centerZ, radius, block, filled, smoothnessFactor);
        return this;
    }

    public SchematicBuilder SetSphere(int centerX, int centerY, int centerZ, int radius, string blockStateString, bool filled = true, double smoothnessFactor = 1.0)
    {
        _schematic.SetSphere(centerX, centerY, centerZ, radius, blockStateString, filled, smoothnessFactor);
        return this;
    }

    public SchematicBuilder SetSphere(BlockPosition center, int radius, BlockState block, bool filled = true, double smoothnessFactor = 1.0)
    {
        _schematic.SetSphere(center, radius, block, filled, smoothnessFactor);
        return this;
    }

    public SchematicBuilder SetSphere(BlockPosition center, int radius, string blockStateString, bool filled = true, double smoothnessFactor = 1.0)
    {
        _schematic.SetSphere(center, radius, blockStateString, filled, smoothnessFactor);
        return this;
    }

    public SchematicBuilder SetSphere(int centerX, int centerY, int centerZ, int radius, BlockGradient gradient, bool filled = true, double smoothnessFactor = 1.0)
    {
        _schematic.SetSphere(centerX, centerY, centerZ, radius, gradient, filled, smoothnessFactor);
        return this;
    }

    public SchematicBuilder SetSphere(BlockPosition center, int radius, BlockGradient gradient, bool filled = true, double smoothnessFactor = 1.0)
    {
        _schematic.SetSphere(center, radius, gradient, filled, smoothnessFactor);
        return this;
    }

    public SchematicBuilder SetRectangle(int startX, int startY, int startZ, int width, int length, BlockState block, bool filled = true, Axis normalAxis = Axis.Y)
    {
        _schematic.SetRectangle(startX, startY, startZ, width, length, block, filled, normalAxis);
        return this;
    }

    public SchematicBuilder SetRectangle(int startX, int startY, int startZ, int width, int length, string blockStateString, bool filled = true, Axis normalAxis = Axis.Y)
    {
        _schematic.SetRectangle(startX, startY, startZ, width, length, blockStateString, filled, normalAxis);
        return this;
    }

    public SchematicBuilder SetRectangle(BlockPosition start, int width, int length, BlockState block, bool filled = true, Axis normalAxis = Axis.Y)
    {
        _schematic.SetRectangle(start, width, length, block, filled, normalAxis);
        return this;
    }

    public SchematicBuilder SetRectangle(BlockPosition start, int width, int length, string blockStateString, bool filled = true, Axis normalAxis = Axis.Y)
    {
        _schematic.SetRectangle(start, width, length, blockStateString, filled, normalAxis);
        return this;
    }

    public SchematicBuilder SetRectangle(int startX, int startY, int startZ, int width, int length, BlockGradient gradient, bool filled = true, Axis normalAxis = Axis.Y)
    {
        _schematic.SetRectangle(startX, startY, startZ, width, length, gradient, filled, normalAxis);
        return this;
    }

    public SchematicBuilder SetRectangle(BlockPosition start, int width, int length, BlockGradient gradient, bool filled = true, Axis normalAxis = Axis.Y)
    {
        _schematic.SetRectangle(start, width, length, gradient, filled, normalAxis);
        return this;
    }

    public SchematicBuilder SetCube(int startX, int startY, int startZ, int size, BlockState block, bool filled = true)
    {
        _schematic.SetCube(startX, startY, startZ, size, block, filled);
        return this;
    }

    public SchematicBuilder SetCube(int startX, int startY, int startZ, int size, string blockStateString, bool filled = true)
    {
        _schematic.SetCube(startX, startY, startZ, size, blockStateString, filled);
        return this;
    }

    public SchematicBuilder SetCube(BlockPosition start, int size, BlockState block, bool filled = true)
    {
        _schematic.SetCube(start, size, block, filled);
        return this;
    }

    public SchematicBuilder SetCube(BlockPosition start, int size, string blockStateString, bool filled = true)
    {
        _schematic.SetCube(start, size, blockStateString, filled);
        return this;
    }

    public SchematicBuilder SetCube(int startX, int startY, int startZ, int size, BlockGradient gradient, bool filled = true)
    {
        _schematic.SetCube(startX, startY, startZ, size, gradient, filled);
        return this;
    }

    public SchematicBuilder SetCube(BlockPosition start, int size, BlockGradient gradient, bool filled = true)
    {
        _schematic.SetCube(start, size, gradient, filled);
        return this;
    }

    public SchematicBuilder SetOval(int centerX, int centerY, int centerZ, int radiusU, int radiusV, BlockState block, bool filled = true, Axis normalAxis = Axis.Y, double smoothnessFactor = 1.0)
    {
        _schematic.SetOval(centerX, centerY, centerZ, radiusU, radiusV, block, filled, normalAxis, smoothnessFactor);
        return this;
    }

    public SchematicBuilder SetOval(int centerX, int centerY, int centerZ, int radiusU, int radiusV, string blockStateString, bool filled = true, Axis normalAxis = Axis.Y, double smoothnessFactor = 1.0)
    {
        _schematic.SetOval(centerX, centerY, centerZ, radiusU, radiusV, blockStateString, filled, normalAxis, smoothnessFactor);
        return this;
    }

    public SchematicBuilder SetOval(BlockPosition center, int radiusU, int radiusV, BlockState block, bool filled = true, Axis normalAxis = Axis.Y, double smoothnessFactor = 1.0)
    {
        _schematic.SetOval(center, radiusU, radiusV, block, filled, normalAxis, smoothnessFactor);
        return this;
    }

    public SchematicBuilder SetOval(BlockPosition center, int radiusU, int radiusV, string blockStateString, bool filled = true, Axis normalAxis = Axis.Y, double smoothnessFactor = 1.0)
    {
        _schematic.SetOval(center, radiusU, radiusV, blockStateString, filled, normalAxis, smoothnessFactor);
        return this;
    }

    public SchematicBuilder SetOval(int centerX, int centerY, int centerZ, int radiusU, int radiusV, BlockGradient gradient, bool filled = true, Axis normalAxis = Axis.Y, double smoothnessFactor = 1.0)
    {
        _schematic.SetOval(centerX, centerY, centerZ, radiusU, radiusV, gradient, filled, normalAxis, smoothnessFactor);
        return this;
    }

    public SchematicBuilder SetOval(BlockPosition center, int radiusU, int radiusV, BlockGradient gradient, bool filled = true, Axis normalAxis = Axis.Y, double smoothnessFactor = 1.0)
    {
        _schematic.SetOval(center, radiusU, radiusV, gradient, filled, normalAxis, smoothnessFactor);
        return this;
    }

    public SchematicBuilder SetCylinder(
        int centerX,
        int startY,
        int centerZ,
        int radius,
        int height,
        BlockState block,
        bool filled = true,
        Axis axis = Axis.Y,
        double smoothnessFactor = 1.0,
        int baseWidth = 0,
        int baseHeight = 0,
        BlockGradient? baseGradient = null,
        bool baseUseStairs = true,
        string? baseStairBlockId = null)
    {
        _schematic.SetCylinder(centerX, startY, centerZ, radius, height, block, filled, axis, smoothnessFactor, baseWidth, baseHeight, baseGradient, baseUseStairs, baseStairBlockId);
        return this;
    }

    public SchematicBuilder SetCylinder(
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
        string? baseStairBlockId = null)
    {
        _schematic.SetCylinder(centerX, startY, centerZ, radius, height, blockStateString, filled, axis, smoothnessFactor, baseWidth, baseHeight, baseGradient, baseUseStairs, baseStairBlockId);
        return this;
    }

    public SchematicBuilder SetCylinder(
        BlockPosition baseCenter,
        int radius,
        int height,
        BlockState block,
        bool filled = true,
        Axis axis = Axis.Y,
        double smoothnessFactor = 1.0,
        int baseWidth = 0,
        int baseHeight = 0,
        BlockGradient? baseGradient = null,
        bool baseUseStairs = true,
        string? baseStairBlockId = null)
    {
        _schematic.SetCylinder(baseCenter, radius, height, block, filled, axis, smoothnessFactor, baseWidth, baseHeight, baseGradient, baseUseStairs, baseStairBlockId);
        return this;
    }

    public SchematicBuilder SetCylinder(
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
        string? baseStairBlockId = null)
    {
        _schematic.SetCylinder(baseCenter, radius, height, blockStateString, filled, axis, smoothnessFactor, baseWidth, baseHeight, baseGradient, baseUseStairs, baseStairBlockId);
        return this;
    }

    public SchematicBuilder SetCylinder(
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
        string? baseStairBlockId = null)
    {
        _schematic.SetCylinder(centerX, startY, centerZ, radius, height, gradient, filled, axis, smoothnessFactor, baseWidth, baseHeight, baseGradient, baseUseStairs, baseStairBlockId);
        return this;
    }

    public SchematicBuilder SetCylinder(
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
        string? baseStairBlockId = null)
    {
        _schematic.SetCylinder(baseCenter, radius, height, gradient, filled, axis, smoothnessFactor, baseWidth, baseHeight, baseGradient, baseUseStairs, baseStairBlockId);
        return this;
    }

    public SchematicBuilder SetCylinderBase(
        int centerX,
        int startY,
        int centerZ,
        int cylinderRadius,
        int baseWidth,
        int baseHeight,
        BlockState block,
        Axis axis = Axis.Y,
        double smoothnessFactor = 1.0,
        bool useStairs = true,
        string? stairBlockId = null)
    {
        _schematic.SetCylinderBase(centerX, startY, centerZ, cylinderRadius, baseWidth, baseHeight, block, axis, smoothnessFactor, useStairs, stairBlockId);
        return this;
    }

    public SchematicBuilder SetCylinderBase(
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
        string? stairBlockId = null)
    {
        _schematic.SetCylinderBase(centerX, startY, centerZ, cylinderRadius, baseWidth, baseHeight, blockStateString, axis, smoothnessFactor, useStairs, stairBlockId);
        return this;
    }

    public SchematicBuilder SetCylinderBase(
        BlockPosition baseCenter,
        int cylinderRadius,
        int baseWidth,
        int baseHeight,
        BlockState block,
        Axis axis = Axis.Y,
        double smoothnessFactor = 1.0,
        bool useStairs = true,
        string? stairBlockId = null)
    {
        _schematic.SetCylinderBase(baseCenter, cylinderRadius, baseWidth, baseHeight, block, axis, smoothnessFactor, useStairs, stairBlockId);
        return this;
    }

    public SchematicBuilder SetCylinderBase(
        BlockPosition baseCenter,
        int cylinderRadius,
        int baseWidth,
        int baseHeight,
        string blockStateString,
        Axis axis = Axis.Y,
        double smoothnessFactor = 1.0,
        bool useStairs = true,
        string? stairBlockId = null)
    {
        _schematic.SetCylinderBase(baseCenter, cylinderRadius, baseWidth, baseHeight, blockStateString, axis, smoothnessFactor, useStairs, stairBlockId);
        return this;
    }

    public SchematicBuilder SetCylinderBase(
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
        string? stairBlockId = null)
    {
        _schematic.SetCylinderBase(centerX, startY, centerZ, cylinderRadius, baseWidth, baseHeight, gradient, axis, smoothnessFactor, useStairs, stairBlockId);
        return this;
    }

    public SchematicBuilder SetCylinderBase(
        BlockPosition baseCenter,
        int cylinderRadius,
        int baseWidth,
        int baseHeight,
        BlockGradient gradient,
        Axis axis = Axis.Y,
        double smoothnessFactor = 1.0,
        bool useStairs = true,
        string? stairBlockId = null)
    {
        _schematic.SetCylinderBase(baseCenter, cylinderRadius, baseWidth, baseHeight, gradient, axis, smoothnessFactor, useStairs, stairBlockId);
        return this;
    }

    public SchematicBuilder SetTorus(int centerX, int centerY, int centerZ, int majorRadius, int minorRadius, BlockState block, bool filled = true, Axis axis = Axis.Y, double smoothnessFactor = 1.0)
    {
        _schematic.SetTorus(centerX, centerY, centerZ, majorRadius, minorRadius, block, filled, axis, smoothnessFactor);
        return this;
    }

    public SchematicBuilder SetTorus(int centerX, int centerY, int centerZ, int majorRadius, int minorRadius, string blockStateString, bool filled = true, Axis axis = Axis.Y, double smoothnessFactor = 1.0)
    {
        _schematic.SetTorus(centerX, centerY, centerZ, majorRadius, minorRadius, blockStateString, filled, axis, smoothnessFactor);
        return this;
    }

    public SchematicBuilder SetTorus(BlockPosition center, int majorRadius, int minorRadius, BlockState block, bool filled = true, Axis axis = Axis.Y, double smoothnessFactor = 1.0)
    {
        _schematic.SetTorus(center, majorRadius, minorRadius, block, filled, axis, smoothnessFactor);
        return this;
    }

    public SchematicBuilder SetTorus(BlockPosition center, int majorRadius, int minorRadius, string blockStateString, bool filled = true, Axis axis = Axis.Y, double smoothnessFactor = 1.0)
    {
        _schematic.SetTorus(center, majorRadius, minorRadius, blockStateString, filled, axis, smoothnessFactor);
        return this;
    }

    public SchematicBuilder SetTorus(int centerX, int centerY, int centerZ, int majorRadius, int minorRadius, BlockGradient gradient, bool filled = true, Axis axis = Axis.Y, double smoothnessFactor = 1.0)
    {
        _schematic.SetTorus(centerX, centerY, centerZ, majorRadius, minorRadius, gradient, filled, axis, smoothnessFactor);
        return this;
    }

    public SchematicBuilder SetTorus(BlockPosition center, int majorRadius, int minorRadius, BlockGradient gradient, bool filled = true, Axis axis = Axis.Y, double smoothnessFactor = 1.0)
    {
        _schematic.SetTorus(center, majorRadius, minorRadius, gradient, filled, axis, smoothnessFactor);
        return this;
    }

    public SchematicBuilder SetPyramid(int startX, int startY, int startZ, int baseSize, BlockState block, bool filled = true, bool useStairs = false, string? stairBlockId = null)
    {
        _schematic.SetPyramid(startX, startY, startZ, baseSize, block, filled, useStairs, stairBlockId);
        return this;
    }

    public SchematicBuilder SetPyramid(int startX, int startY, int startZ, int baseSize, string blockStateString, bool filled = true, bool useStairs = false, string? stairBlockId = null)
    {
        _schematic.SetPyramid(startX, startY, startZ, baseSize, blockStateString, filled, useStairs, stairBlockId);
        return this;
    }

    public SchematicBuilder SetPyramid(BlockPosition start, int baseSize, BlockState block, bool filled = true, bool useStairs = false, string? stairBlockId = null)
    {
        _schematic.SetPyramid(start, baseSize, block, filled, useStairs, stairBlockId);
        return this;
    }

    public SchematicBuilder SetPyramid(BlockPosition start, int baseSize, string blockStateString, bool filled = true, bool useStairs = false, string? stairBlockId = null)
    {
        _schematic.SetPyramid(start, baseSize, blockStateString, filled, useStairs, stairBlockId);
        return this;
    }

    public SchematicBuilder SetPyramid(int startX, int startY, int startZ, int baseSize, BlockGradient gradient, bool filled = true, bool useStairs = false, string? stairBlockId = null)
    {
        _schematic.SetPyramid(startX, startY, startZ, baseSize, gradient, filled, useStairs, stairBlockId);
        return this;
    }

    public SchematicBuilder SetPyramid(BlockPosition start, int baseSize, BlockGradient gradient, bool filled = true, bool useStairs = false, string? stairBlockId = null)
    {
        _schematic.SetPyramid(start, baseSize, gradient, filled, useStairs, stairBlockId);
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
    /// Sets a chest block and adds inventory items to its block entity at (x, y, z).
    /// </summary>
    public SchematicBuilder SetChest(int x, int y, int z, IEnumerable<ItemStack> items)
    {
        _schematic.SetChest(x, y, z, items);
        return this;
    }

    /// <summary>
    /// Sets a chest block and adds inventory items to its block entity at the specified position.
    /// </summary>
    public SchematicBuilder SetChest(BlockPosition position, IEnumerable<ItemStack> items)
    {
        _schematic.SetChest(position, items);
        return this;
    }

    /// <summary>
    /// Sets a double chest starting at (x, y, z) with the specified facing direction.
    /// (x, y, z) becomes the right half, and the adjacent connected block becomes the left half.
    /// Items with slot 0..26 are placed in the right half; items with slot 27..53 are placed in the left half (with adjusted slot 0..26).
    /// </summary>
    public SchematicBuilder SetDoubleChest(int x, int y, int z, Direction facing = Direction.North, params ItemStack[] items)
    {
        _schematic.SetDoubleChest(x, y, z, facing, items);
        return this;
    }

    /// <summary>
    /// Sets a double chest starting at the specified position with the specified facing direction.
    /// </summary>
    public SchematicBuilder SetDoubleChest(BlockPosition position, Direction facing = Direction.North, params ItemStack[] items)
    {
        _schematic.SetDoubleChest(position, facing, items);
        return this;
    }

    /// <summary>
    /// Sets a double chest starting at (x, y, z) with the specified facing direction.
    /// </summary>
    public SchematicBuilder SetDoubleChest(int x, int y, int z, Direction facing, IEnumerable<ItemStack> items)
    {
        _schematic.SetDoubleChest(x, y, z, facing, items);
        return this;
    }

    /// <summary>
    /// Sets a double chest starting at the specified position with the specified facing direction.
    /// </summary>
    public SchematicBuilder SetDoubleChest(BlockPosition position, Direction facing, IEnumerable<ItemStack> items)
    {
        _schematic.SetDoubleChest(position, facing, items);
        return this;
    }

    /// <summary>
    /// Sets a double chest with explicitly specified coordinates for the right and left halves and facing direction.
    /// </summary>
    public SchematicBuilder SetDoubleChest(int rightX, int rightY, int rightZ, int leftX, int leftY, int leftZ, Direction facing = Direction.North, params ItemStack[] items)
    {
        _schematic.SetDoubleChest(rightX, rightY, rightZ, leftX, leftY, leftZ, facing, items);
        return this;
    }

    /// <summary>
    /// Sets a double chest with explicitly specified coordinates for the right and left halves and facing direction.
    /// </summary>
    public SchematicBuilder SetDoubleChest(int rightX, int rightY, int rightZ, int leftX, int leftY, int leftZ, Direction facing, IEnumerable<ItemStack>? items)
    {
        _schematic.SetDoubleChest(rightX, rightY, rightZ, leftX, leftY, leftZ, facing, items);
        return this;
    }

    /// <summary>
    /// Sets a double chest with explicitly specified positions for the right and left halves and facing direction.
    /// </summary>
    public SchematicBuilder SetDoubleChest(BlockPosition rightPos, BlockPosition leftPos, Direction facing = Direction.North, params ItemStack[] items)
    {
        _schematic.SetDoubleChest(rightPos, leftPos, facing, items);
        return this;
    }

    /// <summary>
    /// Sets a double chest with explicitly specified positions for the right and left halves and facing direction.
    /// </summary>
    public SchematicBuilder SetDoubleChest(BlockPosition rightPos, BlockPosition leftPos, Direction facing, IEnumerable<ItemStack>? items)
    {
        _schematic.SetDoubleChest(rightPos, leftPos, facing, items);
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
    /// Sets a sign block entity with front messages at (x, y, z).
    /// </summary>
    public SchematicBuilder SetSign(int x, int y, int z, params string[] frontMessages)
    {
        _schematic.SetSign(x, y, z, frontMessages);
        return this;
    }

    /// <summary>
    /// Sets a sign block entity with front messages at the specified position.
    /// </summary>
    public SchematicBuilder SetSign(BlockPosition position, params string[] frontMessages)
    {
        _schematic.SetSign(position, frontMessages);
        return this;
    }

    /// <summary>
    /// Sets a sign block entity with front messages, optional back messages, and waxed state at (x, y, z).
    /// </summary>
    public SchematicBuilder SetSign(int x, int y, int z, IEnumerable<string> frontMessages, IEnumerable<string>? backMessages = null, bool isWaxed = false)
    {
        _schematic.SetSign(x, y, z, frontMessages, backMessages, isWaxed);
        return this;
    }

    /// <summary>
    /// Sets a sign block entity with front messages, optional back messages, and waxed state at the specified position.
    /// </summary>
    public SchematicBuilder SetSign(BlockPosition position, IEnumerable<string> frontMessages, IEnumerable<string>? backMessages = null, bool isWaxed = false)
    {
        _schematic.SetSign(position, frontMessages, backMessages, isWaxed);
        return this;
    }

    /// <summary>
    /// Sets a decorated pot with an item at (x, y, z).
    /// </summary>
    public SchematicBuilder SetDecoratedPot(int x, int y, int z, ItemStack item)
    {
        _schematic.SetDecoratedPot(x, y, z, item);
        return this;
    }

    /// <summary>
    /// Sets a decorated pot with an item at the specified position.
    /// </summary>
    public SchematicBuilder SetDecoratedPot(BlockPosition position, ItemStack item)
    {
        _schematic.SetDecoratedPot(position, item);
        return this;
    }

    /// <summary>
    /// Sets a decorated pot with an optional item and sherds at (x, y, z).
    /// </summary>
    public SchematicBuilder SetDecoratedPot(int x, int y, int z, ItemStack? item = null, string? frontSherd = null, string? backSherd = null, string? leftSherd = null, string? rightSherd = null)
    {
        _schematic.SetDecoratedPot(x, y, z, item, frontSherd, backSherd, leftSherd, rightSherd);
        return this;
    }

    /// <summary>
    /// Sets a decorated pot with an optional item and sherds at the specified position.
    /// </summary>
    public SchematicBuilder SetDecoratedPot(BlockPosition position, ItemStack? item = null, string? frontSherd = null, string? backSherd = null, string? leftSherd = null, string? rightSherd = null)
    {
        _schematic.SetDecoratedPot(position, item, frontSherd, backSherd, leftSherd, rightSherd);
        return this;
    }

    /// <summary>
    /// Sets a crafter block and its block entity with disabled slots at (x, y, z).
    /// </summary>
    public SchematicBuilder SetCrafter(int x, int y, int z, params int[] disabledSlots)
    {
        _schematic.SetCrafter(x, y, z, disabledSlots);
        return this;
    }

    /// <summary>
    /// Sets a crafter block and its block entity with disabled slots at the specified position.
    /// </summary>
    public SchematicBuilder SetCrafter(BlockPosition position, params int[] disabledSlots)
    {
        _schematic.SetCrafter(position, disabledSlots);
        return this;
    }

    /// <summary>
    /// Sets a crafter block and its block entity with disabled slots and items at (x, y, z).
    /// </summary>
    public SchematicBuilder SetCrafter(int x, int y, int z, IEnumerable<int>? disabledSlots, IEnumerable<ItemStack>? items)
    {
        _schematic.SetCrafter(x, y, z, disabledSlots, items);
        return this;
    }

    /// <summary>
    /// Sets a crafter block and its block entity with disabled slots and items at (x, y, z).
    /// </summary>
    public SchematicBuilder SetCrafter(int x, int y, int z, IEnumerable<int>? disabledSlots, params ItemStack[] items)
    {
        _schematic.SetCrafter(x, y, z, disabledSlots, (IEnumerable<ItemStack>)items);
        return this;
    }

    /// <summary>
    /// Sets a crafter block and its block entity with disabled slots and items at the specified position.
    /// </summary>
    public SchematicBuilder SetCrafter(BlockPosition position, IEnumerable<int>? disabledSlots, IEnumerable<ItemStack>? items)
    {
        _schematic.SetCrafter(position, disabledSlots, items);
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

    /// <summary>
    /// Sets a redstone repeater at (x, y, z) with specified facing, delay (1-4), locked, and powered states.
    /// </summary>
    public SchematicBuilder SetRepeater(int x, int y, int z, string facing = "north", int delay = 1, bool locked = false, bool powered = false)
    {
        _schematic.SetRepeater(x, y, z, facing, delay, locked, powered);
        return this;
    }

    /// <summary>
    /// Sets a redstone repeater at the specified position.
    /// </summary>
    public SchematicBuilder SetRepeater(BlockPosition position, string facing = "north", int delay = 1, bool locked = false, bool powered = false)
    {
        _schematic.SetRepeater(position, facing, delay, locked, powered);
        return this;
    }

    /// <summary>
    /// Sets a redstone repeater at (x, y, z) with strongly-typed facing, delay (1-4), locked, and powered states.
    /// </summary>
    public SchematicBuilder SetRepeater(int x, int y, int z, Direction facing, int delay = 1, bool locked = false, bool powered = false)
    {
        _schematic.SetRepeater(x, y, z, facing, delay, locked, powered);
        return this;
    }

    /// <summary>
    /// Sets a redstone repeater at the specified position with strongly-typed facing.
    /// </summary>
    public SchematicBuilder SetRepeater(BlockPosition position, Direction facing, int delay = 1, bool locked = false, bool powered = false)
    {
        _schematic.SetRepeater(position, facing, delay, locked, powered);
        return this;
    }

    /// <summary>
    /// Sets a redstone comparator at (x, y, z) with specified facing, subtraction mode, and powered states.
    /// </summary>
    public SchematicBuilder SetComparator(int x, int y, int z, string facing = "north", bool subtractMode = false, bool powered = false)
    {
        _schematic.SetComparator(x, y, z, facing, subtractMode, powered);
        return this;
    }

    /// <summary>
    /// Sets a redstone comparator at the specified position.
    /// </summary>
    public SchematicBuilder SetComparator(BlockPosition position, string facing = "north", bool subtractMode = false, bool powered = false)
    {
        _schematic.SetComparator(position, facing, subtractMode, powered);
        return this;
    }

    /// <summary>
    /// Sets a redstone comparator at (x, y, z) with strongly-typed facing, subtraction mode, and powered states.
    /// </summary>
    public SchematicBuilder SetComparator(int x, int y, int z, Direction facing, bool subtractMode = false, bool powered = false)
    {
        _schematic.SetComparator(x, y, z, facing, subtractMode, powered);
        return this;
    }

    /// <summary>
    /// Sets a redstone comparator at the specified position with strongly-typed facing.
    /// </summary>
    public SchematicBuilder SetComparator(BlockPosition position, Direction facing, bool subtractMode = false, bool powered = false)
    {
        _schematic.SetComparator(position, facing, subtractMode, powered);
        return this;
    }

    /// <summary>
    /// Sets a lever at (x, y, z) with specified attachment face ("floor", "wall", "ceiling"), facing, and powered states.
    /// </summary>
    public SchematicBuilder SetLever(int x, int y, int z, string face = "floor", string facing = "north", bool powered = false)
    {
        _schematic.SetLever(x, y, z, face, facing, powered);
        return this;
    }

    /// <summary>
    /// Sets a lever at the specified position.
    /// </summary>
    public SchematicBuilder SetLever(BlockPosition position, string face = "floor", string facing = "north", bool powered = false)
    {
        _schematic.SetLever(position, face, facing, powered);
        return this;
    }

    /// <summary>
    /// Sets a lever at (x, y, z) with strongly-typed face attachment, facing direction, and powered state.
    /// </summary>
    public SchematicBuilder SetLever(int x, int y, int z, BlockFace face, Direction facing = Direction.North, bool powered = false)
    {
        _schematic.SetLever(x, y, z, face, facing, powered);
        return this;
    }

    /// <summary>
    /// Sets a lever at the specified position with strongly-typed face attachment, facing direction, and powered state.
    /// </summary>
    public SchematicBuilder SetLever(BlockPosition position, BlockFace face, Direction facing = Direction.North, bool powered = false)
    {
        _schematic.SetLever(position, face, facing, powered);
        return this;
    }

    /// <summary>
    /// Sets redstone wire at (x, y, z) with signal power (0 to 15).
    /// </summary>
    public SchematicBuilder SetRedstoneWire(int x, int y, int z, int power = 0)
    {
        _schematic.SetRedstoneWire(x, y, z, power);
        return this;
    }

    /// <summary>
    /// Sets redstone wire at the specified position.
    /// </summary>
    public SchematicBuilder SetRedstoneWire(BlockPosition position, int power = 0)
    {
        _schematic.SetRedstoneWire(position, power);
        return this;
    }

    /// <summary>
    /// Sets a piston block at (x, y, z) with specified facing, sticky, and extended states.
    /// </summary>
    public SchematicBuilder SetPiston(int x, int y, int z, string facing = "up", bool sticky = false, bool extended = false)
    {
        _schematic.SetPiston(x, y, z, facing, sticky, extended);
        return this;
    }

    /// <summary>
    /// Sets a piston block at the specified position.
    /// </summary>
    public SchematicBuilder SetPiston(BlockPosition position, string facing = "up", bool sticky = false, bool extended = false)
    {
        _schematic.SetPiston(position, facing, sticky, extended);
        return this;
    }

    /// <summary>
    /// Sets a piston block at (x, y, z) with strongly-typed facing direction, sticky, and extended states.
    /// </summary>
    public SchematicBuilder SetPiston(int x, int y, int z, Direction facing, bool sticky = false, bool extended = false)
    {
        _schematic.SetPiston(x, y, z, facing, sticky, extended);
        return this;
    }

    /// <summary>
    /// Sets a piston block at the specified position with strongly-typed facing direction.
    /// </summary>
    public SchematicBuilder SetPiston(BlockPosition position, Direction facing, bool sticky = false, bool extended = false)
    {
        _schematic.SetPiston(position, facing, sticky, extended);
        return this;
    }

    public SchematicBuilder AddEntity(Entity entity)
    {
        _schematic.AddEntity(entity);
        return this;
    }

    /// <summary>
    /// Adds an entity (optionally named, such as when renamed with a Name Tag) to the schematic.
    /// </summary>
    public SchematicBuilder AddEntity(string entityId, double x, double y, double z, string? customName = null, bool customNameVisible = true, bool persistenceRequired = true)
    {
        _schematic.AddEntity(entityId, x, y, z, customName, customNameVisible, persistenceRequired);
        return this;
    }

    /// <summary>
    /// Adds an entity to the schematic using block coordinates (x, y, z).
    /// Applies the schematic's current offset and centers the entity horizontally within the block
    /// at (x + Offset.X + 0.5, y + Offset.Y, z + Offset.Z + 0.5).
    /// </summary>
    public SchematicBuilder AddEntity(int x, int y, int z, Entity entity)
    {
        _schematic.AddEntity(x, y, z, entity);
        return this;
    }

    /// <summary>
    /// Adds an entity to the schematic at the specified block position.
    /// Applies the schematic's current offset and centers the entity horizontally within the block
    /// at (position.X + Offset.X + 0.5, position.Y + Offset.Y, position.Z + Offset.Z + 0.5).
    /// </summary>
    public SchematicBuilder AddEntity(BlockPosition position, Entity entity)
    {
        _schematic.AddEntity(position, entity);
        return this;
    }

    /// <summary>
    /// Adds an entity (optionally named, such as when renamed with a Name Tag) to the schematic using block coordinates (x, y, z).
    /// Applies the schematic's current offset and centers the entity horizontally within the block.
    /// </summary>
    public SchematicBuilder AddEntity(int x, int y, int z, string entityId, string? customName = null, bool customNameVisible = true, bool persistenceRequired = true)
    {
        _schematic.AddEntity(x, y, z, entityId, customName, customNameVisible, persistenceRequired);
        return this;
    }

    /// <summary>
    /// Adds an entity (optionally named, such as when renamed with a Name Tag) to the schematic at the specified block position.
    /// Applies the schematic's current offset and centers the entity horizontally within the block.
    /// </summary>
    public SchematicBuilder AddEntity(BlockPosition position, string entityId, string? customName = null, bool customNameVisible = true, bool persistenceRequired = true)
    {
        _schematic.AddEntity(position, entityId, customName, customNameVisible, persistenceRequired);
        return this;
    }

    /// <summary>
    /// Adds a minecart entity to the schematic with an optional passenger.
    /// </summary>
    public SchematicBuilder AddMinecart(
        double x, double y, double z,
        Entity? passenger = null,
        double motionX = 0, double motionY = 0, double motionZ = 0,
        bool isFlipped = false,
        string minecartType = Minecraft.Entities.Minecart)
    {
        _schematic.AddMinecart(x, y, z, passenger, motionX, motionY, motionZ, isFlipped, minecartType);
        return this;
    }

    /// <summary>
    /// Adds a minecart entity with multiple passengers to the schematic.
    /// </summary>
    public SchematicBuilder AddMinecart(
        double x, double y, double z,
        IEnumerable<Entity> passengers,
        double motionX = 0, double motionY = 0, double motionZ = 0,
        bool isFlipped = false,
        string minecartType = Minecraft.Entities.Minecart)
    {
        _schematic.AddMinecart(x, y, z, passengers, motionX, motionY, motionZ, isFlipped, minecartType);
        return this;
    }

    /// <summary>
    /// Adds a minecart entity positioned relative to a block position (centered horizontally at y + 0.0625).
    /// </summary>
    public SchematicBuilder AddMinecart(
        BlockPosition position,
        Entity? passenger = null,
        double motionX = 0, double motionY = 0, double motionZ = 0,
        bool isFlipped = false,
        string minecartType = Minecraft.Entities.Minecart) =>
        AddMinecart(position.X + 0.5, position.Y + 0.0625, position.Z + 0.5, passenger, motionX, motionY, motionZ, isFlipped, minecartType);

    /// <summary>
    /// Adds an item frame entity (or glow item frame) to the schematic.
    /// </summary>
    public SchematicBuilder AddItemFrame(
        double x, double y, double z,
        Direction facing = Direction.North,
        ItemStack? item = null,
        int rotation = 0,
        bool isGlow = false,
        bool isInvisible = false,
        bool isFixed = false)
    {
        _schematic.AddItemFrame(x, y, z, facing, item, rotation, isGlow, isInvisible, isFixed);
        return this;
    }

    /// <summary>
    /// Adds an item frame entity with a specific item identifier.
    /// </summary>
    public SchematicBuilder AddItemFrame(
        double x, double y, double z,
        Direction facing,
        string itemId,
        int rotation = 0,
        bool isGlow = false,
        bool isInvisible = false,
        bool isFixed = false)
    {
        _schematic.AddItemFrame(x, y, z, facing, itemId, rotation, isGlow, isInvisible, isFixed);
        return this;
    }

    /// <summary>
    /// Adds an item frame entity positioned relative to a block position.
    /// </summary>
    public SchematicBuilder AddItemFrame(
        BlockPosition position,
        Direction facing = Direction.North,
        ItemStack? item = null,
        int rotation = 0,
        bool isGlow = false,
        bool isInvisible = false,
        bool isFixed = false)
    {
        _schematic.AddItemFrame(position, facing, item, rotation, isGlow, isInvisible, isFixed);
        return this;
    }

    /// <summary>
    /// Adds a dropped item entity ("minecraft:item") to the schematic.
    /// </summary>
    public SchematicBuilder AddDroppedItem(
        double x, double y, double z,
        ItemStack item,
        short age = 0,
        short pickupDelay = 0)
    {
        _schematic.AddDroppedItem(x, y, z, item, age, pickupDelay);
        return this;
    }

    /// <summary>
    /// Adds a dropped item entity ("minecraft:item") with a specific item ID and count to the schematic.
    /// </summary>
    public SchematicBuilder AddDroppedItem(
        double x, double y, double z,
        string itemId,
        int count = 1,
        short age = 0,
        short pickupDelay = 0)
    {
        _schematic.AddDroppedItem(x, y, z, itemId, count, age, pickupDelay);
        return this;
    }

    /// <summary>
    /// Adds a dropped item entity ("minecraft:item") positioned relative to a block position (centered horizontally at y + 0.1).
    /// </summary>
    public SchematicBuilder AddDroppedItem(
        BlockPosition position,
        ItemStack item,
        short age = 0,
        short pickupDelay = 0)
    {
        _schematic.AddDroppedItem(position, item, age, pickupDelay);
        return this;
    }

    /// <summary>
    /// Adds a dropped item entity ("minecraft:item") with a specific item ID and count relative to a block position.
    /// </summary>
    public SchematicBuilder AddDroppedItem(
        BlockPosition position,
        string itemId,
        int count = 1,
        short age = 0,
        short pickupDelay = 0)
    {
        _schematic.AddDroppedItem(position, itemId, count, age, pickupDelay);
        return this;
    }

    /// <summary>
    /// Ensures the schematic being built is at least <paramref name="minWidth"/> x <paramref name="minHeight"/> x <paramref name="minLength"/>,
    /// expanding its dimensions while preserving existing contents.
    /// </summary>
    public SchematicBuilder EnsureDimensions(int minWidth, int minHeight, int minLength)
    {
        _schematic.EnsureDimensions(minWidth, minHeight, minLength);
        return this;
    }

    /// <summary>
    /// Places an existing <paramref name="schematic"/> into this schematic starting at (startX, startY, startZ),
    /// extending dimensions automatically if needed.
    /// </summary>
    public SchematicBuilder Place(Schematic schematic, int startX = 0, int startY = 0, int startZ = 0, bool includeAir = false)
    {
        _schematic.Place(schematic, startX, startY, startZ, includeAir);
        return this;
    }

    /// <summary>
    /// Places an existing <paramref name="schematic"/> into this schematic starting at <paramref name="start"/>,
    /// extending dimensions automatically if needed.
    /// </summary>
    public SchematicBuilder Place(Schematic schematic, BlockPosition start, bool includeAir = false)
    {
        _schematic.Place(schematic, start, includeAir);
        return this;
    }

    /// <summary>
    /// Places a <see cref="SchematicCreator"/> into this schematic starting at (startX, startY, startZ),
    /// extending dimensions automatically if needed.
    /// </summary>
    public SchematicBuilder Place(SchematicCreator creator, int startX = 0, int startY = 0, int startZ = 0)
    {
        ArgumentNullException.ThrowIfNull(creator);
        return creator.Place(this, startX, startY, startZ);
    }

    /// <summary>
    /// Places a <see cref="SchematicCreator"/> into this schematic starting at <paramref name="start"/>,
    /// extending dimensions automatically if needed.
    /// </summary>
    public SchematicBuilder Place(SchematicCreator creator, BlockPosition start) =>
        Place(creator, start.X, start.Y, start.Z);

    /// <summary>
    /// Places an existing <paramref name="schematic"/> into this schematic starting at (startX, startY, startZ),
    /// extending dimensions automatically if needed.
    /// </summary>
    public SchematicBuilder Create(Schematic schematic, int startX = 0, int startY = 0, int startZ = 0, bool includeAir = false) =>
        Place(schematic, startX, startY, startZ, includeAir);

    /// <summary>
    /// Places an existing <paramref name="schematic"/> into this schematic starting at <paramref name="start"/>,
    /// extending dimensions automatically if needed.
    /// </summary>
    public SchematicBuilder Create(Schematic schematic, BlockPosition start, bool includeAir = false) =>
        Place(schematic, start, includeAir);

    /// <summary>
    /// Places a <see cref="SchematicCreator"/> into this schematic starting at (startX, startY, startZ),
    /// extending dimensions automatically if needed.
    /// </summary>
    public SchematicBuilder Create(SchematicCreator creator, int startX = 0, int startY = 0, int startZ = 0) =>
        Place(creator, startX, startY, startZ);

    /// <summary>
    /// Places a <see cref="SchematicCreator"/> into this schematic starting at <paramref name="start"/>,
    /// extending dimensions automatically if needed.
    /// </summary>
    public SchematicBuilder Create(SchematicCreator creator, BlockPosition start) =>
        Place(creator, start);

    /// <summary>
    /// Builds and returns the finished schematic, applying <see cref="Rotation"/> if configured.
    /// </summary>
    public Schematic Build() =>
        Rotation == Rotation.None ? _schematic : _schematic.Rotate(Rotation);

    /// <summary>
    /// Generates C# fluent builder source code that reconstructs the schematic currently configured.
    /// </summary>
    public string ToFluentBuilderCode(FluentCodeGeneratorOptions? options = null) =>
        Build().ToFluentBuilderCode(options);
}
