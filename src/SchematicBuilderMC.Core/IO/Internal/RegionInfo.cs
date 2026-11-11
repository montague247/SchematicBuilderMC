using fNbt;

namespace SchematicBuilderMC.IO.Internal;

/// <summary>
/// Internal metadata record describing a region within a Litematica schematic.
/// </summary>
internal sealed record RegionInfo(
    string Name,
    NbtCompound Compound,
    int SizeX,
    int SizeY,
    int SizeZ,
    int MinX,
    int MinY,
    int MinZ,
    int MaxX,
    int MaxY,
    int MaxZ,
    int PosX,
    int PosY,
    int PosZ);
