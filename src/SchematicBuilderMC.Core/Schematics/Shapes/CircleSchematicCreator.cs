using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;
using SchematicBuilderMC.Registries;

namespace SchematicBuilderMC.Schematics.Shapes;

/// <summary>
/// Generates a filled or hollow circle schematic in the plane perpendicular to <see cref="NormalAxis"/>.
/// </summary>
public sealed class CircleSchematicCreator : SchematicCreator
{
    /// <summary>
    /// Circle radius in blocks (0 or greater).
    /// </summary>
    public int Radius { get; set; } = 5;

    /// <summary>
    /// Block identifier used to build the circle.
    /// </summary>
    public string Block { get; set; } = Minecraft.Blocks.StoneBricks;

    /// <summary>
    /// Whether the interior of the circle is filled (<c>true</c>) or hollow outline only (<c>false</c>).
    /// </summary>
    public bool Filled { get; set; } = true;

    /// <summary>
    /// Axis perpendicular to the plane of the circle.
    /// </summary>
    public Axis NormalAxis { get; set; } = Axis.Y;

    /// <summary>
    /// Places the circle into an existing <see cref="SchematicBuilder"/> with its bounding box starting at (startX, startY, startZ).
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
                $"A circle ({width}x{height}x{length}) placed at ({startX}, {startY}, {startZ}) exceeds schematic bounds ({builder.Width}x{builder.Height}x{builder.Length}).");
        }

        var (cx, cy, cz) = NormalAxis switch
        {
            Axis.X => (startX, startY + Radius, startZ + Radius),
            Axis.Y => (startX + Radius, startY, startZ + Radius),
            Axis.Z => (startX + Radius, startY + Radius, startZ),
            _ => throw new ArgumentOutOfRangeException(nameof(NormalAxis), NormalAxis, "Unsupported axis value.")
        };

        return builder.SetCircle(cx, cy, cz, Radius, Block, Filled, NormalAxis);
    }

    /// <summary>
    /// Creates a new schematic containing this circle.
    /// </summary>
    public override SchematicBuilder Create()
    {
        ValidateConfiguration();

        var (width, height, length) = GetDimensions();
        var builder = SchematicBuilder.Create(width, height, length)
            .WithRotation(Rotation)
            .WithOffset(0, 0, 0)
            .WithName("Circle")
            .WithAuthor("SchematicBuilderMC")
            .WithDescription($"A {(Filled ? "filled" : "hollow")} circle of radius {Radius} on normal axis {NormalAxis}.");

        return Place(builder, 0, 0, 0);
    }

    private (int Width, int Height, int Length) GetDimensions()
    {
        int diameter = 2 * Radius + 1;
        return NormalAxis switch
        {
            Axis.X => (1, diameter, diameter),
            Axis.Y => (diameter, 1, diameter),
            Axis.Z => (diameter, diameter, 1),
            _ => throw new ArgumentOutOfRangeException(nameof(NormalAxis), NormalAxis, "Unsupported axis value.")
        };
    }

    private void ValidateConfiguration()
    {
        if (Radius < 0)
            throw new ArgumentOutOfRangeException(nameof(Radius), Radius, "Radius must be zero or greater.");
        if (!Enum.IsDefined(NormalAxis))
            throw new ArgumentOutOfRangeException(nameof(NormalAxis), NormalAxis, "Unsupported axis value.");
        ArgumentException.ThrowIfNullOrWhiteSpace(Block);
    }
}
