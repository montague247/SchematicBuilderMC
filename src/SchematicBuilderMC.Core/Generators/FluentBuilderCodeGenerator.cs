using System.Globalization;
using System.Text;
using System.Text.Json;
using fNbt;
using SchematicBuilderMC.Enums;
using SchematicBuilderMC.IO;
using SchematicBuilderMC.Models;
using SchematicBuilderMC.Registries;

namespace SchematicBuilderMC.Generators;

/// <summary>
/// Generates C# source code for constructing schematics using the fluent <see cref="SchematicBuilder"/> API.
/// </summary>
public static class FluentBuilderCodeGenerator
{
    /// <summary>
    /// Generates C# source code for the specified schematic according to the provided generator options.
    /// </summary>
    /// <param name="schematic">The schematic instance to generate code for.</param>
    /// <param name="options">Options configuring code structure, formatting, and optimization.</param>
    /// <returns>A string containing the generated C# source code.</returns>
    public static string Generate(Schematic schematic, FluentCodeGeneratorOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(schematic);
        options ??= new FluentCodeGeneratorOptions();

        string indentStep = options.IndentString;
        bool needsFNbt = false;

        // Build list of fluent chain calls
        var calls = new List<string>
        {
            $"SchematicBuilder.Create({schematic.Width}, {schematic.Height}, {schematic.Length})"
        };

        // Offset
        if (options.AlwaysIncludeOffset || schematic.Offset != BlockPosition.Zero)
        {
            calls.Add($".WithOffset({schematic.Offset.X}, {schematic.Offset.Y}, {schematic.Offset.Z})");
        }

        // DataVersion
        if (schematic.DataVersion.HasValue)
        {
            calls.Add($".WithDataVersion({schematic.DataVersion.Value})");
        }

        // Metadata
        if (!string.IsNullOrEmpty(schematic.Metadata.Name))
        {
            calls.Add($".WithName(\"{EscapeString(schematic.Metadata.Name)}\")");
        }

        if (!string.IsNullOrEmpty(schematic.Metadata.Author))
        {
            calls.Add($".WithAuthor(\"{EscapeString(schematic.Metadata.Author)}\")");
        }

        if (!string.IsNullOrEmpty(schematic.Metadata.Description))
        {
            calls.Add($".WithDescription(\"{EscapeString(schematic.Metadata.Description)}\")");
        }

        foreach (var mod in schematic.Metadata.RequiredMods)
        {
            calls.Add($".WithRequiredMod(\"{EscapeString(mod)}\")");
        }

        // Block operations
        var handledBlockEntities = new HashSet<BlockPosition>();
        GenerateBlockCalls(schematic, options, calls, handledBlockEntities, ref needsFNbt);

        // Block entities (ordered by position for deterministic output)
        foreach (var be in schematic.BlockEntities
            .Where(b => !handledBlockEntities.Contains(b.Position))
            .OrderBy(b => b.Position.Y).ThenBy(b => b.Position.Z).ThenBy(b => b.Position.X))
        {
            if (IsChest(be.Id) && be.HasItems)
            {
                calls.Add(FormatChestCall(be.Position.X, be.Position.Y, be.Position.Z, be.GetItems(), options, ref needsFNbt));
            }
            else if (be.IsSign)
            {
                var (front, back, isWaxed) = be.GetSignText();
                if (front.Count > 0 || back.Count > 0 || isWaxed)
                {
                    calls.Add(FormatSignCall(be.Position.X, be.Position.Y, be.Position.Z, front, back, isWaxed));
                }
            }
            else if (be.IsDecoratedPot)
            {
                var (item, front, back, left, right) = be.GetDecoratedPotData();
                bool isDefaultSherds = (front == null || front == "minecraft:brick") &&
                                       (back == null || back == "minecraft:brick") &&
                                       (left == null || left == "minecraft:brick") &&
                                       (right == null || right == "minecraft:brick");

                if (item != null || !isDefaultSherds)
                {
                    calls.Add(FormatDecoratedPotCall(be.Position.X, be.Position.Y, be.Position.Z, item, front, back, left, right, options, ref needsFNbt));
                }
            }
            else if (be.IsCrafter)
            {
                var disabled = be.GetDisabledSlots();
                var items = be.GetItems();
                if (disabled.Count > 0 || items.Count > 0)
                {
                    calls.Add(FormatCrafterCall(be.Position.X, be.Position.Y, be.Position.Z, disabled, items, options, ref needsFNbt));
                }
            }
            else if (be.HasItems)
            {
                var block = schematic[be.Position.X, be.Position.Y, be.Position.Z];
                string blockId = block.IsAir ? be.Id : block.Identifier;
                calls.Add(FormatContainerCall(blockId, be.Id, be.Position.X, be.Position.Y, be.Position.Z, be.GetItems(), options, ref needsFNbt));
            }
            else if (HasCustomPayloadData(be) || be.Data.Count == 0)
            {
                bool hasCustom = HasCustomPayloadData(be);
                if (hasCustom) needsFNbt = true;
                string beCode = FormatBlockEntity(be, options, hasCustomData: hasCustom, indentStep);
                calls.Add($".AddBlockEntity({beCode})");
            }
        }

        // Entities (ordered by position for deterministic output)
        foreach (var ent in schematic.Entities.OrderBy(e => e.Y).ThenBy(e => e.Z).ThenBy(e => e.X))
        {
            bool hasCustomData = HasCustomPayloadData(ent);
            if (hasCustomData)
            {
                needsFNbt = true;
            }

            string entCode = FormatEntity(ent, options, hasCustomData, indentStep);
            calls.Add($".AddEntity({entCode})");
        }

        // Finalize builder chain
        calls.Add(".Build()");

        // Format code according to requested mode
        return FormatOutput(calls, options, needsFNbt);
    }

    /// <summary>
    /// Reads a schematic file from disk and generates C# fluent builder code.
    /// </summary>
    /// <param name="filePath">Path to the schematic file (.schem, .schematic).</param>
    /// <param name="options">Generator options.</param>
    /// <param name="format">Schematic format (default: <see cref="SchematicFormat.Auto"/>).</param>
    /// <returns>A string containing the generated C# code.</returns>
    public static string GenerateFromFile(string filePath, FluentCodeGeneratorOptions? options = null, SchematicFormat format = SchematicFormat.Auto)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Schematic file not found: {filePath}", filePath);
        }

        var schematic = SchematicFile.Read(filePath, format);
        return Generate(schematic, options);
    }

    /// <summary>
    /// Generates C# fluent builder source code for a schematic and writes it to a file.
    /// </summary>
    /// <param name="schematic">The schematic instance.</param>
    /// <param name="outputPath">The file path where generated C# code will be saved.</param>
    /// <param name="options">Generator options.</param>
    public static void GenerateToFile(Schematic schematic, string outputPath, FluentCodeGeneratorOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        options ??= new FluentCodeGeneratorOptions();

        // If user didn't explicitly customize mode, File mode is preferred when saving to disk
        if (options.Mode == CodeGenerationMode.Statement)
        {
            options.Mode = CodeGenerationMode.File;
        }

        string? dir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        string code = Generate(schematic, options);
        File.WriteAllText(outputPath, code, Encoding.UTF8);
    }

    /// <summary>
    /// Reads a schematic file from disk and writes the generated fluent builder C# code to an output file.
    /// </summary>
    /// <param name="inputFilePath">Path to the input schematic file.</param>
    /// <param name="outputFilePath">Path to the destination C# file.</param>
    /// <param name="options">Generator options.</param>
    /// <param name="format">Schematic format (default: <see cref="SchematicFormat.Auto"/>).</param>
    public static void GenerateFromFileToFile(string inputFilePath, string outputFilePath, FluentCodeGeneratorOptions? options = null, SchematicFormat format = SchematicFormat.Auto)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputFilePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputFilePath);

        if (!File.Exists(inputFilePath))
        {
            throw new FileNotFoundException($"Input schematic file not found: {inputFilePath}", inputFilePath);
        }

        var schematic = SchematicFile.Read(inputFilePath, format);
        GenerateToFile(schematic, outputFilePath, options);
    }

    private static void GenerateBlockCalls(
        Schematic schematic,
        FluentCodeGeneratorOptions options,
        List<string> calls,
        HashSet<BlockPosition> handledBlockEntities,
        ref bool needsFNbt)
    {
        int w = schematic.Width;
        int h = schematic.Height;
        int l = schematic.Length;

        // Special case: check if entire schematic consists of a single identical block state
        if (options.OptimizeFills)
        {
            var firstBlock = schematic[0, 0, 0];
            if (!options.OmitAir || !firstBlock.IsAir)
            {
                bool allIdentical = true;
                for (int y = 0; y < h && allIdentical; y++)
                {
                    for (int z = 0; z < l && allIdentical; z++)
                    {
                        for (int x = 0; x < w; x++)
                        {
                            if (schematic[x, y, z] != firstBlock)
                            {
                                allIdentical = false;
                                break;
                            }
                        }
                    }
                }

                if (allIdentical)
                {
                    calls.Add($".FillAll({FormatBlockState(firstBlock, options)})");
                    return;
                }
            }
        }

        if (!options.OptimizeFills)
        {
            // Simple non-optimized block emitting
            for (int y = 0; y < h; y++)
            {
                for (int z = 0; z < l; z++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        var block = schematic[x, y, z];
                        if (options.OmitAir && block.IsAir) continue;

                        string blockCode = FormatBlockState(block, options);
                        var be = schematic.GetBlockEntity(x, y, z);
                        if (be != null && TryGenerateSpecialBlockEntityCall(schematic, be, block, blockCode, options, calls, ref needsFNbt))
                        {
                            handledBlockEntities.Add(be.Position);
                        }
                        else
                        {
                            calls.Add($".SetBlock({x}, {y}, {z}, {blockCode})");
                        }
                    }
                }
            }
            return;
        }

        // Greedy 3D Cuboid / Box Decomposition
        bool[,,] covered = new bool[w, h, l];

        if (options.OmitAir)
        {
            for (int y = 0; y < h; y++)
            {
                for (int z = 0; z < l; z++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        if (schematic[x, y, z].IsAir)
                        {
                            covered[x, y, z] = true;
                        }
                    }
                }
            }
        }

        // Permutations of axis expansion (0=X, 1=Y, 2=Z)
        int[][] permutations =
        [
            [0, 2, 1], // X then Z then Y (great for floors and slabs)
            [0, 1, 2], // X then Y then Z
            [2, 0, 1], // Z then X then Y
            [2, 1, 0], // Z then Y then X
            [1, 0, 2], // Y then X then Z (great for columns and walls)
            [1, 2, 0]  // Y then Z then X
        ];

        for (int y = 0; y < h; y++)
        {
            for (int z = 0; z < l; z++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (covered[x, y, z]) continue;

                    var target = schematic[x, y, z];
                    int bestMaxX = x;
                    int bestMaxY = y;
                    int bestMaxZ = z;
                    int bestVolume = 1;

                    foreach (var perm in permutations)
                    {
                        var (mx, my, mz, vol) = ExpandBox(schematic, covered, x, y, z, target, perm[0], perm[1], perm[2]);
                        if (vol > bestVolume)
                        {
                            bestVolume = vol;
                            bestMaxX = mx;
                            bestMaxY = my;
                            bestMaxZ = mz;
                        }
                    }

                    // Mark best box as covered
                    for (int py = y; py <= bestMaxY; py++)
                    {
                        for (int pz = z; pz <= bestMaxZ; pz++)
                        {
                            for (int px = x; px <= bestMaxX; px++)
                            {
                                covered[px, py, pz] = true;
                            }
                        }
                    }

                    string blockCode = FormatBlockState(target, options);
                    if (bestVolume > 1)
                    {
                        calls.Add($".Fill({x}, {y}, {z}, {bestMaxX}, {bestMaxY}, {bestMaxZ}, {blockCode})");
                    }
                    else
                    {
                        var be = schematic.GetBlockEntity(x, y, z);
                        if (be != null && TryGenerateSpecialBlockEntityCall(schematic, be, target, blockCode, options, calls, ref needsFNbt))
                        {
                            handledBlockEntities.Add(be.Position);
                        }
                        else
                        {
                            calls.Add($".SetBlock({x}, {y}, {z}, {blockCode})");
                        }
                    }
                }
            }
        }
    }

    private static (int maxX, int maxY, int maxZ, int volume) ExpandBox(
        Schematic schematic,
        bool[,,] covered,
        int x0, int y0, int z0,
        BlockState target,
        int axis1, int axis2, int axis3)
    {
        int w = schematic.Width;
        int h = schematic.Height;
        int l = schematic.Length;

        int maxX = x0;
        int maxY = y0;
        int maxZ = z0;

        ExpandAxis(axis1);
        ExpandAxis(axis2);
        ExpandAxis(axis3);

        int volume = (maxX - x0 + 1) * (maxY - y0 + 1) * (maxZ - z0 + 1);
        return (maxX, maxY, maxZ, volume);

        void ExpandAxis(int axis)
        {
            switch (axis)
            {
                case 0: // Expand X
                    while (maxX + 1 < w)
                    {
                        bool ok = true;
                        for (int y = y0; y <= maxY; y++)
                        {
                            for (int z = z0; z <= maxZ; z++)
                            {
                                if (covered[maxX + 1, y, z] || schematic[maxX + 1, y, z] != target)
                                {
                                    ok = false;
                                    break;
                                }
                            }
                            if (!ok) break;
                        }
                        if (!ok) break;
                        maxX++;
                    }
                    break;

                case 1: // Expand Y
                    while (maxY + 1 < h)
                    {
                        bool ok = true;
                        for (int x = x0; x <= maxX; x++)
                        {
                            for (int z = z0; z <= maxZ; z++)
                            {
                                if (covered[x, maxY + 1, z] || schematic[x, maxY + 1, z] != target)
                                {
                                    ok = false;
                                    break;
                                }
                            }
                            if (!ok) break;
                        }
                        if (!ok) break;
                        maxY++;
                    }
                    break;

                case 2: // Expand Z
                    while (maxZ + 1 < l)
                    {
                        bool ok = true;
                        for (int y = y0; y <= maxY; y++)
                        {
                            for (int x = x0; x <= maxX; x++)
                            {
                                if (covered[x, y, maxZ + 1] || schematic[x, y, maxZ + 1] != target)
                                {
                                    ok = false;
                                    break;
                                }
                            }
                            if (!ok) break;
                        }
                        if (!ok) break;
                        maxZ++;
                    }
                    break;
            }
        }
    }

    /// <summary>
    /// Formats a <see cref="BlockState"/> into C# code according to generator options.
    /// </summary>
    public static string FormatBlockState(BlockState blockState, FluentCodeGeneratorOptions options)
    {
        if (options.UseStronglyTypedBlocks && Minecraft.Blocks.IsValid(blockState.Identifier))
        {
            string pascalName = MinecraftRegistryGenerator.ToPascalCase(blockState.Identifier);
            string baseCode = $"Minecraft.Blocks.{pascalName}";

            if (blockState.Properties.Count == 0)
            {
                return baseCode;
            }

            var sb = new StringBuilder(baseCode);
            foreach (var kvp in blockState.Properties.OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase))
            {
                sb.Append(FormatPropertyCall(blockState.Identifier, kvp.Key, kvp.Value, options.UseSemanticPropertyMethods));
            }
            return sb.ToString();
        }

        if (blockState.Properties.Count == 0)
        {
            return $"\"{EscapeString(blockState.Identifier)}\"";
        }

        if (options.UseStronglyTypedBlocks)
        {
            // Modded or non-registry block with properties
            var sb = new StringBuilder($"\"{EscapeString(blockState.Identifier)}\"");
            foreach (var kvp in blockState.Properties.OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase))
            {
                sb.Append(FormatPropertyCall(blockState.Identifier, kvp.Key, kvp.Value, options.UseSemanticPropertyMethods));
            }
            return sb.ToString();
        }

        // Canonical string literal
        return $"\"{EscapeString(blockState.ToString())}\"";
    }

    private static string FormatPropertyCall(string identifier, string key, string value, bool useSemantic)
    {
        if (!useSemantic)
        {
            return $".WithProperty(\"{EscapeString(key)}\", \"{EscapeString(value)}\")";
        }

        switch (key.ToLowerInvariant())
        {
            case "delay":
                if (int.TryParse(value, out int delay))
                    return $".WithDelay({delay})";
                break;

            case "mode":
                if (value.Equals("subtract", StringComparison.OrdinalIgnoreCase))
                    return ".WithSubtractMode()";
                if (value.Equals("compare", StringComparison.OrdinalIgnoreCase))
                    return ".WithMode(\"compare\")";
                return $".WithMode(\"{EscapeString(value)}\")";

            case "face":
                return $".WithFace(\"{EscapeString(value)}\")";

            case "facing":
                return $".WithFacing(\"{EscapeString(value)}\")";

            case "powered":
                if (bool.TryParse(value, out bool pow))
                    return $".WithPowered({(pow ? "true" : "false")})";
                break;

            case "power":
                if (int.TryParse(value, out int power))
                    return $".WithPower({power})";
                break;

            case "waterlogged":
                if (bool.TryParse(value, out bool wl))
                    return $".WithWaterlogged({(wl ? "true" : "false")})";
                break;

            case "open":
                if (bool.TryParse(value, out bool op))
                    return $".WithOpen({(op ? "true" : "false")})";
                break;

            case "lit":
                if (bool.TryParse(value, out bool lit))
                    return $".WithLit({(lit ? "true" : "false")})";
                break;

            case "half":
                return $".WithHalf(\"{EscapeString(value)}\")";

            case "axis":
                return $".WithAxis(\"{EscapeString(value)}\")";

            case "locked":
                if (bool.TryParse(value, out bool locked))
                    return $".WithLocked({(locked ? "true" : "false")})";
                break;

            case "extended":
                if (bool.TryParse(value, out bool ext))
                    return $".WithExtended({(ext ? "true" : "false")})";
                break;

            case "hinge":
                return $".WithHinge(\"{EscapeString(value)}\")";

            case "in_wall":
                if (bool.TryParse(value, out bool inWall))
                    return $".WithInWall({(inWall ? "true" : "false")})";
                break;

            case "level":
                if (int.TryParse(value, out int level))
                    return $".WithLevel({level})";
                break;

            case "shape":
                return $".WithShape(\"{EscapeString(value)}\")";

            case "type":
                if (identifier.Contains("chest", StringComparison.OrdinalIgnoreCase))
                    return $".WithChestType(\"{EscapeString(value)}\")";
                if (identifier.Contains("slab", StringComparison.OrdinalIgnoreCase))
                    return $".WithSlabType(\"{EscapeString(value)}\")";
                return $".WithType(\"{EscapeString(value)}\")";
        }

        return $".WithProperty(\"{EscapeString(key)}\", \"{EscapeString(value)}\")";
    }

    private static bool IsChest(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return false;
        return id.Equals("minecraft:chest", StringComparison.OrdinalIgnoreCase) ||
               id.Equals("chest", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryGenerateSpecialBlockEntityCall(
        Schematic schematic,
        BlockEntity be,
        BlockState block,
        string blockCode,
        FluentCodeGeneratorOptions options,
        List<string> calls,
        ref bool needsFNbt)
    {
        int x = be.Position.X;
        int y = be.Position.Y;
        int z = be.Position.Z;

        if (IsChest(be.Id) && be.HasItems)
        {
            if (block.Properties.Count > 0)
            {
                calls.Add($".SetBlock({x}, {y}, {z}, {blockCode})");
            }
            calls.Add(FormatChestCall(x, y, z, be.GetItems(), options, ref needsFNbt));
            return true;
        }

        if (be.IsSign)
        {
            var (front, back, isWaxed) = be.GetSignText();
            if (front.Count > 0 || back.Count > 0 || isWaxed)
            {
                if (block.Properties.Count > 0)
                {
                    calls.Add($".SetBlock({x}, {y}, {z}, {blockCode})");
                }
                calls.Add(FormatSignCall(x, y, z, front, back, isWaxed));
                return true;
            }
        }

        if (be.IsDecoratedPot)
        {
            var (item, front, back, left, right) = be.GetDecoratedPotData();
            bool isDefaultSherds = (front == null || front == "minecraft:brick") &&
                                   (back == null || back == "minecraft:brick") &&
                                   (left == null || left == "minecraft:brick") &&
                                   (right == null || right == "minecraft:brick");
            if (item != null || !isDefaultSherds)
            {
                if (block.Properties.Count > 0)
                {
                    calls.Add($".SetBlock({x}, {y}, {z}, {blockCode})");
                }
                calls.Add(FormatDecoratedPotCall(x, y, z, item, front, back, left, right, options, ref needsFNbt));
                return true;
            }
        }

        if (be.IsCrafter)
        {
            var disabled = be.GetDisabledSlots();
            var items = be.GetItems();
            if (disabled.Count > 0 || items.Count > 0)
            {
                if (block.Properties.Count > 0)
                {
                    calls.Add($".SetBlock({x}, {y}, {z}, {blockCode})");
                }
                calls.Add(FormatCrafterCall(x, y, z, disabled, items, options, ref needsFNbt));
                return true;
            }
        }

        if (be.HasItems)
        {
            if (block.Properties.Count > 0)
            {
                calls.Add($".SetBlock({x}, {y}, {z}, {blockCode})");
            }
            calls.Add(FormatContainerCall(block.Identifier, be.Id, x, y, z, be.GetItems(), options, ref needsFNbt));
            return true;
        }

        return false;
    }

    private static string FormatSignCall(int x, int y, int z, IReadOnlyList<string> front, IReadOnlyList<string> back, bool isWaxed)
    {
        if (back.Count == 0 && !isWaxed)
        {
            string frontArgs = string.Join(", ", front.Select(m => $"\"{EscapeString(m)}\""));
            return $".SetSign({x}, {y}, {z}, {frontArgs})";
        }

        string fList = front.Count > 0
            ? $"new[] {{ {string.Join(", ", front.Select(m => $"\"{EscapeString(m)}\""))} }}"
            : "Array.Empty<string>()";

        string bList = back.Count > 0
            ? $", new[] {{ {string.Join(", ", back.Select(m => $"\"{EscapeString(m)}\""))} }}"
            : "";

        string wax = isWaxed ? ", isWaxed: true" : "";
        return $".SetSign({x}, {y}, {z}, {fList}{bList}{wax})";
    }

    private static string FormatDecoratedPotCall(
        int x, int y, int z,
        ItemStack? item,
        string? frontSherd, string? backSherd, string? leftSherd, string? rightSherd,
        FluentCodeGeneratorOptions options,
        ref bool needsFNbt)
    {
        string? itemStr = item != null ? FormatItemStack(item, options, ref needsFNbt, includeSlot: false) : null;
        bool isDefaultSherds = (frontSherd == null || frontSherd == "minecraft:brick") &&
                               (backSherd == null || backSherd == "minecraft:brick") &&
                               (leftSherd == null || leftSherd == "minecraft:brick") &&
                               (rightSherd == null || rightSherd == "minecraft:brick");

        if (isDefaultSherds)
        {
            return $".SetDecoratedPot({x}, {y}, {z}, {itemStr ?? "null"})";
        }

        string FormatSherdVal(string s)
        {
            if (options.UseStronglyTypedItems && Minecraft.Items.IsValid(s))
                return $"Minecraft.Items.{MinecraftRegistryGenerator.ToPascalCase(s)}";
            return $"\"{EscapeString(s)}\"";
        }

        var args = new List<string>();
        if (itemStr != null)
        {
            args.Add(itemStr);
        }

        if (frontSherd != null && frontSherd != "minecraft:brick")
            args.Add($"frontSherd: {FormatSherdVal(frontSherd)}");
        if (backSherd != null && backSherd != "minecraft:brick")
            args.Add($"backSherd: {FormatSherdVal(backSherd)}");
        if (leftSherd != null && leftSherd != "minecraft:brick")
            args.Add($"leftSherd: {FormatSherdVal(leftSherd)}");
        if (rightSherd != null && rightSherd != "minecraft:brick")
            args.Add($"rightSherd: {FormatSherdVal(rightSherd)}");

        return $".SetDecoratedPot({x}, {y}, {z}, {string.Join(", ", args)})";
    }

    private static string FormatCrafterCall(
        int x, int y, int z,
        IReadOnlyList<int> disabledSlots,
        IReadOnlyList<ItemStack> items,
        FluentCodeGeneratorOptions options,
        ref bool needsFNbt)
    {
        if (items.Count == 0 && disabledSlots.Count > 0)
        {
            return $".SetCrafter({x}, {y}, {z}, {string.Join(", ", disabledSlots)})";
        }

        if (disabledSlots.Count == 0 && items.Count > 0)
        {
            string blockIdStr = options.UseStronglyTypedBlocks
                ? "Minecraft.Blocks.Crafter"
                : "\"minecraft:crafter\"";
            string beIdStr = options.UseStronglyTypedEntities
                ? "Minecraft.BlockEntities.Crafter"
                : "\"minecraft:crafter\"";
            return FormatContainerCall(blockIdStr, beIdStr, x, y, z, items, options, ref needsFNbt);
        }

        string disabledExpr = $"new[] {{ {string.Join(", ", disabledSlots)} }}";
        var lines = new List<string>(items.Count + 1)
        {
            $".SetCrafter({x}, {y}, {z}, {disabledExpr},"
        };

        var sortedItems = items.OrderBy(i => i.Slot).ToList();
        for (int i = 0; i < sortedItems.Count; i++)
        {
            string itemStr = FormatItemStack(sortedItems[i], options, ref needsFNbt);
            bool isLast = i == sortedItems.Count - 1;
            lines.Add(isLast ? $"{itemStr})" : $"{itemStr},");
        }

        return string.Join("\n", lines);
    }

    private static string FormatChestCall(
        int x, int y, int z,
        IReadOnlyList<ItemStack> items,
        FluentCodeGeneratorOptions options,
        ref bool needsFNbt)
    {
        if (items == null || items.Count == 0)
        {
            return $".SetChest({x}, {y}, {z})";
        }

        var lines = new List<string>(items.Count + 1)
        {
            $".SetChest({x}, {y}, {z},"
        };

        var sortedItems = items.OrderBy(i => i.Slot).ToList();

        for (int i = 0; i < sortedItems.Count; i++)
        {
            string itemStr = FormatItemStack(sortedItems[i], options, ref needsFNbt);
            bool isLast = i == sortedItems.Count - 1;
            lines.Add(isLast ? $"{itemStr})" : $"{itemStr},");
        }

        return string.Join("\n", lines);
    }

    private static string FormatContainerCall(
        string blockId,
        string blockEntityId,
        int x, int y, int z,
        IReadOnlyList<ItemStack> items,
        FluentCodeGeneratorOptions options,
        ref bool needsFNbt)
    {
        string blockIdStr = options.UseStronglyTypedBlocks && Minecraft.Blocks.IsValid(blockId)
            ? $"Minecraft.Blocks.{MinecraftRegistryGenerator.ToPascalCase(blockId)}"
            : $"\"{EscapeString(blockId)}\"";

        string beIdStr = options.UseStronglyTypedEntities && Minecraft.BlockEntities.IsValid(blockEntityId)
            ? $"Minecraft.BlockEntities.{MinecraftRegistryGenerator.ToPascalCase(blockEntityId)}"
            : $"\"{EscapeString(blockEntityId)}\"";

        if (items == null || items.Count == 0)
        {
            return $".SetContainer({blockIdStr}, {beIdStr}, {x}, {y}, {z})";
        }

        var lines = new List<string>(items.Count + 1)
        {
            $".SetContainer({blockIdStr}, {beIdStr}, {x}, {y}, {z},"
        };

        var sortedItems = items.OrderBy(i => i.Slot).ToList();

        for (int i = 0; i < sortedItems.Count; i++)
        {
            string itemStr = FormatItemStack(sortedItems[i], options, ref needsFNbt);
            bool isLast = i == sortedItems.Count - 1;
            lines.Add(isLast ? $"{itemStr})" : $"{itemStr},");
        }

        return string.Join("\n", lines);
    }

    private static string FormatItemStack(ItemStack item, FluentCodeGeneratorOptions options, ref bool needsFNbt, bool includeSlot = true)
    {
        string itemIdCode;
        if (options.UseStronglyTypedItems && Minecraft.Items.IsValid(item.Id))
        {
            itemIdCode = $"Minecraft.Items.{MinecraftRegistryGenerator.ToPascalCase(item.Id)}";
        }
        else
        {
            itemIdCode = $"\"{EscapeString(item.Id)}\"";
        }

        var sb = new StringBuilder();
        if (includeSlot)
        {
            sb.Append($"new ItemStack({itemIdCode}, count: {item.Count}, slot: {item.Slot})");
        }
        else if (item.Count > 1)
        {
            sb.Append($"new ItemStack({itemIdCode}, count: {item.Count})");
        }
        else
        {
            sb.Append($"new ItemStack({itemIdCode})");
        }

        if (item.CustomData == null || item.CustomData.Count == 0)
        {
            return sb.ToString();
        }

        var custom = (NbtCompound)item.CustomData.Clone();

        // 1. Display Name & Lore
        if (custom["display"] is NbtCompound display)
        {
            if (display["Name"] is NbtString nameTag)
            {
                string rawName = nameTag.StringValue;
                string displayName = ExtractTextFromTextComponent(rawName);
                sb.Append($".WithDisplayName(\"{EscapeString(displayName)}\")");
                display.Remove("Name");
            }

            if (display["Lore"] is NbtList loreList && loreList.ListType == NbtTagType.String && loreList.Count > 0)
            {
                var loreLines = new List<string>();
                foreach (var tag in loreList)
                {
                    if (tag is NbtString lineTag)
                    {
                        loreLines.Add(ExtractTextFromTextComponent(lineTag.StringValue));
                    }
                }
                if (loreLines.Count > 0)
                {
                    string loreArgs = string.Join(", ", loreLines.Select(l => $"\"{EscapeString(l)}\""));
                    sb.Append($".WithLore({loreArgs})");
                }
                display.Remove("Lore");
            }

            if (display.Count == 0)
            {
                custom.Remove("display");
            }
        }

        // Modern 1.20.5+ / 26.3 components support
        if (custom["minecraft:custom_name"] is NbtString compNameTag)
        {
            string displayName = ExtractTextFromTextComponent(compNameTag.StringValue);
            sb.Append($".WithDisplayName(\"{EscapeString(displayName)}\")");
            custom.Remove("minecraft:custom_name");
        }

        if (custom["minecraft:lore"] is NbtList compLoreList && compLoreList.Count > 0)
        {
            var loreLines = new List<string>();
            foreach (var tag in compLoreList)
            {
                if (tag is NbtString lineTag)
                {
                    loreLines.Add(ExtractTextFromTextComponent(lineTag.StringValue));
                }
            }
            if (loreLines.Count > 0)
            {
                string loreArgs = string.Join(", ", loreLines.Select(l => $"\"{EscapeString(l)}\""));
                sb.Append($".WithLore({loreArgs})");
            }
            custom.Remove("minecraft:lore");
        }

        // 2. Enchantments
        if (custom["Enchantments"] is NbtList enchList && enchList.ListType == NbtTagType.Compound)
        {
            foreach (var tag in enchList)
            {
                if (tag is NbtCompound enchComp)
                {
                    string? enchId = enchComp["id"]?.StringValue;
                    short lvl = enchComp["lvl"]?.ShortValue ?? 1;
                    if (!string.IsNullOrEmpty(enchId))
                    {
                        sb.Append($".WithEnchantment(\"{EscapeString(enchId)}\", {lvl})");
                    }
                }
            }
            custom.Remove("Enchantments");
        }
        else if (custom["StoredEnchantments"] is NbtList storedList && storedList.ListType == NbtTagType.Compound)
        {
            foreach (var tag in storedList)
            {
                if (tag is NbtCompound enchComp)
                {
                    string? enchId = enchComp["id"]?.StringValue;
                    short lvl = enchComp["lvl"]?.ShortValue ?? 1;
                    if (!string.IsNullOrEmpty(enchId))
                    {
                        sb.Append($".WithEnchantment(\"{EscapeString(enchId)}\", {lvl})");
                    }
                }
            }
            custom.Remove("StoredEnchantments");
        }

        // 3. Durability Damage
        if (custom["Damage"] is NbtInt dmgInt)
        {
            sb.Append($".WithDamage({dmgInt.Value})");
            custom.Remove("Damage");
        }
        else if (custom["Damage"] is NbtShort dmgShort)
        {
            sb.Append($".WithDamage({dmgShort.Value})");
            custom.Remove("Damage");
        }
        else if (custom["minecraft:damage"] is NbtInt compDmgInt)
        {
            sb.Append($".WithDamage({compDmgInt.Value})");
            custom.Remove("minecraft:damage");
        }

        // 4. Remaining unhandled custom data
        if (custom.Count > 0)
        {
            needsFNbt = true;
            sb.Append($".WithCustomData({FormatNbtCompound(custom, options.IndentString)})");
        }

        return sb.ToString();
    }

    private static string ExtractTextFromTextComponent(string jsonOrText)
    {
        if (string.IsNullOrWhiteSpace(jsonOrText)) return "";

        if (!jsonOrText.StartsWith('{') || !jsonOrText.EndsWith('}'))
        {
            return jsonOrText;
        }

        try
        {
            using var doc = JsonDocument.Parse(jsonOrText);
            if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                doc.RootElement.TryGetProperty("text", out var textProp))
            {
                int count = 0;
                foreach (var _ in doc.RootElement.EnumerateObject()) count++;
                if (count == 1)
                {
                    return textProp.GetString() ?? jsonOrText;
                }
            }
        }
        catch
        {
            // Fallback to raw string if parsing fails
        }

        return jsonOrText;
    }

    private static string FormatBlockEntity(BlockEntity be, FluentCodeGeneratorOptions options, bool hasCustomData, string indentStep)
    {
        string idStr = options.UseStronglyTypedEntities && Minecraft.BlockEntities.IsValid(be.Id)
            ? $"Minecraft.BlockEntities.{MinecraftRegistryGenerator.ToPascalCase(be.Id)}"
            : $"\"{EscapeString(be.Id)}\"";

        string posStr = $"new BlockPosition({be.Position.X}, {be.Position.Y}, {be.Position.Z})";

        if (!hasCustomData)
        {
            return $"new BlockEntity({idStr}, {posStr})";
        }

        string dataCode = FormatNbtCompound(be.Data, indentStep, level: 1, be.Id);
        if (dataCode.Contains('\n'))
        {
            return $"new BlockEntity(\n{indentStep}{idStr},\n{indentStep}{posStr},\n{indentStep}{dataCode})";
        }

        return $"new BlockEntity({idStr}, {posStr}, {dataCode})";
    }

    private static string FormatEntity(Entity ent, FluentCodeGeneratorOptions options, bool hasCustomData, string indentStep)
    {
        string idStr = options.UseStronglyTypedEntities && Minecraft.Entities.IsValid(ent.Id)
            ? $"Minecraft.Entities.{MinecraftRegistryGenerator.ToPascalCase(ent.Id)}"
            : $"\"{EscapeString(ent.Id)}\"";

        string coords = $"{FormatDouble(ent.X)}, {FormatDouble(ent.Y)}, {FormatDouble(ent.Z)}";

        if (!hasCustomData)
        {
            return $"new Entity({idStr}, {coords})";
        }

        string dataCode = FormatNbtCompound(ent.Data, indentStep, level: 1);
        if (dataCode.Contains('\n'))
        {
            return $"new Entity(\n{indentStep}{idStr},\n{indentStep}{coords},\n{indentStep}{dataCode})";
        }

        return $"new Entity({idStr}, {coords}, {dataCode})";
    }

    private static bool HasCustomPayloadData(BlockEntity be)
    {
        if (be.Data == null || be.Data.Count == 0) return false;
        foreach (var tag in be.Data)
        {
            string name = tag.Name ?? "";
            if (name.Equals("id", StringComparison.OrdinalIgnoreCase)) continue;
            if (name.Equals("x", StringComparison.OrdinalIgnoreCase)) continue;
            if (name.Equals("y", StringComparison.OrdinalIgnoreCase)) continue;
            if (name.Equals("z", StringComparison.OrdinalIgnoreCase)) continue;
            if (name.Equals("Pos", StringComparison.OrdinalIgnoreCase)) continue;

            // Empty components compound
            if (name.Equals("components", StringComparison.OrdinalIgnoreCase) && tag is NbtCompound { Count: 0 })
                continue;

            // Empty Items list
            if (name.Equals("Items", StringComparison.OrdinalIgnoreCase) && tag is NbtList { Count: 0 })
                continue;

            // Default TransferCooldown on hopper/dispenser/dropper
            if (name.Equals("TransferCooldown", StringComparison.OrdinalIgnoreCase) && tag is NbtInt { Value: 0 })
                continue;

            // Comparator output signal (transient in-game calculation)
            if (name.Equals("OutputSignal", StringComparison.OrdinalIgnoreCase) &&
                be.Id.Equals("minecraft:comparator", StringComparison.OrdinalIgnoreCase))
                continue;

            // Default crafter state
            if (name.Equals("crafting_ticks_remaining", StringComparison.OrdinalIgnoreCase) && tag is NbtInt { Value: 0 })
                continue;
            if (name.Equals("triggered", StringComparison.OrdinalIgnoreCase) && tag is NbtByte { Value: 0 })
                continue;
            if (name.Equals("disabled_slots", StringComparison.OrdinalIgnoreCase) &&
                tag is NbtIntArray { Value.Length: 9 } dia &&
                dia.Value.SequenceEqual([0, 1, 2, 3, 4, 5, 6, 7, 8]))
                continue;

            // Sign text handled by FormatSignCall
            if (be.IsSign && (name.Equals("front_text", StringComparison.OrdinalIgnoreCase) ||
                              name.Equals("back_text", StringComparison.OrdinalIgnoreCase) ||
                              name.Equals("is_waxed", StringComparison.OrdinalIgnoreCase) ||
                              name.StartsWith("Text", StringComparison.OrdinalIgnoreCase)))
                continue;

            // Decorated pot item and sherds handled by FormatDecoratedPotCall
            if (be.IsDecoratedPot && (name.Equals("item", StringComparison.OrdinalIgnoreCase) ||
                                      name.Equals("sherds", StringComparison.OrdinalIgnoreCase)))
                continue;

            return true;
        }
        return false;
    }

    private static bool HasCustomPayloadData(Entity ent)
    {
        if (ent.Data == null || ent.Data.Count == 0) return false;
        foreach (var tag in ent.Data)
        {
            string name = tag.Name ?? "";
            if (name.Equals("id", StringComparison.OrdinalIgnoreCase)) continue;
            if (name.Equals("Pos", StringComparison.OrdinalIgnoreCase)) continue;
            if (name.Equals("x", StringComparison.OrdinalIgnoreCase)) continue;
            if (name.Equals("y", StringComparison.OrdinalIgnoreCase)) continue;
            if (name.Equals("z", StringComparison.OrdinalIgnoreCase)) continue;
            return true;
        }
        return false;
    }

    private static List<NbtTag> GetMeaningfulTags(NbtCompound compound, string? blockEntityId = null)
    {
        return compound
            .Where(t =>
            {
                string n = t.Name ?? "";
                if (n.Equals("id", StringComparison.OrdinalIgnoreCase)) return false;
                if (n.Equals("x", StringComparison.OrdinalIgnoreCase)) return false;
                if (n.Equals("y", StringComparison.OrdinalIgnoreCase)) return false;
                if (n.Equals("z", StringComparison.OrdinalIgnoreCase)) return false;
                if (n.Equals("Pos", StringComparison.OrdinalIgnoreCase)) return false;

                if (n.Equals("components", StringComparison.OrdinalIgnoreCase) && t is NbtCompound { Count: 0 })
                    return false;

                if (n.Equals("Items", StringComparison.OrdinalIgnoreCase) && t is NbtList { Count: 0 })
                    return false;

                if (n.Equals("TransferCooldown", StringComparison.OrdinalIgnoreCase) && t is NbtInt { Value: 0 })
                    return false;

                if (n.Equals("OutputSignal", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(blockEntityId, "minecraft:comparator", StringComparison.OrdinalIgnoreCase))
                    return false;

                return true;
            })
            .ToList();
    }

    private static string FormatNbtCompound(NbtCompound compound, string indentStep, int level = 0, string? blockEntityId = null)
    {
        var tags = GetMeaningfulTags(compound, blockEntityId);

        if (tags.Count == 0)
        {
            return "new NbtCompound()";
        }

        if (tags.Count == 1 && IsSimplePrimitive(tags[0]))
        {
            return $"new NbtCompound {{ {FormatNbtTag(tags[0], indentStep, level)} }}";
        }

        string baseIndent = string.Concat(Enumerable.Repeat(indentStep, level));
        string childIndent = string.Concat(Enumerable.Repeat(indentStep, level + 1));

        var sb = new StringBuilder();
        sb.AppendLine("new NbtCompound");
        sb.AppendLine($"{baseIndent}{{");
        for (int i = 0; i < tags.Count; i++)
        {
            sb.Append($"{childIndent}{FormatNbtTag(tags[i], indentStep, level + 1)}");
            if (i < tags.Count - 1) sb.Append(',');
            sb.AppendLine();
        }
        sb.Append($"{baseIndent}}}");
        return sb.ToString();
    }

    private static bool IsSimplePrimitive(NbtTag tag) => tag.TagType switch
    {
        NbtTagType.Byte or NbtTagType.Short or NbtTagType.Int or NbtTagType.Long or
        NbtTagType.Float or NbtTagType.Double or NbtTagType.String => true,
        _ => false
    };

    private static string FormatNbtTag(NbtTag tag, string indentStep, int level)
    {
        string nameParam = tag.Name != null ? $"\"{EscapeString(tag.Name)}\", " : "";

        switch (tag.TagType)
        {
            case NbtTagType.Byte:
                return $"new NbtByte({nameParam}{tag.ByteValue})";
            case NbtTagType.Short:
                return $"new NbtShort({nameParam}{tag.ShortValue})";
            case NbtTagType.Int:
                return $"new NbtInt({nameParam}{tag.IntValue})";
            case NbtTagType.Long:
                return $"new NbtLong({nameParam}{tag.LongValue}L)";
            case NbtTagType.Float:
                return $"new NbtFloat({nameParam}{tag.FloatValue.ToString("R", CultureInfo.InvariantCulture)}f)";
            case NbtTagType.Double:
                return $"new NbtDouble({nameParam}{tag.DoubleValue.ToString("R", CultureInfo.InvariantCulture)}d)";
            case NbtTagType.String:
                return $"new NbtString({nameParam}\"{EscapeString(tag.StringValue)}\")";
            case NbtTagType.ByteArray:
                string bytes = string.Join(", ", ((NbtByteArray)tag).Value);
                return $"new NbtByteArray({nameParam}new byte[] {{ {bytes} }})";
            case NbtTagType.IntArray:
                string ints = string.Join(", ", ((NbtIntArray)tag).Value);
                return $"new NbtIntArray({nameParam}new int[] {{ {ints} }})";
            case NbtTagType.LongArray:
                string longs = string.Join(", ", ((NbtLongArray)tag).Value.Select(l => l + "L"));
                return $"new NbtLongArray({nameParam}new long[] {{ {longs} }})";
            case NbtTagType.Compound:
                var comp = (NbtCompound)tag;
                var meaningfulChildren = GetMeaningfulTags(comp);
                string compName = tag.Name != null ? $"new NbtCompound(\"{EscapeString(tag.Name)}\")" : "new NbtCompound()";

                if (meaningfulChildren.Count == 0)
                {
                    return compName;
                }

                if (meaningfulChildren.Count == 1 && IsSimplePrimitive(meaningfulChildren[0]))
                {
                    string header = tag.Name != null ? $"new NbtCompound(\"{EscapeString(tag.Name)}\") {{ " : "new NbtCompound { ";
                    return $"{header}{FormatNbtTag(meaningfulChildren[0], indentStep, level)} }}";
                }

                string cBaseIndent = string.Concat(Enumerable.Repeat(indentStep, level));
                string cChildIndent = string.Concat(Enumerable.Repeat(indentStep, level + 1));

                var innerSb = new StringBuilder();
                if (tag.Name != null) innerSb.AppendLine($"new NbtCompound(\"{EscapeString(tag.Name)}\")");
                else innerSb.AppendLine("new NbtCompound");
                innerSb.AppendLine($"{cBaseIndent}{{");
                for (int ci = 0; ci < meaningfulChildren.Count; ci++)
                {
                    innerSb.Append($"{cChildIndent}{FormatNbtTag(meaningfulChildren[ci], indentStep, level + 1)}");
                    if (ci < meaningfulChildren.Count - 1) innerSb.Append(',');
                    innerSb.AppendLine();
                }
                innerSb.Append($"{cBaseIndent}}}");
                return innerSb.ToString();

            case NbtTagType.List:
                var list = (NbtList)tag;
                if (list.Count == 0)
                {
                    return tag.Name != null
                        ? $"new NbtList(\"{EscapeString(tag.Name)}\", NbtTagType.{list.ListType})"
                        : $"new NbtList(NbtTagType.{list.ListType})";
                }

                if (list.Count <= 4 && list.All(IsSimplePrimitive))
                {
                    string header = tag.Name != null
                        ? $"new NbtList(\"{EscapeString(tag.Name)}\", NbtTagType.{list.ListType}) {{ "
                        : $"new NbtList(NbtTagType.{list.ListType}) {{ ";
                    return $"{header}{string.Join(", ", list.Select(c => FormatNbtTag(c, indentStep, level)))} }}";
                }

                string lBaseIndent = string.Concat(Enumerable.Repeat(indentStep, level));
                string lChildIndent = string.Concat(Enumerable.Repeat(indentStep, level + 1));

                var listSb = new StringBuilder();
                if (tag.Name != null) listSb.AppendLine($"new NbtList(\"{EscapeString(tag.Name)}\", NbtTagType.{list.ListType})");
                else listSb.AppendLine($"new NbtList(NbtTagType.{list.ListType})");
                listSb.AppendLine($"{lBaseIndent}{{");
                for (int li = 0; li < list.Count; li++)
                {
                    listSb.Append($"{lChildIndent}{FormatNbtTag(list[li], indentStep, level + 1)}");
                    if (li < list.Count - 1) listSb.Append(',');
                    listSb.AppendLine();
                }
                listSb.Append($"{lBaseIndent}}}");
                return listSb.ToString();

            default:
                return $"new NbtCompound() /* {tag.TagType} */";
        }
    }

    private static string FormatOutput(List<string> calls, FluentCodeGeneratorOptions options, bool needsFNbt)
    {
        string indentStep = options.IndentString;
        var sb = new StringBuilder();

        switch (options.Mode)
        {
            case CodeGenerationMode.Expression:
                AppendCalls(sb, calls, "", indentStep, isStatement: false);
                break;

            case CodeGenerationMode.Statement:
                sb.Append($"var {options.VariableName} = ");
                AppendCalls(sb, calls, "", indentStep, isStatement: true, firstLineOffset: $"var {options.VariableName} = ".Length);
                break;

            case CodeGenerationMode.Method:
                sb.AppendLine($"public static Schematic {options.MethodName}()");
                sb.AppendLine("{");
                sb.Append($"{indentStep}return ");
                AppendCalls(sb, calls, indentStep, indentStep, isStatement: true, firstLineOffset: "return ".Length);
                sb.AppendLine("}");
                break;

            case CodeGenerationMode.File:
                sb.AppendLine("// <auto-generated/>");
                sb.AppendLine("#nullable enable");
                sb.AppendLine();
                sb.AppendLine("using SchematicBuilderMC;");
                sb.AppendLine("using SchematicBuilderMC.Builder;");
                sb.AppendLine("using SchematicBuilderMC.Enums;");
                sb.AppendLine("using SchematicBuilderMC.Extensions;");
                sb.AppendLine("using SchematicBuilderMC.Models;");
                sb.AppendLine("using SchematicBuilderMC.Registries;");
                if (needsFNbt)
                {
                    sb.AppendLine("using fNbt;");
                }
                sb.AppendLine();

                if (options.UseFileScopedNamespace)
                {
                    sb.AppendLine($"namespace {options.TargetNamespace};");
                    sb.AppendLine();
                    sb.AppendLine($"public static class {options.ClassName}");
                    sb.AppendLine("{");
                    sb.AppendLine($"{indentStep}public static Schematic {options.MethodName}()");
                    sb.AppendLine($"{indentStep}{{");
                    sb.Append($"{indentStep}{indentStep}return ");
                    AppendCalls(sb, calls, $"{indentStep}{indentStep}", indentStep, isStatement: true, firstLineOffset: "return ".Length);
                    sb.AppendLine($"{indentStep}}}");
                    sb.AppendLine("}");
                }
                else
                {
                    sb.AppendLine($"namespace {options.TargetNamespace}");
                    sb.AppendLine("{");
                    sb.AppendLine($"{indentStep}public static class {options.ClassName}");
                    sb.AppendLine($"{indentStep}{{");
                    sb.AppendLine($"{indentStep}{indentStep}public static Schematic {options.MethodName}()");
                    sb.AppendLine($"{indentStep}{indentStep}{{");
                    sb.Append($"{indentStep}{indentStep}{indentStep}return ");
                    AppendCalls(sb, calls, $"{indentStep}{indentStep}{indentStep}", indentStep, isStatement: true, firstLineOffset: "return ".Length);
                    sb.AppendLine($"{indentStep}{indentStep}}}");
                    sb.AppendLine($"{indentStep}}}");
                    sb.AppendLine("}");
                }
                break;
        }

        return sb.ToString().TrimEnd();
    }

    private static void AppendCalls(
        StringBuilder sb,
        List<string> calls,
        string baseIndent,
        string indentStep,
        bool isStatement,
        int firstLineOffset = 0)
    {
        for (int i = 0; i < calls.Count; i++)
        {
            if (i == 0)
            {
                sb.AppendLine(calls[i]);
            }
            else
            {
                string call = calls[i];
                if (call.Contains('\n'))
                {
                    string[] lines = call.Split('\n');
                    for (int l = 0; l < lines.Length; l++)
                    {
                        if (l == 0)
                        {
                            sb.Append($"{baseIndent}{indentStep}{lines[l].TrimEnd()}");
                        }
                        else
                        {
                            sb.Append($"{baseIndent}{indentStep}{indentStep}{lines[l].TrimEnd()}");
                        }

                        if (l == lines.Length - 1 && i == calls.Count - 1 && isStatement)
                        {
                            sb.Append(';');
                        }
                        sb.AppendLine();
                    }
                }
                else
                {
                    sb.Append($"{baseIndent}{indentStep}{call}");
                    if (i == calls.Count - 1 && isStatement)
                    {
                        sb.Append(';');
                    }
                    sb.AppendLine();
                }
            }
        }
    }

    private static string FormatDouble(double val)
    {
        string str = val.ToString("G", CultureInfo.InvariantCulture);
        return str.Contains('.') || str.Contains('E') || str.Contains('e') ? str : str + ".0";
    }

    /// <summary>
    /// Escapes string contents for C# string literals.
    /// </summary>
    public static string EscapeString(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\r", "\\r")
            .Replace("\n", "\\n")
            .Replace("\t", "\\t");
    }
}
