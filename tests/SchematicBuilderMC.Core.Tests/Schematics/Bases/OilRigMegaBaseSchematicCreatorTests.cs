using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;
using SchematicBuilderMC.IO;
using SchematicBuilderMC.Models;
using SchematicBuilderMC.Registries;
using SchematicBuilderMC.Schematics.Bases;

namespace SchematicBuilderMC.Core.Tests.Schematics.Bases;

public sealed class OilRigMegaBaseSchematicCreatorTests
{
    [Fact]
    public void Build_DefaultConfiguration_CreatesFourOuterColumnsCenterColumnPlatformAndStairs_AndWritesToDisk()
    {
        var creator = new OilRigMegaBaseSchematicCreator();
        var schematic = creator.Build();

        Assert.Equal("Oil Rig Mega Base", schematic.Metadata.Name);
        Assert.Equal(43, schematic.Width);
        Assert.Equal(22, schematic.Height);
        Assert.Equal(43, schematic.Length);
        Assert.True(schematic.CountNonAirBlocks() > 0);

        var outerCenters = creator.GetOuterColumnBasePositions();
        var centerBase = creator.GetCenterColumnBasePosition();
        Assert.Equal(4, outerCenters.Count);
        Assert.Equal(new BlockPosition(21, 0, 21), centerBase);

        // Column walls exist at expected radii and centers are hollow
        Assert.Equal(BlockState.Air, schematic[centerBase]);
        foreach (var outer in outerCenters)
        {
            Assert.Equal(BlockState.Air, schematic[outer]);
            Assert.False(schematic[outer.X + creator.OuterColumnRadius, 0, outer.Z].IsAir);
        }
        Assert.False(schematic[centerBase.X + creator.InnerColumnRadius, 0, centerBase.Z].IsAir);

        // Usage platform connects center and outer columns and has hazard border + guardrails
        int topDeckY = creator.PlatformHeight + creator.PlatformThickness - 1;
        int deckBridgeZ = outerCenters[0].Z + 3;
        Assert.True(schematic[centerBase.X, creator.PlatformHeight - 1, deckBridgeZ].IsAir);
        Assert.Equal(creator.PlatformBlock, schematic[centerBase.X, topDeckY, deckBridgeZ].Identifier);
        Assert.Equal(creator.PlatformBorderBlock, schematic[0, topDeckY, 0].Identifier);
        Assert.Equal(creator.GuardrailBlock, schematic[0, topDeckY + 1, 0].Identifier);

        // Inner stairs start at ground y = 0 inside all 5 columns (5 stairs per layer from 0..topDeckY)
        // Outer stairs start at OuterStairsStartHeight = 6 around the 4 outer columns (4 stairs per layer from 6..PlatformHeight-1)
        for (int y = 0; y <= topDeckY; y++)
        {
            int stairsInLayer = CountBlocksInLayer(schematic, y, creator.InnerStairBlock);
            int expectedStairs = y < creator.OuterStairsStartHeight || y >= creator.PlatformHeight ? 5 : 9;
            Assert.Equal(expectedStairs, stairsInLayer);
        }

        // Write to file and round-trip verify
        string spongePath = Path.Combine(SchematicCreatorTests.TestOutputDirectory, $"{schematic.Metadata.Name}.schem");
        SchematicFile.Write(schematic, spongePath, SchematicFormat.SpongeV2);

        Assert.True(File.Exists(spongePath));
        var loaded = SchematicFile.Read(spongePath, SchematicFormat.SpongeV2);
        Assert.Equal(schematic.Width, loaded.Width);
        Assert.Equal(schematic.Height, loaded.Height);
        Assert.Equal(schematic.Length, loaded.Length);
        Assert.Equal(schematic[0, topDeckY, 0].Identifier, loaded[0, topDeckY, 0].Identifier);
    }

    [Fact]
    public void Build_CustomRadiiHeightsThicknessAndStairStartHeights_AppliesAllConfigurations()
    {
        var creator = new OilRigMegaBaseSchematicCreator
        {
            OuterColumnRadius = 4,
            InnerColumnRadius = 6,
            ColumnSpacing = 22,
            PlatformHeight = 12,
            PlatformThickness = 4,
            PlatformOverhang = 1,
            OuterStairsStartHeight = 4,
            InnerStairsStartHeight = 0,
            IncludeGuardrails = false,
            ColumnGradientBlocks = [Minecraft.Blocks.BlackConcrete, Minecraft.Blocks.GrayConcrete],
            ColumnGradientTransition = GradientTransition.Strict,
            ColumnGradientWeights = [],
            PlatformBlock = Minecraft.Blocks.SmoothSandstone,
            PlatformBorderBlock = Minecraft.Blocks.RedConcrete,
            OuterStairBlock = Minecraft.Blocks.BlackstoneStairs,
            InnerStairBlock = Minecraft.Blocks.OakStairs
        };

        var schematic = creator.Build();
        Assert.Equal(16, schematic.Height);

        var outerCenters = creator.GetOuterColumnBasePositions();
        var centerBase = creator.GetCenterColumnBasePosition();
        int bridgeZ = outerCenters[0].Z + 3;

        // Platform exists across y = 12..15 (PlatformThickness = 4) and not below y = 12 outside beams/columns
        Assert.True(schematic[centerBase.X, 11, bridgeZ].IsAir);
        for (int y = 12; y <= 15; y++)
        {
            Assert.Equal(Minecraft.Blocks.SmoothSandstone, schematic[centerBase.X, y, bridgeZ].Identifier);
        }
        Assert.Equal(Minecraft.Blocks.RedConcrete, schematic[0, 15, 0].Identifier);

        // Outer stairs (BlackstoneStairs) only appear from y = 4..11 (4 per level); none below y = 4
        for (int y = 0; y < 4; y++)
        {
            Assert.Equal(0, CountBlocksInLayer(schematic, y, Minecraft.Blocks.BlackstoneStairs));
        }
        for (int y = 4; y < 12; y++)
        {
            Assert.Equal(4, CountBlocksInLayer(schematic, y, Minecraft.Blocks.BlackstoneStairs));
        }

        // Inner stairs (OakStairs) run from ground y = 0..15 across all 5 columns (5 per level)
        for (int y = 0; y < 16; y++)
        {
            Assert.Equal(5, CountBlocksInLayer(schematic, y, Minecraft.Blocks.OakStairs));
        }

        // Placing into a smaller builder automatically extends its dimensions
        var expanded = SchematicBuilder.Create(1, 1, 1)
            .Create(creator, startX: 2, startY: 1, startZ: 2)
            .Build();
        Assert.Equal(creator.Width + 2, expanded.Width);
        Assert.Equal(creator.Height + 1, expanded.Height);
        Assert.Equal(creator.Length + 2, expanded.Length);
    }

    [Fact]
    public void ValidateConfiguration_InvalidParameters_ThrowArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new OilRigMegaBaseSchematicCreator { OuterColumnRadius = 1 }.Create());

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new OilRigMegaBaseSchematicCreator { InnerColumnRadius = 1 }.Create());

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new OilRigMegaBaseSchematicCreator { OuterColumnRadius = 5, ColumnSpacing = 11 }.Create());

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new OilRigMegaBaseSchematicCreator { PlatformHeight = 1 }.Create());

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new OilRigMegaBaseSchematicCreator { PlatformThickness = 0 }.Create());

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new OilRigMegaBaseSchematicCreator { PlatformOverhang = -1 }.Create());

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new OilRigMegaBaseSchematicCreator { OuterStairsStartHeight = -1 }.Create());

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new OilRigMegaBaseSchematicCreator { PlatformHeight = 10, OuterStairsStartHeight = 10 }.Create());

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new OilRigMegaBaseSchematicCreator { InnerStairsStartHeight = -1 }.Create());

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new OilRigMegaBaseSchematicCreator { PlatformHeight = 10, PlatformThickness = 2, InnerStairsStartHeight = 12 }.Create());
    }

    private static int CountBlocksInLayer(Schematic schematic, int y, string blockIdentifier)
    {
        int count = 0;
        for (int z = 0; z < schematic.Length; z++)
        {
            for (int x = 0; x < schematic.Width; x++)
            {
                if (string.Equals(schematic[x, y, z].Identifier, blockIdentifier, StringComparison.Ordinal))
                {
                    count++;
                }
            }
        }
        return count;
    }
}
