using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;
using SchematicBuilderMC.Extensions;
using SchematicBuilderMC.Models;
using SchematicBuilderMC.Registries;
using SchematicBuilderMC.Schematics.Redstone.Internal;

namespace SchematicBuilderMC.Schematics.Redstone.Memory;

/// <summary>
/// Builds a compact repeater-lock D-Latch with configurable Enable polarity (<see cref="ActiveHighWrite"/>),
/// optional Data/Clock input levers, and optional output lamp.
/// </summary>
public class DLatchSchematicCreator : SchematicCreator
{
    /// <summary>
    /// When true (default), Enable=ON allows writing D (inverter unlocks repeater) and Enable=OFF holds stored state.
    /// When false, Enable directly drives the locking repeater (active-low transparent).
    /// </summary>
    public bool ActiveHighWrite { get; set; } = true;

    public Direction Facing { get; set; } = Direction.East;

    public string CircuitBlock { get; set; } = Minecraft.Blocks.SmoothStone;

    public bool IncludeInputLevers { get; set; }

    public bool IncludeOutputLamp { get; set; }

    public override SchematicBuilder Create()
    {
        CircuitLayout.ValidateHorizontalFacing(Facing);
        CircuitLayout.ValidateBlockId(CircuitBlock, nameof(CircuitBlock));

        int inputLead = IncludeInputLevers ? 1 : 0;
        const int coreLengthU = 5;
        const int widthV = 2;
        int totalLengthU = inputLead + coreLengthU + (IncludeOutputLamp ? 1 : 0);
        int height = IncludeInputLevers ? 3 : 2;
        int maxU = totalLengthU - 1;
        int maxV = widthV - 1;

        var builder = CircuitLayout.CreateBuilder(
            this,
            CircuitLayout.GetWidth(totalLengthU, widthV, Facing),
            height,
            CircuitLayout.GetLength(totalLengthU, widthV, Facing),
            "Repeater-Lock D-Latch",
            $"Gated repeater D-latch ({(ActiveHighWrite ? "active-high write" : "active-low write")}) facing {Facing}.");

        for (int u = 0; u < totalLengthU; u++)
        {
            for (int v = 0; v < widthV; v++)
            {
                builder.SetBlock(CircuitLayout.Map(0, 0, 0, u, 0, v, maxU, maxV, Facing), CircuitBlock);
            }
        }

        if (IncludeInputLevers)
        {
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, 0, 1, 0, maxU, maxV, Facing), CircuitBlock);
            builder.SetLever(CircuitLayout.Map(0, 0, 0, 0, 2, 0, maxU, maxV, Facing), BlockFace.Floor, Facing, powered: false);
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, 0, 1, 1, maxU, maxV, Facing), CircuitBlock);
            builder.SetLever(CircuitLayout.Map(0, 0, 0, 0, 2, 1, maxU, maxV, Facing), BlockFace.Floor, Facing, powered: false);
        }

        Direction lockDir = CircuitLayout.RotateDirection(Direction.South, Facing);
        Direction torchFacing = CircuitLayout.RotateDirection(Direction.East, Facing);

        // Enable / Clock lane on v=0 feeding locking repeater at (inputLead+3, 1, 0)
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, inputLead, 1, 0, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));
        if (ActiveHighWrite)
        {
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, inputLead + 1, 1, 0, maxU, maxV, Facing), CircuitBlock);
            builder.SetBlock(
                CircuitLayout.Map(0, 0, 0, inputLead + 2, 1, 0, maxU, maxV, Facing),
                Minecraft.Blocks.RedstoneWallTorch.WithFacing(torchFacing).WithLit(true));
        }
        else
        {
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, inputLead + 1, 1, 0, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, inputLead + 2, 1, 0, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));
        }

        CircuitLayout.SetRepeater(builder, CircuitLayout.Map(0, 0, 0, inputLead + 3, 1, 0, maxU, maxV, Facing), lockDir, delay: 1);

        // Data lane on v=1
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, inputLead, 1, 1, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, inputLead + 1, 1, 1, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, inputLead + 2, 1, 1, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));
        CircuitLayout.SetRepeater(builder, CircuitLayout.Map(0, 0, 0, inputLead + 3, 1, 1, maxU, maxV, Facing), Facing, delay: 1, locked: ActiveHighWrite);
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, inputLead + 4, 1, 1, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));

        if (IncludeOutputLamp)
        {
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, totalLengthU - 1, 1, 1, maxU, maxV, Facing), Minecraft.Blocks.RedstoneLamp);
        }

        return builder;
    }
}
