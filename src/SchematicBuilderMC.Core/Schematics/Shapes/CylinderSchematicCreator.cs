using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;
using SchematicBuilderMC.Registries;

namespace SchematicBuilderMC.Schematics.Shapes;

/// <summary>
/// Generates a filled or hollow 3D cylinder schematic extruded along <see cref="Axis"/>.
/// </summary>
public sealed class CylinderSchematicCreator : SchematicCreator
{
    /// <summary>
    /// Cylinder cross-section radius in blocks (0 or greater).
    /// </summary>
    public int Radius { get; set; } = 4;

    /// <summary>
    /// Cylinder length along <see cref="Axis"/> (greater than 0).
    /// </summary>
    public int Height { get; set; } = 8;

    /// <summary>
    /// Block identifier used to build the cylinder.
    /// </summary>
    public string Block { get; set; } = Minecraft.Blocks.StoneBricks;

    /// <summary>
    /// Whether the cylinder interior is filled (<c>true</c>) or a hollow tube (<c>false</c>).
    /// </summary>
    public bool Filled { get; set; } = false;

    /// <summary>
    /// Axis along which the cylinder extends.
    /// </summary>
    public Axis Axis { get; set; } = Axis.Y;

    /// <summary>
    /// Places the cylinder into an existing <see cref="SchematicBuilder"/> with its bounding box starting at (startX, startY, startZ).
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
                $"A cylinder ({width}x{height}x{length}) placed at ({startX}, {startY}, {startZ}) exceeds schematic bounds ({builder.Width}x{builder.Height}x{builder.Length}).");
        }

        var (bx, by, bz) = Axis switch
        {
            Axis.X => (startX, startY + Radius, startZ + Radius),
            Axis.Y => (startX + Radius, startY, startZ + Radius),
            Axis.Z => (startX + Radius, startY + Radius, startZ),
            _ => throw new ArgumentOutOfRangeException(nameof(Axis), Axis, "Unsupported axis value.")
        };

        return builder.SetCylinder(bx, by, bz, Radius, Height, Block, Filled, Axis);
    }

    /// <summary>
    /// Creates a new schematic containing this cylinder.
    /// </summary>
    public override SchematicBuilder Create()
    {
        ValidateConfiguration();

        var (width, height, length) = GetDimensions();
        var builder = SchematicBuilder.Create(width, height, length)
            .WithRotation(Rotation)
            .WithOffset(0, 0, 0)
            .WithName("Cylinder")
            .WithAuthor("SchematicBuilderMC")
            .WithDescription($"A {(Filled ? "filled" : "hollow")} cylinder (radius {Radius}, height {Height}) along axis {Axis}.");

        return Place(builder, 0, 0, 0);
    }

    private (int Width, int Height, int Length) GetDimensions()
    {
        int diameter = 2 * Radius + 1;
        return Axis switch
        {
            Axis.X => (Height, diameter, diameter),
            Axis.Y => (diameter, Height, diameter),
            Axis.Z => (diameter, diameter, Height),
            _ => throw new ArgumentOutOfRangeException(nameof(Axis), Axis, "Unsupported axis value.")
        };
    }

    private void ValidateConfiguration()
    {
        if (Radius < 0)
            throw new ArgumentOutOfRangeException(nameof(Radius), Radius, "Radius must be zero or greater.");
        if (Height <= 0)
            throw new ArgumentOutOfRangeException(nameof(Height), Height, "Height must be greater than zero.");
        if (!Enum.IsDefined(Axis))
            throw new ArgumentOutOfRangeException(nameof(Axis), Axis, "Unsupported axis value.");
        ArgumentException.ThrowIfNullOrWhiteSpace(Block);
    }
}
