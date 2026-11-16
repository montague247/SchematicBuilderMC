using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Registries;

namespace SchematicBuilderMC.Schematics.Shapes;

/// <summary>
/// Generates a filled or hollow 3D cube schematic.
/// </summary>
public sealed class CubeSchematicCreator : SchematicCreator
{
    /// <summary>
    /// Edge length of the cube in blocks (greater than 0).
    /// </summary>
    public int Size { get; set; } = 5;

    /// <summary>
    /// Block identifier used to build the cube.
    /// </summary>
    public string Block { get; set; } = Minecraft.Blocks.StoneBricks;

    /// <summary>
    /// Whether the interior of the cube is filled (<c>true</c>) or hollow shell only (<c>false</c>).
    /// </summary>
    public bool Filled { get; set; } = true;

    /// <summary>
    /// Places the cube into an existing <see cref="SchematicBuilder"/> starting at (startX, startY, startZ).
    /// </summary>
    public SchematicBuilder Place(SchematicBuilder builder, int startX = 0, int startY = 0, int startZ = 0)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ValidateConfiguration();

        int effectiveSize = GetEffectiveSize();
        if (startX < 0 || startY < 0 || startZ < 0 ||
            startX + effectiveSize > builder.Width ||
            startY + effectiveSize > builder.Height ||
            startZ + effectiveSize > builder.Length)
        {
            throw new ArgumentOutOfRangeException(
                nameof(startX),
                $"A cube ({effectiveSize}x{effectiveSize}x{effectiveSize}) placed at ({startX}, {startY}, {startZ}) exceeds schematic bounds ({builder.Width}x{builder.Height}x{builder.Length}).");
        }

        return builder.SetCube(startX, startY, startZ, effectiveSize, Block, Filled);
    }

    /// <summary>
    /// Creates a new schematic containing this cube.
    /// </summary>
    public override SchematicBuilder Create()
    {
        ValidateConfiguration();

        int effectiveSize = GetEffectiveSize();
        var builder = SchematicBuilder.Create(effectiveSize, effectiveSize, effectiveSize)
            .WithRotation(Rotation)
            .WithOffset(0, 0, 0)
            .WithName("Cube")
            .WithAuthor("SchematicBuilderMC")
            .WithDescription($"A {(Filled ? "filled" : "hollow")} {effectiveSize}x{effectiveSize}x{effectiveSize} cube.");

        return Place(builder, 0, 0, 0);
    }

    private int GetEffectiveSize() => ScaleDimension(Size, minimum: 1);

    private void ValidateConfiguration()
    {
        if (Size <= 0)
            throw new ArgumentOutOfRangeException(nameof(Size), Size, "Size must be greater than zero.");
        ValidateSmoothnessFactor();
        ArgumentException.ThrowIfNullOrWhiteSpace(Block);
    }
}
