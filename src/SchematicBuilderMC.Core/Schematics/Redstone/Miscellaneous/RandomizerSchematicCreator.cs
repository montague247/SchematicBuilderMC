using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;
using SchematicBuilderMC.Models;
using SchematicBuilderMC.Registries;
using SchematicBuilderMC.Schematics.Redstone.Internal;

namespace SchematicBuilderMC.Schematics.Redstone.Miscellaneous;

/// <summary>
/// Supported randomizer generator architectures.
/// </summary>
public enum RandomizerMode
{
    /// <summary>1-wide coin-flip dropper-hopper-comparator trial mixing stackable &amp; unstackable tokens.</summary>
    TokenCoinFlip,

    /// <summary>Multi-dropper randomizer array generating N independent random bits/channels.</summary>
    DropperArray
}

/// <summary>
/// Builds a configurable dropper-based Redstone randomizer (analog comparator strength or N-channel random bit array).
/// </summary>
public class RandomizerSchematicCreator : SchematicCreator
{
    public RandomizerMode Mode { get; set; } = RandomizerMode.TokenCoinFlip;

    /// <summary>
    /// Number of random output channels in <see cref="RandomizerMode.DropperArray"/> mode (default 1 in coin-flip, &gt;= 2 in array).
    /// </summary>
    public int OutputCount { get; set; } = 2;

    public Direction Facing { get; set; } = Direction.East;

    public string CircuitBlock { get; set; } = Minecraft.Blocks.SmoothStone;

    public string StackableItemId { get; set; } = Minecraft.Items.Cobblestone;

    public string NonStackableItemId { get; set; } = Minecraft.Items.WoodenSword;

    public bool IncludeInputButton { get; set; }

    public bool IncludeOutputLamps { get; set; }

    public override SchematicBuilder Create()
    {
        if (!Enum.IsDefined(Mode))
        {
            throw new ArgumentOutOfRangeException(nameof(Mode), Mode, "Unsupported randomizer mode.");
        }

        CircuitLayout.ValidateHorizontalFacing(Facing);
        CircuitLayout.ValidatePositive(OutputCount, nameof(OutputCount), 1);
        CircuitLayout.ValidateBlockId(CircuitBlock, nameof(CircuitBlock));
        CircuitLayout.ValidateBlockId(StackableItemId, nameof(StackableItemId));
        CircuitLayout.ValidateBlockId(NonStackableItemId, nameof(NonStackableItemId));

        int channels = Mode == RandomizerMode.TokenCoinFlip ? 1 : OutputCount;
        int inputLead = IncludeInputButton ? 1 : 0;
        const int coreLengthU = 5;
        int widthV = channels;
        int totalLengthU = inputLead + coreLengthU + (IncludeOutputLamps ? 1 : 0);
        int height = IncludeInputButton ? 3 : 2;
        int maxU = totalLengthU - 1;
        int maxV = widthV - 1;

        var builder = CircuitLayout.CreateBuilder(
            this,
            CircuitLayout.GetWidth(totalLengthU, widthV, Facing),
            height,
            CircuitLayout.GetLength(totalLengthU, widthV, Facing),
            "Dropper-Hopper Randomizer",
            $"{channels}-channel dropper-hopper Redstone randomizer facing {Facing}.");

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

        for (int c = 0; c < channels; c++)
        {
            // Shared trigger bus at inputLead across all channels
            builder.SetBlock(
                CircuitLayout.Map(0, 0, 0, inputLead, 1, c, maxU, maxV, Facing),
                CircuitLayout.CreateWire(Facing, north: c > 0, east: true, south: c < channels - 1, west: c == 0));

            // Dropper holding 1 stackable (strength 1) + 1 unstackable (strength 3) shooting into hopper returning back to dropper
            CircuitLayout.SetDropper(
                builder,
                CircuitLayout.Map(0, 0, 0, inputLead + 1, 1, c, maxU, maxV, Facing),
                Facing,
                new ItemStack(StackableItemId, 1, 0),
                new ItemStack(NonStackableItemId, 1, 1));
            CircuitLayout.SetHopper(builder, CircuitLayout.Map(0, 0, 0, inputLead + 2, 1, c, maxU, maxV, Facing), reverse, enabled: true);
            builder.SetComparator(CircuitLayout.Map(0, 0, 0, inputLead + 3, 1, c, maxU, maxV, Facing), Facing, subtractMode: false);
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, inputLead + 4, 1, c, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));

            if (IncludeOutputLamps)
            {
                builder.SetBlock(CircuitLayout.Map(0, 0, 0, totalLengthU - 1, 1, c, maxU, maxV, Facing), Minecraft.Blocks.RedstoneLamp);
            }
        }

        return builder;
    }
}
