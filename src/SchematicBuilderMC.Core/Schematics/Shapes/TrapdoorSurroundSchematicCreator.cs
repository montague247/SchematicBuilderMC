using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Models;
using SchematicBuilderMC.Registries;

namespace SchematicBuilderMC.Schematics.Shapes;

/// <summary>
/// Builds a schematic containing one or more blocks (either a <see cref="Width"/> x <see cref="Height"/> x <see cref="Length"/> cuboid
/// or explicit <see cref="Positions"/>) surrounded on their exposed horizontal sides with outward-facing trapdoors.
/// </summary>
public class TrapdoorSurroundSchematicCreator : SchematicCreator
{
    /// <summary>
    /// Block placed at every target position before surrounding with trapdoors.
    /// Set to <c>null</c> or <see cref="Minecraft.Blocks.Air"/> to leave target cells empty.
    /// </summary>
    public string? Block { get; set; } = Minecraft.Blocks.SmoothStone;

    /// <summary>
    /// Trapdoor block identifier used to surround the target block(s). Defaults to <see cref="Minecraft.Blocks.OakTrapdoor"/>.
    /// </summary>
    public string TrapdoorBlock { get; set; } = Minecraft.Blocks.OakTrapdoor;

    /// <summary>
    /// Width (X axis) of the target block region when <see cref="Positions"/> is empty. Defaults to 1.
    /// </summary>
    public int Width { get; set; } = 1;

    /// <summary>
    /// Height (Y axis) of the target block region when <see cref="Positions"/> is empty. Defaults to 1.
    /// </summary>
    public int Height { get; set; } = 1;

    /// <summary>
    /// Length (Z axis) of the target block region when <see cref="Positions"/> is empty. Defaults to 1.
    /// </summary>
    public int Length { get; set; } = 1;

    /// <summary>
    /// Optional explicit set of block positions to surround instead of the rectangular <see cref="Width"/> x <see cref="Height"/> x <see cref="Length"/> box.
    /// </summary>
    public IReadOnlyList<BlockPosition> Positions { get; set; } = [];

    /// <summary>
    /// When true (default), places trapdoors on the bottom half (<c>half=bottom</c>); otherwise places them on the top half (<c>half=top</c>).
    /// </summary>
    public bool Bottom { get; set; } = true;

    /// <summary>
    /// When true (default), places trapdoors in the open vertical state (<c>open=true</c>); otherwise places them closed (<c>open=false</c>).
    /// </summary>
    public bool Open { get; set; } = true;

    /// <summary>
    /// Sets the <c>powered</c> blockstate property on placed trapdoors. Defaults to false.
    /// </summary>
    public bool Powered { get; set; }

    /// <summary>
    /// Sets the <c>waterlogged</c> blockstate property on placed trapdoors. Defaults to false.
    /// </summary>
    public bool Waterlogged { get; set; }

    public override SchematicBuilder Create()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(TrapdoorBlock);

        List<BlockPosition> normalizedTargets;
        int boundingWidth;
        int boundingHeight;
        int boundingLength;

        if (Positions is { Count: > 0 })
        {
            var unique = new HashSet<BlockPosition>(Positions);
            int minX = unique.Min(p => p.X);
            int minY = unique.Min(p => p.Y);
            int minZ = unique.Min(p => p.Z);
            int maxX = unique.Max(p => p.X);
            int maxY = unique.Max(p => p.Y);
            int maxZ = unique.Max(p => p.Z);

            normalizedTargets = unique
                .Select(p => new BlockPosition((p.X - minX) + 1, p.Y - minY, (p.Z - minZ) + 1))
                .ToList();

            boundingWidth = (maxX - minX) + 3;
            boundingHeight = (maxY - minY) + 1;
            boundingLength = (maxZ - minZ) + 3;
        }
        else
        {
            if (Width <= 0) throw new ArgumentOutOfRangeException(nameof(Width), Width, "Width must be greater than zero.");
            if (Height <= 0) throw new ArgumentOutOfRangeException(nameof(Height), Height, "Height must be greater than zero.");
            if (Length <= 0) throw new ArgumentOutOfRangeException(nameof(Length), Length, "Length must be greater than zero.");

            normalizedTargets = new List<BlockPosition>(checked(Width * Height * Length));
            for (int y = 0; y < Height; y++)
            {
                for (int z = 0; z < Length; z++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        normalizedTargets.Add(new BlockPosition(x + 1, y, z + 1));
                    }
                }
            }

            boundingWidth = Width + 2;
            boundingHeight = Height;
            boundingLength = Length + 2;
        }

        var builder = SchematicBuilder.Create(boundingWidth, boundingHeight, boundingLength, Rotation)
            .WithOffset(0, 0, 0)
            .WithName("Trapdoor Surround")
            .WithAuthor("SchematicBuilderMC")
            .WithDescription($"Trapdoor-surrounded block structure ({normalizedTargets.Count} block(s), bottom={Bottom}, open={Open}).");

        if (!string.IsNullOrWhiteSpace(Block))
        {
            var targetState = BlockState.Parse(Block);
            if (!targetState.IsAir)
            {
                foreach (var pos in normalizedTargets)
                {
                    builder.SetBlock(pos, targetState);
                }
            }
        }

        builder.SurroundWithTrapdoors(
            normalizedTargets,
            TrapdoorBlock,
            bottom: Bottom,
            open: Open,
            powered: Powered,
            waterlogged: Waterlogged);

        return builder;
    }
}
