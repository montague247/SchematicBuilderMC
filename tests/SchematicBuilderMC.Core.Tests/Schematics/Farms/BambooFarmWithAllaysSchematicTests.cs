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
        // Default Length = 1 (front padding) + 1 (start wall) + 32 (dirt) + 1 (end wall) + 2 (back clock) = 37
        // Default Height = 2 (soilY) + 7 (wallHeight) + 1 + 1 (roof) = 11
        Assert.Equal(35, schematic.Width);
        Assert.Equal(11, schematic.Height);
        Assert.Equal(37, schematic.Length);

        // Verify exact dirt bed size matches Width (33) x Length (32) at SoilLevel (y = 2)
        for (int x = 1; x <= 33; x++)
        {
            for (int z = 2; z <= 33; z++)
            {
                Assert.Equal(Minecraft.Blocks.Dirt, schematic[x, 2, z].Identifier);
            }
        }

        // Walls surround dirt cleanly on x=0/34 and z=1/34
        Assert.Equal(Minecraft.Blocks.WaxedCopperBlock, schematic[0, 2, 1].Identifier);
        Assert.Equal(Minecraft.Blocks.WaxedCopperBlock, schematic[34, 2, 34].Identifier);
        Assert.Equal(Minecraft.Blocks.Chest, schematic[17, 7, 0].Identifier);
        Assert.Equal(Minecraft.Blocks.NoteBlock, schematic[17, 8, 18].Identifier);

        // End wall remains solid (no gap for allays to escape) while the repeater powers internal dust.
        Assert.Equal(Minecraft.Blocks.WaxedCopperBlock, schematic[17, 8, 34].Identifier);
        Assert.Equal(Minecraft.Blocks.Repeater, schematic[17, 8, 35].Identifier);
        Assert.Equal(Minecraft.Blocks.RedstoneWallTorch, schematic[17, 8, 36].Identifier);
        Assert.Equal(Minecraft.Blocks.RedstoneWire, schematic[17, 8, 33].Identifier);
        Assert.Equal(Minecraft.Blocks.RedstoneWire, schematic[17, 8, 19].Identifier);

        // Glass roof covers the interior dirt area (x=1..33, z=2..33) and excludes the walls.
        Assert.Equal(Minecraft.Blocks.Air, schematic[0, 10, 1].Identifier);
        Assert.Equal(Minecraft.Blocks.Air, schematic[34, 10, 34].Identifier);
        Assert.Equal(Minecraft.Blocks.Glass, schematic[1, 10, 2].Identifier);
        Assert.Equal(Minecraft.Blocks.Glass, schematic[33, 10, 33].Identifier);

        // Cross-connected default alternates left (x=14) and right (x=19) flying machines via CopperBulbs
        // and keeps both start/end trigger wiring 1-wide at z=0 (start) and z=35 (end).
        Assert.Equal(Minecraft.Blocks.WaxedCopperBulb, schematic[17, 1, 13].Identifier);
        Assert.Equal(Minecraft.Blocks.Comparator, schematic[16, 1, 13].Identifier);
        Assert.Equal(Minecraft.Blocks.WaxedCopperBulb, schematic[17, 1, 22].Identifier);
        Assert.Equal(Minecraft.Blocks.Comparator, schematic[18, 1, 22].Identifier);

        Assert.Equal(Minecraft.Blocks.Observer, schematic[9, 6, 1].Identifier);
        Assert.Equal("north", schematic[9, 6, 1].Properties["facing"]);
        Assert.Equal(Minecraft.Blocks.Observer, schematic[26, 6, 1].Identifier);
        Assert.Equal(Minecraft.Blocks.RedstoneWire, schematic[9, 6, 0].Identifier);
        Assert.Equal(Minecraft.Blocks.RedstoneWire, schematic[14, 6, 0].Identifier);
        Assert.Equal(Minecraft.Blocks.RedstoneTorch, schematic[14, 4, 0].Identifier);
        Assert.Equal(Minecraft.Blocks.RedstoneWire, schematic[26, 6, 0].Identifier);
        Assert.Equal(Minecraft.Blocks.RedstoneWire, schematic[19, 6, 0].Identifier);
        Assert.Equal(Minecraft.Blocks.RedstoneTorch, schematic[19, 4, 0].Identifier);
        Assert.Equal(Minecraft.Blocks.Air, schematic[17, 6, 0].Identifier);

        Assert.Equal(Minecraft.Blocks.Observer, schematic[8, 6, 34].Identifier);
        Assert.Equal("south", schematic[8, 6, 34].Properties["facing"]);
        Assert.Equal(Minecraft.Blocks.Observer, schematic[25, 6, 34].Identifier);
        Assert.Equal(Minecraft.Blocks.RedstoneWire, schematic[8, 6, 35].Identifier);
        Assert.Equal(Minecraft.Blocks.RedstoneWire, schematic[14, 6, 35].Identifier);
        Assert.Equal(Minecraft.Blocks.RedstoneTorch, schematic[14, 4, 35].Identifier);
        Assert.Equal(Minecraft.Blocks.RedstoneWire, schematic[25, 6, 35].Identifier);
        Assert.Equal(Minecraft.Blocks.RedstoneWire, schematic[19, 6, 35].Identifier);
        Assert.Equal(Minecraft.Blocks.RedstoneTorch, schematic[19, 4, 35].Identifier);
        Assert.Equal(Minecraft.Blocks.Air, schematic[17, 6, 35].Identifier);

        Assert.Equal(4, schematic.Entities.Count);
        var firstAllay = schematic.Entities[0];
        Assert.Equal(Minecraft.Entities.Allay, firstAllay.Id);
        Assert.Equal(1.5, firstAllay.X);
        Assert.Equal(6.0, firstAllay.Y);
        Assert.Equal(2.5, firstAllay.Z);

        var secondAllay = schematic.Entities[1];
        Assert.Equal(Minecraft.Entities.Allay, secondAllay.Id);
        Assert.Equal(33.5, secondAllay.X);
        Assert.Equal(6.0, secondAllay.Y);
        Assert.Equal(2.5, secondAllay.Z);

        var thirdAllay = schematic.Entities[2];
        Assert.Equal(Minecraft.Entities.Allay, thirdAllay.Id);
        Assert.Equal(33.5, thirdAllay.X);
        Assert.Equal(6.0, thirdAllay.Y);
        Assert.Equal(33.5, thirdAllay.Z);

        var fourthAllay = schematic.Entities[3];
        Assert.Equal(Minecraft.Entities.Allay, fourthAllay.Id);
        Assert.Equal(1.5, fourthAllay.X);
        Assert.Equal(6.0, fourthAllay.Y);
        Assert.Equal(33.5, fourthAllay.Z);

        // Write to file
        string spongePath = Path.Combine(SchematicCreatorTests.TestOutputDirectory, $"{schematic.Metadata.Name}.schem");
        SchematicFile.Write(schematic, spongePath, SchematicFormat.SpongeV2);
    }

    [Theory]
    [InlineData(10, 12, 1)]
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

        int expectedRightPadding = Math.Max(0, 6 - (dirtWidth - (dirtWidth / 2)));
        Assert.Equal(dirtWidth + 2 + expectedRightPadding, schematic.Width);
        Assert.Equal(frontPadding + dirtLength + 4, schematic.Length);

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

    [Theory]
    [InlineData(33, 32, 35, 37)]
    [InlineData(10, 12, 13, 17)]
    [InlineData(4, 5, 10, 10)]
    public void Build_UnrotatedNoteBlockClock_PlacesClockFlushBehindEndWall(
        int dirtWidth,
        int dirtLength,
        int expectedWidth,
        int expectedLength)
    {
        var farm = new BambooFarmWithAllaysSchematicCreator
        {
            Width = dirtWidth,
            Length = dirtLength,
            RotateNoteBlockClock = false
        };

        var schematic = farm.Build();

        Assert.Equal(expectedWidth, schematic.Width);
        Assert.Equal(expectedLength, schematic.Length);

        int endWallZ = 1 + 1 + dirtLength + 1 - 1; // wallEndZ = frontPadding(1) + 1 + dirtLength
        int centerX = 1 + (dirtWidth / 2);

        // Flush clock sits along +X at endWallZ + 1..endWallZ + 2 with its end torch directly behind the wall repeater.
        Assert.Equal(Minecraft.Blocks.RedstoneWallTorch, schematic[centerX, 8, endWallZ + 2].Identifier);
        Assert.Equal("west", schematic[centerX, 8, endWallZ + 2].Properties["facing"]);
        Assert.Equal(Minecraft.Blocks.Repeater, schematic[centerX, 8, endWallZ + 1].Identifier);
        Assert.Equal("south", schematic[centerX, 8, endWallZ + 1].Properties["facing"]);
        Assert.Equal(Minecraft.Blocks.WaxedCopperBlock, schematic[centerX, 8, endWallZ].Identifier);
        Assert.Equal(Minecraft.Blocks.RedstoneWire, schematic[centerX + 6, 8, endWallZ + 1].Identifier);
    }

    [Theory]
    [InlineData(33, 32)]
    [InlineData(10, 12)]
    [InlineData(4, 5)]
    public void Build_CollectionArea_PlacesHopperAtLastWireSupportPosition(int dirtWidth, int dirtLength)
    {
        var farm = new BambooFarmWithAllaysSchematicCreator
        {
            Width = dirtWidth,
            Length = dirtLength
        };

        var schematic = farm.Build();

        int centerX = 1 + (dirtWidth / 2);
        int centerZ = 2 + (dirtLength / 2);

        Assert.Equal(Minecraft.Blocks.Hopper, schematic[centerX, 7, centerZ + 1].Identifier);
        Assert.Equal("west", schematic[centerX, 7, centerZ + 1].Properties["facing"]);
        Assert.Equal(Minecraft.Blocks.RedstoneWire, schematic[centerX, 8, centerZ + 1].Identifier);
        Assert.Equal(Minecraft.Blocks.SmoothStone, schematic[centerX, 7, centerZ + 2].Identifier);
    }

    [Fact]
    public void Build_CrossConnectTriggerWiringDisabled_UsesSharedTriggerBuses()
    {
        var farm = new BambooFarmWithAllaysSchematicCreator
        {
            CrossConnectTriggerWiring = false
        };

        var schematic = farm.Build();

        // Direct mode uses a single centerX=17 inline 1-wide lift tower and horizontal bus at z=0 (start) and z=35 (end).
        Assert.Equal(Minecraft.Blocks.RedstoneWire, schematic[17, 6, 0].Identifier);
        Assert.Equal(Minecraft.Blocks.RedstoneTorch, schematic[17, 4, 0].Identifier);
        Assert.Equal(Minecraft.Blocks.RedstoneWire, schematic[17, 1, 1].Identifier);
        Assert.Equal(Minecraft.Blocks.RedstoneWire, schematic[9, 6, 0].Identifier);
        Assert.Equal(Minecraft.Blocks.RedstoneWire, schematic[26, 6, 0].Identifier);
        Assert.Equal(Minecraft.Blocks.RedstoneWire, schematic[17, 6, 35].Identifier);
        Assert.Equal(Minecraft.Blocks.RedstoneTorch, schematic[17, 4, 35].Identifier);
        Assert.Equal(Minecraft.Blocks.RedstoneWire, schematic[17, 1, 34].Identifier);
        Assert.Equal(Minecraft.Blocks.RedstoneWire, schematic[8, 6, 35].Identifier);
        Assert.Equal(Minecraft.Blocks.RedstoneWire, schematic[25, 6, 35].Identifier);
    }

    [Theory]
    [InlineData(4, 5)]
    [InlineData(7, 7)]
    [InlineData(10, 12)]
    [InlineData(20, 20)]
    [InlineData(33, 32)]
    public void Build_AllBambooPositions_ReceiveAtLeastLightLevel9(int dirtWidth, int dirtLength)
    {
        var farm = new BambooFarmWithAllaysSchematicCreator
        {
            Width = dirtWidth,
            Length = dirtLength
        };

        var schematic = farm.Build();

        // Compute 3D Manhattan block-light BFS (emission 15 from LightBlock, treating opaque farm blocks & hoppers as blockers).
        var light = new int[schematic.Width, schematic.Height, schematic.Length];
        var queue = new Queue<(int X, int Y, int Z)>();

        for (int x = 0; x < schematic.Width; x++)
        {
            for (int y = 0; y < schematic.Height; y++)
            {
                for (int z = 0; z < schematic.Length; z++)
                {
                    if (schematic[x, y, z].Identifier == Minecraft.Blocks.VerdantFroglight)
                    {
                        light[x, y, z] = 15;
                        queue.Enqueue((x, y, z));
                    }
                }
            }
        }

        ReadOnlySpan<(int Dx, int Dy, int Dz)> dirs =
        [
            (1, 0, 0), (-1, 0, 0),
            (0, 1, 0), (0, -1, 0),
            (0, 0, 1), (0, 0, -1)
        ];

        while (queue.Count > 0)
        {
            var (x, y, z) = queue.Dequeue();
            int nextLevel = light[x, y, z] - 1;
            if (nextLevel <= 0)
            {
                continue;
            }

            foreach (var (dx, dy, dz) in dirs)
            {
                int nx = x + dx;
                int ny = y + dy;
                int nz = z + dz;
                if (nx < 0 || nx >= schematic.Width || ny < 0 || ny >= schematic.Height || nz < 0 || nz >= schematic.Length)
                {
                    continue;
                }

                string id = schematic[nx, ny, nz].Identifier;
                bool isOpaque = id is Minecraft.Blocks.Dirt
                    or Minecraft.Blocks.WaxedCopperBlock
                    or Minecraft.Blocks.SmoothStone
                    or Minecraft.Blocks.NoteBlock
                    or Minecraft.Blocks.Furnace
                    or Minecraft.Blocks.Hopper
                    or Minecraft.Blocks.Observer;

                if (isOpaque || light[nx, ny, nz] >= nextLevel)
                {
                    continue;
                }

                light[nx, ny, nz] = nextLevel;
                queue.Enqueue((nx, ny, nz));
            }
        }

        const int dirtStartX = 1;
        int dirtEndX = dirtStartX + dirtWidth - 1;
        const int dirtStartZ = 2; // frontPadding(1) + wall(1)
        int dirtEndZ = dirtStartZ + dirtLength - 1;

        for (int x = dirtStartX; x <= dirtEndX; x++)
        {
            for (int z = dirtStartZ + 1; z <= dirtEndZ - 1; z++)
            {
                Assert.True(
                    light[x, 3, z] >= 9,
                    $"Expected bamboo at ({x}, 3, {z}) for {dirtWidth}x{dirtLength} to have light >= 9, but was {light[x, 3, z]}.");
                Assert.True(
                    light[x, 4, z] >= 9,
                    $"Expected growth space above bamboo at ({x}, 4, {z}) for {dirtWidth}x{dirtLength} to have light >= 9, but was {light[x, 4, z]}.");
            }
        }
    }
}
