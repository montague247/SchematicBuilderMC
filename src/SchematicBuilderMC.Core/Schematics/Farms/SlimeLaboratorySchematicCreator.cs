using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;
using SchematicBuilderMC.Models;
using SchematicBuilderMC.Registries;

namespace SchematicBuilderMC.Schematics.Farms;

public sealed class SlimeLaboratorySchematicCreator : SchematicCreator
{
    public override SchematicBuilder Create()
    {
        const int Radius = 7;
        const int Width = (Radius + 2) * 2;
        const int Height = 16;
        const int Length = Width;

        var builder = SchematicBuilder.Create(Width, Height, Length, Rotation)
                .WithOffset(0, 0, 0)
                .WithName("Slime Laboratory")
                .WithAuthor("Dirk Helbig")
                .WithDescription("A slime laboratory schematic to produce slimes");

        // gradient for the cylinder (from bottom to top)
        var gradient = new BlockGradient(
            [Minecraft.Blocks.LimeStainedGlass, Minecraft.Blocks.GreenStainedGlass],
            GradientTransition.Fuzzy, [35], true, 42);

        // cylinder for slimes to spawn in
        builder.SetCylinder(new BlockPosition(Radius, 0, Radius), Radius, Height, gradient, filled: false, axis: Axis.Y, smoothnessFactor: 2.0);

        return builder;
    }
}