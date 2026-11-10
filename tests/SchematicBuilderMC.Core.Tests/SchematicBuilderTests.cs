namespace SchematicBuilderMC.Core.Tests;

public class SchematicBuilderTests
{
    [Fact]
    public void Builder_FluentConfiguration_CreatesExpectedSchematic()
    {
        var schematic = SchematicBuilder.Create(5, 6, 7)
            .WithOffset(1, 2, 3)
            .WithDataVersion(3465)
            .WithName("Pyramid")
            .WithAuthor("Alex")
            .WithDescription("A small sandstone pyramid")
            .WithRequiredMod("worldedit")
            .Fill(0, 0, 0, 4, 0, 4, "minecraft:sandstone")
            .SetBlock(2, 1, 2, "minecraft:gold_block")
            .AddBlockEntity(new BlockEntity("minecraft:chest", new BlockPosition(2, 1, 2)))
            .Build();

        Assert.Equal(5, schematic.Width);
        Assert.Equal(6, schematic.Height);
        Assert.Equal(7, schematic.Length);
        Assert.Equal(new BlockPosition(1, 2, 3), schematic.Offset);
        Assert.Equal(3465, schematic.DataVersion);
        Assert.Equal("Pyramid", schematic.Metadata.Name);
        Assert.Equal("Alex", schematic.Metadata.Author);
        Assert.Equal("A small sandstone pyramid", schematic.Metadata.Description);
        Assert.Contains("worldedit", schematic.Metadata.RequiredMods);

        Assert.Equal("minecraft:sandstone", schematic[0, 0, 0].Identifier);
        Assert.Equal("minecraft:gold_block", schematic[2, 1, 2].Identifier);
        Assert.Single(schematic.BlockEntities);
    }
}
