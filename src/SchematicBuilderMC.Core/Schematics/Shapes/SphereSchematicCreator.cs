using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Models;
using SchematicBuilderMC.Registries;

namespace SchematicBuilderMC.Schematics.Shapes;

/// <summary>
/// Generates a filled or hollow 3D sphere schematic.
/// </summary>
public sealed class SphereSchematicCreator : ShapesSchematicCreator
{
    /// <summary>
    /// Sphere radius in blocks (0 or greater).
    /// </summary>
    public int Radius { get; set; } = 8;

    /// <summary>
    /// Block identifier used to build the sphere.
    /// </summary>
    public string Block { get; set; } = Minecraft.Blocks.Glass;

    /// <summary>
    /// Whether the interior of the sphere is filled (<c>true</c>) or hollow shell only (<c>false</c>).
    /// </summary>
    public bool Filled { get; set; } = false;

    /// <summary>
    /// Places the sphere into an existing <see cref="SchematicBuilder"/> with its bounding box starting at (startX, startY, startZ),
    /// extending its dimensions automatically if needed.
    /// </summary>
    public override SchematicBuilder Place(SchematicBuilder builder, int startX = 0, int startY = 0, int startZ = 0)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ValidateConfiguration();

        int effectiveRadius = GetEffectiveRadius();
        int diameter = 2 * effectiveRadius + 1;
        EnsurePlacementBounds(builder, startX, startY, startZ, diameter, diameter, diameter);

        BlockGradient gradient = ResolveGradient(Block);
        return builder.SetSphere(startX + effectiveRadius, startY + effectiveRadius, startZ + effectiveRadius, effectiveRadius, gradient, Filled, SmoothnessFactor);
    }

    /// <summary>
    /// Creates a new schematic containing this sphere.
    /// </summary>
    public override SchematicBuilder Create()
    {
        ValidateConfiguration();

        int effectiveRadius = GetEffectiveRadius();
        int diameter = 2 * effectiveRadius + 1;
        var builder = SchematicBuilder.Create(diameter, diameter, diameter)
            .WithRotation(Rotation)
            .WithOffset(0, 0, 0)
            .WithName("Sphere")
            .WithAuthor("SchematicBuilderMC")
            .WithDescription($"A {(Filled ? "filled" : "hollow")} sphere of radius {effectiveRadius}.");

        return Place(builder, 0, 0, 0);
    }

    private int GetEffectiveRadius() => ScaleDimension(Radius, minimum: 0);

    private void ValidateConfiguration()
    {
        if (Radius < 0)
            throw new ArgumentOutOfRangeException(nameof(Radius), Radius, "Radius must be zero or greater.");
        ValidateSmoothnessFactor();
        _ = ResolveGradient(Block);
    }
}
