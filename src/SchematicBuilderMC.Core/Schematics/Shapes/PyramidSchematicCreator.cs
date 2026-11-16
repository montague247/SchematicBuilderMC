using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Registries;

namespace SchematicBuilderMC.Schematics.Shapes;

/// <summary>
/// Generates a filled or hollow square-base pyramid schematic.
/// </summary>
public sealed class PyramidSchematicCreator : SchematicCreator
{
    /// <summary>
    /// Edge length of the square pyramid base in blocks (greater than 0).
    /// </summary>
    public int BaseSize { get; set; } = 11;

    /// <summary>
    /// Block identifier used to build the pyramid.
    /// </summary>
    public string Block { get; set; } = Minecraft.Blocks.Sandstone;

    /// <summary>
    /// Whether the pyramid interior is filled (<c>true</c>) or hollow shell only (<c>false</c>).
    /// </summary>
    public bool Filled { get; set; } = true;

    /// <summary>
    /// Whether to use oriented stair blocks along the outer pyramid slopes (<c>true</c>) instead of full blocks only (<c>false</c>).
    /// </summary>
    public bool UseStairs { get; set; } = true;

    /// <summary>
    /// Optional stair block identifier used when <see cref="UseStairs"/> is <c>true</c>. When <c>null</c>, it is inferred from <see cref="Block"/>.
    /// </summary>
    public string? StairBlock { get; set; } = Minecraft.Blocks.SandstoneStairs;

    /// <summary>
    /// Places the pyramid into an existing <see cref="SchematicBuilder"/> starting at (startX, startY, startZ).
    /// </summary>
    public SchematicBuilder Place(SchematicBuilder builder, int startX = 0, int startY = 0, int startZ = 0)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ValidateConfiguration();

        int effectiveBaseSize = GetEffectiveBaseSize();
        int pyramidHeight = (effectiveBaseSize + 1) / 2;
        if (startX < 0 || startY < 0 || startZ < 0 ||
            startX + effectiveBaseSize > builder.Width ||
            startY + pyramidHeight > builder.Height ||
            startZ + effectiveBaseSize > builder.Length)
        {
            throw new ArgumentOutOfRangeException(
                nameof(startX),
                $"A pyramid ({effectiveBaseSize}x{pyramidHeight}x{effectiveBaseSize}) placed at ({startX}, {startY}, {startZ}) exceeds schematic bounds ({builder.Width}x{builder.Height}x{builder.Length}).");
        }

        return builder.SetPyramid(startX, startY, startZ, effectiveBaseSize, Block, Filled, UseStairs, StairBlock);
    }

    /// <summary>
    /// Creates a new schematic containing this pyramid.
    /// </summary>
    public override SchematicBuilder Create()
    {
        ValidateConfiguration();

        int effectiveBaseSize = GetEffectiveBaseSize();
        int pyramidHeight = (effectiveBaseSize + 1) / 2;
        var builder = SchematicBuilder.Create(effectiveBaseSize, pyramidHeight, effectiveBaseSize)
            .WithRotation(Rotation)
            .WithOffset(0, 0, 0)
            .WithName("Pyramid")
            .WithAuthor("SchematicBuilderMC")
            .WithDescription($"A {(Filled ? "filled" : "hollow")} square-base pyramid of base size {effectiveBaseSize}.");

        return Place(builder, 0, 0, 0);
    }

    private int GetEffectiveBaseSize() => ScaleDimension(BaseSize, minimum: 1);

    private void ValidateConfiguration()
    {
        if (BaseSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(BaseSize), BaseSize, "BaseSize must be greater than zero.");
        ValidateSmoothnessFactor();
        ArgumentException.ThrowIfNullOrWhiteSpace(Block);
    }
}
