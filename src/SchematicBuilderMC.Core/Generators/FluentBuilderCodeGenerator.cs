using System.Globalization;
using System.Text;
using fNbt;

namespace SchematicBuilderMC.Core.Generators;

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
        GenerateBlockCalls(schematic, options, calls);

        // Block entities (ordered by position for deterministic output)
        foreach (var be in schematic.BlockEntities.OrderBy(b => b.Position.Y).ThenBy(b => b.Position.Z).ThenBy(b => b.Position.X))
        {
            bool hasCustomData = HasCustomPayloadData(be);
            if (hasCustomData)
            {
                needsFNbt = true;
            }

            string beCode = FormatBlockEntity(be, options, hasCustomData, indentStep);
            calls.Add($".AddBlockEntity({beCode})");
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

    private static void GenerateBlockCalls(Schematic schematic, FluentCodeGeneratorOptions options, List<string> calls)
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

                        calls.Add($".SetBlock({x}, {y}, {z}, {FormatBlockState(block, options)})");
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
                        calls.Add($".SetBlock({x}, {y}, {z}, {blockCode})");
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
                sb.Append($".WithProperty(\"{EscapeString(kvp.Key)}\", \"{EscapeString(kvp.Value)}\")");
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
                sb.Append($".WithProperty(\"{EscapeString(kvp.Key)}\", \"{EscapeString(kvp.Value)}\")");
            }
            return sb.ToString();
        }

        // Canonical string literal
        return $"\"{EscapeString(blockState.ToString())}\"";
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

        string dataCode = FormatNbtCompound(be.Data, indentStep);
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

        string dataCode = FormatNbtCompound(ent.Data, indentStep);
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

    private static string FormatNbtCompound(NbtCompound compound, string indentStep)
    {
        var tags = compound
            .Where(t =>
            {
                string n = t.Name ?? "";
                return !n.Equals("id", StringComparison.OrdinalIgnoreCase) &&
                       !n.Equals("x", StringComparison.OrdinalIgnoreCase) &&
                       !n.Equals("y", StringComparison.OrdinalIgnoreCase) &&
                       !n.Equals("z", StringComparison.OrdinalIgnoreCase) &&
                       !n.Equals("Pos", StringComparison.OrdinalIgnoreCase);
            })
            .ToList();

        if (tags.Count == 0)
        {
            return "new NbtCompound()";
        }

        var sb = new StringBuilder();
        sb.Append("new NbtCompound { ");
        for (int i = 0; i < tags.Count; i++)
        {
            sb.Append(FormatNbtTag(tags[i]));
            if (i < tags.Count - 1) sb.Append(", ");
        }
        sb.Append(" }");
        return sb.ToString();
    }

    private static string FormatNbtTag(NbtTag tag)
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
                if (comp.Count == 0)
                {
                    return tag.Name != null ? $"new NbtCompound(\"{EscapeString(tag.Name)}\")" : "new NbtCompound()";
                }
                var innerSb = new StringBuilder();
                if (tag.Name != null) innerSb.Append($"new NbtCompound(\"{EscapeString(tag.Name)}\") {{ ");
                else innerSb.Append("new NbtCompound { ");
                int c = 0;
                foreach (var child in comp)
                {
                    innerSb.Append(FormatNbtTag(child));
                    if (++c < comp.Count) innerSb.Append(", ");
                }
                innerSb.Append(" }");
                return innerSb.ToString();
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
                sb.AppendLine("using SchematicBuilderMC.Core;");
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
                sb.Append($"{baseIndent}{indentStep}{calls[i]}");
                if (i == calls.Count - 1 && isStatement)
                {
                    sb.Append(';');
                }
                sb.AppendLine();
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
