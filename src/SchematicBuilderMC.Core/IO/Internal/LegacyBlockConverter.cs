namespace SchematicBuilderMC.Core.IO.Internal;

/// <summary>
/// Converts between legacy Minecraft block IDs / data values and modern BlockState identifiers.
/// </summary>
internal static class LegacyBlockConverter
{
    private static readonly Dictionary<(int id, byte data), BlockState> LegacyToModern = [];
    private static readonly Dictionary<string, (int id, byte data)> ModernToLegacy = new(StringComparer.OrdinalIgnoreCase);

    static LegacyBlockConverter()
    {
        // Air
        Register(0, 0, "minecraft:air");

        // Stone variants
        Register(1, 0, "minecraft:stone");
        Register(1, 1, "minecraft:granite");
        Register(1, 2, "minecraft:polished_granite");
        Register(1, 3, "minecraft:diorite");
        Register(1, 4, "minecraft:polished_diorite");
        Register(1, 5, "minecraft:andesite");
        Register(1, 6, "minecraft:polished_andesite");

        // Dirt / Grass
        Register(2, 0, "minecraft:grass_block");
        Register(3, 0, "minecraft:dirt");
        Register(3, 1, "minecraft:coarse_dirt");
        Register(3, 2, "minecraft:podzol");
        Register(4, 0, "minecraft:cobblestone");

        // Planks
        Register(5, 0, "minecraft:oak_planks");
        Register(5, 1, "minecraft:spruce_planks");
        Register(5, 2, "minecraft:birch_planks");
        Register(5, 3, "minecraft:jungle_planks");
        Register(5, 4, "minecraft:acacia_planks");
        Register(5, 5, "minecraft:dark_oak_planks");

        // Bedrock
        Register(7, 0, "minecraft:bedrock");

        // Liquids
        Register(8, 0, "minecraft:water");
        Register(9, 0, "minecraft:water");
        Register(10, 0, "minecraft:lava");
        Register(11, 0, "minecraft:lava");

        // Sand / Gravel
        Register(12, 0, "minecraft:sand");
        Register(12, 1, "minecraft:red_sand");
        Register(13, 0, "minecraft:gravel");

        // Ores
        Register(14, 0, "minecraft:gold_ore");
        Register(15, 0, "minecraft:iron_ore");
        Register(16, 0, "minecraft:coal_ore");
        Register(21, 0, "minecraft:lapis_ore");
        Register(56, 0, "minecraft:diamond_ore");
        Register(129, 0, "minecraft:emerald_ore");

        // Logs (Axis not encoded for basic mapping, defaults to y)
        Register(17, 0, "minecraft:oak_log[axis=y]");
        Register(17, 1, "minecraft:spruce_log[axis=y]");
        Register(17, 2, "minecraft:birch_log[axis=y]");
        Register(17, 3, "minecraft:jungle_log[axis=y]");
        Register(162, 0, "minecraft:acacia_log[axis=y]");
        Register(162, 1, "minecraft:dark_oak_log[axis=y]");

        // Leaves
        Register(18, 0, "minecraft:oak_leaves");
        Register(18, 1, "minecraft:spruce_leaves");
        Register(18, 2, "minecraft:birch_leaves");
        Register(18, 3, "minecraft:jungle_leaves");
        Register(161, 0, "minecraft:acacia_leaves");
        Register(161, 1, "minecraft:dark_oak_leaves");

        // Glass & Sponge
        Register(19, 0, "minecraft:sponge");
        Register(19, 1, "minecraft:wet_sponge");
        Register(20, 0, "minecraft:glass");
        Register(22, 0, "minecraft:lapis_block");
        Register(24, 0, "minecraft:sandstone");

        // Wool colors
        string[] colors =
        [
            "white", "orange", "magenta", "light_blue", "yellow", "lime", "pink", "gray",
            "light_gray", "cyan", "purple", "blue", "brown", "green", "red", "black"
        ];
        for (byte c = 0; c < 16; c++)
        {
            Register(35, c, $"minecraft:{colors[c]}_wool");
            Register(159, c, $"minecraft:{colors[c]}_terracotta");
            Register(95, c, $"minecraft:{colors[c]}_stained_glass");
            Register(160, c, $"minecraft:{colors[c]}_stained_glass_pane");
            Register(251, c, $"minecraft:{colors[c]}_concrete");
            Register(252, c, $"minecraft:{colors[c]}_concrete_powder");
        }

        // Metal / Mineral blocks
        Register(41, 0, "minecraft:gold_block");
        Register(42, 0, "minecraft:iron_block");
        Register(45, 0, "minecraft:bricks");
        Register(46, 0, "minecraft:tnt");
        Register(47, 0, "minecraft:bookshelf");
        Register(48, 0, "minecraft:mossy_cobblestone");
        Register(49, 0, "minecraft:obsidian");
        Register(50, 0, "minecraft:torch");
        Register(54, 0, "minecraft:chest");
        Register(57, 0, "minecraft:diamond_block");
        Register(58, 0, "minecraft:crafting_table");
        Register(61, 0, "minecraft:furnace");
        Register(65, 0, "minecraft:ladder");
        Register(66, 0, "minecraft:rail");
        Register(85, 0, "minecraft:oak_fence");
        Register(89, 0, "minecraft:glowstone");
        Register(98, 0, "minecraft:stone_bricks");
        Register(133, 0, "minecraft:emerald_block");
        Register(152, 0, "minecraft:redstone_block");
        Register(155, 0, "minecraft:quartz_block");
        Register(172, 0, "minecraft:terracotta");
        Register(173, 0, "minecraft:coal_block");
        Register(174, 0, "minecraft:packed_ice");
        Register(212, 0, "minecraft:frosted_ice");
        Register(214, 0, "minecraft:nether_wart_block");
        Register(215, 0, "minecraft:red_nether_brick");
        Register(216, 0, "minecraft:bone_block");
    }

    private static void Register(int id, byte data, string modernBlockState)
    {
        var bs = BlockState.Parse(modernBlockState);
        LegacyToModern[(id, data)] = bs;
        // Also register base ID if data is 0
        if (!LegacyToModern.ContainsKey((id, 0)))
        {
            LegacyToModern[(id, 0)] = bs;
        }

        ModernToLegacy[bs.ToString()] = (id, data);
        ModernToLegacy[bs.Identifier] = (id, data);
    }

    public static BlockState ToBlockState(int id, byte data)
    {
        if (LegacyToModern.TryGetValue((id, data), out var state))
            return state;

        if (LegacyToModern.TryGetValue((id, 0), out state))
            return state;

        // Fallback for unmapped legacy blocks
        return new BlockState($"minecraft:legacy_block_{id}_{data}");
    }

    public static (int id, byte data) FromBlockState(BlockState state)
    {
        if (ModernToLegacy.TryGetValue(state.ToString(), out var result))
            return result;

        if (ModernToLegacy.TryGetValue(state.Identifier, out result))
            return result;

        // Check if it's our fallback legacy name
        if (state.Identifier.StartsWith("minecraft:legacy_block_", StringComparison.OrdinalIgnoreCase))
        {
            var parts = state.Identifier.Split('_');
            if (parts.Length >= 4 && int.TryParse(parts[2], out int id) && byte.TryParse(parts[3], out byte data))
            {
                return (id, data);
            }
        }

        // Unknown block defaults to air (or stone if non-air)
        return state.IsAir ? (0, (byte)0) : (1, (byte)0);
    }
}
