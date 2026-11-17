using SchematicBuilderMC.Enums;
using SchematicBuilderMC.IO;
using SchematicBuilderMC.Schematics.Farms;

namespace SchematicBuilderMC.Core.Tests.Schematics.Farms;

public sealed class SlimeLaboratorySchematicCreatorTests
{
    [Fact]
    public void Build_ShouldCreateSchematicWithCorrectDimensions()
    {
        // Arrange
        var farm = new SlimeLaboratorySchematicCreator();

        // Act
        var schematic = farm.Build();

        // Assert
        Assert.Equal(19, schematic.Width);
        Assert.Equal(18, schematic.Height);
        Assert.Equal(19, schematic.Length);

        // Write to file
        string spongePath = Path.Combine(SchematicCreatorTests.TestOutputDirectory, $"{schematic.Metadata.Name}.schem");
        SchematicFile.Write(schematic, spongePath, SchematicFormat.SpongeV2);
    }
}
