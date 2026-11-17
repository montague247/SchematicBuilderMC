using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;
using SchematicBuilderMC.Extensions;
using SchematicBuilderMC.Models;
using SchematicBuilderMC.Registries;
using SchematicBuilderMC.Schematics.Redstone.Internal;

namespace SchematicBuilderMC.Schematics.Redstone.Memory;

/// <summary>
/// Supported SR/RS latch topologies on Minecraft Wiki.
/// </summary>
public enum RsLatchMode
{
    /// <summary>Classic 4x2x2 cross-coupled RS-NOR torch latch with Set and Reset ports.</summary>
    RsNor,

    /// <summary>1-wide silent facing-dropper SR latch holding 1 token item.</summary>
    Dropper,

    /// <summary>Opposing piston SR latch sliding a redstone block between Set and Reset positions.</summary>
    Piston
}

/// <summary>
/// Builds a configurable RS / SR memory latch with Set and Reset inputs and optional test buttons and output lamp.
/// </summary>
public class RsLatchSchematicCreator : SchematicCreator
{
    public RsLatchMode Mode { get; set; } = RsLatchMode.RsNor;

    public Direction Facing { get; set; } = Direction.East;

    public string CircuitBlock { get; set; } = Minecraft.Blocks.SmoothStone;

    public string ItemId { get; set; } = Minecraft.Items.Cobblestone;

    public bool IncludeInputButtons { get; set; }

    public bool IncludeOutputLamp { get; set; }

    public override SchematicBuilder Create()
    {
        if (!Enum.IsDefined(Mode))
        {
            throw new ArgumentOutOfRangeException(nameof(Mode), Mode, "Unsupported RS latch mode.");
        }

        CircuitLayout.ValidateHorizontalFacing(Facing);
        CircuitLayout.ValidateBlockId(CircuitBlock, nameof(CircuitBlock));
        CircuitLayout.ValidateBlockId(ItemId, nameof(ItemId));

        return Mode switch
        {
            RsLatchMode.RsNor => BuildRsNor(),
            RsLatchMode.Dropper => BuildDropperLatch(),
            RsLatchMode.Piston => BuildPistonLatch(),
            _ => throw new ArgumentOutOfRangeException(nameof(Mode), Mode, "Unsupported RS latch mode.")
        };
    }

    private SchematicBuilder BuildRsNor()
    {
        int inputLead = IncludeInputButtons ? 1 : 0;
        const int coreLengthU = 4;
        const int widthV = 2;
        int totalLengthU = inputLead + coreLengthU + (IncludeOutputLamp || IncludeInputButtons ? 1 : 0);
        int height = IncludeInputButtons ? 3 : 2;
        int maxU = totalLengthU - 1;
        int maxV = widthV - 1;

        var builder = CircuitLayout.CreateBuilder(
            this,
            CircuitLayout.GetWidth(totalLengthU, widthV, Facing),
            height,
            CircuitLayout.GetLength(totalLengthU, widthV, Facing),
            "RS-NOR Latch",
            $"Cross-coupled RS-NOR latch facing {Facing}.");

        for (int u = 0; u < totalLengthU; u++)
        {
            for (int v = 0; v < widthV; v++)
            {
                builder.SetBlock(CircuitLayout.Map(0, 0, 0, u, 0, v, maxU, maxV, Facing), CircuitBlock);
            }
        }

        Direction eastTorch = CircuitLayout.RotateDirection(Direction.East, Facing);
        Direction westTorch = CircuitLayout.RotateDirection(Direction.West, Facing);
        int u0 = inputLead;

        // Cross-coupled 4x2 core
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, u0, 1, 0, maxU, maxV, Facing), CircuitLayout.CreateWire(Facing, north: false, east: true, south: true, west: IncludeInputButtons));
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, u0 + 1, 1, 0, maxU, maxV, Facing), CircuitBlock);
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, u0 + 2, 1, 0, maxU, maxV, Facing), Minecraft.Blocks.RedstoneWallTorch.WithFacing(eastTorch).WithLit(true));
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, u0 + 3, 1, 0, maxU, maxV, Facing), CircuitLayout.CreateWire(Facing, north: false, east: IncludeOutputLamp, south: true, west: true));

        builder.SetBlock(CircuitLayout.Map(0, 0, 0, u0, 1, 1, maxU, maxV, Facing), CircuitLayout.CreateWire(Facing, north: true, east: true, south: false, west: false));
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, u0 + 1, 1, 1, maxU, maxV, Facing), Minecraft.Blocks.RedstoneWallTorch.WithFacing(westTorch).WithLit(false));
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, u0 + 2, 1, 1, maxU, maxV, Facing), CircuitBlock);
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, u0 + 3, 1, 1, maxU, maxV, Facing), CircuitLayout.CreateWire(Facing, north: true, east: IncludeInputButtons, south: false, west: true));

        if (IncludeInputButtons)
        {
            // Reset button before (u0,1,0) and Set button after (u0+3,1,1)
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, 0, 1, 0, maxU, maxV, Facing), CircuitBlock);
            CircuitLayout.SetButton(builder, CircuitLayout.Map(0, 0, 0, 0, 2, 0, maxU, maxV, Facing), BlockFace.Floor, Facing);
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, totalLengthU - 1, 1, 1, maxU, maxV, Facing), CircuitBlock);
            CircuitLayout.SetButton(builder, CircuitLayout.Map(0, 0, 0, totalLengthU - 1, 2, 1, maxU, maxV, Facing), BlockFace.Floor, CircuitLayout.GetOpposite(Facing));
        }

        if (IncludeOutputLamp)
        {
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, totalLengthU - 1, 1, 0, maxU, maxV, Facing), Minecraft.Blocks.RedstoneLamp);
        }

        return builder;
    }

    private SchematicBuilder BuildDropperLatch()
    {
        const int coreLengthU = 5;
        const int widthV = 2;
        int height = IncludeInputButtons ? 3 : 2;
        int maxU = coreLengthU - 1;
        int maxV = widthV - 1;

        var builder = CircuitLayout.CreateBuilder(
            this,
            CircuitLayout.GetWidth(coreLengthU, widthV, Facing),
            height,
            CircuitLayout.GetLength(coreLengthU, widthV, Facing),
            "Dropper SR Latch",
            $"Silent facing-dropper SR latch holding 1 token item facing {Facing}.");

        for (int u = 0; u < coreLengthU; u++)
        {
            for (int v = 0; v < widthV; v++)
            {
                builder.SetBlock(CircuitLayout.Map(0, 0, 0, u, 0, v, maxU, maxV, Facing), CircuitBlock);
            }
        }

        Direction reverse = CircuitLayout.GetOpposite(Facing);
        Direction sideSouth = CircuitLayout.RotateDirection(Direction.South, Facing);

        builder.SetBlock(CircuitLayout.Map(0, 0, 0, 0, 1, 0, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));
        CircuitLayout.SetDropper(builder, CircuitLayout.Map(0, 0, 0, 1, 1, 0, maxU, maxV, Facing), Facing, new ItemStack(ItemId, 1, 0));
        CircuitLayout.SetDropper(builder, CircuitLayout.Map(0, 0, 0, 2, 1, 0, maxU, maxV, Facing), reverse);
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, 3, 1, 0, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));

        // Output comparator reads Set dropper on v=1
        CircuitLayout.SetComparator(builder, CircuitLayout.Map(0, 0, 0, 2, 1, 1, maxU, maxV, Facing), sideSouth, subtractMode: false);

        if (IncludeInputButtons)
        {
            CircuitLayout.SetButton(builder, CircuitLayout.Map(0, 0, 0, 1, 2, 0, maxU, maxV, Facing), BlockFace.Floor, Facing);
            CircuitLayout.SetButton(builder, CircuitLayout.Map(0, 0, 0, 2, 2, 0, maxU, maxV, Facing), BlockFace.Floor, reverse);
        }

        if (IncludeOutputLamp)
        {
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, 4, 1, 0, maxU, maxV, Facing), Minecraft.Blocks.RedstoneLamp);
        }

        return builder;
    }

    private SchematicBuilder BuildPistonLatch()
    {
        int inputLead = IncludeInputButtons ? 1 : 0;
        const int coreLengthU = 5;
        const int widthV = 2;
        int totalLengthU = inputLead + coreLengthU + (IncludeInputButtons ? 1 : 0);
        int height = IncludeInputButtons ? 3 : 2;
        int maxU = totalLengthU - 1;
        int maxV = widthV - 1;

        var builder = CircuitLayout.CreateBuilder(
            this,
            CircuitLayout.GetWidth(totalLengthU, widthV, Facing),
            height,
            CircuitLayout.GetLength(totalLengthU, widthV, Facing),
            "Piston SR Latch",
            $"Dual-piston redstone-block SR latch facing {Facing}.");

        for (int u = 0; u < totalLengthU; u++)
        {
            for (int v = 0; v < widthV; v++)
            {
                builder.SetBlock(CircuitLayout.Map(0, 0, 0, u, 0, v, maxU, maxV, Facing), CircuitBlock);
            }
        }

        Direction reverse = CircuitLayout.GetOpposite(Facing);
        int u0 = inputLead;

        builder.SetBlock(CircuitLayout.Map(0, 0, 0, u0, 1, 0, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));
        builder.SetPiston(CircuitLayout.Map(0, 0, 0, u0 + 1, 1, 0, maxU, maxV, Facing), Facing, sticky: false);
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, u0 + 2, 1, 0, maxU, maxV, Facing), Minecraft.Blocks.RedstoneBlock);
        builder.SetPiston(CircuitLayout.Map(0, 0, 0, u0 + 4, 1, 0, maxU, maxV, Facing), reverse, sticky: false);

        if (IncludeInputButtons)
        {
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, 0, 1, 0, maxU, maxV, Facing), CircuitBlock);
            CircuitLayout.SetButton(builder, CircuitLayout.Map(0, 0, 0, 0, 2, 0, maxU, maxV, Facing), BlockFace.Floor, Facing);
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, totalLengthU - 1, 1, 0, maxU, maxV, Facing), CircuitBlock);
            CircuitLayout.SetButton(builder, CircuitLayout.Map(0, 0, 0, totalLengthU - 1, 2, 0, maxU, maxV, Facing), BlockFace.Floor, reverse);
        }

        if (IncludeOutputLamp)
        {
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, u0 + 3, 1, 1, maxU, maxV, Facing), Minecraft.Blocks.RedstoneLamp);
        }
        else
        {
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, u0 + 3, 1, 1, maxU, maxV, Facing), CircuitLayout.CreateWire(Facing, north: true, east: true, south: false, west: false));
        }

        return builder;
    }
}
