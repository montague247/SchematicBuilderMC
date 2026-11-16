using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Models;
using SchematicBuilderMC.Registries;

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

    [Fact]
    public void Builder_AddEntityWithBlockCoordinates_AppliesOffsetAndCentersHorizontally()
    {
        var schematic = SchematicBuilder.Create(8, 8, 8)
            .WithOffset(-8, -4, 1)
            .AddEntity(1, 5, 2, new Entity(Minecraft.Entities.Allay).WithNoGravity())
            .AddEntity(new BlockPosition(3, 4, 5), new Entity(Minecraft.Entities.ArmorStand))
            .AddEntity(2, 6, 3, Minecraft.Entities.Cow, customName: "Bessie")
            .AddEntity(new BlockPosition(0, 4, 0), Minecraft.Entities.Pig, customName: "Speedy")
            .Build();

        Assert.Equal(4, schematic.Entities.Count);

        var allay = schematic.Entities[0];
        Assert.Equal(-6.5, allay.X);
        Assert.Equal(1.0, allay.Y);
        Assert.Equal(3.5, allay.Z);
        Assert.True(allay.NoGravity);

        var stand = schematic.Entities[1];
        Assert.Equal(-4.5, stand.X);
        Assert.Equal(0.0, stand.Y);
        Assert.Equal(6.5, stand.Z);

        var cow = schematic.Entities[2];
        Assert.Equal(-5.5, cow.X);
        Assert.Equal(2.0, cow.Y);
        Assert.Equal(4.5, cow.Z);
        Assert.Equal("Bessie", cow.CustomName);

        var pig = schematic.Entities[3];
        Assert.Equal(-7.5, pig.X);
        Assert.Equal(0.0, pig.Y);
        Assert.Equal(1.5, pig.Z);
        Assert.Equal("Speedy", pig.CustomName);
    }
}
