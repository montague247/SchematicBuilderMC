using SchematicBuilderMC.Enums;
using SchematicBuilderMC.IO;
using SchematicBuilderMC.Schematics.Contraptions;

namespace SchematicBuilderMC.Core.Tests.Schematics.Contraptions;

public sealed class FlatFylingMachineSchematicCreatorTests
{
    [Fact]
    public void Build_ShouldCreateSchematicWithCorrectDimensions()
    {
        // Arrange
        var bambooFarm = new FlatFylingMachineSchematicCreator();

        // Act
        var schematic = bambooFarm.Build();

        // Assert
        Assert.Equal(20, schematic.Width);
        Assert.Equal(2, schematic.Height);

        // Write to file
        string spongePath = Path.Combine(Path.GetFullPath("."), $"{schematic.Metadata.Name}.schem");
        SchematicFile.Write(schematic, spongePath, SchematicFormat.SpongeV2);
    }
}