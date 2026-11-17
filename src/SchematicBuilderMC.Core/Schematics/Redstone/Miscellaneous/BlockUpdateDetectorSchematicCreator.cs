using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;
using SchematicBuilderMC.Models;
using SchematicBuilderMC.Registries;
using SchematicBuilderMC.Schematics.Redstone.Internal;

namespace SchematicBuilderMC.Schematics.Redstone.Miscellaneous;

/// <summary>
/// Supported Block Update Detector (BUD) designs on Minecraft Wiki.
/// </summary>
public enum BudMode
{
    /// <summary>Compact observer sensing block-state changes on the front face with optional pulse-shaping repeater.</summary>
    Observer,

    /// <summary>Classic quasi-connectivity piston + redstone block BUD detecting adjacent updates.</summary>
    PistonBud,

    /// <summary>Quasi-connectivity dropper/comparator BUD detecting inventory or adjacent updates cleanly.</summary>
    TorchDropper,

    /// <summary>Water/observer or block-placement-only BUD sensor reacting to solid block placement at the target spot.</summary>
    PlaceOnly
}

/// <summary>
/// Builds a configurable Redstone Block Update Detector (BUD) circuit.
/// </summary>
public class BlockUpdateDetectorSchematicCreator : SchematicCreator
{
    public BudMode Mode { get; set; } = BudMode.PistonBud;

    public Direction Facing { get; set; } = Direction.East;

    public string CircuitBlock { get; set; } = Minecraft.Blocks.SmoothStone;

    public bool IncludeOutputLamp { get; set; } = true;

    public override SchematicBuilder Create()
    {
        if (!Enum.IsDefined(Mode))
        {
            throw new ArgumentOutOfRangeException(nameof(Mode), Mode, "Unsupported BUD mode.");
        }

        CircuitLayout.ValidateHorizontalFacing(Facing);
        CircuitLayout.ValidateBlockId(CircuitBlock, nameof(CircuitBlock));

        return Mode switch
        {
            BudMode.Observer => BuildObserverBud(),
            BudMode.PistonBud => BuildPistonBud(),
            BudMode.TorchDropper => BuildTorchDropperBud(),
            BudMode.PlaceOnly => BuildPlaceOnlyBud(),
            _ => throw new ArgumentOutOfRangeException(nameof(Mode), Mode, "Unsupported BUD mode.")
        };
    }

    private SchematicBuilder BuildObserverBud()
    {
        int coreLengthU = IncludeOutputLamp ? 4 : 3;
        int maxU = coreLengthU - 1;
        var builder = CircuitLayout.CreateBuilder(
            this,
            CircuitLayout.GetWidth(coreLengthU, 1, Facing),
            2,
            CircuitLayout.GetLength(coreLengthU, 1, Facing),
            "Observer BUD",
            $"Observer block update detector facing {Facing}.");

        for (int u = 0; u < coreLengthU; u++)
        {
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, u, 0, 0, maxU, 0, Facing), CircuitBlock);
        }

        CircuitLayout.SetObserver(builder, CircuitLayout.Map(0, 0, 0, 0, 1, 0, maxU, 0, Facing), CircuitLayout.GetOpposite(Facing));
        builder.SetRepeater(CircuitLayout.Map(0, 0, 0, 1, 1, 0, maxU, 0, Facing), Facing, delay: 1);
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, 2, 1, 0, maxU, 0, Facing), CircuitLayout.CreateAxisWire(Facing, true));

        if (IncludeOutputLamp)
        {
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, 3, 1, 0, maxU, 0, Facing), Minecraft.Blocks.RedstoneLamp);
        }

        return builder;
    }

    private SchematicBuilder BuildPistonBud()
    {
        const int coreLengthU = 5;
        const int widthV = 2;
        int totalLengthU = coreLengthU + (IncludeOutputLamp ? 1 : 0);
        int maxU = totalLengthU - 1;
        int maxV = widthV - 1;

        var builder = CircuitLayout.CreateBuilder(
            this,
            CircuitLayout.GetWidth(totalLengthU, widthV, Facing),
            3,
            CircuitLayout.GetLength(totalLengthU, widthV, Facing),
            "Piston BUD",
            $"Quasi-connectivity sticky-piston BUD sensor facing {Facing}.");

        for (int u = 0; u < totalLengthU; u++)
        {
            for (int v = 0; v < widthV; v++)
            {
                builder.SetBlock(CircuitLayout.Map(0, 0, 0, u, 0, v, maxU, maxV, Facing), CircuitBlock);
            }
        }

        // Piston at (1,1,0) BUD-powered via RedstoneBlock pushed onto (2,1,0) resetting via Repeater on v=1
        builder.SetPiston(CircuitLayout.Map(0, 0, 0, 1, 1, 0, maxU, maxV, Facing), Facing, sticky: true);
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, 2, 1, 0, maxU, maxV, Facing), Minecraft.Blocks.RedstoneBlock);
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, 1, 2, 0, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, 2, 2, 0, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, 3, 1, 0, maxU, maxV, Facing), CircuitLayout.CreateWire(Facing, north: false, east: true, south: true, west: true));
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, 4, 1, 0, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));

        // Reset branch along v=1
        builder.SetRepeater(CircuitLayout.Map(0, 0, 0, 3, 1, 1, maxU, maxV, Facing), CircuitLayout.GetOpposite(Facing), delay: 2);
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, 2, 1, 1, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, 1, 1, 1, maxU, maxV, Facing), CircuitLayout.CreateWire(Facing, north: true, east: true, south: false, west: false));

        if (IncludeOutputLamp)
        {
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, totalLengthU - 1, 1, 0, maxU, maxV, Facing), Minecraft.Blocks.RedstoneLamp);
        }

        return builder;
    }

    private SchematicBuilder BuildTorchDropperBud()
    {
        int totalLengthU = 4 + (IncludeOutputLamp ? 1 : 0);
        int maxU = totalLengthU - 1;
        var builder = CircuitLayout.CreateBuilder(
            this,
            CircuitLayout.GetWidth(totalLengthU, 1, Facing),
            3,
            CircuitLayout.GetLength(totalLengthU, 1, Facing),
            "Torch-Dropper BUD",
            $"Quasi-connectivity dropper + redstone torch BUD facing {Facing}.");

        for (int u = 0; u < totalLengthU; u++)
        {
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, u, 0, 0, maxU, 0, Facing), CircuitBlock);
        }

        builder.SetBlock(CircuitLayout.Map(0, 0, 0, 1, 1, 0, maxU, 0, Facing), Minecraft.Blocks.RedstoneBlock);
        CircuitLayout.SetDropper(builder, CircuitLayout.Map(0, 0, 0, 1, 2, 0, maxU, 0, Facing), Facing);
        builder.SetComparator(CircuitLayout.Map(0, 0, 0, 2, 2, 0, maxU, 0, Facing), Facing, subtractMode: false);
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, 3, 1, 0, maxU, 0, Facing), CircuitLayout.CreateAxisWire(Facing, true));

        if (IncludeOutputLamp)
        {
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, totalLengthU - 1, 1, 0, maxU, 0, Facing), Minecraft.Blocks.RedstoneLamp);
        }

        return builder;
    }

    private SchematicBuilder BuildPlaceOnlyBud()
    {
        int totalLengthU = 4 + (IncludeOutputLamp ? 1 : 0);
        int maxU = totalLengthU - 1;
        var builder = CircuitLayout.CreateBuilder(
            this,
            CircuitLayout.GetWidth(totalLengthU, 1, Facing),
            3,
            CircuitLayout.GetLength(totalLengthU, 1, Facing),
            "Block-Placement BUD",
            $"Redstone conductor placement-only sensor facing {Facing}.");

        for (int u = 0; u < totalLengthU; u++)
        {
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, u, 0, 0, maxU, 0, Facing), CircuitBlock);
        }

        // Strongly powered repeater at u=0 conducts through target block gap at u=1 ONLY when an opaque block is placed at u=1!
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, 0, 1, 0, maxU, 0, Facing), Minecraft.Blocks.RedstoneTorch);
        builder.SetRepeater(CircuitLayout.Map(0, 0, 0, 0, 2, 0, maxU, 0, Facing), Facing, delay: 1);
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, 2, 2, 0, maxU, 0, Facing), CircuitLayout.CreateAxisWire(Facing, true));
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, 3, 1, 0, maxU, 0, Facing), CircuitLayout.CreateAxisWire(Facing, true));

        if (IncludeOutputLamp)
        {
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, totalLengthU - 1, 1, 0, maxU, 0, Facing), Minecraft.Blocks.RedstoneLamp);
        }

        return builder;
    }
}
