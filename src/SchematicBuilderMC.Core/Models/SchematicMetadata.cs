using fNbt;

namespace SchematicBuilderMC.Core;

/// <summary>
/// Metadata describing a schematic.
/// </summary>
public sealed class SchematicMetadata
{
    public string? Name { get; set; }
    public string? Author { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset? DateCreated { get; set; }
    public List<string> RequiredMods { get; } = [];
    public NbtCompound ExtraData { get; } = [];

    public SchematicMetadata Clone()
    {
        var clone = new SchematicMetadata
        {
            Name = Name,
            Author = Author,
            Description = Description,
            DateCreated = DateCreated
        };

        clone.RequiredMods.AddRange(RequiredMods);
        foreach (var tag in ExtraData)
        {
            clone.ExtraData.Add((NbtTag)tag.Clone());
        }

        return clone;
    }
}
