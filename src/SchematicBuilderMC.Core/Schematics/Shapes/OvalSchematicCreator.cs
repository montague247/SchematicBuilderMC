using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;
using SchematicBuilderMC.Registries;

namespace SchematicBuilderMC.Schematics.Shapes;

/// <summary>
/// Generates a filled or hollow oval (ellipse) schematic in the plane perpendicular to <see cref="NormalAxis"/>.
/// </summary>
public sealed class OvalSchematicCreator : SchematicCreator
{
    /// <summary>
    /// Semi-axis radius along the first planar axis (0 or greater).
    /// </summary>
    public int RadiusU { get; set; } = 6;

    /// <summary>
    /// Semi-axis radius along the second planar axis (0 or greater).
    /// </summary>
    public int RadiusV { get; set; } = 4;

    /// <summary>
    /// Block identifier used to build the oval.
    /// </summary>
    public string Block { get; set; } = Minecraft.Blocks.StoneBricks;

    /// <summary>
    /// Whether the interior of the oval is filled (<c>true</c>) or hollow outline only (<c>false</c>).
    /// </summary>
    public bool Filled { get; set; } = true;

    /// <summary>
    /// Axis perpendicular to the plane of the oval.
    /// </summary>
    public Axis NormalAxis { get; set; } = Axis.Y;

    /// <summary>
    /// Places the oval into an existing <see cref="SchematicBuilder"/> with its bounding box starting at (startX, startY, startZ).
    /// </summary>
    public SchematicBuilder Place(SchematicBuilder builder, int startX = 0, int startY = 0, int startZ = 0)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ValidateConfiguration();

        var (width, height, length) = GetDimensions();
        if (startX < 0 || startY < 0 || startZ < 0 ||
            startX + width > builder.Width ||
            startY + height > builder.Height ||
            startZ + length > builder.Length)
        {
            throw new ArgumentOutOfRangeException(
                nameof(startX),
                $"An oval ({width}x{height}x{length}) placed at ({startX}, {startY}, {startZ}) exceeds schematic bounds ({builder.Width}x{builder.Height}x{builder.Length}).");
        }

        var (cx, cy, cz) = NormalAxis switch
        {
            Axis.X => (startX, startY + RadiusU, startZ + RadiusV),
            Axis.Y => (startX + RadiusU, startY, startZ + RadiusV),
            Axis.Z => (startX + RadiusU, startY + RadiusV, startZ),
            _ => throw new ArgumentOutOfRangeException(nameof(NormalAxis), NormalAxis, "Unsupported axis value.")
        };

        return builder.SetOval(cx, cy, cz, RadiusU, RadiusV, Block, Filled, NormalAxis);
    }

    /// <summary>
    /// Creates a new schematic containing this oval.
    /// </summary>
    public override SchematicBuilder Create()
    {
        ValidateConfiguration();

        var (width, height, length) = GetDimensions();
        var builder = SchematicBuilder.Create(width, height, length)
            .WithRotation(Rotation)
            .WithOffset(0, 0, 0)
            .WithName("Oval")
            .WithAuthor("SchematicBuilderMC")
            .WithDescription($"A {(Filled ? "filled" : "hollow")} oval with radii ({RadiusU}, {RadiusV}) on normal axis {NormalAxis}.");

        return Place(builder, 0, 0, 0);
    }

    private (int Width, int Height, int Length) GetDimensions()
    {
        int spanU = 2 * RadiusU + 1;
        int spanV = 2 * RadiusV + 1;
        return NormalAxis switch
        {
            Axis.X => (1, spanU, spanV),
            Axis.Y => (spanU, 1, spanV),
            Axis.Z => (spanU, spanV, 1),
            _ => throw new ArgumentOutOfRangeException(nameof(NormalAxis), NormalAxis, "Unsupported axis value.")
        };
    }

    private void ValidateConfiguration()
    {
        if (RadiusU < 0)
            throw new ArgumentOutOfRangeException(nameof(RadiusU), RadiusU, "RadiusU must be zero or greater.");
        if (RadiusV < 0)
            throw new ArgumentOutOfRangeException(nameof(RadiusV), RadiusV, "RadiusV must be zero or greater.");
        if (!Enum.IsDefined(NormalAxis))
            throw new ArgumentOutOfRangeException(nameof(NormalAxis), NormalAxis, "Unsupported axis value.");
        ArgumentException.ThrowIfNullOrWhiteSpace(Block);
    }
}
