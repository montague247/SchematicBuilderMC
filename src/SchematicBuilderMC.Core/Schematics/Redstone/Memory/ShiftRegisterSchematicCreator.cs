using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;
using SchematicBuilderMC.Models;
using SchematicBuilderMC.Registries;
using SchematicBuilderMC.Schematics.Redstone.Internal;

namespace SchematicBuilderMC.Schematics.Redstone.Memory;

/// <summary>
/// Builds a configurable N-bit Serial-In Parallel-Out (SIPO) shift register using synchronized locking repeaters
/// with parallel output taps per bit stage.
/// </summary>
public class ShiftRegisterSchematicCreator : SchematicCreator
{
    /// <summary>
    /// Number of register bit stages (&gt;= 2). Defaults to 4.
    /// </summary>
    public int BitCount { get; set; } = 4;

    public Direction Facing { get; set; } = Direction.East;

    public string CircuitBlock { get; set; } = Minecraft.Blocks.SmoothStone;

    public bool IncludeInputControls { get; set; }

    public bool IncludeOutputLamps { get; set; }

    public override SchematicBuilder Create()
    {
        CircuitLayout.ValidateHorizontalFacing(Facing);
        if (BitCount < 2 || BitCount > 16)
        {
            throw new ArgumentOutOfRangeException(nameof(BitCount), BitCount, "BitCount must be between 2 and 16.");
        }

        CircuitLayout.ValidateBlockId(CircuitBlock, nameof(CircuitBlock));

        int inputLead = IncludeInputControls ? 1 : 0;
        int coreLengthU = 1 + (BitCount * 2);
        int widthV = IncludeOutputLamps ? 4 : 3;
        int totalLengthU = inputLead + coreLengthU;
        int height = IncludeInputControls ? 3 : 2;
        int maxU = totalLengthU - 1;
        int maxV = widthV - 1;

        var builder = CircuitLayout.CreateBuilder(
            this,
            CircuitLayout.GetWidth(totalLengthU, widthV, Facing),
            height,
            CircuitLayout.GetLength(totalLengthU, widthV, Facing),
            $"{BitCount}-Bit Shift Register",
            $"Locked-repeater {BitCount}-bit shift register facing {Facing}.");

        for (int u = 0; u < totalLengthU; u++)
        {
            for (int v = 0; v < widthV; v++)
            {
                builder.SetBlock(CircuitLayout.Map(0, 0, 0, u, 0, v, maxU, maxV, Facing), CircuitBlock);
            }
        }

        if (IncludeInputControls)
        {
            // Clock button on v=0, Data lever on v=2
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, 0, 1, 0, maxU, maxV, Facing), CircuitBlock);
            CircuitLayout.SetButton(builder, CircuitLayout.Map(0, 0, 0, 0, 2, 0, maxU, maxV, Facing), BlockFace.Floor, Facing);
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, 0, 1, 2, maxU, maxV, Facing), CircuitBlock);
            builder.SetLever(CircuitLayout.Map(0, 0, 0, 0, 2, 2, maxU, maxV, Facing), BlockFace.Floor, Facing, powered: false);
        }

        Direction lockDir = CircuitLayout.RotateDirection(Direction.South, Facing);

        // v=0: Shift Clock wire bus (repeating boost every 15 blocks)
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, inputLead, 1, 0, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));

        // v=2: Data input wire
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, inputLead, 1, 2, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));

        int wireRun = 0;
        for (int b = 0; b < BitCount; b++)
        {
            int repU = inputLead + 1 + (b * 2);
            int tapU = repU + 1;

            // Clock distribution along v=0 feeding locking repeater on v=1
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, repU, 1, 0, maxU, maxV, Facing), CircuitLayout.CreateWire(Facing, north: false, east: true, south: true, west: true));
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, tapU, 1, 0, maxU, maxV, Facing), wireRun >= 12 ? CircuitLayout.CreateAxisWire(Facing, true) : CircuitLayout.CreateAxisWire(Facing, true));
            wireRun += 2;
            if (wireRun >= 14)
            {
                wireRun = 0;
            }

            CircuitLayout.SetRepeater(builder, CircuitLayout.Map(0, 0, 0, repU, 1, 1, maxU, maxV, Facing), lockDir, delay: 1);

            // Data shift stage + output tap onto v=3 lamp row
            CircuitLayout.SetRepeater(builder, CircuitLayout.Map(0, 0, 0, repU, 1, 2, maxU, maxV, Facing), Facing, delay: 1, locked: true);
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, tapU, 1, 2, maxU, maxV, Facing), CircuitLayout.CreateWire(Facing, north: false, east: true, south: IncludeOutputLamps, west: true));

            if (IncludeOutputLamps)
            {
                builder.SetBlock(CircuitLayout.Map(0, 0, 0, tapU, 1, 3, maxU, maxV, Facing), Minecraft.Blocks.RedstoneLamp);
            }
        }

        return builder;
    }
}
