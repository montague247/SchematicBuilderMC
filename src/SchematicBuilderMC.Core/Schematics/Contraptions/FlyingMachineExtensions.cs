using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;
using SchematicBuilderMC.Extensions;
using SchematicBuilderMC.Registries;

namespace SchematicBuilderMC.Schematics.Contraptions;

public static class FlyingMachineExtensions
{
    public static SchematicBuilder Create(this SchematicBuilder builder, FlyingMachine flyingMachine, int startX, int startY, int startZ, int width, string? fillBlockId = null, int? endZ = null)
    {
        if (width > flyingMachine.Width)
        {
            // Create multiple flying machines if the width is greater than the flying machine's width
            var numberOfMachines = (int)Math.Ceiling((double)width / flyingMachine.Width);
            var remainingWidth = width;
            var machineWidth = width / (double)numberOfMachines;

            do
            {
                var currentWidth = Math.Min(remainingWidth, (int)Math.Ceiling(machineWidth));
                builder.Create(flyingMachine, startX, startY, startZ, currentWidth, fillBlockId, endZ);

                remainingWidth -= currentWidth;
                startX += currentWidth;
            }
            while (remainingWidth > 0);

            return builder;
        }

        int requiredHeight = checked(startY + (endZ.HasValue ? 3 : 2));
        int requiredLength = Math.Max(checked(startZ + 2), endZ.HasValue && fillBlockId != null ? checked(endZ.Value + 2) : checked(startZ + 2));
        builder.EnsureDimensions(checked(startX + width), requiredHeight, requiredLength);

        var leftWidth = width / 2;

        // build left side of flying machine
        // set slime blocks
        builder.Fill(startX + 1, startY, startZ + 1, startX + leftWidth - 1, startY, startZ + 1, Minecraft.Blocks.SlimeBlock);
        // set trapdoor at the end of the slime blocks
        builder.SetBlock(startX, startY, startZ + 1, Minecraft.Blocks.BambooTrapdoor.FacingWest().WithOpen());
        // set sticky piston
        builder.SetPiston(startX + leftWidth, startY, startZ + 1, Direction.North, true);
        // set observer
        builder.SetBlock(startX + leftWidth - 1, startY + 1, startZ + 1, Minecraft.Blocks.Observer.FacingUp());

        // build right side of flying machine
        // set slime blocks
        builder.Fill(startX + leftWidth, startY, startZ, startX + width - 2, startY, startZ, Minecraft.Blocks.SlimeBlock);
        if (endZ.HasValue)
        {
            // set furnaces to prevent sticking
            builder.Fill(startX + leftWidth, startY, startZ - 1, startX + width - 2, startY, startZ - 1, Minecraft.Blocks.Furnace);
        }
        // set trapdoor at the end of the slime blocks
        builder.SetBlock(startX + width - 1, startY, startZ, Minecraft.Blocks.BambooTrapdoor.FacingEast().TopHalf().WithOpen());
        // set sticky piston
        builder.SetPiston(startX + leftWidth - 1, startY, startZ, Direction.South, true);
        // set observer
        builder.SetBlock(startX + leftWidth, startY + 1, startZ, Minecraft.Blocks.Observer.FacingUp());
        if (endZ.HasValue)
        {
            // set trapdoor to start the machine
            builder.SetBlock(startX + leftWidth, startY + 2, startZ, Minecraft.Blocks.BambooTrapdoor.FacingSouth());
        }

        if (fillBlockId != null)
        {
            // fill the area under the right side of the flying machine with the specified block
            builder.Fill(startX + leftWidth - 1, startY - 1, startZ, startX + width - 1, startY - 1, startZ, fillBlockId);

            if (endZ.HasValue)
            {
                // fill the area under the left side of the flying machine at the end with the specified block
                builder.Fill(startX, startY - 1, endZ.Value, startX + leftWidth, startY - 1, endZ.Value, fillBlockId);

                // set furnaces to prevent sticking
                builder.Fill(startX + 1, startY, endZ.Value + 1, startX + leftWidth - 1, startY, endZ.Value + 1, Minecraft.Blocks.Furnace);

                // set trapdoor to start the machine
                builder.SetBlock(startX + leftWidth - 1, startY + 2, endZ.Value, Minecraft.Blocks.BambooTrapdoor);
            }
        }

        return builder;
    }
}