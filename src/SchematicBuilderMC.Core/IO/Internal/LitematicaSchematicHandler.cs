using fNbt;
using SchematicBuilderMC.Exceptions;
using SchematicBuilderMC.Models;

namespace SchematicBuilderMC.IO.Internal;

/// <summary>
/// Handles reading and writing of Litematica (.litematic) schematics.
/// </summary>
internal static class LitematicaSchematicHandler
{
    public static Schematic Read(NbtCompound root)
    {
        if (root["Regions"] is not NbtCompound regionsTag || regionsTag.Count == 0)
        {
            throw new SchematicFormatException("Missing or empty 'Regions' compound tag.");
        }

        int? dataVersion = root["MinecraftDataVersion"]?.IntValue;

        var regionInfos = new List<RegionInfo>();
        foreach (var tag in regionsTag)
        {
            if (tag is NbtCompound regComp)
            {
                if (regComp["Size"] is not NbtCompound sizeComp)
                {
                    throw new SchematicFormatException($"Region '{tag.Name}' is missing 'Size' compound tag.");
                }

                int rawSx = sizeComp["x"]?.IntValue ?? 0;
                int rawSy = sizeComp["y"]?.IntValue ?? 0;
                int rawSz = sizeComp["z"]?.IntValue ?? 0;

                int sizeX = Math.Abs(rawSx);
                int sizeY = Math.Abs(rawSy);
                int sizeZ = Math.Abs(rawSz);

                if (sizeX == 0 || sizeY == 0 || sizeZ == 0)
                    continue;

                int posX = 0, posY = 0, posZ = 0;
                if (regComp["Position"] is NbtCompound posComp)
                {
                    posX = posComp["x"]?.IntValue ?? 0;
                    posY = posComp["y"]?.IntValue ?? 0;
                    posZ = posComp["z"]?.IntValue ?? 0;
                }

                int minX = rawSx < 0 ? posX + rawSx + 1 : posX;
                int minY = rawSy < 0 ? posY + rawSy + 1 : posY;
                int minZ = rawSz < 0 ? posZ + rawSz + 1 : posZ;

                int maxX = minX + sizeX - 1;
                int maxY = minY + sizeY - 1;
                int maxZ = minZ + sizeZ - 1;

                regionInfos.Add(new RegionInfo(
                    tag.Name ?? "Unnamed",
                    regComp,
                    sizeX,
                    sizeY,
                    sizeZ,
                    minX,
                    minY,
                    minZ,
                    maxX,
                    maxY,
                    maxZ,
                    posX,
                    posY,
                    posZ));
            }
        }

        if (regionInfos.Count == 0)
        {
            throw new SchematicFormatException("Litematica schematic contains no valid regions with non-zero dimensions.");
        }

        int globalMinX = regionInfos.Min(r => r.MinX);
        int globalMinY = regionInfos.Min(r => r.MinY);
        int globalMinZ = regionInfos.Min(r => r.MinZ);

        int globalMaxX = regionInfos.Max(r => r.MaxX);
        int globalMaxY = regionInfos.Max(r => r.MaxY);
        int globalMaxZ = regionInfos.Max(r => r.MaxZ);

        int totalWidth = globalMaxX - globalMinX + 1;
        int totalHeight = globalMaxY - globalMinY + 1;
        int totalLength = globalMaxZ - globalMinZ + 1;

        if (totalWidth <= 0 || totalHeight <= 0 || totalLength <= 0)
        {
            throw new SchematicFormatException("Invalid schematic dimensions calculated from regions.");
        }

        if (totalWidth > short.MaxValue || totalHeight > short.MaxValue || totalLength > short.MaxValue)
        {
            throw new SchematicFormatException("Schematic dimensions exceed maximum allowed size.");
        }

        var schematic = new Schematic((short)totalWidth, (short)totalHeight, (short)totalLength)
        {
            DataVersion = dataVersion,
            Offset = new BlockPosition(globalMinX, globalMinY, globalMinZ)
        };

        // Metadata
        if (root["Metadata"] is NbtCompound metaComp)
        {
            if (metaComp["Name"]?.StringValue is { Length: > 0 } name)
                schematic.Metadata.Name = name;
            if (metaComp["Author"]?.StringValue is { Length: > 0 } author)
                schematic.Metadata.Author = author;
            if (metaComp["Description"]?.StringValue is { Length: > 0 } desc)
                schematic.Metadata.Description = desc;
            if (metaComp["TimeCreated"] is NbtLong timeTag)
                schematic.Metadata.DateCreated = DateTimeOffset.FromUnixTimeMilliseconds(timeTag.Value);
        }

        // Process each region
        foreach (var reg in regionInfos)
        {
            var regComp = reg.Compound;

            // BlockStatePalette
            var paletteList = new List<BlockState>();
            if (regComp["BlockStatePalette"] is NbtList paletteTag)
            {
                foreach (var item in paletteTag)
                {
                    if (item is NbtCompound pComp)
                    {
                        string blockName = pComp["Name"]?.StringValue ?? "minecraft:air";
                        Dictionary<string, string>? properties = null;

                        if (pComp["Properties"] is NbtCompound propComp && propComp.Count > 0)
                        {
                            properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                            foreach (var propTag in propComp)
                            {
                                if (string.IsNullOrEmpty(propTag.Name))
                                    continue;

                                if (propTag is NbtString sTag)
                                {
                                    properties[propTag.Name] = sTag.Value;
                                }
                                else
                                {
                                    properties[propTag.Name] = propTag.StringValue;
                                }
                            }
                        }

                        paletteList.Add(new BlockState(blockName, properties));
                    }
                }
            }

            if (paletteList.Count == 0)
            {
                paletteList.Add(BlockState.Air);
            }

            // Unpack BlockStates
            if (regComp["BlockStates"] is NbtLongArray blockStatesTag && blockStatesTag.Value.Length > 0)
            {
                long[] blockStates = blockStatesTag.Value;
                int bitsPerEntry = Math.Max(2, (int)Math.Ceiling(Math.Log2(paletteList.Count)));
                ulong mask = bitsPerEntry >= 64 ? ulong.MaxValue : ((1UL << bitsPerEntry) - 1UL);

                int regionBlocks = reg.SizeX * reg.SizeY * reg.SizeZ;
                for (int i = 0; i < regionBlocks; i++)
                {
                    int startBit = i * bitsPerEntry;
                    int longIndex = startBit / 64;
                    int bitOffset = startBit % 64;

                    if (longIndex >= blockStates.Length)
                        break;

                    ulong currentLong = (ulong)blockStates[longIndex];
                    ulong val = (currentLong >> bitOffset) & mask;

                    if (bitOffset + bitsPerEntry > 64 && longIndex + 1 < blockStates.Length)
                    {
                        int bitsInFirst = 64 - bitOffset;
                        int remBits = bitsPerEntry - bitsInFirst;
                        ulong nextLong = (ulong)blockStates[longIndex + 1];
                        ulong secondVal = nextLong & ((1UL << remBits) - 1UL);
                        val |= (secondVal << bitsInFirst);
                    }

                    int paletteIndex = (int)val;
                    var state = paletteIndex >= 0 && paletteIndex < paletteList.Count
                        ? paletteList[paletteIndex]
                        : BlockState.Air;

                    // Litematica packing order: (dy * sizeZ + dz) * sizeX + dx
                    int dx = i % reg.SizeX;
                    int dz = (i / reg.SizeX) % reg.SizeZ;
                    int dy = i / (reg.SizeX * reg.SizeZ);

                    int worldX = reg.MinX + dx;
                    int worldY = reg.MinY + dy;
                    int worldZ = reg.MinZ + dz;

                    int schemX = worldX - globalMinX;
                    int schemY = worldY - globalMinY;
                    int schemZ = worldZ - globalMinZ;

                    if (schemX >= 0 && schemX < schematic.Width &&
                        schemY >= 0 && schemY < schematic.Height &&
                        schemZ >= 0 && schemZ < schematic.Length)
                    {
                        schematic.SetBlock(schemX, schemY, schemZ, state);
                    }
                }
            }

            // TileEntities
            if (regComp["TileEntities"] is NbtList tileEntitiesList)
            {
                foreach (var item in tileEntitiesList)
                {
                    if (item is NbtCompound teComp)
                    {
                        string id = teComp["id"]?.StringValue ?? teComp["Id"]?.StringValue ?? "minecraft:unknown";

                        int rawX = teComp["x"]?.IntValue ?? 0;
                        int rawY = teComp["y"]?.IntValue ?? 0;
                        int rawZ = teComp["z"]?.IntValue ?? 0;

                        // Support both positioned space and local origin offsets
                        int worldX = (rawX >= reg.MinX && rawX <= reg.MaxX) ? rawX : (reg.MinX + rawX);
                        int worldY = (rawY >= reg.MinY && rawY <= reg.MaxY) ? rawY : (reg.MinY + rawY);
                        int worldZ = (rawZ >= reg.MinZ && rawZ <= reg.MaxZ) ? rawZ : (reg.MinZ + rawZ);

                        int schemX = worldX - globalMinX;
                        int schemY = worldY - globalMinY;
                        int schemZ = worldZ - globalMinZ;

                        if (schemX >= 0 && schemX < schematic.Width &&
                            schemY >= 0 && schemY < schematic.Height &&
                            schemZ >= 0 && schemZ < schematic.Length)
                        {
                            var pos = new BlockPosition(schemX, schemY, schemZ);
                            var dataClone = (NbtCompound)teComp.Clone();
                            schematic.SetBlockEntity(new BlockEntity(id, pos, dataClone));
                        }
                    }
                }
            }

            // Entities
            if (regComp["Entities"] is NbtList entitiesList)
            {
                foreach (var item in entitiesList)
                {
                    if (item is NbtCompound entComp)
                    {
                        string id = entComp["id"]?.StringValue ?? entComp["Id"]?.StringValue ?? "minecraft:unknown";

                        double rawX = 0, rawY = 0, rawZ = 0;
                        if (entComp["Pos"] is NbtList posList && posList.Count >= 3)
                        {
                            rawX = posList[0].DoubleValue;
                            rawY = posList[1].DoubleValue;
                            rawZ = posList[2].DoubleValue;
                        }

                        double worldX = (rawX >= reg.MinX - 0.5 && rawX <= reg.MaxX + 1.5) ? rawX : (reg.PosX + rawX);
                        double worldY = (rawY >= reg.MinY - 0.5 && rawY <= reg.MaxY + 1.5) ? rawY : (reg.PosY + rawY);
                        double worldZ = (rawZ >= reg.MinZ - 0.5 && rawZ <= reg.MaxZ + 1.5) ? rawZ : (reg.PosZ + rawZ);

                        double schemX = worldX - globalMinX;
                        double schemY = worldY - globalMinY;
                        double schemZ = worldZ - globalMinZ;

                        var dataClone = (NbtCompound)entComp.Clone();
                        schematic.AddEntity(new Entity(id, schemX, schemY, schemZ, dataClone));
                    }
                }
            }
        }

        return schematic;
    }

    public static NbtFile Write(Schematic schematic)
    {
        ArgumentNullException.ThrowIfNull(schematic);

        var root = new NbtCompound("");
        root.Add(new NbtInt("MinecraftDataVersion", schematic.DataVersion ?? 3953));
        root.Add(new NbtInt("Version", 6));
        root.Add(new NbtInt("SubVersion", 1));

        // Metadata
        var metaComp = new NbtCompound("Metadata")
        {
            new NbtString("Author", schematic.Metadata.Author ?? string.Empty),
            new NbtString("Description", schematic.Metadata.Description ?? string.Empty),
            new NbtCompound("EnclosingSize")
            {
                new NbtInt("x", schematic.Width),
                new NbtInt("y", schematic.Height),
                new NbtInt("z", schematic.Length)
            },
            new NbtString("Name", string.IsNullOrEmpty(schematic.Metadata.Name) ? "Schematic" : schematic.Metadata.Name),
            new NbtInt("RegionCount", 1),
            new NbtLong("TimeCreated", schematic.Metadata.DateCreated?.ToUnixTimeMilliseconds() ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()),
            new NbtLong("TimeModified", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()),
            new NbtInt("TotalBlocks", schematic.CountNonAirBlocks()),
            new NbtInt("TotalVolume", schematic.TotalBlocks)
        };
        root.Add(metaComp);

        // Region
        string regionName = string.IsNullOrEmpty(schematic.Metadata.Name) ? "Unnamed" : schematic.Metadata.Name;
        var regComp = new NbtCompound(regionName)
        {
            new NbtCompound("Position")
            {
                new NbtInt("x", schematic.Offset.X),
                new NbtInt("y", schematic.Offset.Y),
                new NbtInt("z", schematic.Offset.Z)
            },
            new NbtCompound("Size")
            {
                new NbtInt("x", schematic.Width),
                new NbtInt("y", schematic.Height),
                new NbtInt("z", schematic.Length)
            }
        };

        // Build Palette
        var stateToId = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var paletteListTag = new NbtList("BlockStatePalette", NbtTagType.Compound);
        int nextId = 0;

        // Ensure air is ID 0
        stateToId[BlockState.Air.ToString()] = nextId++;
        paletteListTag.Add(new NbtCompound
        {
            new NbtString("Name", BlockState.Air.Identifier)
        });

        for (int y = 0; y < schematic.Height; y++)
        {
            for (int z = 0; z < schematic.Length; z++)
            {
                for (int x = 0; x < schematic.Width; x++)
                {
                    var state = schematic.GetBlock(x, y, z);
                    string key = state.ToString();

                    if (!stateToId.ContainsKey(key))
                    {
                        stateToId[key] = nextId++;
                        var pComp = new NbtCompound
                        {
                            new NbtString("Name", state.Identifier)
                        };

                        if (state.Properties.Count > 0)
                        {
                            var propComp = new NbtCompound("Properties");
                            foreach (var kvp in state.Properties)
                            {
                                propComp.Add(new NbtString(kvp.Key, kvp.Value));
                            }
                            pComp.Add(propComp);
                        }

                        paletteListTag.Add(pComp);
                    }
                }
            }
        }
        regComp.Add(paletteListTag);

        // Pack BlockStates
        int bitsPerEntry = Math.Max(2, (int)Math.Ceiling(Math.Log2(stateToId.Count)));
        int totalBlocks = schematic.TotalBlocks;
        long totalBits = (long)totalBlocks * bitsPerEntry;
        int longCount = (int)Math.Ceiling((double)totalBits / 64);
        long[] blockStates = new long[longCount];

        for (int dy = 0; dy < schematic.Height; dy++)
        {
            for (int dz = 0; dz < schematic.Length; dz++)
            {
                for (int dx = 0; dx < schematic.Width; dx++)
                {
                    int blockIndex = (dy * schematic.Length + dz) * schematic.Width + dx;
                    var state = schematic.GetBlock(dx, dy, dz);
                    int palIdx = stateToId[state.ToString()];

                    int startBit = blockIndex * bitsPerEntry;
                    int longIdx = startBit / 64;
                    int bitOffset = startBit % 64;

                    int bitsInFirst = Math.Min(bitsPerEntry, 64 - bitOffset);
                    ulong firstMask = bitsInFirst >= 64 ? ulong.MaxValue : ((1UL << bitsInFirst) - 1UL);
                    blockStates[longIdx] |= (long)(((ulong)palIdx & firstMask) << bitOffset);

                    if (bitOffset + bitsPerEntry > 64)
                    {
                        int remBits = bitsPerEntry - bitsInFirst;
                        ulong remMask = remBits >= 64 ? ulong.MaxValue : ((1UL << remBits) - 1UL);
                        blockStates[longIdx + 1] |= (long)(((ulong)palIdx >> bitsInFirst) & remMask);
                    }
                }
            }
        }
        regComp.Add(new NbtLongArray("BlockStates", blockStates));

        // TileEntities
        var tileEntitiesList = new NbtList("TileEntities", NbtTagType.Compound);
        foreach (var be in schematic.BlockEntities)
        {
            var dataClone = (NbtCompound)be.Data.Clone();
            dataClone["id"] = new NbtString("id", be.Id);
            dataClone["x"] = new NbtInt("x", be.Position.X);
            dataClone["y"] = new NbtInt("y", be.Position.Y);
            dataClone["z"] = new NbtInt("z", be.Position.Z);
            tileEntitiesList.Add(dataClone);
        }
        regComp.Add(tileEntitiesList);

        // Entities
        var entitiesList = new NbtList("Entities", NbtTagType.Compound);
        foreach (var ent in schematic.Entities)
        {
            var dataClone = (NbtCompound)ent.Data.Clone();
            dataClone["id"] = new NbtString("id", ent.Id);
            dataClone["Pos"] = new NbtList("Pos", [new NbtDouble(ent.X), new NbtDouble(ent.Y), new NbtDouble(ent.Z)]);
            entitiesList.Add(dataClone);
        }
        regComp.Add(entitiesList);

        regComp.Add(new NbtList("PendingBlockTicks", NbtTagType.Compound));
        regComp.Add(new NbtList("PendingFluidTicks", NbtTagType.Compound));

        var regionsComp = new NbtCompound("Regions")
        {
            regComp
        };
        root.Add(regionsComp);

        return new NbtFile(root);
    }
}
