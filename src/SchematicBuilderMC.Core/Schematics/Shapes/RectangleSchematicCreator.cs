using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;
using SchematicBuilderMC.Registries;

namespace SchematicBuilderMC.Schematics.Shapes;

/// <summary>
/// Generates a filled or hollow rectangle schematic in the plane perpendicular to <see cref="NormalAxis"/>.
/// </summary>
public sealed class RectangleSchematicCreator : SchematicCreator
{
    /// <summary>
    /// Rectangle span along its first planar axis (greater than 0).
    /// </summary>
    public int Width { get; set; } = 8;

    /// <summary>
    /// Rectangle span along its second planar axis (greater than 0).
    /// </summary>
    public int Length { get; set; } = 6;

    /// <summary>
    /// Block identifier used to build the rectangle.
    /// </summary>
    public string Block { get; set; } = Minecraft.Blocks.StoneBricks;

    /// <summary>
    /// Whether the interior of the rectangle is filled (<c>true</c>) or frame outline only (<c>false</c>).
    /// </summary>
    public bool Filled { get; set; } = true;

    /// <summary>
    /// Axis perpendicular to the plane of the rectangle.
    /// </summary>
    public Axis NormalAxis { get; set; } = Axis.Y;

    /// <summary>
    /// Places the rectangle into an existing <see cref="SchematicBuilder"/> starting at (startX, startY, startZ).
    /// </summary>
    public SchematicBuilder Place(SchematicBuilder builder, int startX = 0, int startY = 0, int startZ = 0)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ValidateConfiguration();

        var (effectiveWidth, effectiveLength) = GetEffectiveSpan();
        var (width, height, length) = GetDimensions(effectiveWidth, effectiveLength);
        if (startX < 0 || startY < 0 || startZ < 0 ||
            startX + width > builder.Width ||
            startY + height > builder.Height ||
            startZ + length > builder.Length)
        {
            throw new ArgumentOutOfRangeException(
                nameof(startX),
                $"A rectangle ({width}x{height}x{length}) placed at ({startX}, {startY}, {startZ}) exceeds schematic bounds ({builder.Width}x{builder.Height}x{builder.Length}).");
        }

        return builder.SetRectangle(startX, startY, startZ, effectiveWidth, effectiveLength, Block, Filled, NormalAxis);
    }

    /// <summary>
    /// Creates a new schematic containing this rectangle.
    /// </summary>
    public override SchematicBuilder Create()
    {
        ValidateConfiguration();

        var (effectiveWidth, effectiveLength) = GetEffectiveSpan();
        var (width, height, length) = GetDimensions(effectiveWidth, effectiveLength);
        var builder = SchematicBuilder.Create(width, height, length)
            .WithRotation(Rotation)
            .WithOffset(0, 0, 0)
            .WithName("Rectangle")
            .WithAuthor("SchematicBuilderMC")
            .WithDescription($"A {(Filled ? "filled" : "hollow")} {effectiveWidth}x{effectiveLength} rectangle on normal axis {NormalAxis}.");

        return Place(builder, 0, 0, 0);
    }

    private (int Width, int Length) GetEffectiveSpan() =>
        (ScaleDimension(Width, minimum: 1), ScaleDimension(Length, minimum: 1));

    private (int Width, int Height, int Length) GetDimensions(int effectiveWidth, int effectiveLength) => NormalAxis switch
    {
        Axis.X => (1, effectiveWidth, effectiveLength),
        Axis.Y => (effectiveWidth, 1, effectiveLength),
        Axis.Z => (effectiveWidth, effectiveLength, 1),
        _ => throw new ArgumentOutOfRangeException(nameof(NormalAxis), NormalAxis, "Unsupported axis value.")
    };

    private void ValidateConfiguration()
    {
        if (Width <= 0)
            throw new ArgumentOutOfRangeException(nameof(Width), Width, "Width must be greater than zero.");
        if (Length <= 0)
            throw new ArgumentOutOfRangeException(nameof(Length), Length, "Length must be greater than zero.");
        ValidateSmoothnessFactor();
        if (!Enum.IsDefined(NormalAxis))
            throw new ArgumentOutOfRangeException(nameof(NormalAxis), NormalAxis, "Unsupported axis value.");
        ArgumentException.ThrowIfNullOrWhiteSpace(Block);
    }
}
