using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;
using SchematicBuilderMC.Extensions;
using SchematicBuilderMC.Models;
using SchematicBuilderMC.Registries;
using SchematicBuilderMC.Schematics.Redstone.Internal;

namespace SchematicBuilderMC.Schematics.Redstone.Memory;

/// <summary>
/// Builds a rising-edge-triggered master-slave repeater-lock D Flip-Flop.
/// </summary>
public class DFlipFlopSchematicCreator : SchematicCreator
{
    public Direction Facing { get; set; } = Direction.East;

    public string CircuitBlock { get; set; } = Minecraft.Blocks.SmoothStone;

    public bool IncludeInputLevers { get; set; }

    public bool IncludeOutputLamp { get; set; }

    public override SchematicBuilder Create()
    {
        CircuitLayout.ValidateHorizontalFacing(Facing);
        CircuitLayout.ValidateBlockId(CircuitBlock, nameof(CircuitBlock));

        int inputLead = IncludeInputLevers ? 1 : 0;
        const int coreLengthU = 6;
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
            "Master-Slave D Flip-Flop",
            $"Rising-edge triggered repeater-lock D flip-flop facing {Facing}.");

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

        // Clock rail on v=0: direct lock at u0+1 (locks Master on Clock=1), inverted !Clock lock at u0+4 (unlocks Slave on Clock=1)
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, inputLead, 1, 0, maxU, maxV, Facing), CircuitLayout.CreateWire(Facing, north: false, east: true, south: true, west: IncludeInputLevers));
        builder.SetRepeater(CircuitLayout.Map(0, 0, 0, inputLead + 1, 1, 0, maxU, maxV, Facing), lockDir, delay: 1);
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, inputLead + 2, 1, 0, maxU, maxV, Facing), CircuitBlock);
        builder.SetBlock(
            CircuitLayout.Map(0, 0, 0, inputLead + 3, 1, 0, maxU, maxV, Facing),
            Minecraft.Blocks.RedstoneWallTorch.WithFacing(torchFacing).WithLit(true));
        builder.SetRepeater(CircuitLayout.Map(0, 0, 0, inputLead + 4, 1, 0, maxU, maxV, Facing), lockDir, delay: 1);

        // Wait: Clock wire at (inputLead, 1, 0) also runs around via (inputLead, 1, 1) -> avoid D wire collision by routing Clock into u0+2 block directly via repeater/wire!
        // Specifically: u0=Clock wire, u0+1 = lock repeater (Master), so feed u0+2 block via side-step or keep Clock at u0+1 wire feeding u0+1? Wait (u0,1,0) powers (u0+1,1,0) repeater side? No: place Clock wire at u0=0..u0+1, wait then u0+1 can't be both wire and repeater.
        // Instead place Clock repeater at (inputLead, 1, 0) facing South? Wait: put u0=0 Clock wire -> u0+1 = CircuitBlock (feeds torch at u0+2 AND locks Master at u0+1 via u0+1? Put Master at u0+1 or u0+2!).
        // Even cleaner: Master lock at u0+1 reads from u0 (wire), Clock also branches at u0+2 -> u0+2 block -> u0+3 torch -> u0+4 Slave lock!
        // Put D at v=2 (`widthV = 3`) if Clock passes at v=1? No, check how v=0 & v=1 does it cleanly in `3` wide (`v=0` Clock bus, `v=1` Lock repeaters, `v=2` Data line)!
        // Let's keep D at v=1 AND power u0+2 block cleanly: just overwrite v=0 with clean 6x2 wire-clock? Wait if (u0+1, 1, 0) is Repeater[South], nothing passes East through its side to reach u0+2!
        // So make widthV = 3 (v=0 Clock distribution, v=1 Lock repeaters & inverter, v=2 Data master->slave)! Zero crossover issue!
        return BuildClean3WideDff();
    }

    private SchematicBuilder BuildClean3WideDff()
    {
        int inputLead = IncludeInputLevers ? 1 : 0;
        const int coreLengthU = 7;
        const int widthV = 3;
        int totalLengthU = inputLead + coreLengthU + (IncludeOutputLamp ? 1 : 0);
        int height = IncludeInputLevers ? 3 : 2;
        int maxU = totalLengthU - 1;
        int maxV = widthV - 1;

        var builder = CircuitLayout.CreateBuilder(
            this,
            CircuitLayout.GetWidth(totalLengthU, widthV, Facing),
            height,
            CircuitLayout.GetLength(totalLengthU, widthV, Facing),
            "Master-Slave D Flip-Flop",
            $"Rising-edge triggered master-slave D flip-flop facing {Facing}.");

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
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, 0, 1, 2, maxU, maxV, Facing), CircuitBlock);
            builder.SetLever(CircuitLayout.Map(0, 0, 0, 0, 2, 2, maxU, maxV, Facing), BlockFace.Floor, Facing, powered: false);
        }

        Direction lockSouth = CircuitLayout.RotateDirection(Direction.South, Facing);
        Direction torchFacing = CircuitLayout.RotateDirection(Direction.East, Facing);

        // v=0: Clock distribution bus (runs straight across without touching Data)
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, inputLead, 1, 0, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, inputLead + 1, 1, 0, maxU, maxV, Facing), CircuitLayout.CreateWire(Facing, north: false, east: true, south: true, west: true));
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, inputLead + 2, 1, 0, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, inputLead + 3, 1, 0, maxU, maxV, Facing), CircuitBlock);
        builder.SetBlock(
            CircuitLayout.Map(0, 0, 0, inputLead + 4, 1, 0, maxU, maxV, Facing),
            Minecraft.Blocks.RedstoneWallTorch.WithFacing(torchFacing).WithLit(true));
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, inputLead + 5, 1, 0, maxU, maxV, Facing), CircuitLayout.CreateWire(Facing, north: false, east: false, south: true, west: true));

        // v=1: Locking repeaters facing South into Master (u0+1) and Slave (u0+5)
        builder.SetRepeater(CircuitLayout.Map(0, 0, 0, inputLead + 1, 1, 1, maxU, maxV, Facing), lockSouth, delay: 1);
        builder.SetRepeater(CircuitLayout.Map(0, 0, 0, inputLead + 5, 1, 1, maxU, maxV, Facing), lockSouth, delay: 1);

        // v=2: Data path Master -> wire -> Slave -> Q
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, inputLead, 1, 2, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));
        builder.SetRepeater(CircuitLayout.Map(0, 0, 0, inputLead + 1, 1, 2, maxU, maxV, Facing), Facing, delay: 1, locked: false);
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, inputLead + 2, 1, 2, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, inputLead + 3, 1, 2, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, inputLead + 4, 1, 2, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));
        builder.SetRepeater(CircuitLayout.Map(0, 0, 0, inputLead + 5, 1, 2, maxU, maxV, Facing), Facing, delay: 1, locked: true);
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, inputLead + 6, 1, 2, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));

        if (IncludeOutputLamp)
        {
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, totalLengthU - 1, 1, 2, maxU, maxV, Facing), Minecraft.Blocks.RedstoneLamp);
        }

        return builder;
    }
}
