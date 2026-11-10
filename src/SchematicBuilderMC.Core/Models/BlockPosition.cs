namespace SchematicBuilderMC.Core;

/// <summary>
/// Represents a 3D block coordinate in schematic space.
/// </summary>
public readonly record struct BlockPosition(int X, int Y, int Z)
{
    public static readonly BlockPosition Zero = new(0, 0, 0);

    public static BlockPosition operator +(BlockPosition a, BlockPosition b) =>
        new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

    public static BlockPosition operator -(BlockPosition a, BlockPosition b) =>
        new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

    public override string ToString() => $"({X}, {Y}, {Z})";

    public void Deconstruct(out int x, out int y, out int z)
    {
        x = X;
        y = Y;
        z = Z;
    }
}
