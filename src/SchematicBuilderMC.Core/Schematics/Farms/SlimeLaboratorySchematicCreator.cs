using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;
using SchematicBuilderMC.Extensions;
using SchematicBuilderMC.Models;
using SchematicBuilderMC.Registries;

namespace SchematicBuilderMC.Schematics.Farms;

public sealed class SlimeLaboratorySchematicCreator : SchematicCreator
{
    public override SchematicBuilder Create()
    {
        const int Radius = 7;
        const int CylinderHeight = 16;
        const int Height = CylinderHeight + 2;
        const int BaseWidth = 2;
        const int Width = (BaseWidth + Radius) * 2 + 1;
        const int Length = Width;

        var builder = SchematicBuilder.Create(Width, Height, Length, Rotation)
                .WithOffset(0, 0, 0)
                .WithName("Slime Laboratory")
                .WithAuthor("Dirk Helbig")
                .WithDescription("A slime laboratory schematic to produce slimes");

        var levelY = 0;

        // set base layer
        builder.SetCircle(BaseWidth + Radius, levelY, BaseWidth + Radius, BaseWidth + Radius, Minecraft.Blocks.QuartzBlock);

        levelY++;

        // gradient for the cylinder (from bottom to top)
        var gradient = new BlockGradient(
            [Minecraft.Blocks.LimeStainedGlass, Minecraft.Blocks.GreenStainedGlass],
            GradientTransition.Fuzzy, [35], true, 42);

        // cylinder for slimes to spawn in
        builder.SetCylinder(BaseWidth + Radius, levelY, BaseWidth + Radius, Radius, CylinderHeight, gradient, filled: false);
        builder.SetCylinderWater(BaseWidth + Radius, levelY, BaseWidth + Radius, Radius);
        builder.SetCylinderBase(BaseWidth + Radius, levelY, BaseWidth + Radius, Radius, 2, 2, Minecraft.Blocks.DeepslateBricks);
        builder.SetCylinderTop(BaseWidth + Radius, levelY + CylinderHeight - 2, BaseWidth + Radius, Radius, 2, 2, Minecraft.Blocks.DeepslateBricks);

        // place water at the bottom of the cylinder
        builder.SetBlock(BaseWidth + 1, levelY, BaseWidth + Radius, Minecraft.Blocks.Water.WithLevel(8));
        builder.SetBlock(BaseWidth + Radius, levelY, BaseWidth + 1, Minecraft.Blocks.Water.WithLevel(8));
        builder.SetBlock(BaseWidth + Radius, levelY, (BaseWidth + Radius) * 2 - 1, Minecraft.Blocks.Water.WithLevel(8));
        builder.SetBlock((BaseWidth + Radius) * 2 - 1, levelY, BaseWidth + Radius, Minecraft.Blocks.Water.WithLevel(8));

        levelY += CylinderHeight;

        // set top layer
        builder.SetCircle(BaseWidth + Radius, levelY, BaseWidth + Radius, BaseWidth + Radius, Minecraft.Blocks.QuartzBlock);

        return builder;
    }
}