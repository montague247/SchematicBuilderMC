using SchematicBuilderMC.Enums;
using SchematicBuilderMC.IO;
using SchematicBuilderMC.Registries;
using SchematicBuilderMC.Schematics.Farms;

namespace SchematicBuilderMC.Core.Tests.Schematics.Farms;

public sealed class KelpFarmWithAllaysSchematicCreatorTests
{
    [Fact]
    public void Build_DefaultConfiguration_CreatesUnderwaterKelpFarmWithoutLights()
    {
        var kelpFarm = new KelpFarmWithAllaysSchematicCreator();

        Assert.Equal(Minecraft.Blocks.Sand, kelpFarm.SoilBlock);
        Assert.Null(kelpFarm.LightBlock);

        var schematic = kelpFarm.Build();

        Assert.Equal(35, schematic.Width);
        Assert.Equal(11, schematic.Height);
        Assert.Equal(37, schematic.Length);
        Assert.Equal("Kelp Farm with Allays", schematic.Metadata.Name);

        // Exact sand bed (33x32) and initial kelp crop rows (leaving z=2 and z=33 for flying machine stations).
        for (int x = 1; x <= 33; x++)
        {
            for (int z = 2; z <= 33; z++)
            {
                Assert.Equal(Minecraft.Blocks.Sand, schematic[x, 2, z].Identifier);
                if (z is >= 3 and <= 32)
                {
                    Assert.Equal(Minecraft.Blocks.Kelp, schematic[x, 3, z].Identifier);
                }
            }
        }

        // Water source blocks fill interior growth volume (y=3..6) where kelp/machine blocks aren't placed,
        // and collection level (y=7..8) remains dry.
        Assert.Equal(Minecraft.Blocks.Water, schematic[1, 3, 2].Identifier);
        Assert.Equal("0", schematic[1, 3, 2].Properties["level"]);
        Assert.Equal(Minecraft.Blocks.Water, schematic[10, 4, 15].Identifier);
        Assert.Equal(Minecraft.Blocks.Water, schematic[10, 5, 15].Identifier);
        Assert.Equal(Minecraft.Blocks.Water, schematic[10, 6, 15].Identifier);
        Assert.Equal("0", schematic[10, 6, 15].Properties["level"]);
        Assert.Equal(Minecraft.Blocks.Air, schematic[10, 7, 15].Identifier);

        // Flying machine blocks stay intact inside the submerged tank.
        Assert.Equal(Minecraft.Blocks.SlimeBlock, schematic[2, 4, 3].Identifier);
        Assert.Equal(Minecraft.Blocks.Observer, schematic[8, 5, 3].Identifier);
        Assert.Equal(Minecraft.Blocks.BambooTrapdoor, schematic[9, 6, 2].Identifier);

        // No light sources placed by default anywhere in the schematic.
        for (int x = 0; x < schematic.Width; x++)
        {
            for (int y = 0; y < schematic.Height; y++)
            {
                for (int z = 0; z < schematic.Length; z++)
                {
                    Assert.NotEqual(Minecraft.Blocks.VerdantFroglight, schematic[x, y, z].Identifier);
                }
            }
        }

        // Collection ring, 1-wide trigger wiring, flush note block clock, and Allays above water (y = 7.0).
        Assert.Equal(Minecraft.Blocks.Chest, schematic[17, 7, 0].Identifier);
        Assert.Equal(Minecraft.Blocks.Hopper, schematic[17, 7, 19].Identifier);
        Assert.Equal("west", schematic[17, 7, 19].Properties["facing"]);
        Assert.Equal(Minecraft.Blocks.NoteBlock, schematic[17, 8, 18].Identifier);
        Assert.Equal(Minecraft.Blocks.RedstoneWallTorch, schematic[17, 8, 36].Identifier);

        Assert.Equal(4, schematic.Entities.Count);
        Assert.All(schematic.Entities, allay =>
        {
            Assert.Equal(Minecraft.Entities.Allay, allay.Id);
            Assert.Equal(7.0, allay.Y);
        });

        string spongePath = Path.Combine(SchematicCreatorTests.TestOutputDirectory, $"{schematic.Metadata.Name}.schem");
        SchematicFile.Write(schematic, spongePath, SchematicFormat.SpongeV2);
        var roundTripped = SchematicFile.Read(spongePath);
        Assert.Equal(Minecraft.Blocks.Sand, roundTripped[10, 2, 10].Identifier);
        Assert.Equal(Minecraft.Blocks.Kelp, roundTripped[10, 3, 10].Identifier);
        Assert.Equal(Minecraft.Blocks.Water, roundTripped[10, 5, 10].Identifier);
        Assert.Equal(4, roundTripped.Entities.Count);
    }

    [Fact]
    public void Build_CustomOptions_SupportsWaterDepthOptionalLightAndNoRoof()
    {
        var kelpFarm = new KelpFarmWithAllaysSchematicCreator
        {
            Width = 10,
            Length = 12,
            MaxKelpHeight = 3,
            SoilBlock = Minecraft.Blocks.Dirt,
            LightBlock = Minecraft.Blocks.VerdantFroglight,
            RoofBlock = null,
            RotateNoteBlockClock = true
        };

        var schematic = kelpFarm.Build();

        Assert.Equal(12, schematic.Width);
        Assert.Equal(10, schematic.Height);
        Assert.Equal(23, schematic.Length); // frontPadding(1) + startWall(1) + length(12) + endWall(1) + rotatedBackPadding(8)

        Assert.Equal(Minecraft.Blocks.Dirt, schematic[1, 2, 2].Identifier);
        Assert.Equal(Minecraft.Blocks.Water, schematic[2, 5, 6].Identifier);
        Assert.Equal(Minecraft.Blocks.Air, schematic[2, 6, 6].Identifier); // Water capped at MaxKelpHeight = 3 (y = 3..5)
        Assert.Equal(Minecraft.Blocks.Air, schematic[2, 9, 6].Identifier); // No roof (TotalHeight = 10 -> y = 0..9)
        Assert.All(schematic.Entities, allay => Assert.Equal(6.0, allay.Y));

        bool hasFroglight = false;
        for (int x = 1; x <= 10; x++)
        {
            for (int z = 2; z <= 13; z++)
            {
                if (schematic[x, 7, z].Identifier == Minecraft.Blocks.VerdantFroglight)
                {
                    hasFroglight = true;
                }
            }
        }

        Assert.True(hasFroglight);
    }

    [Fact]
    public void Build_MaxKelpHeightAboveFour_CapsWaterBelowCollectionHoppers()
    {
        var kelpFarm = new KelpFarmWithAllaysSchematicCreator
        {
            Width = 12,
            Length = 12,
            MaxKelpHeight = 8
        };

        var schematic = kelpFarm.Build();

        // WallHeight scales to MaxKelpHeight + roof, while water stays strictly below HopperY (y = 7).
        Assert.Equal(12, schematic.Height); // SoilLevel(2) + WallHeight(8) + 1 + Roof(1)
        Assert.Equal(Minecraft.Blocks.Water, schematic[2, 6, 6].Identifier);
        Assert.Equal(Minecraft.Blocks.Air, schematic[2, 7, 6].Identifier);
        Assert.Equal(Minecraft.Blocks.WaxedCopperBlock, schematic[7, 7, 8].Identifier);
        Assert.Equal(Minecraft.Blocks.Hopper, schematic[7, 7, 9].Identifier);
    }
}
