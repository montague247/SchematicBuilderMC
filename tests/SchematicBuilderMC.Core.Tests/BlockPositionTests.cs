namespace SchematicBuilderMC.Core.Tests;

public class BlockPositionTests
{
    [Fact]
    public void AdditionAndSubtraction_WorkCorrectly()
    {
        var a = new BlockPosition(10, 20, 30);
        var b = new BlockPosition(1, -2, 3);

        var sum = a + b;
        Assert.Equal(11, sum.X);
        Assert.Equal(18, sum.Y);
        Assert.Equal(33, sum.Z);

        var diff = a - b;
        Assert.Equal(9, diff.X);
        Assert.Equal(22, diff.Y);
        Assert.Equal(27, diff.Z);
    }

    [Fact]
    public void Deconstruct_WorksCorrectly()
    {
        var pos = new BlockPosition(5, 10, 15);
        var (x, y, z) = pos;

        Assert.Equal(5, x);
        Assert.Equal(10, y);
        Assert.Equal(15, z);
    }

    [Fact]
    public void EqualityAndToString_WorkCorrectly()
    {
        var a = new BlockPosition(1, 2, 3);
        var b = new BlockPosition(1, 2, 3);
        var c = new BlockPosition(4, 5, 6);

        Assert.Equal(a, b);
        Assert.NotEqual(a, c);
        Assert.Equal("(1, 2, 3)", a.ToString());
    }
}
