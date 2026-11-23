using SchematicBuilderMC.Enums;
using SchematicBuilderMC.IO;
using SchematicBuilderMC.Registries;
using SchematicBuilderMC.Schematics.Farms;

namespace SchematicBuilderMC.Core.Tests.Schematics.Farms;

public sealed class BambooFarmWithAllaysSchematicCreatorTests
{
    [Fact]
    public void Build_ShouldCreateSchematicWithCorrectDimensions()
    {
        // Arrange
        var bambooFarm = new BambooFarmWithAllaysSchematicCreator();

        // Act
        var schematic = bambooFarm.Build();

        // Assert
        Assert.Equal(35, schematic.Width);
        Assert.Equal(9, schematic.Height);
        Assert.Equal(35, schematic.Length);

        Assert.Equal(4, schematic.Entities.Count);
        var firstAllay = schematic.Entities[0];
        Assert.Equal(Minecraft.Entities.Allay, firstAllay.Id);
        Assert.Equal(1.5, firstAllay.X);
        Assert.Equal(6.0, firstAllay.Y);
        Assert.Equal(1.5, firstAllay.Z);

        var secondAllay = schematic.Entities[1];
        Assert.Equal(Minecraft.Entities.Allay, secondAllay.Id);
        Assert.Equal(33.5, secondAllay.X);
        Assert.Equal(6.0, secondAllay.Y);
        Assert.Equal(1.5, secondAllay.Z);

        var thirdAllay = schematic.Entities[2];
        Assert.Equal(Minecraft.Entities.Allay, thirdAllay.Id);
        Assert.Equal(33.5, thirdAllay.X);
        Assert.Equal(6.0, thirdAllay.Y);
        Assert.Equal(30.5, thirdAllay.Z);

        var fourthAllay = schematic.Entities[3];
        Assert.Equal(Minecraft.Entities.Allay, fourthAllay.Id);
        Assert.Equal(1.5, fourthAllay.X);
        Assert.Equal(6.0, fourthAllay.Y);
        Assert.Equal(30.5, fourthAllay.Z);

        // Write to file
        string spongePath = Path.Combine(SchematicCreatorTests.TestOutputDirectory, $"{schematic.Metadata.Name}.schem");
        SchematicFile.Write(schematic, spongePath, SchematicFormat.SpongeV2);
    }
}
