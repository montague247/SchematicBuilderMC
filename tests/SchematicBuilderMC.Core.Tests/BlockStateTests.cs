namespace SchematicBuilderMC.Core.Tests;

public class BlockStateTests
{
    [Fact]
    public void Parse_SimpleIdentifier_AddsDefaultNamespace()
    {
        var state = BlockState.Parse("stone");
        Assert.Equal("minecraft:stone", state.Identifier);
        Assert.Empty(state.Properties);
        Assert.Equal("minecraft:stone", state.ToString());
    }

    [Fact]
    public void Parse_WithNamespaceAndProperties_ExtractsCorrectly()
    {
        var state = BlockState.Parse("minecraft:oak_stairs[waterlogged=false,facing=north,half=bottom]");
        Assert.Equal("minecraft:oak_stairs", state.Identifier);
        Assert.Equal(3, state.Properties.Count);
        Assert.Equal("north", state.GetProperty("facing"));
        Assert.Equal("bottom", state.GetProperty("half"));
        Assert.Equal("false", state.GetProperty("waterlogged"));

        // Canonical string must sort properties alphabetically
        Assert.Equal("minecraft:oak_stairs[facing=north,half=bottom,waterlogged=false]", state.ToString());
    }

    [Fact]
    public void Parse_CanonicalStringComparison_EnsuresEquivalence()
    {
        var state1 = BlockState.Parse("minecraft:chest[waterlogged=false,facing=south]");
        var state2 = BlockState.Parse("minecraft:chest[facing=south,waterlogged=false]");

        Assert.Equal(state1, state2);
        Assert.Equal(state1.GetHashCode(), state2.GetHashCode());
        Assert.True(state1 == state2);
    }

    [Fact]
    public void WithProperty_And_WithoutProperty_WorkCorrectly()
    {
        var initial = BlockState.Parse("minecraft:door[facing=east]");
        var updated = initial.WithProperty("half", "upper");

        Assert.Equal("upper", updated.GetProperty("half"));
        Assert.Equal("east", updated.GetProperty("facing"));

        var removed = updated.WithoutProperty("facing");
        Assert.Null(removed.GetProperty("facing"));
        Assert.Equal("upper", removed.GetProperty("half"));
    }

    [Theory]
    [InlineData("minecraft:air", true)]
    [InlineData("minecraft:cave_air", true)]
    [InlineData("minecraft:void_air", true)]
    [InlineData("air", true)]
    [InlineData("minecraft:stone", false)]
    public void IsAir_IdentifiesAirCorrectly(string id, bool expectedAir)
    {
        var state = BlockState.Parse(id);
        Assert.Equal(expectedAir, state.IsAir);
    }

    [Fact]
    public void ImplicitOperator_AllowsStringConversion()
    {
        BlockState state = "minecraft:diamond_block";
        Assert.Equal("minecraft:diamond_block", state.Identifier);
    }

    [Fact]
    public void TryParse_HandlesValidAndInvalidStrings()
    {
        Assert.True(BlockState.TryParse("stone", out var s1));
        Assert.NotNull(s1);
        Assert.Equal("minecraft:stone", s1.Identifier);

        Assert.False(BlockState.TryParse("", out var s2));
        Assert.Null(s2);

        Assert.False(BlockState.TryParse("   ", out var s3));
        Assert.Null(s3);
    }
}
