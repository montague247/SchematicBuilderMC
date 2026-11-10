using fNbt;
using SchematicBuilderMC.Core.Exceptions;

namespace SchematicBuilderMC.Core.IO.Internal;

/// <summary>
/// Handles reading and writing of Sponge schematics (.schem) versions 1, 2, and 3.
/// </summary>
internal static class SpongeSchematicHandler
{
    public static Schematic Read(NbtCompound root)
    {
        var schematicTag = FindSchematicCompound(root);

        int version = schematicTag["Version"]?.IntValue ?? 2;
        int? dataVersion = schematicTag["DataVersion"]?.IntValue;

        short width = schematicTag["Width"]?.ShortValue ?? throw new SchematicFormatException("Missing 'Width' tag.");
        short height = schematicTag["Height"]?.ShortValue ?? throw new SchematicFormatException("Missing 'Height' tag.");
        short length = schematicTag["Length"]?.ShortValue ?? throw new SchematicFormatException("Missing 'Length' tag.");

        var schematic = new Schematic(width, height, length)
        {
            DataVersion = dataVersion
        };

        // Offset
        if (schematicTag["Offset"] is NbtIntArray offsetTag && offsetTag.Value.Length >= 3)
        {
            schematic.Offset = new BlockPosition(offsetTag.Value[0], offsetTag.Value[1], offsetTag.Value[2]);
        }

        // Palette & BlockData
        NbtCompound? paletteCompound = null;
        byte[]? blockDataBytes = null;
        NbtList? blockEntitiesList = null;

        if (version >= 3 && schematicTag["Blocks"] is NbtCompound blocksCompound)
        {
            paletteCompound = blocksCompound["Palette"] as NbtCompound;
            blockDataBytes = blocksCompound["Data"]?.ByteArrayValue;
            blockEntitiesList = blocksCompound["BlockEntities"] as NbtList;
        }
        else
        {
            paletteCompound = schematicTag["Palette"] as NbtCompound;
            blockDataBytes = (schematicTag["BlockData"] ?? schematicTag["Data"])?.ByteArrayValue;
            blockEntitiesList = (schematicTag["BlockEntities"] ?? schematicTag["TileEntities"]) as NbtList;
        }

        if (paletteCompound == null)
            throw new SchematicFormatException("Missing 'Palette' compound tag.");
        if (blockDataBytes == null)
            throw new SchematicFormatException("Missing 'BlockData' byte array tag.");

        // Read palette: mapping palette ID (int) -> BlockState
        var idToState = new Dictionary<int, BlockState>();
        foreach (var tag in paletteCompound)
        {
            if (tag is NbtInt intTag && !string.IsNullOrEmpty(tag.Name))
            {
                idToState[intTag.Value] = BlockState.Parse(tag.Name);
            }
        }

        // Decode VarInt array
        int totalBlocks = width * height * length;
        int[] paletteIndices = VarIntHelper.DecodeVarInts(blockDataBytes, totalBlocks);

        for (int y = 0; y < height; y++)
        {
            for (int z = 0; z < length; z++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = schematic.GetIndex(x, y, z);
                    int paletteId = paletteIndices[index];

                    if (idToState.TryGetValue(paletteId, out var state))
                    {
                        schematic.SetBlock(x, y, z, state);
                    }
                    else
                    {
                        schematic.SetBlock(x, y, z, BlockState.Air);
                    }
                }
            }
        }

        // Read Block Entities
        if (blockEntitiesList != null)
        {
            foreach (var item in blockEntitiesList)
            {
                if (item is NbtCompound compound)
                {
                    string id = compound["Id"]?.StringValue ?? compound["id"]?.StringValue ?? "minecraft:unknown";

                    BlockPosition pos = BlockPosition.Zero;
                    if (compound["Pos"] is NbtIntArray posArray && posArray.Value.Length >= 3)
                    {
                        pos = new BlockPosition(posArray.Value[0], posArray.Value[1], posArray.Value[2]);
                    }
                    else if (compound["x"] is NbtTag xTag && compound["y"] is NbtTag yTag && compound["z"] is NbtTag zTag)
                    {
                        pos = new BlockPosition(xTag.IntValue, yTag.IntValue, zTag.IntValue);
                    }

                    var data = compound["Data"] is NbtCompound dataComp ? (NbtCompound)dataComp.Clone() : (NbtCompound)compound.Clone();
                    schematic.SetBlockEntity(new BlockEntity(id, pos, data));
                }
            }
        }

        // Read Entities
        if (schematicTag["Entities"] is NbtList entitiesList)
        {
            foreach (var item in entitiesList)
            {
                if (item is NbtCompound compound)
                {
                    string id = compound["Id"]?.StringValue ?? compound["id"]?.StringValue ?? "minecraft:unknown";
                    double ex = 0, ey = 0, ez = 0;
                    if (compound["Pos"] is NbtList posList && posList.Count >= 3)
                    {
                        ex = posList[0].DoubleValue;
                        ey = posList[1].DoubleValue;
                        ez = posList[2].DoubleValue;
                    }

                    var data = compound["Data"] is NbtCompound dataComp ? (NbtCompound)dataComp.Clone() : (NbtCompound)compound.Clone();
                    schematic.AddEntity(new Entity(id, ex, ey, ez, data));
                }
            }
        }

        // Read Metadata
        if (schematicTag["Metadata"] is NbtCompound metaCompound)
        {
            schematic.Metadata.Name = metaCompound["Name"]?.StringValue;
            schematic.Metadata.Author = metaCompound["Author"]?.StringValue;
            schematic.Metadata.Description = metaCompound["Description"]?.StringValue;

            if (metaCompound["Date"] is NbtLong dateLong)
            {
                schematic.Metadata.DateCreated = DateTimeOffset.FromUnixTimeMilliseconds(dateLong.Value);
            }
            else if (metaCompound["Date"] is NbtString dateStr && DateTimeOffset.TryParse(dateStr.Value, out var dt))
            {
                schematic.Metadata.DateCreated = dt;
            }

            if (metaCompound["RequiredMods"] is NbtList modList)
            {
                foreach (var modTag in modList)
                {
                    if (modTag is NbtString s)
                    {
                        schematic.Metadata.RequiredMods.Add(s.Value);
                    }
                }
            }
        }

        return schematic;
    }

    public static NbtFile Write(Schematic schematic, SchematicFormat format)
    {
        int version = format switch
        {
            SchematicFormat.SpongeV1 => 1,
            SchematicFormat.SpongeV3 => 3,
            _ => 2
        };

        var root = new NbtCompound("Schematic")
        {
            new NbtInt("Version", version),
            new NbtInt("DataVersion", schematic.DataVersion ?? 3465),
            new NbtShort("Width", (short)schematic.Width),
            new NbtShort("Height", (short)schematic.Height),
            new NbtShort("Length", (short)schematic.Length),
            new NbtIntArray("Offset", [schematic.Offset.X, schematic.Offset.Y, schematic.Offset.Z])
        };

        // Build Palette
        var stateToId = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var paletteCompound = new NbtCompound("Palette");
        int nextId = 0;

        // Ensure air is ID 0 if present or by convention
        stateToId[BlockState.Air.ToString()] = nextId;
        paletteCompound.Add(new NbtInt(BlockState.Air.ToString(), nextId++));

        int totalBlocks = schematic.TotalBlocks;
        var paletteIndices = new int[totalBlocks];

        for (int y = 0; y < schematic.Height; y++)
        {
            for (int z = 0; z < schematic.Length; z++)
            {
                for (int x = 0; x < schematic.Width; x++)
                {
                    int index = schematic.GetIndex(x, y, z);
                    var state = schematic.GetBlock(x, y, z);
                    string key = state.ToString();

                    if (!stateToId.TryGetValue(key, out int id))
                    {
                        id = nextId++;
                        stateToId[key] = id;
                        paletteCompound.Add(new NbtInt(key, id));
                    }

                    paletteIndices[index] = id;
                }
            }
        }

        byte[] varIntBytes = VarIntHelper.EncodeVarInts(paletteIndices);

        // Block Entities
        var blockEntitiesList = new NbtList("BlockEntities", NbtTagType.Compound);
        foreach (var be in schematic.BlockEntities)
        {
            var dataClone = (NbtCompound)be.Data.Clone();
            dataClone.Name = "Data";

            var beCompound = new NbtCompound
            {
                new NbtString("Id", be.Id),
                new NbtIntArray("Pos", [be.Position.X, be.Position.Y, be.Position.Z]),
                dataClone
            };
            blockEntitiesList.Add(beCompound);
        }

        if (version >= 3)
        {
            var blocksCompound = new NbtCompound("Blocks")
            {
                paletteCompound,
                new NbtByteArray("Data", varIntBytes),
                blockEntitiesList
            };
            root.Add(blocksCompound);
        }
        else
        {
            root.Add(new NbtInt("PaletteMax", stateToId.Count));
            root.Add(paletteCompound);
            root.Add(new NbtByteArray("BlockData", varIntBytes));
            root.Add(version == 1 ? new NbtList("TileEntities", blockEntitiesList) : blockEntitiesList);
        }

        // Entities
        if (schematic.Entities.Count > 0)
        {
            var entitiesList = new NbtList("Entities", NbtTagType.Compound);
            foreach (var ent in schematic.Entities)
            {
                var dataClone = (NbtCompound)ent.Data.Clone();
                dataClone.Name = "Data";

                var entCompound = new NbtCompound
                {
                    new NbtString("Id", ent.Id),
                    new NbtList("Pos", [new NbtDouble(ent.X), new NbtDouble(ent.Y), new NbtDouble(ent.Z)]),
                    dataClone
                };
                entitiesList.Add(entCompound);
            }
            root.Add(entitiesList);
        }

        // Metadata
        var metaCompound = new NbtCompound("Metadata");
        if (!string.IsNullOrEmpty(schematic.Metadata.Name))
            metaCompound.Add(new NbtString("Name", schematic.Metadata.Name));
        if (!string.IsNullOrEmpty(schematic.Metadata.Author))
            metaCompound.Add(new NbtString("Author", schematic.Metadata.Author));
        if (!string.IsNullOrEmpty(schematic.Metadata.Description))
            metaCompound.Add(new NbtString("Description", schematic.Metadata.Description));
        if (schematic.Metadata.DateCreated.HasValue)
            metaCompound.Add(new NbtLong("Date", schematic.Metadata.DateCreated.Value.ToUnixTimeMilliseconds()));

        if (schematic.Metadata.RequiredMods.Count > 0)
        {
            var modList = new NbtList("RequiredMods", NbtTagType.String);
            foreach (var mod in schematic.Metadata.RequiredMods)
            {
                modList.Add(new NbtString(mod));
            }
            metaCompound.Add(modList);
        }

        if (metaCompound.Count > 0)
        {
            root.Add(metaCompound);
        }

        return new NbtFile(root);
    }

    private static NbtCompound FindSchematicCompound(NbtCompound root)
    {
        if (root.Contains("Version") && (root.Contains("Palette") || root.Contains("Blocks") || root.Contains("BlockData")))
        {
            return root;
        }

        if (root.Contains("Schematic") && root["Schematic"] is NbtCompound schematicTag)
        {
            return schematicTag;
        }

        // Sometimes root contains a single compound child
        if (root.Count == 1 && root.First() is NbtCompound singleChild)
        {
            if (singleChild.Contains("Version"))
                return singleChild;
        }

        return root;
    }
}
