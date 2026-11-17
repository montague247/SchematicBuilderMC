using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;
using SchematicBuilderMC.IO;
using SchematicBuilderMC.Models;
using SchematicBuilderMC.Registries;
using SchematicBuilderMC.Schematics;
using SchematicBuilderMC.Schematics.Shapes;

namespace SchematicBuilderMC.Core.Tests.Schematics.Shapes;

public sealed class GeometricShapesTests
{
    [Fact]
    public void SetCircle_FilledAndHollow_PlacesExpectedBlocksAcrossAxes()
    {
        var filled = SchematicBuilder.Create(11, 1, 11)
            .SetCircle(5, 0, 5, 4, Minecraft.Blocks.StoneBricks, filled: true, normalAxis: Axis.Y)
            .Build();

        var hollow = SchematicBuilder.Create(11, 1, 11)
            .SetCircle(new BlockPosition(5, 0, 5), 4, Minecraft.Blocks.StoneBricks, filled: false, normalAxis: Axis.Y)
            .Build();

        Assert.Equal(Minecraft.Blocks.StoneBricks, filled[5, 0, 5].Identifier);
        Assert.Equal(Minecraft.Blocks.StoneBricks, filled[9, 0, 5].Identifier);
        Assert.Equal(BlockState.Air, hollow[5, 0, 5]);
        Assert.Equal(Minecraft.Blocks.StoneBricks, hollow[9, 0, 5].Identifier);
        Assert.True(hollow.CountNonAirBlocks() < filled.CountNonAirBlocks());

        var vertical = SchematicBuilder.Create(1, 9, 9)
            .SetCircle(0, 4, 4, 3, Minecraft.Blocks.Glass, filled: false, normalAxis: Axis.X)
            .Build();

        Assert.Equal(BlockState.Air, vertical[0, 4, 4]);
        Assert.Equal(Minecraft.Blocks.Glass, vertical[0, 7, 4].Identifier);
        Assert.Equal(Minecraft.Blocks.Glass, vertical[0, 4, 7].Identifier);
    }

    [Fact]
    public void SetSphere_FilledAndHollow_PlacesExpectedShellAndInterior()
    {
        var filled = SchematicBuilder.Create(11, 11, 11)
            .SetSphere(5, 5, 5, 4, Minecraft.Blocks.Glass, filled: true)
            .Build();

        var hollow = SchematicBuilder.Create(11, 11, 11)
            .SetSphere(new BlockPosition(5, 5, 5), 4, Minecraft.Blocks.Glass, filled: false)
            .Build();

        Assert.Equal(Minecraft.Blocks.Glass, filled[5, 5, 5].Identifier);
        Assert.Equal(BlockState.Air, hollow[5, 5, 5]);
        Assert.Equal(Minecraft.Blocks.Glass, hollow[9, 5, 5].Identifier);
        Assert.Equal(Minecraft.Blocks.Glass, hollow[5, 9, 5].Identifier);
        Assert.Equal(Minecraft.Blocks.Glass, hollow[5, 5, 9].Identifier);
        Assert.True(hollow.CountNonAirBlocks() < filled.CountNonAirBlocks());
    }

    [Fact]
    public void SetRectangle_FilledAndHollow_PlacesExactCountsAndAxes()
    {
        var filled = SchematicBuilder.Create(8, 1, 6)
            .SetRectangle(0, 0, 0, 8, 6, Minecraft.Blocks.OakPlanks, filled: true, normalAxis: Axis.Y)
            .Build();

        var hollow = SchematicBuilder.Create(8, 1, 6)
            .SetRectangle(new BlockPosition(0, 0, 0), 8, 6, Minecraft.Blocks.OakPlanks, filled: false, normalAxis: Axis.Y)
            .Build();

        Assert.Equal(48, filled.CountNonAirBlocks());
        Assert.Equal(24, hollow.CountNonAirBlocks());
        Assert.Equal(BlockState.Air, hollow[3, 0, 3]);
        Assert.Equal(Minecraft.Blocks.OakPlanks, hollow[0, 0, 0].Identifier);
        Assert.Equal(Minecraft.Blocks.OakPlanks, hollow[7, 0, 5].Identifier);

        var wall = SchematicBuilder.Create(5, 4, 1)
            .SetRectangle(0, 0, 0, 5, 4, Minecraft.Blocks.Stone, filled: false, normalAxis: Axis.Z)
            .Build();

        Assert.Equal(14, wall.CountNonAirBlocks());
        Assert.Equal(BlockState.Air, wall[2, 2, 0]);
        Assert.Equal(Minecraft.Blocks.Stone, wall[4, 3, 0].Identifier);
    }

    [Fact]
    public void SetCube_FilledAndHollow_PlacesExactBlockCounts()
    {
        var filled = SchematicBuilder.Create(5, 5, 5)
            .SetCube(0, 0, 0, 5, Minecraft.Blocks.StoneBricks, filled: true)
            .Build();

        var hollow = SchematicBuilder.Create(5, 5, 5)
            .SetCube(new BlockPosition(0, 0, 0), 5, Minecraft.Blocks.StoneBricks, filled: false)
            .Build();

        Assert.Equal(125, filled.CountNonAirBlocks());
        Assert.Equal(125 - 27, hollow.CountNonAirBlocks());
        Assert.Equal(BlockState.Air, hollow[2, 2, 2]);
        Assert.Equal(Minecraft.Blocks.StoneBricks, hollow[0, 0, 0].Identifier);
        Assert.Equal(Minecraft.Blocks.StoneBricks, hollow[4, 4, 4].Identifier);
    }

    [Fact]
    public void SetOval_FilledAndHollow_RespectsSemiAxes()
    {
        var filled = SchematicBuilder.Create(13, 1, 9)
            .SetOval(6, 0, 4, 6, 4, Minecraft.Blocks.Stone, filled: true, normalAxis: Axis.Y)
            .Build();

        var hollow = SchematicBuilder.Create(13, 1, 9)
            .SetOval(new BlockPosition(6, 0, 4), 6, 4, Minecraft.Blocks.Stone, filled: false, normalAxis: Axis.Y)
            .Build();

        Assert.Equal(Minecraft.Blocks.Stone, filled[6, 0, 4].Identifier);
        Assert.Equal(BlockState.Air, hollow[6, 0, 4]);
        Assert.Equal(Minecraft.Blocks.Stone, hollow[12, 0, 4].Identifier);
        Assert.Equal(Minecraft.Blocks.Stone, hollow[0, 0, 4].Identifier);
        Assert.Equal(Minecraft.Blocks.Stone, hollow[6, 0, 8].Identifier);
        Assert.Equal(Minecraft.Blocks.Stone, hollow[6, 0, 0].Identifier);
        Assert.True(hollow.CountNonAirBlocks() < filled.CountNonAirBlocks());
    }

    [Fact]
    public void SetCylinder_FilledAndHollow_ExtendsAlongSelectedAxis()
    {
        var filled = SchematicBuilder.Create(9, 6, 9)
            .SetCylinder(4, 0, 4, 3, 6, Minecraft.Blocks.StoneBricks, filled: true, axis: Axis.Y)
            .Build();

        var hollow = SchematicBuilder.Create(9, 6, 9)
            .SetCylinder(new BlockPosition(4, 0, 4), 3, 6, Minecraft.Blocks.StoneBricks, filled: false, axis: Axis.Y)
            .Build();

        for (int y = 0; y < 6; y++)
        {
            Assert.Equal(Minecraft.Blocks.StoneBricks, filled[4, y, 4].Identifier);
            Assert.Equal(BlockState.Air, hollow[4, y, 4]);
            Assert.Equal(Minecraft.Blocks.StoneBricks, hollow[7, y, 4].Identifier);
        }

        Assert.True(hollow.CountNonAirBlocks() < filled.CountNonAirBlocks());

        var horizontalX = SchematicBuilder.Create(5, 7, 7)
            .SetCylinder(0, 3, 3, 2, 5, Minecraft.Blocks.QuartzBlock, filled: false, axis: Axis.X)
            .Build();

        for (int x = 0; x < 5; x++)
        {
            Assert.Equal(BlockState.Air, horizontalX[x, 3, 3]);
            Assert.Equal(Minecraft.Blocks.QuartzBlock, horizontalX[x, 5, 3].Identifier);
        }
    }

    [Fact]
    public void SetTorus_FilledAndHollow_FormsRingWithHollowCenterHole()
    {
        var filled = SchematicBuilder.Create(17, 5, 17)
            .SetTorus(8, 2, 8, majorRadius: 6, minorRadius: 2, Minecraft.Blocks.GoldBlock, filled: true, axis: Axis.Y)
            .Build();

        var hollow = SchematicBuilder.Create(17, 5, 17)
            .SetTorus(new BlockPosition(8, 2, 8), majorRadius: 6, minorRadius: 2, Minecraft.Blocks.GoldBlock, filled: false, axis: Axis.Y)
            .Build();

        // Center hole is empty in both filled and hollow torus
        Assert.Equal(BlockState.Air, filled[8, 2, 8]);
        Assert.Equal(BlockState.Air, hollow[8, 2, 8]);

        // Tube centerline at (8 + 6, 2, 8) = (14, 2, 8) is filled in solid torus and air in hollow torus
        Assert.Equal(Minecraft.Blocks.GoldBlock, filled[14, 2, 8].Identifier);
        Assert.Equal(BlockState.Air, hollow[14, 2, 8]);

        // Outer skin of the tube at (16, 2, 8) is solid in both
        Assert.Equal(Minecraft.Blocks.GoldBlock, hollow[16, 2, 8].Identifier);
        Assert.True(hollow.CountNonAirBlocks() < filled.CountNonAirBlocks());
    }

    [Fact]
    public void SetPyramid_FilledAndHollow_BuildsSteppedLayersAndApex()
    {
        var filled = SchematicBuilder.Create(9, 5, 9)
            .SetPyramid(0, 0, 0, 9, Minecraft.Blocks.Sandstone, filled: true)
            .Build();

        var hollow = SchematicBuilder.Create(9, 5, 9)
            .SetPyramid(new BlockPosition(0, 0, 0), 9, Minecraft.Blocks.Sandstone, filled: false)
            .Build();

        // Filled: 9^2 + 7^2 + 5^2 + 3^2 + 1^2 = 165
        Assert.Equal(165, filled.CountNonAirBlocks());
        // Hollow: 32 + 24 + 16 + 8 + 1 = 81
        Assert.Equal(81, hollow.CountNonAirBlocks());

        Assert.Equal(Minecraft.Blocks.Sandstone, filled[4, 1, 4].Identifier);
        Assert.Equal(BlockState.Air, hollow[4, 1, 4]);
        Assert.Equal(Minecraft.Blocks.Sandstone, hollow[4, 4, 4].Identifier);
    }

    [Fact]
    public void GeometricShapes_InvalidArgumentsOrOutOfBounds_ThrowArgumentOutOfRangeException()
    {
        var schematic = new Schematic(5, 5, 5);

        Assert.Throws<ArgumentOutOfRangeException>(() => schematic.SetCircle(2, 0, 2, -1, Minecraft.Blocks.Stone));
        Assert.Throws<ArgumentOutOfRangeException>(() => schematic.SetCircle(2, 0, 2, 3, Minecraft.Blocks.Stone));
        Assert.Throws<ArgumentOutOfRangeException>(() => schematic.SetSphere(2, 2, 2, -1, Minecraft.Blocks.Stone));
        Assert.Throws<ArgumentOutOfRangeException>(() => schematic.SetSphere(2, 2, 2, 3, Minecraft.Blocks.Stone));
        Assert.Throws<ArgumentOutOfRangeException>(() => schematic.SetRectangle(0, 0, 0, 0, 3, Minecraft.Blocks.Stone));
        Assert.Throws<ArgumentOutOfRangeException>(() => schematic.SetRectangle(0, 0, 0, 6, 3, Minecraft.Blocks.Stone));
        Assert.Throws<ArgumentOutOfRangeException>(() => schematic.SetCube(0, 0, 0, 0, Minecraft.Blocks.Stone));
        Assert.Throws<ArgumentOutOfRangeException>(() => schematic.SetCube(0, 0, 0, 6, Minecraft.Blocks.Stone));
        Assert.Throws<ArgumentOutOfRangeException>(() => schematic.SetOval(2, 0, 2, -1, 1, Minecraft.Blocks.Stone));
        Assert.Throws<ArgumentOutOfRangeException>(() => schematic.SetOval(2, 0, 2, 3, 1, Minecraft.Blocks.Stone));
        Assert.Throws<ArgumentOutOfRangeException>(() => schematic.SetCylinder(2, 0, 2, 1, 0, Minecraft.Blocks.Stone));
        Assert.Throws<ArgumentOutOfRangeException>(() => schematic.SetCylinder(2, 0, 2, 3, 3, Minecraft.Blocks.Stone));
        Assert.Throws<ArgumentOutOfRangeException>(() => schematic.SetTorus(2, 2, 2, 0, 0, Minecraft.Blocks.Stone));
        Assert.Throws<ArgumentOutOfRangeException>(() => schematic.SetTorus(2, 2, 2, 2, 2, Minecraft.Blocks.Stone));
        Assert.Throws<ArgumentOutOfRangeException>(() => schematic.SetPyramid(0, 0, 0, 0, Minecraft.Blocks.Stone));
        Assert.Throws<ArgumentOutOfRangeException>(() => schematic.SetPyramid(0, 0, 0, 6, Minecraft.Blocks.Stone));
    }

    [Fact]
    public void ShapeCreators_BuildAndWriteToOutputDirectory_SucceedAndSupportRotation()
    {
        var creators = new SchematicCreator[]
        {
            new CircleSchematicCreator(),
            new SphereSchematicCreator(),
            new RectangleSchematicCreator(),
            new CubeSchematicCreator(),
            new OvalSchematicCreator(),
            new CylinderSchematicCreator(),
            new TorusSchematicCreator(),
            new PyramidSchematicCreator()
        };

        foreach (var creator in creators)
        {
            var schematic = creator.Build();
            Assert.True(schematic.CountNonAirBlocks() > 0);

            string spongePath = Path.Combine(SchematicCreatorTests.TestOutputDirectory, $"{schematic.Metadata.Name}.schem");
            SchematicFile.Write(schematic, spongePath, SchematicFormat.SpongeV2);

            Assert.True(File.Exists(spongePath));
            var loaded = SchematicFile.Read(spongePath, SchematicFormat.SpongeV2);
            Assert.Equal(schematic.Width, loaded.Width);
            Assert.Equal(schematic.Height, loaded.Height);
            Assert.Equal(schematic.Length, loaded.Length);
        }

        // Verify Rotation before Create() swaps Width and Length on asymmetric shapes
        var rotatedRect = new RectangleSchematicCreator { Width = 8, Length = 6, Rotation = Rotation.Clockwise90 }.Build();
        Assert.Equal(6, rotatedRect.Width);
        Assert.Equal(1, rotatedRect.Height);
        Assert.Equal(8, rotatedRect.Length);

        var rotatedOval = new OvalSchematicCreator { RadiusU = 6, RadiusV = 4, Rotation = Rotation.Clockwise90 }.Build();
        Assert.Equal(9, rotatedOval.Width);
        Assert.Equal(1, rotatedOval.Height);
        Assert.Equal(13, rotatedOval.Length);
    }

    [Fact]
    public void CurvedShapes_SmoothnessFactor_ProducesMoreRoundedProfilesThanLegacyPadding()
    {
        var roundedCircle = SchematicBuilder.Create(11, 1, 11)
            .SetCircle(5, 0, 5, 5, Minecraft.Blocks.StoneBricks, filled: true, normalAxis: Axis.Y, smoothnessFactor: 1.0)
            .Build();
        var legacyCircle = SchematicBuilder.Create(11, 1, 11)
            .SetCircle(5, 0, 5, 5, Minecraft.Blocks.StoneBricks, filled: true, normalAxis: Axis.Y, smoothnessFactor: 0.0)
            .Build();

        int roundedNorthEdgeCount = Enumerable.Range(0, 11).Count(x => !roundedCircle[x, 0, 0].IsAir);
        int legacyNorthEdgeCount = Enumerable.Range(0, 11).Count(x => !legacyCircle[x, 0, 0].IsAir);
        Assert.Equal(3, roundedNorthEdgeCount);
        Assert.Equal(5, legacyNorthEdgeCount);
        Assert.True(roundedCircle.CountNonAirBlocks() < legacyCircle.CountNonAirBlocks());

        var roundedSphere = SchematicBuilder.Create(11, 11, 11)
            .SetSphere(5, 5, 5, 5, Minecraft.Blocks.Glass, filled: true, smoothnessFactor: 1.0)
            .Build();
        var legacySphere = SchematicBuilder.Create(11, 11, 11)
            .SetSphere(5, 5, 5, 5, Minecraft.Blocks.Glass, filled: true, smoothnessFactor: 0.0)
            .Build();
        Assert.True(roundedSphere.CountNonAirBlocks() < legacySphere.CountNonAirBlocks());

        var roundedOval = SchematicBuilder.Create(15, 1, 11)
            .SetOval(7, 0, 5, 7, 5, Minecraft.Blocks.StoneBricks, filled: true, normalAxis: Axis.Y, smoothnessFactor: 1.0)
            .Build();
        var legacyOval = SchematicBuilder.Create(15, 1, 11)
            .SetOval(7, 0, 5, 7, 5, Minecraft.Blocks.StoneBricks, filled: true, normalAxis: Axis.Y, smoothnessFactor: 0.0)
            .Build();
        Assert.True(roundedOval.CountNonAirBlocks() < legacyOval.CountNonAirBlocks());

        var roundedCylinder = SchematicBuilder.Create(11, 4, 11)
            .SetCylinder(5, 0, 5, 5, 4, Minecraft.Blocks.StoneBricks, filled: true, axis: Axis.Y, smoothnessFactor: 1.0)
            .Build();
        var legacyCylinder = SchematicBuilder.Create(11, 4, 11)
            .SetCylinder(5, 0, 5, 5, 4, Minecraft.Blocks.StoneBricks, filled: true, axis: Axis.Y, smoothnessFactor: 0.0)
            .Build();
        Assert.True(roundedCylinder.CountNonAirBlocks() < legacyCylinder.CountNonAirBlocks());

        var roundedTorus = SchematicBuilder.Create(21, 9, 21)
            .SetTorus(10, 4, 10, 6, 4, Minecraft.Blocks.GoldBlock, filled: true, axis: Axis.Y, smoothnessFactor: 1.0)
            .Build();
        var legacyTorus = SchematicBuilder.Create(21, 9, 21)
            .SetTorus(10, 4, 10, 6, 4, Minecraft.Blocks.GoldBlock, filled: true, axis: Axis.Y, smoothnessFactor: 0.0)
            .Build();
        Assert.True(roundedTorus.CountNonAirBlocks() < legacyTorus.CountNonAirBlocks());

        var schematic = new Schematic(11, 11, 11);
        Assert.Throws<ArgumentOutOfRangeException>(() => schematic.SetCircle(5, 0, 5, 4, Minecraft.Blocks.Stone, smoothnessFactor: -0.1));
        Assert.Throws<ArgumentOutOfRangeException>(() => schematic.SetSphere(5, 5, 5, 4, Minecraft.Blocks.Stone, smoothnessFactor: double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => schematic.SetTorus(5, 5, 5, 3, 1, Minecraft.Blocks.Stone, smoothnessFactor: double.PositiveInfinity));
    }

    [Fact]
    public void ShapeCreators_SmoothnessFactor_ScalesDimensionsAndValidatesInputs()
    {
        var circle = new CircleSchematicCreator { Radius = 4 }.WithSmoothnessFactor(2.0).Build();
        Assert.Equal(17, circle.Width);
        Assert.Equal(1, circle.Height);
        Assert.Equal(17, circle.Length);

        var sphere = new SphereSchematicCreator { Radius = 4 }.WithSmoothnessFactor(2.0).Build();
        Assert.Equal(17, sphere.Width);
        Assert.Equal(17, sphere.Height);
        Assert.Equal(17, sphere.Length);

        var oval = new OvalSchematicCreator { RadiusU = 5, RadiusV = 3 }.WithSmoothnessFactor(2.0).Build();
        Assert.Equal(21, oval.Width);
        Assert.Equal(1, oval.Height);
        Assert.Equal(13, oval.Length);

        var rectangle = new RectangleSchematicCreator { Width = 4, Length = 3 }.WithSmoothnessFactor(2.0).Build();
        Assert.Equal(8, rectangle.Width);
        Assert.Equal(1, rectangle.Height);
        Assert.Equal(6, rectangle.Length);
        Assert.Equal(48, rectangle.CountNonAirBlocks());

        var cube = new CubeSchematicCreator { Size = 3 }.WithSmoothnessFactor(2.0).Build();
        Assert.Equal(6, cube.Width);
        Assert.Equal(6, cube.Height);
        Assert.Equal(6, cube.Length);
        Assert.Equal(216, cube.CountNonAirBlocks());

        var cylinder = new CylinderSchematicCreator { Radius = 3, Height = 5 }.WithSmoothnessFactor(2.0).Build();
        Assert.Equal(13, cylinder.Width);
        Assert.Equal(5, cylinder.Height);
        Assert.Equal(13, cylinder.Length);

        var torus = new TorusSchematicCreator { MajorRadius = 5, MinorRadius = 2 }.WithSmoothnessFactor(2.0).Build();
        Assert.Equal(29, torus.Width);
        Assert.Equal(9, torus.Height);
        Assert.Equal(29, torus.Length);

        var pyramid = new PyramidSchematicCreator { BaseSize = 5 }.WithSmoothnessFactor(2.0).Build();
        Assert.Equal(10, pyramid.Width);
        Assert.Equal(5, pyramid.Height);
        Assert.Equal(10, pyramid.Length);

        Assert.Throws<ArgumentOutOfRangeException>(() => new CircleSchematicCreator().WithSmoothnessFactor(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CircleSchematicCreator().WithSmoothnessFactor(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CircleSchematicCreator().WithSmoothnessFactor(double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CircleSchematicCreator().WithSmoothnessFactor(double.PositiveInfinity));

        var invalidAssigned = new SphereSchematicCreator { SmoothnessFactor = 0 };
        Assert.Throws<ArgumentOutOfRangeException>(() => invalidAssigned.Build());

        var collapsedTorus = new TorusSchematicCreator { MajorRadius = 3, MinorRadius = 2, SmoothnessFactor = 0.3 };
        Assert.Throws<ArgumentOutOfRangeException>(() => collapsedTorus.Build());
    }

    [Fact]
    public void SetPyramid_WithStairs_PlacesOrientedStairsAndCornersAndRespectsFilled()
    {
        var filled = SchematicBuilder.Create(5, 3, 5)
            .SetPyramid(0, 0, 0, 5, Minecraft.Blocks.Sandstone, filled: true, useStairs: true)
            .Build();

        AssertStair(filled[0, 0, 0], Minecraft.Blocks.SandstoneStairs, "south", "outer_left");
        AssertStair(filled[4, 0, 0], Minecraft.Blocks.SandstoneStairs, "south", "outer_right");
        AssertStair(filled[0, 0, 4], Minecraft.Blocks.SandstoneStairs, "north", "outer_right");
        AssertStair(filled[4, 0, 4], Minecraft.Blocks.SandstoneStairs, "north", "outer_left");
        AssertStair(filled[2, 0, 0], Minecraft.Blocks.SandstoneStairs, "south", "straight");
        AssertStair(filled[2, 0, 4], Minecraft.Blocks.SandstoneStairs, "north", "straight");
        AssertStair(filled[0, 0, 2], Minecraft.Blocks.SandstoneStairs, "east", "straight");
        AssertStair(filled[4, 0, 2], Minecraft.Blocks.SandstoneStairs, "west", "straight");

        Assert.Equal(Minecraft.Blocks.Sandstone, filled[2, 0, 2].Identifier);
        Assert.Equal(Minecraft.Blocks.Sandstone, filled[2, 2, 2].Identifier);
        Assert.Equal(35, filled.CountNonAirBlocks());

        var hollow = SchematicBuilder.Create(5, 3, 5)
            .SetPyramid(new BlockPosition(0, 0, 0), 5, Minecraft.Blocks.Sandstone, filled: false, useStairs: true)
            .Build();

        Assert.Equal(BlockState.Air, hollow[2, 0, 2]);
        Assert.Equal(BlockState.Air, hollow[1, 0, 1]);
        AssertStair(hollow[1, 1, 1], Minecraft.Blocks.SandstoneStairs, "south", "outer_left");
        Assert.Equal(Minecraft.Blocks.Sandstone, hollow[2, 2, 2].Identifier);
        Assert.Equal(25, hollow.CountNonAirBlocks());

        var autoResolved = SchematicBuilder.Create(3, 2, 3)
            .SetPyramid(0, 0, 0, 3, Minecraft.Blocks.StoneBricks, filled: true, useStairs: true)
            .Build();
        AssertStair(autoResolved[1, 0, 0], Minecraft.Blocks.StoneBrickStairs, "south", "straight");

        var defaultCreator = new PyramidSchematicCreator();
        Assert.Equal(11, defaultCreator.BaseSize);
        Assert.True(defaultCreator.UseStairs);
        Assert.Equal(Minecraft.Blocks.SandstoneStairs, defaultCreator.StairBlock);

        var defaultPyramid = defaultCreator.Build();
        Assert.Equal(11, defaultPyramid.Width);
        Assert.Equal(6, defaultPyramid.Height);
        Assert.Equal(11, defaultPyramid.Length);
        AssertStair(defaultPyramid[0, 0, 0], Minecraft.Blocks.SandstoneStairs, "south", "outer_left");
        Assert.Equal(Minecraft.Blocks.Sandstone, defaultPyramid[5, 5, 5].Identifier);
    }

    [Fact]
    public void ShapeGradient_StrictTransition_EvenlyDistributesCylinderLayers()
    {
        var schematic = new CylinderSchematicCreator
        {
            Radius = 3,
            Height = 9,
            Filled = true
        }
        .WithGradient(
            GradientTransition.Strict,
            Minecraft.Blocks.RedConcrete,
            Minecraft.Blocks.YellowConcrete,
            Minecraft.Blocks.LimeConcrete)
        .Build();

        for (int y = 0; y < 9; y++)
        {
            string expected = y switch
            {
                < 3 => Minecraft.Blocks.RedConcrete,
                < 6 => Minecraft.Blocks.YellowConcrete,
                _ => Minecraft.Blocks.LimeConcrete
            };

            Assert.All(GetLayerNonAirIdentifiers(schematic, y), id => Assert.Equal(expected, id));
        }
    }

    [Fact]
    public void ShapeGradient_WeightedTransition_DistributesByPercentageAndRemainder_AndValidatesWeights()
    {
        string[] palette =
        [
            Minecraft.Blocks.WhiteConcrete,
            Minecraft.Blocks.GrayConcrete,
            Minecraft.Blocks.BlackConcrete
        ];

        var percentCreator = new CylinderSchematicCreator
        {
            Radius = 3,
            Height = 10,
            Filled = true
        };
        percentCreator.WithWeightedGradient(palette, 30, 60);
        var schematic = percentCreator.Build();

        for (int y = 0; y < 10; y++)
        {
            string expected = y switch
            {
                < 3 => Minecraft.Blocks.WhiteConcrete,
                < 9 => Minecraft.Blocks.GrayConcrete,
                _ => Minecraft.Blocks.BlackConcrete
            };

            Assert.All(GetLayerNonAirIdentifiers(schematic, y), id => Assert.Equal(expected, id));
        }

        var fractionGradient = BlockGradient.Weighted(palette, 0.3, 0.6);
        var explicitGradient = BlockGradient.Weighted(palette, 30, 60, 10);
        for (int step = 0; step < 10; step++)
        {
            Assert.Equal(fractionGradient.GetBlock(step, 10), explicitGradient.GetBlock(step, 10));
        }

        Assert.Throws<ArgumentException>(() => new BlockGradient(Array.Empty<string>()));
        Assert.Throws<ArgumentException>(() => new BlockGradient(palette, GradientTransition.Weighted));
        Assert.Throws<ArgumentException>(() => BlockGradient.Weighted(palette, 30));
        Assert.Throws<ArgumentException>(() => BlockGradient.Weighted(palette, 10, 20, 30, 40));
        Assert.Throws<ArgumentOutOfRangeException>(() => BlockGradient.Weighted(palette, 50, 60));
        Assert.Throws<ArgumentOutOfRangeException>(() => BlockGradient.Weighted(palette, -5, 50));
        Assert.Throws<ArgumentOutOfRangeException>(() => BlockGradient.Weighted(palette, double.NaN, 50));
    }

    [Fact]
    public void ShapeGradient_FuzzyTransition_RandomizesBoundariesForStrictAndWeighted()
    {
        var fuzzyStrict = new CylinderSchematicCreator
        {
            Radius = 3,
            Height = 9,
            Filled = true,
            RandomSeed = 42
        }
        .WithGradient(
            GradientTransition.Fuzzy,
            Minecraft.Blocks.RedConcrete,
            Minecraft.Blocks.YellowConcrete,
            Minecraft.Blocks.LimeConcrete)
        .Build();

        Assert.Equal([Minecraft.Blocks.RedConcrete], GetLayerNonAirIdentifiers(fuzzyStrict, 0).Distinct().ToArray());
        Assert.Equal([Minecraft.Blocks.YellowConcrete], GetLayerNonAirIdentifiers(fuzzyStrict, 4).Distinct().ToArray());
        Assert.Equal([Minecraft.Blocks.LimeConcrete], GetLayerNonAirIdentifiers(fuzzyStrict, 8).Distinct().ToArray());

        var strictBoundaryDistinct = GetLayerNonAirIdentifiers(fuzzyStrict, 2)
            .Concat(GetLayerNonAirIdentifiers(fuzzyStrict, 3))
            .Distinct()
            .ToHashSet();
        Assert.Contains(Minecraft.Blocks.RedConcrete, strictBoundaryDistinct);
        Assert.Contains(Minecraft.Blocks.YellowConcrete, strictBoundaryDistinct);

        var fuzzyWeighted = new CylinderSchematicCreator
        {
            Radius = 4,
            Height = 10,
            Filled = true
        }
        .WithGradient(
            [Minecraft.Blocks.WhiteConcrete, Minecraft.Blocks.GrayConcrete, Minecraft.Blocks.BlackConcrete],
            GradientTransition.Weighted,
            [30, 60],
            fuzzy: true,
            seed: 123)
        .Build();

        Assert.Equal([Minecraft.Blocks.WhiteConcrete], GetLayerNonAirIdentifiers(fuzzyWeighted, 0).Distinct().ToArray());
        Assert.Equal([Minecraft.Blocks.GrayConcrete], GetLayerNonAirIdentifiers(fuzzyWeighted, 5).Distinct().ToArray());

        var weightedBoundaryDistinct = GetLayerNonAirIdentifiers(fuzzyWeighted, 2)
            .Concat(GetLayerNonAirIdentifiers(fuzzyWeighted, 3))
            .Distinct()
            .ToHashSet();
        Assert.Contains(Minecraft.Blocks.WhiteConcrete, weightedBoundaryDistinct);
        Assert.Contains(Minecraft.Blocks.GrayConcrete, weightedBoundaryDistinct);
    }

    [Fact]
    public void ShapeGradient_OtherShapesAndStairPyramid_UseMultipleBlocks()
    {
        var circle = new CircleSchematicCreator { Radius = 6, Filled = true }
            .WithGradient(GradientTransition.Strict, Minecraft.Blocks.RedConcrete, Minecraft.Blocks.YellowConcrete, Minecraft.Blocks.LimeConcrete)
            .Build();
        Assert.Equal(Minecraft.Blocks.RedConcrete, circle[6, 0, 6].Identifier);
        Assert.Equal(Minecraft.Blocks.LimeConcrete, circle[12, 0, 6].Identifier);

        var oval = new OvalSchematicCreator { RadiusU = 6, RadiusV = 4, Filled = true }
            .WithBlocks(Minecraft.Blocks.RedConcrete, Minecraft.Blocks.LimeConcrete)
            .Build();
        Assert.Equal(Minecraft.Blocks.RedConcrete, oval[6, 0, 4].Identifier);
        Assert.Equal(Minecraft.Blocks.LimeConcrete, oval[12, 0, 4].Identifier);

        var rectangle = new RectangleSchematicCreator { Width = 6, Length = 6, Filled = true }
            .WithBlocks(Minecraft.Blocks.RedConcrete, Minecraft.Blocks.LimeConcrete)
            .Build();
        Assert.Equal(Minecraft.Blocks.RedConcrete, rectangle[0, 0, 0].Identifier);
        Assert.Equal(Minecraft.Blocks.LimeConcrete, rectangle[0, 0, 5].Identifier);

        var cube = new CubeSchematicCreator { Size = 6, Filled = true }
            .WithWeightedGradient([Minecraft.Blocks.WhiteConcrete, Minecraft.Blocks.BlackConcrete], 50)
            .Build();
        Assert.Equal(Minecraft.Blocks.WhiteConcrete, cube[0, 0, 0].Identifier);
        Assert.Equal(Minecraft.Blocks.BlackConcrete, cube[0, 5, 0].Identifier);

        var sphere = new SphereSchematicCreator { Radius = 4, Filled = true }
            .WithBlocks(Minecraft.Blocks.RedConcrete, Minecraft.Blocks.LimeConcrete)
            .Build();
        Assert.Equal(Minecraft.Blocks.RedConcrete, sphere[4, 0, 4].Identifier);
        Assert.Equal(Minecraft.Blocks.LimeConcrete, sphere[4, 8, 4].Identifier);

        var torus = new TorusSchematicCreator { MajorRadius = 6, MinorRadius = 3, Filled = true }
            .WithBlocks(Minecraft.Blocks.RedConcrete, Minecraft.Blocks.LimeConcrete)
            .Build();
        Assert.Equal(Minecraft.Blocks.RedConcrete, torus[15, 0, 9].Identifier);
        Assert.Equal(Minecraft.Blocks.LimeConcrete, torus[15, 6, 9].Identifier);

        var stairPyramid = new PyramidSchematicCreator { BaseSize = 9, UseStairs = true }
            .WithBlocks(Minecraft.Blocks.Sandstone, Minecraft.Blocks.RedSandstone)
            .Build();
        AssertStair(stairPyramid[0, 0, 0], Minecraft.Blocks.SandstoneStairs, "south", "outer_left");
        AssertStair(stairPyramid[3, 3, 3], Minecraft.Blocks.RedSandstoneStairs, "south", "outer_left");
        Assert.Equal(Minecraft.Blocks.RedSandstone, stairPyramid[4, 4, 4].Identifier);

        var gradientCylinder = new CylinderSchematicCreator { Radius = 5, Height = 12, Filled = true }
            .WithGradient(
                GradientTransition.Fuzzy,
                Minecraft.Blocks.WhiteConcrete,
                Minecraft.Blocks.LightGrayConcrete,
                Minecraft.Blocks.GrayConcrete,
                Minecraft.Blocks.BlackConcrete)
            .Build();
        string spongePath = Path.Combine(SchematicCreatorTests.TestOutputDirectory, "GradientCylinder.schem");
        SchematicFile.Write(gradientCylinder, spongePath, SchematicFormat.SpongeV2);
        Assert.True(File.Exists(spongePath));
    }

    [Fact]
    public void SetCylinderBase_StairTransition_OrientsStairsInwardAndPreservesHollowCore()
    {
        var schematic = SchematicBuilder.Create(15, 8, 15)
            .SetCylinder(7, 0, 7, radius: 4, height: 8, Minecraft.Blocks.LightGrayConcrete, filled: false)
            .SetCylinderBase(7, 0, 7, cylinderRadius: 4, baseWidth: 3, baseHeight: 3, Minecraft.Blocks.StoneBricks)
            .Build();

        // Cardinal outer stairs on bottom ring (y = 0, outer radius = 7)
        AssertStair(schematic[7, 0, 0], Minecraft.Blocks.StoneBrickStairs, "south", "straight");
        AssertStair(schematic[7, 0, 14], Minecraft.Blocks.StoneBrickStairs, "north", "straight");
        AssertStair(schematic[0, 0, 7], Minecraft.Blocks.StoneBrickStairs, "east", "straight");
        AssertStair(schematic[14, 0, 7], Minecraft.Blocks.StoneBrickStairs, "west", "straight");

        // Diagonal outer corners on bottom ring (du = -5, dv = -5 => x = 2, z = 2)
        AssertStair(schematic[2, 0, 2], Minecraft.Blocks.StoneBrickStairs, "south", "outer_left");
        AssertStair(schematic[12, 0, 2], Minecraft.Blocks.StoneBrickStairs, "south", "outer_right");
        AssertStair(schematic[2, 0, 12], Minecraft.Blocks.StoneBrickStairs, "north", "outer_right");
        AssertStair(schematic[12, 0, 12], Minecraft.Blocks.StoneBrickStairs, "north", "outer_left");

        // Covered lower step directly under y = 1 stair tier stays solid fill block
        Assert.Equal(Minecraft.Blocks.StoneBricks, schematic[7, 0, 2].Identifier);

        // Rising stair tiers at y = 1 (radius 6) and y = 2 (radius 5)
        AssertStair(schematic[7, 1, 1], Minecraft.Blocks.StoneBrickStairs, "south", "straight");
        AssertStair(schematic[7, 2, 2], Minecraft.Blocks.StoneBrickStairs, "south", "straight");

        // Above baseHeight (y = 3): base does not extend outward; cylinder wall & hollow interior remain intact
        Assert.Equal(BlockState.Air, schematic[7, 3, 2]);
        Assert.Equal(Minecraft.Blocks.LightGrayConcrete, schematic[7, 3, 3].Identifier);
        Assert.Equal(BlockState.Air, schematic[7, 0, 7]);
        Assert.Equal(BlockState.Air, schematic[7, 3, 7]);
    }

    [Fact]
    public void SetCylinder_WithBaseWidthAndHeightRatios_HandlesSlopesGradientsAndStairInputs()
    {
        // Width > Height (4 wide, 2 high): layer 0 extends +4 (solid inner tread + outer stair), layer 1 extends +2
        var wideBase = SchematicBuilder.Create(17, 6, 17)
            .SetCylinder(
                centerX: 8,
                startY: 0,
                centerZ: 8,
                radius: 4,
                height: 6,
                blockStateString: Minecraft.Blocks.QuartzBlock,
                filled: true,
                axis: Axis.Y,
                smoothnessFactor: 1.0,
                baseWidth: 4,
                baseHeight: 2,
                baseGradient: new BlockGradient([Minecraft.Blocks.DeepslateBricks, Minecraft.Blocks.StoneBricks]))
            .Build();

        AssertStair(wideBase[8, 0, 0], Minecraft.Blocks.DeepslateBrickStairs, "south", "straight");
        Assert.Equal(Minecraft.Blocks.DeepslateBricks, wideBase[8, 0, 1].Identifier);
        AssertStair(wideBase[8, 1, 2], Minecraft.Blocks.StoneBrickStairs, "south", "straight");
        Assert.Equal(BlockState.Air, wideBase[8, 2, 2]);

        // Width < Height (2 wide, 4 high) using a *_stairs input resolves solid stone_bricks under upper layers
        var tallBase = SchematicBuilder.Create(13, 6, 13)
            .SetCylinder(8 - 2, 0, 6, radius: 3, height: 6, Minecraft.Blocks.Stone)
            .SetCylinderBase(6, 0, 6, cylinderRadius: 3, baseWidth: 2, baseHeight: 4, Minecraft.Blocks.StoneBrickStairs)
            .Build();

        Assert.Equal(Minecraft.Blocks.StoneBricks, tallBase[6, 0, 1].Identifier);
        AssertStair(tallBase[6, 1, 1], Minecraft.Blocks.StoneBrickStairs, "south", "straight");
        Assert.Equal(Minecraft.Blocks.StoneBricks, tallBase[6, 2, 2].Identifier);
        AssertStair(tallBase[6, 3, 2], Minecraft.Blocks.StoneBrickStairs, "south", "straight");
        Assert.Equal(BlockState.Air, tallBase[6, 4, 2]);
    }

    [Fact]
    public void CylinderSchematicCreator_WithBase_ExtendsBoundsValidationAndWritesSchematic()
    {
        // Auto-extends a smaller SchematicBuilder when placing cylinder + base
        var creator = new CylinderSchematicCreator
        {
            Radius = 5,
            Height = 12,
            Block = Minecraft.Blocks.LightGrayConcrete,
            Filled = false
        }.WithBase(width: 3, height: 3, Minecraft.Blocks.DeepslateBricks, Minecraft.Blocks.StoneBricks);

        var extendedBuilder = SchematicBuilder.Create(4, 4, 4);
        creator.Place(extendedBuilder, startX: 1, startY: 0, startZ: 1);
        var placed = extendedBuilder.Build();
        Assert.Equal(18, placed.Width);
        Assert.Equal(12, placed.Height);
        Assert.Equal(18, placed.Length);

        var standalone = creator.Build();
        Assert.Equal(17, standalone.Width);
        Assert.Equal(12, standalone.Height);
        Assert.Equal(17, standalone.Length);
        AssertStair(standalone[8, 0, 0], Minecraft.Blocks.DeepslateBrickStairs, "south", "straight");
        AssertStair(standalone[8, 2, 2], Minecraft.Blocks.StoneBrickStairs, "south", "straight");

        string spongePath = Path.Combine(SchematicCreatorTests.TestOutputDirectory, "CylinderWithStairBase.schem");
        SchematicFile.Write(standalone, spongePath, SchematicFormat.SpongeV2);
        var readBack = SchematicFile.Read(spongePath, SchematicFormat.SpongeV2);
        Assert.Equal(standalone.Width, readBack.Width);
        Assert.Equal(standalone.Height, readBack.Height);
        Assert.Equal(standalone.Length, readBack.Length);
        AssertStair(readBack[8, 0, 0], Minecraft.Blocks.DeepslateBrickStairs, "south", "straight");

        // Validation guards
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new CylinderSchematicCreator { Radius = 3, Height = 5, BaseWidth = 2, BaseHeight = 0 }.Build());
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new CylinderSchematicCreator { Radius = 3, Height = 5, BaseWidth = 0, BaseHeight = 2 }.Build());
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new CylinderSchematicCreator { Radius = 3, Height = 3, BaseWidth = 2, BaseHeight = 4 }.Build());
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Schematic(10, 10, 10).SetCylinderBase(5, 0, 5, cylinderRadius: 2, baseWidth: 0, baseHeight: 2, Minecraft.Blocks.StoneBricks));
    }

    [Fact]
    public void SetCylinderWater_MediumCylinder_PlacesNonChainingSourcesAndFlowsToCenter()
    {
        var schematic = SchematicBuilder.Create(15, 10, 15)
            .SetCylinder(7, 0, 7, radius: 7, height: 10, Minecraft.Blocks.LimeStainedGlass, filled: false)
            .SetCylinderWater(7, 0, 7, cylinderRadius: 7)
            .Build();

        // Collect all water source blocks (level=0) and verify none can chain into an infinite water lake (Manhattan distance >= 3)
        var sources = new List<(int X, int Z)>();
        for (int z = 0; z < schematic.Length; z++)
        {
            for (int x = 0; x < schematic.Width; x++)
            {
                var block = schematic[x, 0, z];
                if (block.Identifier == Minecraft.Blocks.Water && block.Properties.GetValueOrDefault("level") == "0")
                {
                    sources.Add((x, z));
                }
            }
        }

        Assert.True(sources.Count >= 4);
        Assert.Contains((1, 7), sources);
        Assert.Contains((13, 7), sources);
        Assert.Contains((7, 1), sources);
        Assert.Contains((7, 13), sources);

        for (int i = 0; i < sources.Count; i++)
        {
            for (int j = i + 1; j < sources.Count; j++)
            {
                int manhattan = Math.Abs(sources[i].X - sources[j].X) + Math.Abs(sources[i].Z - sources[j].Z);
                Assert.True(manhattan >= 3, $"Water sources at {sources[i]} and {sources[j]} are too close (Manhattan distance {manhattan}) and would create an infinite water source.");
            }
        }

        // Verify strictly increasing water level (decreasing water height) from inner wall toward center (7, 0, 7)
        for (int offset = 0; offset <= 6; offset++)
        {
            int expectedLevel = 6 - offset;
            var sample = schematic[7 + offset, 0, 7];
            Assert.Equal(Minecraft.Blocks.Water, sample.Identifier);
            Assert.Equal(expectedLevel.ToString(), sample.Properties["level"]);
        }

        // Verify cylinder wall itself remains glass and is not overwritten by water
        Assert.Equal(Minecraft.Blocks.LimeStainedGlass, schematic[14, 0, 7].Identifier);
        Assert.Equal(Minecraft.Blocks.LimeStainedGlass, schematic[0, 0, 7].Identifier);
        Assert.Equal(Minecraft.Blocks.LimeStainedGlass, schematic[7, 0, 14].Identifier);
        Assert.Equal(Minecraft.Blocks.LimeStainedGlass, schematic[7, 0, 0].Identifier);
    }

    [Fact]
    public void SetCylinderWater_LargeCylinder_BuildsSteppedTiersAndFlowsToCenter()
    {
        var schematic = SchematicBuilder.Create(25, 10, 25)
            .SetCylinder(
                centerX: 12,
                startY: 0,
                centerZ: 12,
                radius: 12,
                height: 10,
                blockStateString: Minecraft.Blocks.StoneBricks,
                filled: false,
                includeWater: true,
                waterFloorBlock: new BlockState(Minecraft.Blocks.DeepslateBricks))
            .Build();

        // Outer tier (tier 1) has solid floor at y=0 and water source at y=1
        Assert.Equal(Minecraft.Blocks.DeepslateBricks, schematic[23, 0, 12].Identifier);
        Assert.Equal(Minecraft.Blocks.Water, schematic[23, 1, 12].Identifier);
        Assert.Equal("0", schematic[23, 1, 12].Properties["level"]);

        // Transition edge (radius 6 from center => x = 18, z = 12) receives overhanging flow at y=1 and falling water (level=8) at y=0
        Assert.Equal(Minecraft.Blocks.Water, schematic[18, 1, 12].Identifier);
        Assert.Equal("8", schematic[18, 0, 12].Properties["level"]);

        // Inner tier spreads from level=8 drop at (18, 0, 12) inward to center (12, 0, 12)
        for (int step = 1; step <= 6; step++)
        {
            var innerSample = schematic[18 - step, 0, 12];
            Assert.Equal(Minecraft.Blocks.Water, innerSample.Identifier);
            Assert.Equal(step.ToString(), innerSample.Properties["level"]);
        }

        var centerBlock = schematic[12, 0, 12];
        Assert.Equal(Minecraft.Blocks.Water, centerBlock.Identifier);
        Assert.Equal("6", centerBlock.Properties["level"]);
    }

    [Fact]
    public void CylinderSchematicCreator_WithWater_ValidatesAndWritesSchematicFile()
    {
        var creator = new CylinderSchematicCreator
        {
            Radius = 7,
            Height = 16,
            Block = Minecraft.Blocks.LimeStainedGlass,
            Filled = false
        }
            .WithBase(width: 2, height: 2, Minecraft.Blocks.DeepslateBricks)
            .WithWater(includeWater: true);

        var schematic = creator.Build();
        Assert.Equal(19, schematic.Width);
        Assert.Equal(16, schematic.Height);
        Assert.Equal(19, schematic.Length);
        Assert.Equal(Minecraft.Blocks.Water, schematic[9, 0, 9].Identifier);
        Assert.Equal("6", schematic[9, 0, 9].Properties["level"]);
        AssertStair(schematic[9, 0, 0], Minecraft.Blocks.DeepslateBrickStairs, "south", "straight");

        string spongePath = Path.Combine(SchematicCreatorTests.TestOutputDirectory, "CylinderWithWaterFlow.schem");
        SchematicFile.Write(schematic, spongePath, SchematicFormat.SpongeV2);
        var readBack = SchematicFile.Read(spongePath, SchematicFormat.SpongeV2);
        Assert.Equal(Minecraft.Blocks.Water, readBack[9, 0, 9].Identifier);
        Assert.Equal("6", readBack[9, 0, 9].Properties["level"]);

        // Validation guards
        Assert.Throws<ArgumentException>(() =>
            new CylinderSchematicCreator { Radius = 5, Height = 8, Filled = true }.WithWater().Build());
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new CylinderSchematicCreator { Radius = 5, Height = 8, Filled = false, Axis = Axis.X }.WithWater().Build());
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new CylinderSchematicCreator { Radius = 0, Height = 8, Filled = false }.WithWater().Build());
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new CylinderSchematicCreator { Radius = 12, Height = 1, Filled = false }.WithWater().Build());
    }

    [Fact]
    public void SetCylinderTop_InvertedStairTransition_OrientsTopStairsInwardAndPreservesHollowCore()
    {
        var schematic = SchematicBuilder.Create(15, 8, 15)
            .SetCylinder(7, 0, 7, radius: 4, height: 8, Minecraft.Blocks.LightGrayConcrete, filled: false)
            .SetCylinderTop(7, 5, 7, cylinderRadius: 4, topWidth: 3, topHeight: 3, Minecraft.Blocks.StoneBricks)
            .Build();

        // Cardinal outer inverted stairs on top ring (y = 7, outer radius = 7)
        AssertStair(schematic[7, 7, 0], Minecraft.Blocks.StoneBrickStairs, "south", "straight", "top");
        AssertStair(schematic[7, 7, 14], Minecraft.Blocks.StoneBrickStairs, "north", "straight", "top");
        AssertStair(schematic[0, 7, 7], Minecraft.Blocks.StoneBrickStairs, "east", "straight", "top");
        AssertStair(schematic[14, 7, 7], Minecraft.Blocks.StoneBrickStairs, "west", "straight", "top");

        // Diagonal outer corners on top ring (du = -5, dv = -5 => x = 2, z = 2)
        AssertStair(schematic[2, 7, 2], Minecraft.Blocks.StoneBrickStairs, "south", "outer_left", "top");
        AssertStair(schematic[12, 7, 2], Minecraft.Blocks.StoneBrickStairs, "south", "outer_right", "top");
        AssertStair(schematic[2, 7, 12], Minecraft.Blocks.StoneBrickStairs, "north", "outer_right", "top");
        AssertStair(schematic[12, 7, 12], Minecraft.Blocks.StoneBrickStairs, "north", "outer_left", "top");

        // Covered upper step above y = 6 stair tier stays solid fill block
        Assert.Equal(Minecraft.Blocks.StoneBricks, schematic[7, 7, 2].Identifier);

        // Descending underside stair tiers at y = 6 (radius 6) and y = 5 (radius 5)
        AssertStair(schematic[7, 6, 1], Minecraft.Blocks.StoneBrickStairs, "south", "straight", "top");
        AssertStair(schematic[7, 5, 2], Minecraft.Blocks.StoneBrickStairs, "south", "straight", "top");

        // Below topStartY (y = 4): top collar does not extend outward; cylinder wall & hollow interior remain intact
        Assert.Equal(BlockState.Air, schematic[7, 4, 2]);
        Assert.Equal(Minecraft.Blocks.LightGrayConcrete, schematic[7, 4, 3].Identifier);
        Assert.Equal(BlockState.Air, schematic[7, 7, 7]);
        Assert.Equal(BlockState.Air, schematic[7, 4, 7]);
    }

    [Fact]
    public void SetCylinder_WithTopWidthAndHeightRatios_HandlesSlopesGradientsAndStairInputs()
    {
        // Width > Height (4 wide, 2 high): layer 5 (y=5) extends +4 at top, layer 4 extends +2
        var wideTop = SchematicBuilder.Create(17, 6, 17)
            .SetCylinder(
                centerX: 8,
                startY: 0,
                centerZ: 8,
                radius: 4,
                height: 6,
                blockStateString: Minecraft.Blocks.QuartzBlock,
                filled: true,
                axis: Axis.Y,
                smoothnessFactor: 1.0,
                topWidth: 4,
                topHeight: 2,
                topGradient: new BlockGradient([Minecraft.Blocks.StoneBricks, Minecraft.Blocks.DeepslateBricks]))
            .Build();

        AssertStair(wideTop[8, 4, 2], Minecraft.Blocks.StoneBrickStairs, "south", "straight", "top");
        AssertStair(wideTop[8, 5, 0], Minecraft.Blocks.DeepslateBrickStairs, "south", "straight", "top");
        Assert.Equal(Minecraft.Blocks.DeepslateBricks, wideTop[8, 5, 1].Identifier);
        Assert.Equal(BlockState.Air, wideTop[8, 3, 2]);

        // Width < Height (2 wide, 4 high) using a *_stairs input resolves solid stone_bricks behind outer tiers
        var tallTop = SchematicBuilder.Create(13, 6, 13)
            .SetCylinder(6, 0, 6, radius: 3, height: 6, Minecraft.Blocks.Stone)
            .SetCylinderTop(6, 2, 6, cylinderRadius: 3, topWidth: 2, topHeight: 4, Minecraft.Blocks.StoneBrickStairs)
            .Build();

        AssertStair(tallTop[6, 2, 2], Minecraft.Blocks.StoneBrickStairs, "south", "straight", "top");
        Assert.Equal(Minecraft.Blocks.StoneBricks, tallTop[6, 3, 2].Identifier);
        AssertStair(tallTop[6, 4, 1], Minecraft.Blocks.StoneBrickStairs, "south", "straight", "top");
        Assert.Equal(Minecraft.Blocks.StoneBricks, tallTop[6, 5, 1].Identifier);
        Assert.Equal(BlockState.Air, tallTop[6, 1, 2]);
    }

    [Fact]
    public void CylinderSchematicCreator_WithBaseAndTop_ExtendsBoundsValidatesAndWritesSchematic()
    {
        var creator = new CylinderSchematicCreator
        {
            Radius = 5,
            Height = 12,
            Block = Minecraft.Blocks.LightGrayConcrete,
            Filled = false
        }
            .WithBase(width: 2, height: 2, Minecraft.Blocks.DeepslateBricks)
            .WithTop(width: 4, height: 3, Minecraft.Blocks.StoneBricks, Minecraft.Blocks.DeepslateBricks)
            .WithWater(includeWater: true);

        var extendedBuilder = SchematicBuilder.Create(4, 4, 4);
        creator.Place(extendedBuilder, startX: 1, startY: 0, startZ: 1);
        var placed = extendedBuilder.Build();
        Assert.Equal(20, placed.Width);
        Assert.Equal(12, placed.Height);
        Assert.Equal(20, placed.Length);

        var standalone = creator.Build();
        Assert.Equal(19, standalone.Width);
        Assert.Equal(12, standalone.Height);
        Assert.Equal(19, standalone.Length);
        AssertStair(standalone[9, 0, 2], Minecraft.Blocks.DeepslateBrickStairs, "south", "straight", "bottom");
        AssertStair(standalone[9, 11, 0], Minecraft.Blocks.DeepslateBrickStairs, "south", "straight", "top");
        Assert.Equal(Minecraft.Blocks.Water, standalone[9, 0, 9].Identifier);

        string spongePath = Path.Combine(SchematicCreatorTests.TestOutputDirectory, "CylinderWithStairTop.schem");
        SchematicFile.Write(standalone, spongePath, SchematicFormat.SpongeV2);
        var readBack = SchematicFile.Read(spongePath, SchematicFormat.SpongeV2);
        AssertStair(readBack[9, 11, 0], Minecraft.Blocks.DeepslateBrickStairs, "south", "straight", "top");
        AssertStair(readBack[9, 0, 2], Minecraft.Blocks.DeepslateBrickStairs, "south", "straight", "bottom");

        // Validation guards
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new CylinderSchematicCreator { Radius = 3, Height = 5, TopWidth = 2, TopHeight = 0 }.Build());
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new CylinderSchematicCreator { Radius = 3, Height = 5, TopWidth = 0, TopHeight = 2 }.Build());
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new CylinderSchematicCreator { Radius = 3, Height = 3, TopWidth = 2, TopHeight = 4 }.Build());
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new CylinderSchematicCreator { Radius = 3, Height = 5, BaseWidth = 2, BaseHeight = 3, TopWidth = 2, TopHeight = 3 }.Build());
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Schematic(10, 10, 10).SetCylinderTop(5, 0, 5, cylinderRadius: 2, topWidth: 0, topHeight: 2, Minecraft.Blocks.StoneBricks));
    }

    private static IReadOnlyList<string> GetLayerNonAirIdentifiers(Schematic schematic, int y)
    {
        var ids = new List<string>();
        for (int z = 0; z < schematic.Length; z++)
        {
            for (int x = 0; x < schematic.Width; x++)
            {
                var block = schematic[x, y, z];
                if (!block.IsAir)
                {
                    ids.Add(block.Identifier);
                }
            }
        }
        return ids;
    }

    private static void AssertStair(BlockState state, string expectedStairId, string expectedFacing, string expectedShape, string expectedHalf = "bottom")
    {
        Assert.Equal(expectedStairId, state.Identifier);
        Assert.Equal(expectedHalf, state.Properties["half"]);
        Assert.Equal(expectedFacing, state.Properties["facing"]);
        Assert.Equal(expectedShape, state.Properties["shape"]);
    }
}
