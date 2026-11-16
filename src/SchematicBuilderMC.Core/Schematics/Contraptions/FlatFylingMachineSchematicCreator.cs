using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Models;

namespace SchematicBuilderMC.Schematics.Contraptions;

public sealed class FlatFylingMachineSchematicCreator
{
    private static readonly FlatFlyingMachine _flyingMachine = new();

    public int Width { get; set; } = _flyingMachine.Width;

    public SchematicBuilder Create()
    {
        var builder = SchematicBuilder.Create(Width, _flyingMachine.Height, 2)
                .WithOffset(0, 0, 0)
                .WithName("Flat Flying Machine")
                .WithAuthor("Dirk Helbig")
                .WithDescription("A fyling machine contraption.");

        // create the flying machine(s)
        builder.Create(_flyingMachine, 0, 0, 0, Width);

        return builder;
    }

    public Schematic Build()
    {
        return Create().Build();
    }
}
