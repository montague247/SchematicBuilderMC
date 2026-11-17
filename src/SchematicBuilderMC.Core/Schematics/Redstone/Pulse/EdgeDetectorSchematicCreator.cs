using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;
using SchematicBuilderMC.Extensions;
using SchematicBuilderMC.Models;
using SchematicBuilderMC.Registries;
using SchematicBuilderMC.Schematics.Redstone.Internal;

namespace SchematicBuilderMC.Schematics.Redstone.Pulse;

/// <summary>
/// Supported edge transitions detected by <see cref="EdgeDetectorSchematicCreator"/>.
/// </summary>
public enum EdgeDetectorType
{
    /// <summary>Fires a short positive pulse on 0 -&gt; 1 transitions.</summary>
    RisingEdge,

    /// <summary>Fires a short positive pulse on 1 -&gt; 0 transitions.</summary>
    FallingEdge,

    /// <summary>Fires a short pulse on both rising and falling transitions via lamp/bulb observation.</summary>
    DualEdge,

    /// <summary>Outputs continuous high signal and pulses OFF on 0 -&gt; 1 transitions.</summary>
    InvertedRisingEdge
}

/// <summary>
/// Builds a rising-edge, falling-edge, dual-edge, or inverted-edge Redstone detector with configurable output pulse duration.
/// </summary>
public class EdgeDetectorSchematicCreator : SchematicCreator
{
    public EdgeDetectorType EdgeType { get; set; } = EdgeDetectorType.RisingEdge;

    public TimeSpan? PulseDuration { get; set; }

    public int? PulseDurationTicks { get; set; }

    public Direction Facing { get; set; } = Direction.East;

    public string CircuitBlock { get; set; } = Minecraft.Blocks.SmoothStone;

    public bool IncludeInputLever { get; set; }

    public bool IncludeOutputLamp { get; set; }

    public int GetActualPulseTicks()
    {
        int raw = CircuitLayout.ResolveGameTicks(PulseDuration, PulseDurationTicks, defaultGameTicks: 2, minGameTicks: 2, stepGameTicks: 2);
        return Math.Clamp(raw, 2, 8);
    }

    public override SchematicBuilder Create()
    {
        if (!Enum.IsDefined(EdgeType))
        {
            throw new ArgumentOutOfRangeException(nameof(EdgeType), EdgeType, "Unsupported edge detector type.");
        }

        CircuitLayout.ValidateHorizontalFacing(Facing);
        CircuitLayout.ValidateBlockId(CircuitBlock, nameof(CircuitBlock));

        int pulseTicks = GetActualPulseTicks();
        int repeaterDelay = pulseTicks / 2;

        return EdgeType switch
        {
            EdgeDetectorType.RisingEdge => new PulseGeneratorSchematicCreator
            {
                Mode = PulseGeneratorMode.StickyPistonMonostable,
                PulseDurationTicks = pulseTicks,
                Facing = Facing,
                CircuitBlock = CircuitBlock,
                IncludeInputLever = IncludeInputLever,
                IncludeOutputLamp = IncludeOutputLamp,
                Rotation = Rotation
            }.Create(),
            EdgeDetectorType.FallingEdge => BuildFallingEdge(repeaterDelay, pulseTicks),
            EdgeDetectorType.DualEdge => BuildDualEdge(repeaterDelay, pulseTicks),
            EdgeDetectorType.InvertedRisingEdge => BuildInvertedRisingEdge(repeaterDelay, pulseTicks),
            _ => throw new ArgumentOutOfRangeException(nameof(EdgeType), EdgeType, "Unsupported edge detector type.")
        };
    }

    private SchematicBuilder BuildFallingEdge(int repeaterDelay, int pulseTicks)
    {
        int inputLead = IncludeInputLever ? 1 : 0;
        const int coreLengthU = 7;
        int totalLengthU = inputLead + coreLengthU + (IncludeOutputLamp ? 1 : 0);
        int maxU = totalLengthU - 1;

        var builder = CircuitLayout.CreateBuilder(
            this,
            CircuitLayout.GetWidth(totalLengthU, 1, Facing),
            3,
            CircuitLayout.GetLength(totalLengthU, 1, Facing),
            "Falling Edge Detector",
            $"Falling-edge detector ({pulseTicks} gt pulse on 1 -> 0) facing {Facing}.");

        for (int u = 0; u < totalLengthU; u++)
        {
            if (u == inputLead + 4)
            {
                continue;
            }

            builder.SetBlock(CircuitLayout.Map(0, 0, 0, u, 0, 0, maxU, 0, Facing), CircuitBlock);
        }

        if (IncludeInputLever)
        {
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, 0, 1, 0, maxU, 0, Facing), CircuitBlock);
            builder.SetLever(CircuitLayout.Map(0, 0, 0, 0, 2, 0, maxU, 0, Facing), BlockFace.Floor, Facing, powered: false);
        }

        // Front-end inverter (input 1 -> 0 becomes 0 -> 1) driving a piston monostable
        Direction torchFacing = CircuitLayout.RotateDirection(Direction.East, Facing);
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, inputLead, 1, 0, maxU, 0, Facing), CircuitLayout.CreateAxisWire(Facing, true));
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, inputLead + 1, 1, 0, maxU, 0, Facing), CircuitBlock);
        builder.SetBlock(
            CircuitLayout.Map(0, 0, 0, inputLead + 2, 1, 0, maxU, 0, Facing),
            Minecraft.Blocks.RedstoneWallTorch.WithFacing(torchFacing).WithLit(true));
        CircuitLayout.SetRepeater(builder, CircuitLayout.Map(0, 0, 0, inputLead + 3, 1, 0, maxU, 0, Facing), Facing, delay: 1);
        builder.SetPiston(CircuitLayout.Map(0, 0, 0, inputLead + 4, 0, 0, maxU, 0, Facing), Direction.Up, sticky: true);
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, inputLead + 4, 1, 0, maxU, 0, Facing), CircuitBlock);
        CircuitLayout.SetRepeater(builder, CircuitLayout.Map(0, 0, 0, inputLead + 5, 1, 0, maxU, 0, Facing), Facing, delay: repeaterDelay);
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, inputLead + 6, 1, 0, maxU, 0, Facing), CircuitLayout.CreateAxisWire(Facing, true));

        if (IncludeOutputLamp)
        {
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, totalLengthU - 1, 1, 0, maxU, 0, Facing), Minecraft.Blocks.RedstoneLamp);
        }

        return builder;
    }

    private SchematicBuilder BuildDualEdge(int repeaterDelay, int pulseTicks)
    {
        int inputLead = IncludeInputLever ? 1 : 0;
        const int coreLengthU = 5;
        int totalLengthU = inputLead + coreLengthU + (IncludeOutputLamp ? 1 : 0);
        int height = IncludeInputLever ? 3 : 2;
        int maxU = totalLengthU - 1;

        var builder = CircuitLayout.CreateBuilder(
            this,
            CircuitLayout.GetWidth(totalLengthU, 1, Facing),
            height,
            CircuitLayout.GetLength(totalLengthU, 1, Facing),
            "Dual Edge Detector",
            $"Dual-edge detector ({pulseTicks} gt pulse on both edges) facing {Facing}.");

        for (int u = 0; u < totalLengthU; u++)
        {
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, u, 0, 0, maxU, 0, Facing), CircuitBlock);
        }

        if (IncludeInputLever)
        {
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, 0, 1, 0, maxU, 0, Facing), CircuitBlock);
            builder.SetLever(CircuitLayout.Map(0, 0, 0, 0, 2, 0, maxU, 0, Facing), BlockFace.Floor, Facing, powered: false);
        }

        builder.SetBlock(CircuitLayout.Map(0, 0, 0, inputLead, 1, 0, maxU, 0, Facing), CircuitLayout.CreateAxisWire(Facing, true));
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, inputLead + 1, 1, 0, maxU, 0, Facing), Minecraft.Blocks.RedstoneLamp);
        CircuitLayout.SetObserver(
            builder,
            CircuitLayout.Map(0, 0, 0, inputLead + 2, 1, 0, maxU, 0, Facing),
            CircuitLayout.RotateDirection(Direction.West, Facing));
        CircuitLayout.SetRepeater(builder, CircuitLayout.Map(0, 0, 0, inputLead + 3, 1, 0, maxU, 0, Facing), Facing, delay: repeaterDelay);
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, inputLead + 4, 1, 0, maxU, 0, Facing), CircuitLayout.CreateAxisWire(Facing, true));

        if (IncludeOutputLamp)
        {
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, totalLengthU - 1, 1, 0, maxU, 0, Facing), Minecraft.Blocks.RedstoneLamp);
        }

        return builder;
    }

    private SchematicBuilder BuildInvertedRisingEdge(int repeaterDelay, int pulseTicks)
    {
        int inputLead = IncludeInputLever ? 1 : 0;
        const int coreLengthU = 7;
        int totalLengthU = inputLead + coreLengthU + (IncludeOutputLamp ? 1 : 0);
        int maxU = totalLengthU - 1;

        var builder = CircuitLayout.CreateBuilder(
            this,
            CircuitLayout.GetWidth(totalLengthU, 1, Facing),
            3,
            CircuitLayout.GetLength(totalLengthU, 1, Facing),
            "Inverted Rising Edge Detector",
            $"Inverted rising-edge detector ({pulseTicks} gt OFF pulse) facing {Facing}.");

        for (int u = 0; u < totalLengthU; u++)
        {
            if (u == inputLead + 2)
            {
                continue;
            }

            builder.SetBlock(CircuitLayout.Map(0, 0, 0, u, 0, 0, maxU, 0, Facing), CircuitBlock);
        }

        if (IncludeInputLever)
        {
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, 0, 1, 0, maxU, 0, Facing), CircuitBlock);
            builder.SetLever(CircuitLayout.Map(0, 0, 0, 0, 2, 0, maxU, 0, Facing), BlockFace.Floor, Facing, powered: false);
        }

        Direction torchFacing = CircuitLayout.RotateDirection(Direction.East, Facing);
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, inputLead, 1, 0, maxU, 0, Facing), CircuitLayout.CreateAxisWire(Facing, true));
        CircuitLayout.SetRepeater(builder, CircuitLayout.Map(0, 0, 0, inputLead + 1, 1, 0, maxU, 0, Facing), Facing, delay: 1);
        builder.SetPiston(CircuitLayout.Map(0, 0, 0, inputLead + 2, 0, 0, maxU, 0, Facing), Direction.Up, sticky: true);
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, inputLead + 2, 1, 0, maxU, 0, Facing), CircuitBlock);
        CircuitLayout.SetRepeater(builder, CircuitLayout.Map(0, 0, 0, inputLead + 3, 1, 0, maxU, 0, Facing), Facing, delay: repeaterDelay);
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, inputLead + 4, 1, 0, maxU, 0, Facing), CircuitBlock);
        builder.SetBlock(
            CircuitLayout.Map(0, 0, 0, inputLead + 5, 1, 0, maxU, 0, Facing),
            Minecraft.Blocks.RedstoneWallTorch.WithFacing(torchFacing).WithLit(true));
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, inputLead + 6, 1, 0, maxU, 0, Facing), CircuitLayout.CreateAxisWire(Facing, true));

        if (IncludeOutputLamp)
        {
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, totalLengthU - 1, 1, 0, maxU, 0, Facing), Minecraft.Blocks.RedstoneLamp);
        }

        return builder;
    }
}
