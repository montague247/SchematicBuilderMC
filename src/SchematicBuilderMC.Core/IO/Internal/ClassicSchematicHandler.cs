using fNbt;
using SchematicBuilderMC.Core.Exceptions;

namespace SchematicBuilderMC.Core.IO.Internal;

/// <summary>
/// Handles reading and writing of classic MCEdit .schematic files.
/// </summary>
internal static class ClassicSchematicHandler
{
    public static Schematic Read(NbtCompound root)
    {
        var schematicTag = FindSchematicCompound(root);

        short width = schematicTag["Width"]?.ShortValue ?? throw new SchematicFormatException("Missing 'Width' tag in classic schematic.");
        short height = schematicTag["Height"]?.ShortValue ?? throw new SchematicFormatException("Missing 'Height' tag in classic schematic.");
        short length = schematicTag["Length"]?.ShortValue ?? throw new SchematicFormatException("Missing 'Length' tag in classic schematic.");

        var schematic = new Schematic(width, height, length);

        // Read WorldEdit offsets if present
        int offsetX = schematicTag["WEOffsetX"]?.IntValue ?? 0;
        int offsetY = schematicTag["WEOffsetY"]?.IntValue ?? 0;
        int offsetZ = schematicTag["WEOffsetZ"]?.IntValue ?? 0;
        schematic.Offset = new BlockPosition(offsetX, offsetY, offsetZ);

        byte[]? blocks = schematicTag["Blocks"]?.ByteArrayValue;
        if (blocks == null)
            throw new SchematicFormatException("Missing 'Blocks' tag in classic schematic.");

        byte[] data = schematicTag["Data"]?.ByteArrayValue ?? new byte[blocks.Length];
        byte[]? addBlocks = schematicTag["AddBlocks"]?.ByteArrayValue;

        int totalBlocks = width * height * length;

        for (int y = 0; y < height; y++)
        {
            for (int z = 0; z < length; z++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = schematic.GetIndex(x, y, z);
                    if (index >= blocks.Length) break;

                    int blockId = blocks[index] & 0xFF;
                    if (addBlocks != null)
                    {
                        int addIndex = index >> 1;
                        if (addIndex < addBlocks.Length)
                        {
                            byte addByte = addBlocks[addIndex];
                            int high = (index & 1) == 0 ? (addByte & 0x0F) : ((addByte >> 4) & 0x0F);
                            blockId |= (high << 8);
                        }
                    }

                    byte blockData = (index < data.Length) ? data[index] : (byte)0;
                    var state = LegacyBlockConverter.ToBlockState(blockId, blockData);
                    schematic.SetBlock(x, y, z, state);
                }
            }
        }

        // Read TileEntities
        if (schematicTag["TileEntities"] is NbtList tileList)
        {
            foreach (var item in tileList)
            {
                if (item is NbtCompound comp)
                {
                    string id = comp["id"]?.StringValue ?? "minecraft:unknown";
                    int x = comp["x"]?.IntValue ?? 0;
                    int y = comp["y"]?.IntValue ?? 0;
                    int z = comp["z"]?.IntValue ?? 0;

                    var cloneComp = (NbtCompound)comp.Clone();
                    schematic.SetBlockEntity(new BlockEntity(id, new BlockPosition(x, y, z), cloneComp));
                }
            }
        }

        // Read Entities
        if (schematicTag["Entities"] is NbtList entList)
        {
            foreach (var item in entList)
            {
                if (item is NbtCompound comp)
                {
                    string id = comp["id"]?.StringValue ?? "minecraft:unknown";
                    double ex = 0, ey = 0, ez = 0;
                    if (comp["Pos"] is NbtList posList && posList.Count >= 3)
                    {
                        ex = posList[0].DoubleValue;
                        ey = posList[1].DoubleValue;
                        ez = posList[2].DoubleValue;
                    }

                    var cloneComp = (NbtCompound)comp.Clone();
                    schematic.AddEntity(new Entity(id, ex, ey, ez, cloneComp));
                }
            }
        }

        return schematic;
    }

    public static NbtFile Write(Schematic schematic)
    {
        int totalBlocks = schematic.TotalBlocks;
        byte[] blocks = new byte[totalBlocks];
        byte[] data = new byte[totalBlocks];
        byte[]? addBlocks = null;

        // Check if any block ID > 255
        bool needsAddBlocks = false;
        var legacyBlocks = new (int id, byte data)[totalBlocks];

        for (int y = 0; y < schematic.Height; y++)
        {
            for (int z = 0; z < schematic.Length; z++)
            {
                for (int x = 0; x < schematic.Width; x++)
                {
                    int index = schematic.GetIndex(x, y, z);
                    var state = schematic.GetBlock(x, y, z);
                    var (id, blockData) = LegacyBlockConverter.FromBlockState(state);
                    legacyBlocks[index] = (id, blockData);

                    if (id > 255)
                    {
                        needsAddBlocks = true;
                    }
                }
            }
        }

        if (needsAddBlocks)
        {
            addBlocks = new byte[(totalBlocks + 1) / 2];
        }

        for (int i = 0; i < totalBlocks; i++)
        {
            var (id, blockData) = legacyBlocks[i];
            blocks[i] = (byte)(id & 0xFF);
            data[i] = blockData;

            if (needsAddBlocks && addBlocks != null)
            {
                int high = (id >> 8) & 0x0F;
                int addIndex = i >> 1;
                if ((i & 1) == 0)
                {
                    addBlocks[addIndex] |= (byte)(high & 0x0F);
                }
                else
                {
                    addBlocks[addIndex] |= (byte)((high & 0x0F) << 4);
                }
            }
        }

        var root = new NbtCompound("Schematic")
        {
            new NbtShort("Width", (short)schematic.Width),
            new NbtShort("Height", (short)schematic.Height),
            new NbtShort("Length", (short)schematic.Length),
            new NbtString("Materials", "Alpha"),
            new NbtByteArray("Blocks", blocks),
            new NbtByteArray("Data", data),
            new NbtInt("WEOffsetX", schematic.Offset.X),
            new NbtInt("WEOffsetY", schematic.Offset.Y),
            new NbtInt("WEOffsetZ", schematic.Offset.Z)
        };

        if (addBlocks != null)
        {
            root.Add(new NbtByteArray("AddBlocks", addBlocks));
        }

        // TileEntities
        var tileEntitiesList = new NbtList("TileEntities", NbtTagType.Compound);
        foreach (var be in schematic.BlockEntities)
        {
            var comp = (NbtCompound)be.Data.Clone();
            comp["id"] = new NbtString("id", be.Id);
            comp["x"] = new NbtInt("x", be.Position.X);
            comp["y"] = new NbtInt("y", be.Position.Y);
            comp["z"] = new NbtInt("z", be.Position.Z);
            tileEntitiesList.Add(comp);
        }
        root.Add(tileEntitiesList);

        // Entities
        var entitiesList = new NbtList("Entities", NbtTagType.Compound);
        foreach (var ent in schematic.Entities)
        {
            var comp = (NbtCompound)ent.Data.Clone();
            comp["id"] = new NbtString("id", ent.Id);
            comp["Pos"] = new NbtList("Pos", [new NbtDouble(ent.X), new NbtDouble(ent.Y), new NbtDouble(ent.Z)]);
            entitiesList.Add(comp);
        }
        root.Add(entitiesList);

        return new NbtFile(root);
    }

    private static NbtCompound FindSchematicCompound(NbtCompound root)
    {
        if (root.Contains("Blocks") && root.Contains("Width"))
        {
            return root;
        }

        if (root.Contains("Schematic") && root["Schematic"] is NbtCompound schematicTag)
        {
            return schematicTag;
        }

        if (root.Count == 1 && root.First() is NbtCompound singleChild && singleChild.Contains("Blocks"))
        {
            return singleChild;
        }

        return root;
    }
}
