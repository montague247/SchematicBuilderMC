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
        // Default Width = 33 (dirt) + 2 (walls) = 35
        // Default Length = 2 (front padding) + 1 (start wall) + 32 (dirt) + 1 (end wall) + 8 (back clock) = 44
        // Default Height = 2 (soilY) + 7 (wallHeight) + 1 + 1 (roof) = 11
        Assert.Equal(35, schematic.Width);
        Assert.Equal(11, schematic.Height);
        Assert.Equal(44, schematic.Length);

        // Verify exact dirt bed size matches Width (33) x Length (32) at SoilLevel (y = 2)
        for (int x = 1; x <= 33; x++)
        {
            for (int z = 3; z <= 34; z++)
            {
                Assert.Equal(Minecraft.Blocks.Dirt, schematic[x, 2, z].Identifier);
            }
        }

        // Walls surround dirt cleanly on x=0/34 and z=2/35
        Assert.Equal(Minecraft.Blocks.WaxedCopperBlock, schematic[0, 2, 2].Identifier);
        Assert.Equal(Minecraft.Blocks.WaxedCopperBlock, schematic[34, 2, 35].Identifier);
        Assert.Equal(Minecraft.Blocks.Chest, schematic[17, 7, 1].Identifier);
        Assert.Equal(Minecraft.Blocks.NoteBlock, schematic[17, 8, 19].Identifier);

        // End wall remains solid (no gap for allays to escape) while the repeater powers internal dust.
        Assert.Equal(Minecraft.Blocks.WaxedCopperBlock, schematic[17, 8, 35].Identifier);
        Assert.Equal(Minecraft.Blocks.Repeater, schematic[17, 8, 36].Identifier);
        Assert.Equal(Minecraft.Blocks.RedstoneWire, schematic[17, 8, 34].Identifier);
        Assert.Equal(Minecraft.Blocks.RedstoneWire, schematic[17, 8, 20].Identifier);

        // Glass roof covers the interior dirt area (x=1..33, z=3..34) and excludes the walls.
        Assert.Equal(Minecraft.Blocks.Air, schematic[0, 10, 2].Identifier);
        Assert.Equal(Minecraft.Blocks.Air, schematic[34, 10, 35].Identifier);
        Assert.Equal(Minecraft.Blocks.Glass, schematic[1, 10, 3].Identifier);
        Assert.Equal(Minecraft.Blocks.Glass, schematic[33, 10, 34].Identifier);

        // Both multi-machine start triggers (x=9, 26 at z=2) and end triggers (x=8, 25 at z=35)
        // are observers wired via horizontal buses and vertical torch towers to the global 30s clock.
        Assert.Equal(Minecraft.Blocks.Observer, schematic[9, 6, 2].Identifier);
        Assert.Equal("north", schematic[9, 6, 2].Properties["facing"]);
        Assert.Equal(Minecraft.Blocks.Observer, schematic[26, 6, 2].Identifier);
        Assert.Equal(Minecraft.Blocks.RedstoneWire, schematic[9, 6, 1].Identifier);
        Assert.Equal(Minecraft.Blocks.RedstoneWire, schematic[26, 6, 1].Identifier);
        Assert.Equal(Minecraft.Blocks.RedstoneWire, schematic[17, 6, 0].Identifier);

        Assert.Equal(Minecraft.Blocks.Observer, schematic[8, 6, 35].Identifier);
        Assert.Equal("south", schematic[8, 6, 35].Properties["facing"]);
        Assert.Equal(Minecraft.Blocks.Observer, schematic[25, 6, 35].Identifier);
        Assert.Equal(Minecraft.Blocks.RedstoneWire, schematic[8, 6, 36].Identifier);
        Assert.Equal(Minecraft.Blocks.RedstoneWire, schematic[25, 6, 36].Identifier);
        Assert.Equal(Minecraft.Blocks.RedstoneWire, schematic[17, 6, 37].Identifier);

        Assert.Equal(4, schematic.Entities.Count);
        var firstAllay = schematic.Entities[0];
        Assert.Equal(Minecraft.Entities.Allay, firstAllay.Id);
        Assert.Equal(1.5, firstAllay.X);
        Assert.Equal(6.0, firstAllay.Y);
        Assert.Equal(3.5, firstAllay.Z);

        var secondAllay = schematic.Entities[1];
        Assert.Equal(Minecraft.Entities.Allay, secondAllay.Id);
        Assert.Equal(33.5, secondAllay.X);
        Assert.Equal(6.0, secondAllay.Y);
        Assert.Equal(3.5, secondAllay.Z);

        var thirdAllay = schematic.Entities[2];
        Assert.Equal(Minecraft.Entities.Allay, thirdAllay.Id);
        Assert.Equal(33.5, thirdAllay.X);
        Assert.Equal(6.0, thirdAllay.Y);
        Assert.Equal(34.5, thirdAllay.Z);

        var fourthAllay = schematic.Entities[3];
        Assert.Equal(Minecraft.Entities.Allay, fourthAllay.Id);
        Assert.Equal(1.5, fourthAllay.X);
        Assert.Equal(6.0, fourthAllay.Y);
        Assert.Equal(34.5, fourthAllay.Z);

        // Write to file
        string spongePath = Path.Combine(SchematicCreatorTests.TestOutputDirectory, $"{schematic.Metadata.Name}.schem");
        SchematicFile.Write(schematic, spongePath, SchematicFormat.SpongeV2);
    }

    [Theory]
    [InlineData(10, 12, 2)]
    [InlineData(10, 12, 6)]
    [InlineData(4, 5, 4)]
    public void Build_CustomDirtSizeAndFrontPadding_PlacesExactDirtAndShiftsStartWall(
        int dirtWidth,
        int dirtLength,
        int frontPadding)
    {
        var farm = new BambooFarmWithAllaysSchematicCreator
        {
            Width = dirtWidth,
            Length = dirtLength,
            FrontPadding = frontPadding,
            RoofBlock = null
        };

        var schematic = farm.Build();

        Assert.Equal(dirtWidth + 2, schematic.Width);
        Assert.Equal(frontPadding + dirtLength + 10, schematic.Length);

        int dirtStartX = 1;
        int dirtEndX = dirtWidth;
        int wallStartZ = frontPadding;
        int dirtStartZ = wallStartZ + 1;
        int dirtEndZ = dirtStartZ + dirtLength - 1;
        int wallEndZ = dirtEndZ + 1;

        for (int x = 0; x < schematic.Width; x++)
        {
            for (int z = 0; z < schematic.Length; z++)
            {
                bool insideDirt = x >= dirtStartX && x <= dirtEndX && z >= dirtStartZ && z <= dirtEndZ;
                if (insideDirt)
                {
                    Assert.Equal(Minecraft.Blocks.Dirt, schematic[x, 2, z].Identifier);
                }
                else
                {
                    Assert.NotEqual(Minecraft.Blocks.Dirt, schematic[x, 2, z].Identifier);
                }
            }
        }

        // Front wall is shifted cleanly to frontPadding, leaving frontPadding - 2 blocks free in front of triggers.
        Assert.Equal(Minecraft.Blocks.WaxedCopperBlock, schematic[0, 2, wallStartZ].Identifier);
        Assert.Equal(Minecraft.Blocks.WaxedCopperBlock, schematic[0, 2, wallEndZ].Identifier);
        int centerX = 1 + (dirtWidth / 2);
        int centerZ = dirtStartZ + (dirtLength / 2);
        Assert.Equal(Minecraft.Blocks.Chest, schematic[centerX, 7, wallStartZ - 1].Identifier);
        Assert.Equal(Minecraft.Blocks.Hopper, schematic[centerX, 7, wallStartZ].Identifier);
        Assert.Equal(Minecraft.Blocks.WaxedCopperBlock, schematic[centerX, 8, wallStartZ].Identifier);
        Assert.Equal(Minecraft.Blocks.NoteBlock, schematic[centerX, 8, centerZ].Identifier);
        Assert.Equal(Minecraft.Blocks.Air, schematic[centerX, 9, centerZ].Identifier);

        // Harvest clock below dirt is never overwritten by soil at y = 2.
        Assert.Equal(Minecraft.Blocks.SmoothStone, schematic[centerX, 0, centerZ].Identifier);
        Assert.Equal(Minecraft.Blocks.Hopper, schematic[centerX, 1, centerZ - 1].Identifier);
    }
}
