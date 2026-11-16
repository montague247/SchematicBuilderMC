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

    private static void AssertStair(BlockState state, string expectedStairId, string expectedFacing, string expectedShape)
    {
        Assert.Equal(expectedStairId, state.Identifier);
        Assert.Equal("bottom", state.Properties["half"]);
        Assert.Equal(expectedFacing, state.Properties["facing"]);
        Assert.Equal(expectedShape, state.Properties["shape"]);
    }
}
