using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;
using SchematicBuilderMC.Extensions;
using SchematicBuilderMC.Models;
using SchematicBuilderMC.Registries;
using SchematicBuilderMC.Schematics.Contraptions;

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
        const int CenterX = BaseWidth + Radius;
        const int CenterZ = CenterX;

        var builder = SchematicBuilder.Create(Width, Height, Length, Rotation)
                .WithOffset(0, 0, 0)
                .WithName("Slime Laboratory")
                .WithAuthor("Dirk Helbig")
                .WithDescription("A slime laboratory schematic to produce slimes");

        var levelY = 0;

        // set base layer
        builder.SetCircle(CenterX, levelY, CenterZ, BaseWidth + Radius, Minecraft.Blocks.QuartzBlock);
        // set hopper for slime collection
        builder.SetBlock(CenterX, levelY, CenterZ, Minecraft.Blocks.Hopper.FacingDown());
        // set dispenser for splash potion
        builder.SetBlock(CenterX + 1, levelY, CenterZ, Minecraft.Blocks.Dispenser.FacingUp());

        levelY++;

        // gradient for the cylinder (from bottom to top)
        var gradient = new BlockGradient(
            [Minecraft.Blocks.LimeStainedGlass, Minecraft.Blocks.GreenStainedGlass],
            GradientTransition.Fuzzy, [35], true, 42);

        // cylinder for slimes to spawn in
        builder.SetCylinder(CenterX, levelY, CenterZ, Radius, CylinderHeight, gradient, filled: false);
        builder.SetCylinderWater(CenterX, levelY, CenterZ, Radius);
        builder.SetCylinderBase(CenterX, levelY, CenterZ, Radius, 2, 2, Minecraft.Blocks.DeepslateBricks);
        builder.SetCylinderTop(CenterX, levelY + CylinderHeight - 2, CenterZ, Radius, 2, 2, Minecraft.Blocks.DeepslateBricks);

        levelY += CylinderHeight;

        // set top layer
        builder.SetCircle(CenterX, levelY, CenterZ, BaseWidth + Radius, Minecraft.Blocks.QuartzBlock);
        // set dispenser for splash potion
        builder.SetBlock(CenterX, levelY, CenterZ, Minecraft.Blocks.Dispenser.FacingDown());

        var tntDropX = CenterX + 1;
        var tntDropZ = CenterZ;

        // set hole for tnt duper to drop tnt into the killing chamber
        builder.SetBlock(tntDropX, levelY, tntDropZ, Minecraft.Blocks.Air);
        // set additionals holes for tnt duper to drop tnt into the killing chamber
        builder.SetBlock(tntDropX, levelY, tntDropZ + 1, Minecraft.Blocks.Air);
        builder.SetBlock(tntDropX, levelY, tntDropZ - 1, Minecraft.Blocks.Air);
        builder.SetBlock(tntDropX + 1, levelY, tntDropZ, Minecraft.Blocks.Air);

        // place tnt duper
        builder.CreateAtDropPosition(new TntDuperSchematicCreator()
        {
            IncludeTriggerButton = true
        }, tntDropX, levelY + 1, tntDropZ);

        return builder;
    }
}