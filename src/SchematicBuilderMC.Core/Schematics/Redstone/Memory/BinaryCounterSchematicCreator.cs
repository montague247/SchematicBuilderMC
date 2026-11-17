using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;
using SchematicBuilderMC.Extensions;
using SchematicBuilderMC.Models;
using SchematicBuilderMC.Registries;
using SchematicBuilderMC.Schematics.Redstone.Internal;

namespace SchematicBuilderMC.Schematics.Redstone.Memory;

/// <summary>
/// Builds a configurable N-bit ripple-carry binary counter (0 .. 2^BitCount - 1) using waxed copper bulbs
/// and redstone-torch falling-edge carry stages, with parallel bit output taps and optional lamps.
/// </summary>
public class BinaryCounterSchematicCreator : SchematicCreator
{
    /// <summary>
    /// Number of counter bits (1 to 16). Defaults to 4 (modulo-16 counter).
    /// </summary>
    public int BitCount { get; set; } = 4;

    public Direction Facing { get; set; } = Direction.East;

    public string CircuitBlock { get; set; } = Minecraft.Blocks.SmoothStone;

    public bool IncludeInputButton { get; set; }

    public bool IncludeOutputLamps { get; set; }

    public override SchematicBuilder Create()
    {
        CircuitLayout.ValidateHorizontalFacing(Facing);
        if (BitCount < 1 || BitCount > 16)
        {
            throw new ArgumentOutOfRangeException(nameof(BitCount), BitCount, "BitCount must be between 1 and 16.");
        }

        CircuitLayout.ValidateBlockId(CircuitBlock, nameof(CircuitBlock));

        int inputLead = IncludeInputButton ? 1 : 0;
        // Per bit: bulb + comparator + (for bit < BitCount-1: solid block + wall torch)
        int coreLengthU = 1 + (BitCount * 4) - 2;
        int widthV = IncludeOutputLamps ? 3 : 2;
        int totalLengthU = inputLead + coreLengthU;
        int height = IncludeInputButton ? 3 : 2;
        int maxU = totalLengthU - 1;
        int maxV = widthV - 1;

        var builder = CircuitLayout.CreateBuilder(
            this,
            CircuitLayout.GetWidth(totalLengthU, widthV, Facing),
            height,
            CircuitLayout.GetLength(totalLengthU, widthV, Facing),
            $"{BitCount}-Bit Binary Counter",
            $"Ripple-carry {BitCount}-bit copper-bulb binary counter (mod {1 << BitCount}) facing {Facing}.");

        for (int u = 0; u < totalLengthU; u++)
        {
            for (int v = 0; v < widthV; v++)
            {
                builder.SetBlock(CircuitLayout.Map(0, 0, 0, u, 0, v, maxU, maxV, Facing), CircuitBlock);
            }
        }

        if (IncludeInputButton)
        {
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, 0, 1, 0, maxU, maxV, Facing), CircuitBlock);
            CircuitLayout.SetButton(builder, CircuitLayout.Map(0, 0, 0, 0, 2, 0, maxU, maxV, Facing), BlockFace.Floor, Facing);
        }

        Direction torchFacing = CircuitLayout.RotateDirection(Direction.East, Facing);
        Direction tapDir = CircuitLayout.RotateDirection(Direction.South, Facing);

        builder.SetBlock(CircuitLayout.Map(0, 0, 0, inputLead, 1, 0, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));

        for (int b = 0; b < BitCount; b++)
        {
            int bulbU = inputLead + 1 + (b * 4);
            CircuitLayout.SetCopperBulb(builder, CircuitLayout.Map(0, 0, 0, bulbU, 1, 0, maxU, maxV, Facing));

            // Bit output tap along v=1 (+ optional lamp at v=2)
            builder.SetComparator(CircuitLayout.Map(0, 0, 0, bulbU, 1, 1, maxU, maxV, Facing), tapDir, subtractMode: false);
            if (IncludeOutputLamps)
            {
                builder.SetBlock(CircuitLayout.Map(0, 0, 0, bulbU, 1, 2, maxU, maxV, Facing), Minecraft.Blocks.RedstoneLamp);
            }

            if (b < BitCount - 1)
            {
                builder.SetComparator(CircuitLayout.Map(0, 0, 0, bulbU + 1, 1, 0, maxU, maxV, Facing), Facing, subtractMode: false);
                builder.SetBlock(CircuitLayout.Map(0, 0, 0, bulbU + 2, 1, 0, maxU, maxV, Facing), CircuitBlock);
                builder.SetBlock(
                    CircuitLayout.Map(0, 0, 0, bulbU + 3, 1, 0, maxU, maxV, Facing),
                    Minecraft.Blocks.RedstoneWallTorch.WithFacing(torchFacing).WithLit(true));
            }
        }

        return builder;
    }
}
