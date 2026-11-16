using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Models;

namespace SchematicBuilderMC.Schematics;

public abstract class SchematicCreator
{
    public abstract SchematicBuilder Create();

    public Schematic Build()
    {
        return Create().Build();
    }
}
