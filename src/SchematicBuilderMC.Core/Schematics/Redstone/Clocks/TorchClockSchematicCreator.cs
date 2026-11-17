using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;
using SchematicBuilderMC.Extensions;
using SchematicBuilderMC.Models;
using SchematicBuilderMC.Registries;
using SchematicBuilderMC.Schematics.Redstone.Internal;

namespace SchematicBuilderMC.Schematics.Redstone.Clocks;

/// <summary>
/// Supported redstone-torch clock architectures.
/// </summary>
public enum TorchClockMode
{
    /// <summary>1 redstone wall torch inverter in series with repeaters (burnout-free for period &gt;= 8 gt).</summary>
    TorchRepeater,

    /// <summary>Odd-numbered ring of solid-block wall-torch inverters (classical 5-torch+ loop).</summary>
    TorchLoop
}

/// <summary>
/// Builds a configurable Redstone torch clock with optional period control via <see cref="Period"/> / <see cref="PeriodTicks"/>.
/// </summary>
public class TorchClockSchematicCreator : SchematicCreator
{
    public TorchClockMode Mode { get; set; } = TorchClockMode.TorchRepeater;

    public TimeSpan? Period { get; set; }

    public int? PeriodTicks { get; set; }

    /// <summary>
    /// Number of inverters in <see cref="TorchClockMode.TorchLoop"/> when <see cref="Period"/> and <see cref="PeriodTicks"/> are unset. Forced to odd &gt;= 5.
    /// </summary>
    public int TorchCount { get; set; } = 5;

    public Direction Facing { get; set; } = Direction.East;

    public string CircuitBlock { get; set; } = Minecraft.Blocks.SmoothStone;

    public bool IncludeToggleLever { get; set; }

    public bool IncludeOutputLamp { get; set; }

    public int GetActualPeriodTicks()
    {
        if (Mode == TorchClockMode.TorchLoop)
        {
            int torches = GetResolvedTorchCount();
            return torches * 4;
        }

        int requested = CircuitLayout.ResolveGameTicks(Period, PeriodTicks, defaultGameTicks: 8, minGameTicks: 8, stepGameTicks: 4);
        int feedbackRt = Math.Max(1, (requested - 4) / 4);
        return 4 + (feedbackRt * 4);
    }

    public int GetResolvedTorchCount()
    {
        if (Period.HasValue || PeriodTicks.HasValue)
        {
            int requested = CircuitLayout.ResolveGameTicks(Period, PeriodTicks, defaultGameTicks: 20, minGameTicks: 20, stepGameTicks: 4);
            int raw = Math.Max(5, (int)Math.Ceiling(requested / 4.0));
            return raw % 2 == 0 ? raw + 1 : raw;
        }

        int count = Math.Max(5, TorchCount);
        return count % 2 == 0 ? count + 1 : count;
    }

    public override SchematicBuilder Create()
    {
        if (!Enum.IsDefined(Mode))
        {
            throw new ArgumentOutOfRangeException(nameof(Mode), Mode, "Unsupported torch clock mode.");
        }

        CircuitLayout.ValidateHorizontalFacing(Facing);
        CircuitLayout.ValidatePositive(TorchCount, nameof(TorchCount), 1);
        CircuitLayout.ValidateBlockId(CircuitBlock, nameof(CircuitBlock));

        return Mode == TorchClockMode.TorchLoop
            ? BuildTorchLoop()
            : BuildTorchRepeater();
    }

    private SchematicBuilder BuildTorchRepeater()
    {
        int requested = CircuitLayout.ResolveGameTicks(Period, PeriodTicks, defaultGameTicks: 8, minGameTicks: 8, stepGameTicks: 4);
        int feedbackRt = Math.Max(1, (requested - 4) / 4);
        int repeaterCount = Math.Max(1, (int)Math.Ceiling(feedbackRt / 4.0));

        int leverLead = IncludeToggleLever ? 1 : 0;
        int coreLengthU = 2 + Math.Max(2, repeaterCount + 1);
        const int widthV = 2;
        int totalLengthU = leverLead + coreLengthU + (IncludeOutputLamp ? 1 : 0);
        int maxU = totalLengthU - 1;
        int maxV = widthV - 1;

        var builder = CircuitLayout.CreateBuilder(
            this,
            CircuitLayout.GetWidth(totalLengthU, widthV, Facing),
            2,
            CircuitLayout.GetLength(totalLengthU, widthV, Facing),
            "Torch-Repeater Clock",
            $"Redstone torch + repeater clock ({GetActualPeriodTicks()} gt period) facing {Facing}.");

        for (int u = 0; u < totalLengthU; u++)
        {
            for (int v = 0; v < widthV; v++)
            {
                builder.SetBlock(CircuitLayout.Map(0, 0, 0, u, 0, v, maxU, maxV, Facing), CircuitBlock);
            }
        }

        int startU = leverLead;
        Direction torchFacing = CircuitLayout.RotateDirection(Direction.East, Facing);
        Direction reverse = CircuitLayout.GetOpposite(Facing);

        builder.SetBlock(CircuitLayout.Map(0, 0, 0, startU, 1, 0, maxU, maxV, Facing), CircuitBlock);
        if (IncludeToggleLever)
        {
            builder.SetLever(CircuitLayout.Map(0, 0, 0, 0, 1, 0, maxU, maxV, Facing), BlockFace.Wall, reverse, powered: false);
        }

        builder.SetBlock(
            CircuitLayout.Map(0, 0, 0, startU + 1, 1, 0, maxU, maxV, Facing),
            Minecraft.Blocks.RedstoneWallTorch.WithFacing(torchFacing).WithLit(true));

        int loopEndU = startU + coreLengthU - 1;
        for (int u = startU + 2; u <= loopEndU; u++)
        {
            builder.SetBlock(
                CircuitLayout.Map(0, 0, 0, u, 1, 0, maxU, maxV, Facing),
                CircuitLayout.CreateWire(Facing, north: false, east: u < loopEndU || IncludeOutputLamp, south: u == loopEndU, west: true));
        }

        // Return path along v=1 going West with repeaters back to startU,1,1 -> startU,1,0
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, loopEndU, 1, 1, maxU, maxV, Facing), CircuitLayout.CreateWire(Facing, north: true, east: false, south: false, west: true));
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, startU, 1, 1, maxU, maxV, Facing), CircuitLayout.CreateWire(Facing, north: true, east: true, south: false, west: false));

        int remainingRt = feedbackRt;
        int returnSlots = loopEndU - startU - 1;
        int placed = 0;
        for (int u = loopEndU - 1; u > startU; u--)
        {
            if (placed < repeaterCount)
            {
                int left = repeaterCount - 1 - placed;
                int rt = Math.Clamp(remainingRt - left, 1, 4);
                remainingRt -= rt;
                CircuitLayout.SetRepeater(builder, CircuitLayout.Map(0, 0, 0, u, 1, 1, maxU, maxV, Facing), reverse, delay: rt);
                placed++;
            }
            else
            {
                builder.SetBlock(CircuitLayout.Map(0, 0, 0, u, 1, 1, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));
            }
        }

        if (IncludeOutputLamp)
        {
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, totalLengthU - 1, 1, 0, maxU, maxV, Facing), Minecraft.Blocks.RedstoneLamp);
        }

        return builder;
    }

    private SchematicBuilder BuildTorchLoop()
    {
        int torches = GetResolvedTorchCount();
        // Forward stages on v=0 (with 1 odd stage turning across at end) -> return stages on v=1
        int forwardStages = (torches + 1) / 2;
        int returnStages = torches - forwardStages;
        int stagesPerRow = Math.Max(forwardStages, returnStages);

        int leverLead = IncludeToggleLever ? 1 : 0;
        int coreLengthU = (stagesPerRow * 3) + 1;
        const int widthV = 2;
        int totalLengthU = leverLead + coreLengthU + (IncludeOutputLamp ? 1 : 0);
        int maxU = totalLengthU - 1;
        int maxV = widthV - 1;

        var builder = CircuitLayout.CreateBuilder(
            this,
            CircuitLayout.GetWidth(totalLengthU, widthV, Facing),
            2,
            CircuitLayout.GetLength(totalLengthU, widthV, Facing),
            "Torch Loop Clock",
            $"{torches}-torch odd inverter ring clock ({torches * 4} gt period) facing {Facing}.");

        for (int u = 0; u < totalLengthU; u++)
        {
            for (int v = 0; v < widthV; v++)
            {
                builder.SetBlock(CircuitLayout.Map(0, 0, 0, u, 0, v, maxU, maxV, Facing), CircuitBlock);
            }
        }

        Direction forwardTorch = CircuitLayout.RotateDirection(Direction.East, Facing);
        Direction reverseTorch = CircuitLayout.RotateDirection(Direction.West, Facing);
        int startU = leverLead;

        if (IncludeToggleLever)
        {
            builder.SetLever(CircuitLayout.Map(0, 0, 0, 0, 1, 0, maxU, maxV, Facing), BlockFace.Wall, CircuitLayout.GetOpposite(Facing), powered: false);
        }

        // Forward inverter stages on v=0
        for (int s = 0; s < forwardStages; s++)
        {
            int bU = startU + (s * 3);
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, bU, 1, 0, maxU, maxV, Facing), CircuitBlock);
            builder.SetBlock(
                CircuitLayout.Map(0, 0, 0, bU + 1, 1, 0, maxU, maxV, Facing),
                Minecraft.Blocks.RedstoneWallTorch.WithFacing(forwardTorch).WithLit(s % 2 == 0));
            builder.SetBlock(
                CircuitLayout.Map(0, 0, 0, bU + 2, 1, 0, maxU, maxV, Facing),
                CircuitLayout.CreateWire(Facing, north: false, east: true, south: s == forwardStages - 1, west: true));
        }

        int turnU = startU + ((forwardStages - 1) * 3) + 2;
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, turnU, 1, 1, maxU, maxV, Facing), CircuitLayout.CreateWire(Facing, north: true, east: false, south: false, west: true));

        // Return inverter stages on v=1 heading West toward startU
        for (int r = 0; r < returnStages; r++)
        {
            int bU = turnU - 1 - (r * 3);
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, bU, 1, 1, maxU, maxV, Facing), CircuitBlock);
            builder.SetBlock(
                CircuitLayout.Map(0, 0, 0, bU - 1, 1, 1, maxU, maxV, Facing),
                Minecraft.Blocks.RedstoneWallTorch.WithFacing(reverseTorch).WithLit((forwardStages + r) % 2 == 0));
            if (bU - 2 >= startU)
            {
                builder.SetBlock(CircuitLayout.Map(0, 0, 0, bU - 2, 1, 1, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));
            }
        }

        // Wire closing loop from return row back into startU block
        for (int u = startU; u <= turnU - 1 - (returnStages * 3); u++)
        {
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, u, 1, 1, maxU, maxV, Facing), CircuitLayout.CreateWire(Facing, north: u == startU, east: true, south: false, west: u > startU));
        }

        builder.SetBlock(CircuitLayout.Map(0, 0, 0, startU, 1, 1, maxU, maxV, Facing), CircuitLayout.CreateWire(Facing, north: true, east: true, south: false, west: false));

        if (IncludeOutputLamp)
        {
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, totalLengthU - 1, 1, 0, maxU, maxV, Facing), Minecraft.Blocks.RedstoneLamp);
        }

        return builder;
    }
}
