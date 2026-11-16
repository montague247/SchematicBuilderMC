using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;
using SchematicBuilderMC.IO;
using SchematicBuilderMC.Models;
using SchematicBuilderMC.Registries;
using SchematicBuilderMC.Schematics.Farms;

namespace SchematicBuilderMC.Core.Tests.Schematics.Farms;

public sealed class CompactCropFarmSchematicCreatorTests
{
    [Theory]
    [InlineData(CropType.Wheat, Minecraft.Blocks.Wheat, 4)]
    [InlineData(CropType.Carrots, Minecraft.Blocks.Carrots, 4)]
    [InlineData(CropType.Potatoes, Minecraft.Blocks.Potatoes, 4)]
    [InlineData(CropType.Beetroots, Minecraft.Blocks.Beetroots, 4)]
    [InlineData(CropType.SugarCane, Minecraft.Blocks.SugarCane, 5)]
    public void Build_AllCropTypes_PlacesExpectedCropsSoilAndHarvestingComponents(
        CropType crop,
        string expectedCropBlock,
        int expectedHeight)
    {
        const int rowLength = 6;
        var creator = new CompactCropFarmSchematicCreator
        {
            Crop = crop,
            RowLength = rowLength,
            Facing = Direction.North
        };

        var schematic = creator.Build();

        Assert.Equal(crop == CropType.SugarCane ? 7 : 5, schematic.Width);
        Assert.Equal(expectedHeight, schematic.Height);
        Assert.Equal(rowLength + 3, schematic.Length);

        int cropCount = 0;
        int soilCount = 0;
        int waterCount = 0;
        int dispenserCount = 0;
        int pistonCount = 0;
        int observerCount = 0;
        int redstoneWireCount = 0;

        string expectedSoilBlock = crop == CropType.SugarCane
            ? Minecraft.Blocks.Dirt
            : Minecraft.Blocks.Farmland;

        for (int y = 0; y < schematic.Height; y++)
        {
            for (int z = 0; z < schematic.Length; z++)
            {
                for (int x = 0; x < schematic.Width; x++)
                {
                    var block = schematic[x, y, z];
                    if (block.Identifier == expectedCropBlock)
                    {
                        cropCount++;
                    }
                    else if (block.Identifier == expectedSoilBlock)
                    {
                        soilCount++;
                        if (crop != CropType.SugarCane)
                        {
                            Assert.Equal("7", block.GetProperty("moisture"));
                        }
                    }
                    else if (block.Identifier == Minecraft.Blocks.Water)
                    {
                        waterCount++;
                        Assert.Equal("0", block.GetProperty("level"));
                    }
                    else if (block.Identifier == Minecraft.Blocks.Dispenser)
                    {
                        dispenserCount++;
                        var items = schematic.GetItems(x, y, z);
                        Assert.Single(items);
                        Assert.Equal(Minecraft.Items.WaterBucket, items[0].Id);
                    }
                    else if (block.Identifier == Minecraft.Blocks.Piston)
                    {
                        pistonCount++;
                    }
                    else if (block.Identifier == Minecraft.Blocks.Observer)
                    {
                        observerCount++;
                    }
                    else if (block.Identifier == Minecraft.Blocks.RedstoneWire)
                    {
                        redstoneWireCount++;
                    }
                }
            }
        }

        Assert.Equal(rowLength * 2, cropCount);
        Assert.Equal(rowLength * 2, soilCount);
        Assert.Equal(rowLength, waterCount);

        if (crop == CropType.SugarCane)
        {
            Assert.Equal(rowLength * 2, pistonCount);
            Assert.Equal(rowLength * 2, observerCount);
            Assert.Equal(rowLength * 2, redstoneWireCount);
            Assert.Equal(0, dispenserCount);
        }
        else
        {
            Assert.Equal(2, dispenserCount);
            Assert.Equal(3, redstoneWireCount);
            Assert.Equal(0, pistonCount);
            Assert.Equal(0, observerCount);
        }
    }

    [Theory]
    [InlineData(Direction.North, 5, 11, 2, 1, 0, "north")]
    [InlineData(Direction.South, 5, 11, 2, 1, 10, "south")]
    [InlineData(Direction.East, 11, 5, 10, 1, 2, "east")]
    [InlineData(Direction.West, 11, 5, 0, 1, 2, "west")]
    public void Build_AllOrientations_PlacesCollectionChestAndHopperCorrectly(
        Direction facing,
        int expectedWidth,
        int expectedLength,
        int chestX, int chestY, int chestZ,
        string expectedFacing)
    {
        var creator = new CompactCropFarmSchematicCreator
        {
            Crop = CropType.Wheat,
            RowLength = 8,
            Facing = facing,
            CollectionChestItems = [new ItemStack(Minecraft.Items.Wheat, count: 32, slot: 0)]
        };

        var schematic = creator.Build();

        Assert.Equal(expectedWidth, schematic.Width);
        Assert.Equal(expectedLength, schematic.Length);

        var chest = schematic[chestX, chestY, chestZ];
        Assert.Equal(Minecraft.Blocks.Chest, chest.Identifier);
        Assert.Equal(expectedFacing, chest.GetProperty("facing"));

        var items = schematic.GetItems(chestX, chestY, chestZ);
        Assert.Single(items);
        Assert.Equal(Minecraft.Items.Wheat, items[0].Id);
        Assert.Equal(32, items[0].Count);
    }

    [Fact]
    public void Build_SugarCane_PlacesInwardFacingPistonsObserversAndRearRedstoneWire()
    {
        var creator = new CompactCropFarmSchematicCreator
        {
            Crop = CropType.SugarCane,
            RowLength = 4,
            Facing = Direction.North
        };

        var schematic = creator.Build();

        // Left wall pistons/observers at x = 1 face east; rear redstone wire sits at x = 0
        Assert.Equal("east", schematic[1, 3, 2].GetProperty("facing"));
        Assert.Equal("east", schematic[1, 4, 2].GetProperty("facing"));
        Assert.Equal(Minecraft.Blocks.RedstoneWire, schematic[0, 4, 2].Identifier);

        // Right wall pistons/observers at x = 5 face west; rear redstone wire sits at x = 6
        Assert.Equal("west", schematic[5, 3, 2].GetProperty("facing"));
        Assert.Equal("west", schematic[5, 4, 2].GetProperty("facing"));
        Assert.Equal(Minecraft.Blocks.RedstoneWire, schematic[6, 4, 2].Identifier);
    }

    [Fact]
    public void Create_InvalidConfiguration_ThrowsExpectedExceptions()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new CompactCropFarmSchematicCreator { RowLength = 1 }.Create());

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new CompactCropFarmSchematicCreator { Facing = Direction.Up }.Create());

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new CompactCropFarmSchematicCreator
            {
                CollectionChestItems = [new ItemStack(Minecraft.Items.Carrot, count: 1, slot: 27)]
            }.Create());

        var farm = new CompactCropFarmSchematicCreator { RowLength = 4 };
        var builder = SchematicBuilder.Create(6, 4, 6);
        Assert.Throws<ArgumentOutOfRangeException>(() => builder.Create(farm, 0, 0, 0));
    }

    [Fact]
    public void SpongeRoundtrip_PreservesCropsAndDispenserInventories()
    {
        var creator = new CompactCropFarmSchematicCreator
        {
            Crop = CropType.Potatoes,
            RowLength = 5,
            Facing = Direction.East,
            CollectionChestItems = [new ItemStack(Minecraft.Items.Potato, count: 12, slot: 0)]
        };

        var schematic = creator.Build();
        byte[] bytes = SchematicFile.WriteToBytes(schematic, SchematicFormat.SpongeV2);
        var loaded = SchematicFile.Read(bytes, SchematicFormat.SpongeV2);

        Assert.Equal(schematic.Width, loaded.Width);
        Assert.Equal(schematic.Height, loaded.Height);
        Assert.Equal(schematic.Length, loaded.Length);
        Assert.Equal(Minecraft.Items.Potato, loaded.GetItems(7, 1, 2)[0].Id);
        Assert.Equal(Minecraft.Items.WaterBucket, loaded.GetItems(1, 2, 1)[0].Id);
        Assert.Equal(Minecraft.Items.WaterBucket, loaded.GetItems(1, 2, 3)[0].Id);
    }
}
