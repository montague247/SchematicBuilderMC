using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;
using SchematicBuilderMC.Models;
using SchematicBuilderMC.Registries;
using SchematicBuilderMC.Schematics.Redstone.Internal;

namespace SchematicBuilderMC.Schematics.Redstone.Pulse;

/// <summary>
/// Supported pulse frequency divider topologies.
/// </summary>
public enum PulseDividerMode
{
    /// <summary>Compact chain of waxed copper bulbs + comparators dividing pulse frequency by 2^Stages.</summary>
    CopperBulbBinary,

    /// <summary>Dropper-hopper ring dividing by any arbitrary positive integer >= 2.</summary>
    DropperRing
}

/// <summary>
/// Builds a configurable Redstone pulse divider outputting one pulse every <see cref="Divisor"/> input pulses.
/// </summary>
public class PulseDividerSchematicCreator : SchematicCreator
{
    public PulseDividerMode Mode { get; set; } = PulseDividerMode.CopperBulbBinary;

    /// <summary>
    /// Pulse division factor (&gt;= 2). For <see cref="PulseDividerMode.CopperBulbBinary"/>, rounded up to the nearest power of two.
    /// </summary>
    public int Divisor { get; set; } = 4;

    public Direction Facing { get; set; } = Direction.East;

    public string CircuitBlock { get; set; } = Minecraft.Blocks.SmoothStone;

    public string ItemId { get; set; } = Minecraft.Items.Cobblestone;

    public bool IncludeInputButton { get; set; }

    public bool IncludeOutputLamp { get; set; }

    public int GetBinaryStageCount() => Math.Max(1, (int)Math.Ceiling(Math.Log2(Divisor)));

    public int GetEffectiveDivisor() =>
        Mode == PulseDividerMode.CopperBulbBinary
            ? 1 << GetBinaryStageCount()
            : Divisor;

    public override SchematicBuilder Create()
    {
        if (!Enum.IsDefined(Mode))
        {
            throw new ArgumentOutOfRangeException(nameof(Mode), Mode, "Unsupported pulse divider mode.");
        }

        CircuitLayout.ValidateHorizontalFacing(Facing);
        CircuitLayout.ValidatePositive(Divisor, nameof(Divisor), 2);
        CircuitLayout.ValidateBlockId(CircuitBlock, nameof(CircuitBlock));
        CircuitLayout.ValidateBlockId(ItemId, nameof(ItemId));

        return Mode == PulseDividerMode.DropperRing
            ? BuildDropperRing()
            : BuildCopperBulbBinary();
    }

    private SchematicBuilder BuildCopperBulbBinary()
    {
        int stages = GetBinaryStageCount();
        int inputLead = IncludeInputButton ? 1 : 0;
        int coreLengthU = (stages * 2) + 1;
        int totalLengthU = inputLead + coreLengthU + (IncludeOutputLamp ? 1 : 0);
        int height = IncludeInputButton ? 3 : 2;
        int maxU = totalLengthU - 1;

        var builder = CircuitLayout.CreateBuilder(
            this,
            CircuitLayout.GetWidth(totalLengthU, 1, Facing),
            height,
            CircuitLayout.GetLength(totalLengthU, 1, Facing),
            "Binary Copper-Bulb Pulse Divider",
            $"1/{1 << stages} binary pulse divider ({stages} copper bulb stage(s)) facing {Facing}.");

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

        for (int s = 0; s < stages; s++)
        {
            int bulbU = inputLead + 1 + (s * 2);
            CircuitLayout.SetCopperBulb(builder, CircuitLayout.Map(0, 0, 0, bulbU, 1, 0, maxU, 0, Facing));
            builder.SetComparator(CircuitLayout.Map(0, 0, 0, bulbU + 1, 1, 0, maxU, 0, Facing), Facing, subtractMode: false);
        }

        if (IncludeOutputLamp)
        {
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, totalLengthU - 1, 1, 0, maxU, 0, Facing), Minecraft.Blocks.RedstoneLamp);
        }

        return builder;
    }

    private SchematicBuilder BuildDropperRing()
    {
        int inputLead = IncludeInputButton ? 1 : 0;
        const int coreLengthU = 5;
        const int widthV = 2;
        int totalLengthU = inputLead + coreLengthU + (IncludeOutputLamp ? 1 : 0);
        int height = IncludeInputButton ? 3 : 2;
        int maxU = totalLengthU - 1;
        int maxV = widthV - 1;

        var builder = CircuitLayout.CreateBuilder(
            this,
            CircuitLayout.GetWidth(totalLengthU, widthV, Facing),
            height,
            CircuitLayout.GetLength(totalLengthU, widthV, Facing),
            "Dropper-Hopper Pulse Divider",
            $"1/{Divisor} arbitrary pulse divider using {Divisor} tracked items facing {Facing}.");

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
        ItemStack[] stacks = CircuitLayout.CreateItemStacks(ItemId, Divisor, maxSlots: 9);

        builder.SetBlock(CircuitLayout.Map(0, 0, 0, inputLead, 1, 0, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));
        CircuitLayout.SetDropper(builder, CircuitLayout.Map(0, 0, 0, inputLead + 1, 1, 0, maxU, maxV, Facing), Facing, stacks);
        CircuitLayout.SetHopper(builder, CircuitLayout.Map(0, 0, 0, inputLead + 2, 1, 0, maxU, maxV, Facing), reverse, enabled: false);
        builder.SetComparator(CircuitLayout.Map(0, 0, 0, inputLead + 3, 1, 0, maxU, maxV, Facing), Facing, subtractMode: false);
        builder.SetRepeater(CircuitLayout.Map(0, 0, 0, inputLead + 4, 1, 0, maxU, maxV, Facing), Facing, delay: 1);

        // Reset line draining hopper once dropper empties
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, inputLead + 3, 1, 1, maxU, maxV, Facing), CircuitLayout.CreateWire(Facing, north: true, east: false, south: false, west: true));
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, inputLead + 2, 1, 1, maxU, maxV, Facing), CircuitLayout.CreateWire(Facing, north: true, east: true, south: false, west: false));

        if (IncludeOutputLamp)
        {
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, totalLengthU - 1, 1, 0, maxU, maxV, Facing), Minecraft.Blocks.RedstoneLamp);
        }

        return builder;
    }
}
