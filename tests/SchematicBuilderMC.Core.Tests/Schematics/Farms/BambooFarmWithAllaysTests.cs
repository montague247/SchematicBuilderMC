using SchematicBuilderMC.Enums;
using SchematicBuilderMC.IO;
using SchematicBuilderMC.Schematics.Farms;

namespace SchematicBuilderMC.Core.Tests.Schematics.Farms;

public sealed class BambooFarmWithAllaysTests
{
    [Fact]
    public void Build_ShouldCreateSchematicWithCorrectDimensions()
    {
        // Arrange
        var bambooFarm = new BambooFarmWithAllays();

        // Act
        var schematic = bambooFarm.Build();

        // Assert
        Assert.Equal(35, schematic.Width);
        Assert.Equal(12, schematic.Height);
        Assert.Equal(bambooFarm.Length, schematic.Length);

        // Write to file
        string spongePath = Path.Combine(Path.GetFullPath("."), $"{schematic.Metadata.Name}.schem");
        SchematicFile.Write(schematic, spongePath, SchematicFormat.SpongeV2);
    }
}