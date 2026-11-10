using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Extensions;
using SchematicBuilderMC.Generators;
using SchematicBuilderMC.Models;
using SchematicBuilderMC.Registries;
using Xunit;

namespace SchematicBuilderMC.Core.Tests;

public class MinecraftRegistryTests
{
    [Fact]
    public void Cobblestone_Constant_MatchesRegistryIdentifier()
    {
        Assert.Equal("minecraft:cobblestone", Minecraft.Blocks.Cobblestone);
    }

    [Fact]
    public void CoreBlocks_Constants_MatchExpectedIdentifiers()
    {
        Assert.Equal("minecraft:air", Minecraft.Blocks.Air);
        Assert.Equal("minecraft:stone", Minecraft.Blocks.Stone);
        Assert.Equal("minecraft:stone_bricks", Minecraft.Blocks.StoneBricks);
        Assert.Equal("minecraft:oak_planks", Minecraft.Blocks.OakPlanks);
        Assert.Equal("minecraft:oak_stairs", Minecraft.Blocks.OakStairs);
        Assert.Equal("minecraft:chest", Minecraft.Blocks.Chest);
    }

    [Fact]
    public void Minecraft26_3_NewBlocks_ArePresent()
    {
        // Poplar set from 26.3 Wilderness Bound
        Assert.Equal("minecraft:poplar_log", Minecraft.Blocks.PoplarLog);
        Assert.Equal("minecraft:poplar_planks", Minecraft.Blocks.PoplarPlanks);
        Assert.Equal("minecraft:poplar_shelf", Minecraft.Blocks.PoplarShelf);
        Assert.Equal("minecraft:orange_poplar_leaves", Minecraft.Blocks.OrangePoplarLeaves);
        Assert.Equal("minecraft:straw_bed", Minecraft.Blocks.StrawBed);
    }

    [Fact]
    public void BlockEntities_And_BlockEntityTypes_Match()
    {
        Assert.Equal("minecraft:chest", Minecraft.BlockEntities.Chest);
        Assert.Equal("minecraft:chest", Minecraft.BlockEntityTypes.Chest);
        Assert.Equal("minecraft:furnace", Minecraft.BlockEntities.Furnace);
        Assert.Equal("minecraft:creaking_heart", Minecraft.BlockEntities.CreakingHeart);
        Assert.Equal("minecraft:copper_golem_statue", Minecraft.BlockEntities.CopperGolemStatue);
    }

    [Fact]
    public void Entities_And_EntityTypes_Match()
    {
        Assert.Equal("minecraft:zombie", Minecraft.Entities.Zombie);
        Assert.Equal("minecraft:zombie", Minecraft.EntityTypes.Zombie);
        Assert.Equal("minecraft:allay", Minecraft.Entities.Allay);
        Assert.Equal("minecraft:breeze", Minecraft.Entities.Breeze);
        Assert.Equal("minecraft:bogged", Minecraft.Entities.Bogged);
    }

    [Fact]
    public void Biomes_And_Fluids_ArePresent()
    {
        Assert.Equal("minecraft:plains", Minecraft.Biomes.Plains);
        Assert.Equal("minecraft:dappled_forest", Minecraft.Biomes.DappledForest);
        Assert.Equal("minecraft:water", Minecraft.Fluids.Water);
        Assert.Equal("minecraft:lava", Minecraft.Fluids.Lava);
    }

    [Fact]
    public void Blocks_All_ContainsAllRegistryEntries()
    {
        Assert.Equal(1286, Minecraft.Blocks.All.Count);
        Assert.Contains(Minecraft.Blocks.Cobblestone, Minecraft.Blocks.All);
        Assert.Contains(Minecraft.Blocks.PoplarPlanks, Minecraft.Blocks.All);
    }

    [Fact]
    public void Blocks_IsValid_ValidatesCorrectly()
    {
        Assert.True(Minecraft.Blocks.IsValid(Minecraft.Blocks.Cobblestone));
        Assert.True(Minecraft.Blocks.IsValid("cobblestone"));
        Assert.True(Minecraft.Blocks.IsValid("minecraft:chest[facing=south]"));
        Assert.True(Minecraft.Blocks.IsValid(Minecraft.Blocks.PoplarPlanks));

        Assert.False(Minecraft.Blocks.IsValid("minecraft:non_existent_fake_block"));
        Assert.False(Minecraft.Blocks.IsValid(""));
        Assert.False(Minecraft.Blocks.IsValid(null));
    }

    [Fact]
    public void SchematicBuilder_With_Minecraft_Blocks_BuildsCorrectly()
    {
        var schematic = SchematicBuilder.Create(2, 2, 2)
            .Fill(0, 0, 0, 1, 0, 1, Minecraft.Blocks.Cobblestone)
            .SetBlock(0, 1, 0, Minecraft.Blocks.Air)
            .SetBlock(1, 1, 1, Minecraft.Blocks.Chest.WithProperty("facing", "south"))
            .AddBlockEntity(new BlockEntity(Minecraft.BlockEntities.Chest, new BlockPosition(1, 1, 1)))
            .Build();

        Assert.Equal(Minecraft.Blocks.Cobblestone, schematic[0, 0, 0]);
        Assert.Equal(Minecraft.Blocks.Cobblestone, schematic[0, 0, 0].Identifier);
        Assert.Equal(Minecraft.Blocks.Air, schematic[0, 1, 0]);
        Assert.Equal("minecraft:chest[facing=south]", schematic[1, 1, 1].ToString());
        Assert.Equal(Minecraft.BlockEntities.Chest, schematic.GetBlockEntity(1, 1, 1)?.Id);
    }

    [Fact]
    public void BlockState_String_EqualityOperators_WorkBidirectionally()
    {
        BlockState cobbleState = BlockState.Parse("minecraft:cobblestone");
        Assert.True(cobbleState == Minecraft.Blocks.Cobblestone);
        Assert.True(Minecraft.Blocks.Cobblestone == cobbleState);
        Assert.False(cobbleState != Minecraft.Blocks.Cobblestone);
        Assert.False(Minecraft.Blocks.Cobblestone != cobbleState);

        BlockState airState = BlockState.Parse("minecraft:air");
        Assert.True(airState != Minecraft.Blocks.Cobblestone);
        Assert.True(Minecraft.Blocks.Cobblestone != airState);
    }

    [Fact]
    public void BlockStateExtensions_WithProperty_CreatesCorrectState()
    {
        var state = Minecraft.Blocks.OakStairs
            .WithProperty("facing", "north")
            .WithProperty("half", "bottom");

        Assert.Equal(Minecraft.Blocks.OakStairs, state.Identifier);
        Assert.Equal("north", state.GetProperty("facing"));
        Assert.Equal("bottom", state.GetProperty("half"));
        Assert.Equal("minecraft:oak_stairs[facing=north,half=bottom]", state.ToString());
    }

    [Fact]
    public void Generator_ToPascalCase_HandlesVariousFormats()
    {
        Assert.Equal("Cobblestone", MinecraftRegistryGenerator.ToPascalCase("cobblestone"));
        Assert.Equal("StoneBricks", MinecraftRegistryGenerator.ToPascalCase("stone_bricks"));
        Assert.Equal("StoneBricks", MinecraftRegistryGenerator.ToPascalCase("minecraft:stone_bricks"));
        Assert.Equal("OakStairs", MinecraftRegistryGenerator.ToPascalCase("minecraft:oak_stairs"));
        Assert.Equal("MusicDisc11", MinecraftRegistryGenerator.ToPascalCase("music_disc_11"));
        Assert.Equal("_11", MinecraftRegistryGenerator.ToPascalCase("11"));
    }

    [Fact]
    public void Generator_ParseRegistryJson_SupportsMcmetaFormat()
    {
        string json = """
        {
            "block": ["cobblestone", "stone"],
            "item": ["diamond", "stick"]
        }
        """;

        var parsed = MinecraftRegistryGenerator.ParseRegistryJson(json);
        Assert.Equal(2, parsed.Count);
        Assert.Contains("minecraft:cobblestone", parsed["block"]);
        Assert.Contains("minecraft:stone", parsed["block"]);
        Assert.Contains("minecraft:diamond", parsed["item"]);
    }

    [Fact]
    public void Generator_ParseRegistryJson_SupportsVanillaReportsFormat()
    {
        string json = """
        {
            "minecraft:block": {
                "protocol_id": 1,
                "entries": {
                    "minecraft:cobblestone": { "protocol_id": 0 },
                    "minecraft:stone": { "protocol_id": 1 }
                }
            }
        }
        """;

        var parsed = MinecraftRegistryGenerator.ParseRegistryJson(json);
        Assert.Single(parsed);
        Assert.Contains("minecraft:cobblestone", parsed["minecraft:block"]);
        Assert.Contains("minecraft:stone", parsed["minecraft:block"]);
    }

    [Fact]
    public void Generator_GenerateSourceCode_GeneratesValidSourceCode()
    {
        string json = """
        {
            "block": ["cobblestone", "stone"]
        }
        """;

        string code = MinecraftRegistryGenerator.GenerateSourceCode(json, new GeneratorOptions
        {
            TargetNamespace = "TestNamespace",
            RootClassName = "TestMinecraft",
            MinecraftVersion = "26.3",
            IncludeItems = false,
            IncludeBlockEntities = false,
            IncludeEntities = false,
            IncludeBiomes = false,
            IncludeFluids = false
        });

        Assert.Contains("namespace TestNamespace;", code);
        Assert.Contains("public static partial class TestMinecraft", code);
        Assert.Contains("public static partial class Blocks", code);
        Assert.Contains("public const string Cobblestone = \"minecraft:cobblestone\";", code);
        Assert.Contains("public const string Stone = \"minecraft:stone\";", code);
        Assert.Contains("public static readonly System.Collections.Generic.IReadOnlyList<string> All", code);
        Assert.Contains("public static bool IsValid(string? identifier)", code);
    }

    [Fact]
    public void Generator_GetBundledRegistryJson_LoadsEmbeddedData()
    {
        string json = MinecraftRegistryGenerator.GetBundledRegistryJson();
        Assert.False(string.IsNullOrWhiteSpace(json));

        var parsed = MinecraftRegistryGenerator.ParseRegistryJson(json);
        Assert.Contains("block", parsed.Keys);
        Assert.Contains("item", parsed.Keys);
        Assert.Contains("block_entity_type", parsed.Keys);
        Assert.Contains("entity_type", parsed.Keys);
        Assert.Contains("minecraft:cobblestone", parsed["block"]);
    }
}
