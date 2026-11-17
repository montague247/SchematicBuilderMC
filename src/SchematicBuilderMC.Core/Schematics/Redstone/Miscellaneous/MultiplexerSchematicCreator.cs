using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;
using SchematicBuilderMC.Extensions;
using SchematicBuilderMC.Models;
using SchematicBuilderMC.Registries;
using SchematicBuilderMC.Schematics.Redstone.Internal;

namespace SchematicBuilderMC.Schematics.Redstone.Miscellaneous;

/// <summary>
/// Routing mode for <see cref="MultiplexerSchematicCreator"/>.
/// </summary>
public enum MultiplexerMode
{
    /// <summary>2-to-1 Multiplexer selecting between Data A (Select=0) and Data B (Select=1).</summary>
    Multiplexer,

    /// <summary>1-to-2 Demultiplexer routing a single input onto Out0 (Select=0) or Out1 (Select=1).</summary>
    Demultiplexer
}

/// <summary>
/// Builds a configurable Redstone analog/digital Multiplexer (or Demultiplexer) with configurable stage count (<see cref="Stages"/>).
/// </summary>
public class MultiplexerSchematicCreator : SchematicCreator
{
    public MultiplexerMode Mode { get; set; } = MultiplexerMode.Multiplexer;

    /// <summary>
    /// Number of parallel 2-way selector stages (1 = single 2:1 MUX, N = N parallel channels controlled by Select).
    /// </summary>
    public int Stages { get; set; } = 1;

    public Direction Facing { get; set; } = Direction.East;

    public string CircuitBlock { get; set; } = Minecraft.Blocks.SmoothStone;

    public bool IncludeInputLevers { get; set; }

    public bool IncludeOutputLamps { get; set; }

    public override SchematicBuilder Create()
    {
        if (!Enum.IsDefined(Mode))
        {
            throw new ArgumentOutOfRangeException(nameof(Mode), Mode, "Unsupported multiplexer mode.");
        }

        CircuitLayout.ValidateHorizontalFacing(Facing);
        CircuitLayout.ValidatePositive(Stages, nameof(Stages), 1);
        CircuitLayout.ValidateBlockId(CircuitBlock, nameof(CircuitBlock));

        // Each stage is 4 blocks wide laterally (A, S, B, margin); we tile cleanly along V!
        const int coreLengthU = 5;
        int stageWidthV = 4;
        int widthV = (Stages * stageWidthV) - 1;
        int inputLead = IncludeInputLevers ? 1 : 0;
        int totalLengthU = inputLead + coreLengthU + (IncludeOutputLamps ? 1 : 0);
        int height = IncludeInputLevers ? 3 : 2;
        int maxU = totalLengthU - 1;
        int maxV = widthV - 1;

        var builder = CircuitLayout.CreateBuilder(
            this,
            CircuitLayout.GetWidth(totalLengthU, widthV, Facing),
            height,
            CircuitLayout.GetLength(totalLengthU, widthV, Facing),
            Mode == MultiplexerMode.Demultiplexer ? "Redstone Demultiplexer" : "Redstone Multiplexer",
            $"{Stages}-channel comparator-gated {(Mode == MultiplexerMode.Demultiplexer ? "1:2 DEMUX" : "2:1 MUX")} facing {Facing}.");

        for (int u = 0; u < totalLengthU; u++)
        {
            for (int v = 0; v < widthV; v++)
            {
                builder.SetBlock(CircuitLayout.Map(0, 0, 0, u, 0, v, maxU, maxV, Facing), CircuitBlock);
            }
        }

        Direction torchFacing = CircuitLayout.RotateDirection(Direction.East, Facing);

        for (int s = 0; s < Stages; s++)
        {
            int v0 = s * stageWidthV;

            if (IncludeInputLevers)
            {
                if (Mode == MultiplexerMode.Multiplexer)
                {
                    builder.SetBlock(CircuitLayout.Map(0, 0, 0, 0, 1, v0, maxU, maxV, Facing), CircuitBlock);
                    builder.SetLever(CircuitLayout.Map(0, 0, 0, 0, 2, v0, maxU, maxV, Facing), BlockFace.Floor, Facing, powered: false);
                    builder.SetBlock(CircuitLayout.Map(0, 0, 0, 0, 1, v0 + 1, maxU, maxV, Facing), CircuitBlock);
                    builder.SetLever(CircuitLayout.Map(0, 0, 0, 0, 2, v0 + 1, maxU, maxV, Facing), BlockFace.Floor, Facing, powered: false);
                    builder.SetBlock(CircuitLayout.Map(0, 0, 0, 0, 1, v0 + 2, maxU, maxV, Facing), CircuitBlock);
                    builder.SetLever(CircuitLayout.Map(0, 0, 0, 0, 2, v0 + 2, maxU, maxV, Facing), BlockFace.Floor, Facing, powered: false);
                }
                else
                {
                    builder.SetBlock(CircuitLayout.Map(0, 0, 0, 0, 1, v0, maxU, maxV, Facing), CircuitBlock);
                    builder.SetLever(CircuitLayout.Map(0, 0, 0, 0, 2, v0, maxU, maxV, Facing), BlockFace.Floor, Facing, powered: false);
                    builder.SetBlock(CircuitLayout.Map(0, 0, 0, 0, 1, v0 + 1, maxU, maxV, Facing), CircuitBlock);
                    builder.SetLever(CircuitLayout.Map(0, 0, 0, 0, 2, v0 + 1, maxU, maxV, Facing), BlockFace.Floor, Facing, powered: false);
                }
            }

            if (Mode == MultiplexerMode.Multiplexer)
            {
                // Data A on v0, Select S on v0+1, Data B on v0+2:
                // Subtract comparator on v0 suppresses A when S=1; inverted !S on v0+2 suppresses B when S=0!
                builder.SetBlock(CircuitLayout.Map(0, 0, 0, inputLead, 1, v0, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));
                builder.SetBlock(CircuitLayout.Map(0, 0, 0, inputLead + 1, 1, v0, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));
                builder.SetBlock(CircuitLayout.Map(0, 0, 0, inputLead + 2, 1, v0, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));
                CircuitLayout.SetComparator(builder, CircuitLayout.Map(0, 0, 0, inputLead + 3, 1, v0, maxU, maxV, Facing), Facing, subtractMode: true);

                // Select bus along v0+1: S passes to (inputLead+3, v0) side directly, and inverter drives (inputLead+3, v0+2) via !S!
                builder.SetBlock(CircuitLayout.Map(0, 0, 0, inputLead, 1, v0 + 1, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));
                builder.SetBlock(CircuitLayout.Map(0, 0, 0, inputLead + 1, 1, v0 + 1, maxU, maxV, Facing), CircuitBlock);
                builder.SetBlock(
                    CircuitLayout.Map(0, 0, 0, inputLead + 2, 1, v0 + 1, maxU, maxV, Facing),
                    Minecraft.Blocks.RedstoneWallTorch.WithFacing(torchFacing).WithLit(true));
                builder.SetBlock(CircuitLayout.Map(0, 0, 0, inputLead + 3, 1, v0 + 1, maxU, maxV, Facing), CircuitLayout.CreateWire(Facing, north: true, east: false, south: true, west: true));

                // Data B on v0+2
                builder.SetBlock(CircuitLayout.Map(0, 0, 0, inputLead, 1, v0 + 2, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));
                builder.SetBlock(CircuitLayout.Map(0, 0, 0, inputLead + 1, 1, v0 + 2, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));
                builder.SetBlock(CircuitLayout.Map(0, 0, 0, inputLead + 2, 1, v0 + 2, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));
                CircuitLayout.SetComparator(builder, CircuitLayout.Map(0, 0, 0, inputLead + 3, 1, v0 + 2, maxU, maxV, Facing), Facing, subtractMode: true);

                // Merged output at u = inputLead + 4 across v0..v0+2 exiting at v0+1
                builder.SetBlock(CircuitLayout.Map(0, 0, 0, inputLead + 4, 1, v0, maxU, maxV, Facing), CircuitLayout.CreateWire(Facing, north: false, east: false, south: true, west: true));
                builder.SetBlock(CircuitLayout.Map(0, 0, 0, inputLead + 4, 1, v0 + 1, maxU, maxV, Facing), CircuitLayout.CreateWire(Facing, north: true, east: true, south: true, west: false));
                builder.SetBlock(CircuitLayout.Map(0, 0, 0, inputLead + 4, 1, v0 + 2, maxU, maxV, Facing), CircuitLayout.CreateWire(Facing, north: true, east: false, south: false, west: true));

                if (IncludeOutputLamps)
                {
                    builder.SetBlock(CircuitLayout.Map(0, 0, 0, totalLengthU - 1, 1, v0 + 1, maxU, maxV, Facing), Minecraft.Blocks.RedstoneLamp);
                }
            }
            else
            {
                // 1:2 Demultiplexer: Input D on v0 fans into v0 and v0+2 comparators; Select S on v0+1 enables Out1 or Out0.
                builder.SetBlock(CircuitLayout.Map(0, 0, 0, inputLead, 1, v0, maxU, maxV, Facing), CircuitLayout.CreateWire(Facing, north: false, east: true, south: true, west: true));
                builder.SetBlock(CircuitLayout.Map(0, 0, 0, inputLead, 1, v0 + 2, maxU, maxV, Facing), CircuitLayout.CreateWire(Facing, north: true, east: true, south: false, west: false));
                builder.SetBlock(CircuitLayout.Map(0, 0, 0, inputLead + 1, 1, v0, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));
                builder.SetBlock(CircuitLayout.Map(0, 0, 0, inputLead + 2, 1, v0, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));
                builder.SetBlock(CircuitLayout.Map(0, 0, 0, inputLead + 1, 1, v0 + 2, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));
                builder.SetBlock(CircuitLayout.Map(0, 0, 0, inputLead + 2, 1, v0 + 2, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));

                builder.SetBlock(CircuitLayout.Map(0, 0, 0, inputLead + 1, 1, v0 + 1, maxU, maxV, Facing), CircuitBlock);
                builder.SetBlock(
                    CircuitLayout.Map(0, 0, 0, inputLead + 2, 1, v0 + 1, maxU, maxV, Facing),
                    Minecraft.Blocks.RedstoneWallTorch.WithFacing(torchFacing).WithLit(true));
                builder.SetBlock(CircuitLayout.Map(0, 0, 0, inputLead + 3, 1, v0 + 1, maxU, maxV, Facing), CircuitLayout.CreateWire(Facing, north: true, east: false, south: false, west: true));

                CircuitLayout.SetComparator(builder, CircuitLayout.Map(0, 0, 0, inputLead + 3, 1, v0, maxU, maxV, Facing), Facing, subtractMode: true);
                CircuitLayout.SetComparator(builder, CircuitLayout.Map(0, 0, 0, inputLead + 3, 1, v0 + 2, maxU, maxV, Facing), Facing, subtractMode: false);

                builder.SetBlock(CircuitLayout.Map(0, 0, 0, inputLead + 4, 1, v0, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));
                builder.SetBlock(CircuitLayout.Map(0, 0, 0, inputLead + 4, 1, v0 + 2, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));

                if (IncludeOutputLamps)
                {
                    builder.SetBlock(CircuitLayout.Map(0, 0, 0, totalLengthU - 1, 1, v0, maxU, maxV, Facing), Minecraft.Blocks.RedstoneLamp);
                    builder.SetBlock(CircuitLayout.Map(0, 0, 0, totalLengthU - 1, 1, v0 + 2, maxU, maxV, Facing), Minecraft.Blocks.RedstoneLamp);
                }
            }
        }

        return builder;
    }
}
