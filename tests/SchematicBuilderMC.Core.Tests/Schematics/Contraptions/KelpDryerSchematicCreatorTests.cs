using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;
using SchematicBuilderMC.IO;
using SchematicBuilderMC.Models;
using SchematicBuilderMC.Registries;
using SchematicBuilderMC.Schematics.Contraptions;

namespace SchematicBuilderMC.Core.Tests.Schematics.Contraptions;

public sealed class KelpDryerSchematicCreatorTests
{
    [Fact]
    public void Build_Default_ProducesNamedKelpDryerWith32SmokersFastClockAndDecoration()
    {
        var creator = new KelpDryerSchematicCreator();
        var schematic = creator.Build();

        Assert.Equal("Kelp Dryer", schematic.Metadata.Name);
        Assert.Equal(6, schematic.Width);
        Assert.Equal(10, schematic.Height);
        Assert.Equal(19, schematic.Length);
        Assert.Equal(creator.Width, schematic.Width);
        Assert.Equal(creator.Height, schematic.Height);
        Assert.Equal(creator.Length, schematic.Length);

        int smokerCount = 0;
        int composterCount = 0;
        for (int x = 0; x < schematic.Width; x++)
        {
            for (int y = 0; y < schematic.Height; y++)
            {
                for (int z = 0; z < schematic.Length; z++)
                {
                    string id = schematic[x, y, z].Identifier;
                    if (id == Minecraft.Blocks.Smoker)
                    {
                        smokerCount++;
                    }
                    else if (id == Minecraft.Blocks.Composter)
                    {
                        composterCount++;
                    }
                }
            }
        }

        Assert.Equal(32, smokerCount);
        Assert.Equal(9, composterCount);
        Assert.Equal(Minecraft.Blocks.RedstoneLamp, schematic[1, 8, 1].Identifier);
        Assert.Equal(Minecraft.Blocks.Comparator, schematic[2, 9, 13].Identifier);
        Assert.Equal("compare", schematic[2, 9, 13].Properties["mode"]);
        Assert.Equal("2", schematic[3, 9, 11].Properties["delay"]);
        Assert.Empty(schematic.GetItems(2, 3, 17));
        Assert.Empty(schematic.GetItems(2, 5, 17));
        Assert.Empty(schematic.GetItems(2, 7, 17));
        Assert.Equal(32 + 6, schematic.BlockEntities.Count);

        string spongePath = Path.Combine(SchematicCreatorTests.TestOutputDirectory, "KelpDryer.schem");
        SchematicFile.Write(schematic, spongePath, SchematicFormat.SpongeV2);
        var loaded = SchematicFile.Read(spongePath, SchematicFormat.SpongeV2);
        Assert.Equal("Kelp Dryer", loaded.Metadata.Name);
        Assert.Equal(schematic.Width, loaded.Width);
        Assert.Equal(schematic.Height, loaded.Height);
        Assert.Equal(schematic.Length, loaded.Length);
    }

    [Fact]
    public void Build_Default_MatchesSmokerSupersmelterLayout()
    {
        var kelpDryer = new KelpDryerSchematicCreator().Build();
        var smokerSupersmelter = new SupersmelterSchematicCreator
        {
            MachineType = SmelterType.Smoker
        }.Build();

        Assert.Equal(smokerSupersmelter.Width, kelpDryer.Width);
        Assert.Equal(smokerSupersmelter.Height, kelpDryer.Height);
        Assert.Equal(smokerSupersmelter.Length, kelpDryer.Length);

        for (int x = 0; x < kelpDryer.Width; x++)
        {
            for (int y = 0; y < kelpDryer.Height; y++)
            {
                for (int z = 0; z < kelpDryer.Length; z++)
                {
                    Assert.Equal(smokerSupersmelter[x, y, z], kelpDryer[x, y, z]);
                }
            }
        }
    }

    [Fact]
    public void Build_IncludeDecorationFalse_OmitsCompostersAndReplacesLampWithWire()
    {
        var schematic = new KelpDryerSchematicCreator
        {
            IncludeDecoration = false
        }.Build();

        for (int x = 0; x < schematic.Width; x++)
        {
            for (int y = 0; y < schematic.Height; y++)
            {
                for (int z = 0; z < schematic.Length; z++)
                {
                    Assert.NotEqual(Minecraft.Blocks.Composter, schematic[x, y, z].Identifier);
                    Assert.NotEqual(Minecraft.Blocks.RedstoneLamp, schematic[x, y, z].Identifier);
                }
            }
        }

        Assert.Equal(Minecraft.Blocks.RedstoneWire, schematic[1, 8, 1].Identifier);
        Assert.Equal(Minecraft.Blocks.Air, schematic[0, 3, 16].Identifier);
        Assert.Equal(Minecraft.Blocks.Air, schematic[1, 7, 17].Identifier);
    }

    [Theory]
    [InlineData(Direction.South, 6, 19, "south")]
    [InlineData(Direction.West, 19, 6, "west")]
    [InlineData(Direction.North, 6, 19, "north")]
    [InlineData(Direction.East, 19, 6, "east")]
    public void Build_SupportsAllHorizontalFacings(Direction facing, int expectedWidth, int expectedLength, string expectedChestFacing)
    {
        var creator = new KelpDryerSchematicCreator
        {
            Facing = facing
        };
        var schematic = creator.Build();

        Assert.Equal(expectedWidth, creator.Width);
        Assert.Equal(10, creator.Height);
        Assert.Equal(expectedLength, creator.Length);
        Assert.Equal(expectedWidth, schematic.Width);
        Assert.Equal(10, schematic.Height);
        Assert.Equal(expectedLength, schematic.Length);

        bool foundChest = false;
        for (int x = 0; x < schematic.Width; x++)
        {
            for (int z = 0; z < schematic.Length; z++)
            {
                var block = schematic[x, 3, z];
                if (block.Identifier == Minecraft.Blocks.Chest)
                {
                    Assert.Equal(expectedChestFacing, block.Properties["facing"]);
                    foundChest = true;
                }
            }
        }

        Assert.True(foundChest);
    }

    [Fact]
    public void PlaceAndFluentExtension_SupportKelpItemsAndCustomBlocks()
    {
        var builder = SchematicBuilder.Create(10, 12, 22)
            .Create(new KelpDryerSchematicCreator
            {
                CircuitBlock = Minecraft.Blocks.SmoothStone,
                GlassBlock = Minecraft.Blocks.Glass,
                InputItems = [new ItemStack(Minecraft.Items.Kelp, count: 64, slot: 0), new ItemStack(Minecraft.Items.Kelp, count: 32, slot: 30)],
                FuelItems = [new ItemStack(Minecraft.Items.DriedKelpBlock, count: 64, slot: 2)],
                OutputItems = [new ItemStack(Minecraft.Items.DriedKelp, count: 10, slot: 28)]
            }, startX: 2, startY: 1, startZ: 1);

        var schematic = builder.Build();
        Assert.Equal(Minecraft.Blocks.SmoothStone, schematic[4, 1, 15].Identifier);
        Assert.Equal(Minecraft.Blocks.Glass, schematic[4, 6, 2].Identifier);

        Assert.Equal(Minecraft.Items.Kelp, schematic.GetItems(4, 8, 18)[0].Id);
        Assert.Equal(Minecraft.Items.Kelp, schematic.GetItems(5, 8, 18)[0].Id);
        Assert.Equal(Minecraft.Items.DriedKelpBlock, schematic.GetItems(4, 6, 18)[0].Id);
        Assert.Equal(Minecraft.Items.DriedKelp, schematic.GetItems(5, 4, 18)[0].Id);
    }

    [Fact]
    public void Build_InvalidArguments_ThrowsExpectedExceptions()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new KelpDryerSchematicCreator { Facing = Direction.Up }.Build());
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new KelpDryerSchematicCreator { InputItems = [new ItemStack(Minecraft.Items.Kelp, count: 1, slot: 54)] }.Build());
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new KelpDryerSchematicCreator { FuelItems = [new ItemStack(Minecraft.Items.DriedKelpBlock, count: 65, slot: 0)] }.Build());
    }
}
