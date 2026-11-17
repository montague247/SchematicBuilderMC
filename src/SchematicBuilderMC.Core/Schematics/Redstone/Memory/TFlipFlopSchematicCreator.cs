using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;
using SchematicBuilderMC.Models;
using SchematicBuilderMC.Registries;
using SchematicBuilderMC.Schematics.Redstone.Internal;

namespace SchematicBuilderMC.Schematics.Redstone.Memory;

/// <summary>
/// Supported T-Flip-Flop (toggle flip-flop) architectures.
/// </summary>
public enum TFlipFlopMode
{
    /// <summary>Compact 1.21 waxed copper bulb + comparator T-flip-flop.</summary>
    CopperBulb,

    /// <summary>Classic piston + redstone-block toggle triggered via 2-gt sticky-piston pulse limiter.</summary>
    Piston,

    /// <summary>Silent facing-dropper/hopper 1-item toggle flip-flop.</summary>
    DropperHopper
}

/// <summary>
/// Builds a toggle (T) flip-flop that alternates its stored output state on every rising input edge.
/// </summary>
public class TFlipFlopSchematicCreator : SchematicCreator
{
    public TFlipFlopMode Mode { get; set; } = TFlipFlopMode.CopperBulb;

    public Direction Facing { get; set; } = Direction.East;

    public string CircuitBlock { get; set; } = Minecraft.Blocks.SmoothStone;

    public string ItemId { get; set; } = Minecraft.Items.Cobblestone;

    public bool IncludeInputButton { get; set; }

    public bool IncludeOutputLamp { get; set; }

    public override SchematicBuilder Create()
    {
        if (!Enum.IsDefined(Mode))
        {
            throw new ArgumentOutOfRangeException(nameof(Mode), Mode, "Unsupported T flip-flop mode.");
        }

        CircuitLayout.ValidateHorizontalFacing(Facing);
        CircuitLayout.ValidateBlockId(CircuitBlock, nameof(CircuitBlock));
        CircuitLayout.ValidateBlockId(ItemId, nameof(ItemId));

        return Mode switch
        {
            TFlipFlopMode.CopperBulb => BuildCopperBulb(),
            TFlipFlopMode.Piston => BuildPiston(),
            TFlipFlopMode.DropperHopper => BuildDropperHopper(),
            _ => throw new ArgumentOutOfRangeException(nameof(Mode), Mode, "Unsupported T flip-flop mode.")
        };
    }

    private SchematicBuilder BuildCopperBulb()
    {
        int inputLead = IncludeInputButton ? 1 : 0;
        const int coreLengthU = 4;
        int totalLengthU = inputLead + coreLengthU + (IncludeOutputLamp ? 1 : 0);
        int height = IncludeInputButton ? 3 : 2;
        int maxU = totalLengthU - 1;

        var builder = CircuitLayout.CreateBuilder(
            this,
            CircuitLayout.GetWidth(totalLengthU, 1, Facing),
            height,
            CircuitLayout.GetLength(totalLengthU, 1, Facing),
            "Copper-Bulb T Flip-Flop",
            $"Waxed copper bulb 1-wide T flip-flop facing {Facing}.");

        for (int u = 0; u < totalLengthU; u++)
        {
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, u, 0, 0, maxU, 0, Facing), CircuitBlock);
        }

        if (IncludeInputButton)
        {
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, 0, 1, 0, maxU, 0, Facing), CircuitBlock);
            CircuitLayout.SetButton(builder, CircuitLayout.Map(0, 0, 0, 0, 2, 0, maxU, 0, Facing), BlockFace.Floor, Facing);
        }

        builder.SetBlock(CircuitLayout.Map(0, 0, 0, inputLead, 1, 0, maxU, 0, Facing), CircuitLayout.CreateAxisWire(Facing, true));
        CircuitLayout.SetCopperBulb(builder, CircuitLayout.Map(0, 0, 0, inputLead + 1, 1, 0, maxU, 0, Facing));
        builder.SetComparator(CircuitLayout.Map(0, 0, 0, inputLead + 2, 1, 0, maxU, 0, Facing), Facing, subtractMode: false);
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, inputLead + 3, 1, 0, maxU, 0, Facing), CircuitLayout.CreateAxisWire(Facing, true));

        if (IncludeOutputLamp)
        {
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, totalLengthU - 1, 1, 0, maxU, 0, Facing), Minecraft.Blocks.RedstoneLamp);
        }

        return builder;
    }

    private SchematicBuilder BuildPiston()
    {
        int inputLead = IncludeInputButton ? 1 : 0;
        const int coreLengthU = 7;
        const int widthV = 2;
        int totalLengthU = inputLead + coreLengthU + (IncludeOutputLamp ? 1 : 0);
        const int height = 3;
        int maxU = totalLengthU - 1;
        int maxV = widthV - 1;

        var builder = CircuitLayout.CreateBuilder(
            this,
            CircuitLayout.GetWidth(totalLengthU, widthV, Facing),
            height,
            CircuitLayout.GetLength(totalLengthU, widthV, Facing),
            "Piston T Flip-Flop",
            $"2-tick monostable sticky-piston redstone-block T flip-flop facing {Facing}.");

        for (int u = 0; u < totalLengthU; u++)
        {
            for (int v = 0; v < widthV; v++)
            {
                if (u == inputLead + 2 && v == 0)
                {
                    continue;
                }

                builder.SetBlock(CircuitLayout.Map(0, 0, 0, u, 0, v, maxU, maxV, Facing), CircuitBlock);
            }
        }

        if (IncludeInputButton)
        {
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, 0, 1, 0, maxU, maxV, Facing), CircuitBlock);
            CircuitLayout.SetButton(builder, CircuitLayout.Map(0, 0, 0, 0, 2, 0, maxU, maxV, Facing), BlockFace.Floor, Facing);
        }

        // Rising-edge 2-gt pulse limiter (u0..u0+2) feeds toggle sticky piston (u0+4) sliding RedstoneBlock between u0+5 and u0+6
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, inputLead, 1, 0, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));
        builder.SetRepeater(CircuitLayout.Map(0, 0, 0, inputLead + 1, 1, 0, maxU, maxV, Facing), Facing, delay: 1);
        builder.SetPiston(CircuitLayout.Map(0, 0, 0, inputLead + 2, 0, 0, maxU, maxV, Facing), Direction.Up, sticky: true);
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, inputLead + 2, 1, 0, maxU, maxV, Facing), CircuitBlock);
        builder.SetRepeater(CircuitLayout.Map(0, 0, 0, inputLead + 3, 1, 0, maxU, maxV, Facing), Facing, delay: 1);
        builder.SetPiston(CircuitLayout.Map(0, 0, 0, inputLead + 4, 1, 0, maxU, maxV, Facing), Facing, sticky: true);
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, inputLead + 5, 1, 0, maxU, maxV, Facing), Minecraft.Blocks.RedstoneBlock);

        // Output wire beside extended target (u0+6, 1, 1)
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, inputLead + 6, 1, 1, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));

        if (IncludeOutputLamp)
        {
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, totalLengthU - 1, 1, 1, maxU, maxV, Facing), Minecraft.Blocks.RedstoneLamp);
        }

        return builder;
    }

    private SchematicBuilder BuildDropperHopper()
    {
        int inputLead = IncludeInputButton ? 1 : 0;
        const int coreLengthU = 5;
        const int widthV = 2;
        int totalLengthU = inputLead + coreLengthU + (IncludeOutputLamp ? 1 : 0);
        int height = IncludeInputButtonsOrDefaultHeight();
        int maxU = totalLengthU - 1;
        int maxV = widthV - 1;

        var builder = CircuitLayout.CreateBuilder(
            this,
            CircuitLayout.GetWidth(totalLengthU, widthV, Facing),
            height,
            CircuitLayout.GetLength(totalLengthU, widthV, Facing),
            "Dropper-Hopper T Flip-Flop",
            $"Silent 1-item dropper-hopper T flip-flop facing {Facing}.");

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

        Direction reverse = CircuitLayout.GetOpposite(Facing);
        Direction sideSouth = CircuitLayout.RotateDirection(Direction.South, Facing);

        // Classic 3-dropper + 1-hopper ring toggling item between front hopper & comparator dropper
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, inputLead, 1, 0, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));
        CircuitLayout.SetDropper(builder, CircuitLayout.Map(0, 0, 0, inputLead + 1, 1, 0, maxU, maxV, Facing), Facing, new ItemStack(ItemId, 1, 0));
        CircuitLayout.SetDropper(builder, CircuitLayout.Map(0, 0, 0, inputLead + 2, 1, 0, maxU, maxV, Facing), sideSouth);
        CircuitLayout.SetHopper(builder, CircuitLayout.Map(0, 0, 0, inputLead + 1, 1, 1, maxU, maxV, Facing), Direction.Up, enabled: true);
        CircuitLayout.SetDropper(builder, CircuitLayout.Map(0, 0, 0, inputLead + 2, 1, 1, maxU, maxV, Facing), reverse);
        builder.SetComparator(CircuitLayout.Map(0, 0, 0, inputLead + 3, 1, 1, maxU, maxV, Facing), Facing, subtractMode: false);
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, inputLead + 4, 1, 1, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));

        if (IncludeOutputLamp)
        {
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, totalLengthU - 1, 1, 1, maxU, maxV, Facing), Minecraft.Blocks.RedstoneLamp);
        }

        return builder;
    }

    private int IncludeInputButtonsOrDefaultHeight() => IncludeInputButton ? 3 : 2;
}
