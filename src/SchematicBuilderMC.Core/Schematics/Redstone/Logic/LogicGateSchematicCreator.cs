using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;
using SchematicBuilderMC.Extensions;
using SchematicBuilderMC.Models;
using SchematicBuilderMC.Registries;
using SchematicBuilderMC.Schematics.Redstone.Internal;

namespace SchematicBuilderMC.Schematics.Redstone.Logic;

/// <summary>
/// Supported combinatorial Redstone logic gates from Minecraft Wiki.
/// </summary>
public enum LogicGateType
{
    Not,
    Or,
    Nor,
    And,
    Nand,
    Xor,
    Xnor,
    Imply,
    Nimply
}

/// <summary>
/// Builds any of the nine canonical Minecraft Redstone logic gates (<see cref="LogicGateType"/>)
/// with configurable orientation, building material, optional input levers, and optional output lamp.
/// </summary>
public class LogicGateSchematicCreator : SchematicCreator
{
    /// <summary>
    /// Logic gate function to build. Defaults to <see cref="LogicGateType.And"/>.
    /// </summary>
    public virtual LogicGateType GateType { get; set; } = LogicGateType.And;

    /// <summary>
    /// Direction of signal flow from input(s) to output. Must be horizontal.
    /// </summary>
    public Direction Facing { get; set; } = Direction.East;

    /// <summary>
    /// Solid opaque support/circuit block used across the gate.
    /// </summary>
    public string CircuitBlock { get; set; } = Minecraft.Blocks.SmoothStone;

    /// <summary>
    /// Adds input lever(s) on solid input blocks ahead of the gate inputs.
    /// </summary>
    public bool IncludeInputLevers { get; set; }

    /// <summary>
    /// Adds a redstone lamp connected to the gate output.
    /// </summary>
    public bool IncludeOutputLamp { get; set; }

    public override SchematicBuilder Create()
    {
        if (!Enum.IsDefined(GateType))
        {
            throw new ArgumentOutOfRangeException(nameof(GateType), GateType, "Unsupported logic gate type.");
        }

        CircuitLayout.ValidateHorizontalFacing(Facing);
        CircuitLayout.ValidateBlockId(CircuitBlock, nameof(CircuitBlock));

        return GateType switch
        {
            LogicGateType.Not => BuildNotGate(),
            LogicGateType.Or => BuildOrGate(),
            LogicGateType.Nor => BuildNorGate(),
            LogicGateType.And => BuildAndGate(),
            LogicGateType.Nand => BuildNandGate(),
            LogicGateType.Xor => BuildXnorOrXorGate(invertToXor: true),
            LogicGateType.Xnor => BuildXnorOrXorGate(invertToXor: false),
            LogicGateType.Imply => BuildImplyGate(),
            LogicGateType.Nimply => BuildNimplyGate(),
            _ => throw new ArgumentOutOfRangeException(nameof(GateType), GateType, "Unsupported logic gate type.")
        };
    }

    private SchematicBuilder CreateGateBuilder(
        int coreLengthU,
        int widthV,
        int height,
        int[] inputVs,
        int outputV,
        out int gateOffsetU,
        out int maxU,
        out int maxV)
    {
        gateOffsetU = IncludeInputLevers ? 1 : 0;
        int totalLengthU = gateOffsetU + coreLengthU + (IncludeOutputLamp ? 1 : 0);
        int totalHeight = IncludeInputLevers ? Math.Max(height, 3) : height;
        maxU = totalLengthU - 1;
        maxV = widthV - 1;

        var builder = CircuitLayout.CreateBuilder(
            this,
            CircuitLayout.GetWidth(totalLengthU, widthV, Facing),
            totalHeight,
            CircuitLayout.GetLength(totalLengthU, widthV, Facing),
            $"{GateType} Logic Gate",
            $"Redstone {GateType} logic gate facing {Facing}.");

        for (int u = 0; u < totalLengthU; u++)
        {
            for (int v = 0; v < widthV; v++)
            {
                builder.SetBlock(CircuitLayout.Map(0, 0, 0, u, 0, v, maxU, maxV, Facing), CircuitBlock);
            }
        }

        if (IncludeInputLevers)
        {
            foreach (int v in inputVs)
            {
                builder.SetBlock(CircuitLayout.Map(0, 0, 0, 0, 1, v, maxU, maxV, Facing), CircuitBlock);
                builder.SetLever(CircuitLayout.Map(0, 0, 0, 0, 2, v, maxU, maxV, Facing), BlockFace.Floor, Facing, powered: false);
            }
        }

        if (IncludeOutputLamp)
        {
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, totalLengthU - 1, 1, outputV, maxU, maxV, Facing), Minecraft.Blocks.RedstoneLamp);
        }

        return builder;
    }

    private SchematicBuilder BuildNotGate()
    {
        var builder = CreateGateBuilder(4, 1, 2, [0], 0, out int u0, out int maxU, out int maxV);
        Direction torchFacing = CircuitLayout.RotateDirection(Direction.East, Facing);

        builder.SetBlock(CircuitLayout.Map(0, 0, 0, u0, 1, 0, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, u0 + 1, 1, 0, maxU, maxV, Facing), CircuitBlock);
        builder.SetBlock(
            CircuitLayout.Map(0, 0, 0, u0 + 2, 1, 0, maxU, maxV, Facing),
            Minecraft.Blocks.RedstoneWallTorch.WithFacing(torchFacing).WithLit(true));
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, u0 + 3, 1, 0, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));
        return builder;
    }

    private SchematicBuilder BuildOrGate()
    {
        var builder = CreateGateBuilder(3, 3, 2, [0, 2], 1, out int u0, out int maxU, out int maxV);

        builder.SetBlock(CircuitLayout.Map(0, 0, 0, u0, 1, 0, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, u0, 1, 2, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));

        builder.SetBlock(CircuitLayout.Map(0, 0, 0, u0 + 1, 1, 0, maxU, maxV, Facing), CircuitLayout.CreateWire(Facing, north: false, east: false, south: true, west: true));
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, u0 + 1, 1, 1, maxU, maxV, Facing), CircuitLayout.CreateWire(Facing, north: true, east: true, south: true, west: false));
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, u0 + 1, 1, 2, maxU, maxV, Facing), CircuitLayout.CreateWire(Facing, north: true, east: false, south: false, west: true));

        builder.SetBlock(CircuitLayout.Map(0, 0, 0, u0 + 2, 1, 1, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));
        return builder;
    }

    private SchematicBuilder BuildNorGate()
    {
        var builder = CreateGateBuilder(5, 3, 2, [0, 2], 1, out int u0, out int maxU, out int maxV);
        Direction torchFacing = CircuitLayout.RotateDirection(Direction.East, Facing);

        builder.SetBlock(CircuitLayout.Map(0, 0, 0, u0, 1, 0, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, u0, 1, 2, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));

        builder.SetBlock(CircuitLayout.Map(0, 0, 0, u0 + 1, 1, 0, maxU, maxV, Facing), CircuitLayout.CreateWire(Facing, north: false, east: false, south: true, west: true));
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, u0 + 1, 1, 1, maxU, maxV, Facing), CircuitLayout.CreateWire(Facing, north: true, east: true, south: true, west: false));
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, u0 + 1, 1, 2, maxU, maxV, Facing), CircuitLayout.CreateWire(Facing, north: true, east: false, south: false, west: true));

        builder.SetBlock(CircuitLayout.Map(0, 0, 0, u0 + 2, 1, 1, maxU, maxV, Facing), CircuitBlock);
        builder.SetBlock(
            CircuitLayout.Map(0, 0, 0, u0 + 3, 1, 1, maxU, maxV, Facing),
            Minecraft.Blocks.RedstoneWallTorch.WithFacing(torchFacing).WithLit(true));
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, u0 + 4, 1, 1, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));
        return builder;
    }

    private SchematicBuilder BuildAndGate()
    {
        var builder = CreateGateBuilder(4, 3, 3, [0, 2], 1, out int u0, out int maxU, out int maxV);
        Direction torchFacing = CircuitLayout.RotateDirection(Direction.East, Facing);

        builder.SetBlock(CircuitLayout.Map(0, 0, 0, u0, 1, 0, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, u0, 1, 2, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));

        for (int v = 0; v < 3; v++)
        {
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, u0 + 1, 1, v, maxU, maxV, Facing), CircuitBlock);
        }

        builder.SetBlock(CircuitLayout.Map(0, 0, 0, u0 + 1, 2, 0, maxU, maxV, Facing), Minecraft.Blocks.RedstoneTorch.WithLit(true));
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, u0 + 1, 2, 1, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, alignedWithForwardAxis: false));
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, u0 + 1, 2, 2, maxU, maxV, Facing), Minecraft.Blocks.RedstoneTorch.WithLit(true));

        builder.SetBlock(
            CircuitLayout.Map(0, 0, 0, u0 + 2, 1, 1, maxU, maxV, Facing),
            Minecraft.Blocks.RedstoneWallTorch.WithFacing(torchFacing).WithLit(false));
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, u0 + 3, 1, 1, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));
        return builder;
    }

    private SchematicBuilder BuildNandGate()
    {
        var builder = CreateGateBuilder(4, 3, 2, [0, 2], 1, out int u0, out int maxU, out int maxV);
        Direction torchFacing = CircuitLayout.RotateDirection(Direction.East, Facing);

        builder.SetBlock(CircuitLayout.Map(0, 0, 0, u0, 1, 0, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, u0, 1, 2, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));

        builder.SetBlock(CircuitLayout.Map(0, 0, 0, u0 + 1, 1, 0, maxU, maxV, Facing), CircuitBlock);
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, u0 + 1, 1, 2, maxU, maxV, Facing), CircuitBlock);

        builder.SetBlock(
            CircuitLayout.Map(0, 0, 0, u0 + 2, 1, 0, maxU, maxV, Facing),
            Minecraft.Blocks.RedstoneWallTorch.WithFacing(torchFacing).WithLit(true));
        builder.SetBlock(
            CircuitLayout.Map(0, 0, 0, u0 + 2, 1, 2, maxU, maxV, Facing),
            Minecraft.Blocks.RedstoneWallTorch.WithFacing(torchFacing).WithLit(true));

        builder.SetBlock(CircuitLayout.Map(0, 0, 0, u0 + 3, 1, 0, maxU, maxV, Facing), CircuitLayout.CreateWire(Facing, north: false, east: false, south: true, west: true));
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, u0 + 3, 1, 1, maxU, maxV, Facing), CircuitLayout.CreateWire(Facing, north: true, east: true, south: true, west: false));
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, u0 + 3, 1, 2, maxU, maxV, Facing), CircuitLayout.CreateWire(Facing, north: true, east: false, south: false, west: true));
        return builder;
    }

    private SchematicBuilder BuildXnorOrXorGate(bool invertToXor)
    {
        int coreLengthU = invertToXor ? 10 : 8;
        var builder = CreateGateBuilder(coreLengthU, 3, 3, [0, 2], 1, out int u0, out int maxU, out int maxV);
        Direction torchFacing = CircuitLayout.RotateDirection(Direction.East, Facing);

        // Input wires
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, u0, 1, 0, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, u0, 1, 2, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));

        // NAND detector stage (u0 + 1 .. u0 + 3, y = 2)
        for (int v = 0; v < 3; v++)
        {
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, u0 + 1, 1, v, maxU, maxV, Facing), CircuitBlock);
        }

        builder.SetBlock(CircuitLayout.Map(0, 0, 0, u0 + 1, 2, 0, maxU, maxV, Facing), Minecraft.Blocks.RedstoneTorch.WithLit(true));
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, u0 + 1, 2, 1, maxU, maxV, Facing), CircuitLayout.CreateWire(Facing, north: false, east: true, south: false, west: false));
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, u0 + 1, 2, 2, maxU, maxV, Facing), Minecraft.Blocks.RedstoneTorch.WithLit(true));

        builder.SetBlock(CircuitLayout.Map(0, 0, 0, u0 + 2, 1, 1, maxU, maxV, Facing), CircuitBlock);
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, u0 + 2, 2, 1, maxU, maxV, Facing), CircuitBlock);
        builder.SetBlock(
            CircuitLayout.Map(0, 0, 0, u0 + 3, 2, 1, maxU, maxV, Facing),
            Minecraft.Blocks.RedstoneWallTorch.WithFacing(torchFacing).WithLit(false));

        // Buffered A and B branches around NAND -> NOR detector (u0 + 2 .. u0 + 5)
        builder.SetRepeater(CircuitLayout.Map(0, 0, 0, u0 + 2, 1, 0, maxU, maxV, Facing), Facing, delay: 1);
        builder.SetRepeater(CircuitLayout.Map(0, 0, 0, u0 + 2, 1, 2, maxU, maxV, Facing), Facing, delay: 1);
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, u0 + 3, 1, 0, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, u0 + 3, 1, 2, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, u0 + 4, 1, 0, maxU, maxV, Facing), CircuitLayout.CreateWire(Facing, north: false, east: false, south: true, west: true));
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, u0 + 4, 1, 2, maxU, maxV, Facing), CircuitLayout.CreateWire(Facing, north: true, east: false, south: false, west: true));
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, u0 + 4, 1, 1, maxU, maxV, Facing), CircuitBlock);
        builder.SetBlock(
            CircuitLayout.Map(0, 0, 0, u0 + 5, 1, 1, maxU, maxV, Facing),
            Minecraft.Blocks.RedstoneWallTorch.WithFacing(torchFacing).WithLit(true));

        // Bridge AND output on y = 2 around NOR stage -> join at u0 + 7, v = 1
        builder.SetRepeater(CircuitLayout.Map(0, 0, 0, u0 + 4, 2, 1, maxU, maxV, Facing), Facing, delay: 1);
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, u0 + 5, 1, 0, maxU, maxV, Facing), CircuitBlock);
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, u0 + 5, 2, 1, maxU, maxV, Facing), CircuitBlock);
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, u0 + 5, 2, 0, maxU, maxV, Facing), CircuitLayout.CreateWire(Facing, north: false, east: true, south: true, west: false));
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, u0 + 6, 1, 0, maxU, maxV, Facing), CircuitBlock);
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, u0 + 6, 2, 0, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));

        builder.SetBlock(CircuitLayout.Map(0, 0, 0, u0 + 6, 1, 1, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, u0 + 7, 1, 0, maxU, maxV, Facing), CircuitLayout.CreateWire(Facing, north: false, east: false, south: true, west: true));
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, u0 + 7, 1, 1, maxU, maxV, Facing), CircuitLayout.CreateWire(Facing, north: true, east: true, south: false, west: true));

        if (invertToXor)
        {
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, u0 + 8, 1, 1, maxU, maxV, Facing), CircuitBlock);
            builder.SetBlock(
                CircuitLayout.Map(0, 0, 0, u0 + 9, 1, 1, maxU, maxV, Facing),
                Minecraft.Blocks.RedstoneWallTorch.WithFacing(torchFacing).WithLit(false));
        }

        return builder;
    }

    private SchematicBuilder BuildImplyGate()
    {
        var builder = CreateGateBuilder(4, 3, 2, [0, 2], 1, out int u0, out int maxU, out int maxV);
        Direction torchFacing = CircuitLayout.RotateDirection(Direction.East, Facing);

        builder.SetBlock(CircuitLayout.Map(0, 0, 0, u0, 1, 0, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, u0, 1, 2, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));

        builder.SetBlock(CircuitLayout.Map(0, 0, 0, u0 + 1, 1, 0, maxU, maxV, Facing), CircuitBlock);
        builder.SetRepeater(CircuitLayout.Map(0, 0, 0, u0 + 1, 1, 2, maxU, maxV, Facing), Facing, delay: 1);

        builder.SetBlock(
            CircuitLayout.Map(0, 0, 0, u0 + 2, 1, 0, maxU, maxV, Facing),
            Minecraft.Blocks.RedstoneWallTorch.WithFacing(torchFacing).WithLit(true));
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, u0 + 2, 1, 2, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));

        builder.SetBlock(CircuitLayout.Map(0, 0, 0, u0 + 3, 1, 0, maxU, maxV, Facing), CircuitLayout.CreateWire(Facing, north: false, east: false, south: true, west: true));
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, u0 + 3, 1, 1, maxU, maxV, Facing), CircuitLayout.CreateWire(Facing, north: true, east: true, south: true, west: false));
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, u0 + 3, 1, 2, maxU, maxV, Facing), CircuitLayout.CreateWire(Facing, north: true, east: false, south: false, west: true));
        return builder;
    }

    private SchematicBuilder BuildNimplyGate()
    {
        var builder = CreateGateBuilder(4, 3, 2, [0, 2], 1, out int u0, out int maxU, out int maxV);

        builder.SetBlock(CircuitLayout.Map(0, 0, 0, u0, 1, 0, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, u0, 1, 2, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));

        builder.SetBlock(CircuitLayout.Map(0, 0, 0, u0 + 1, 1, 0, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));
        builder.SetRepeater(CircuitLayout.Map(0, 0, 0, u0 + 1, 1, 2, maxU, maxV, Facing), Facing, delay: 1);

        builder.SetComparator(CircuitLayout.Map(0, 0, 0, u0 + 2, 1, 0, maxU, maxV, Facing), Facing, subtractMode: true);
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, u0 + 2, 1, 1, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, alignedWithForwardAxis: false));
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, u0 + 2, 1, 2, maxU, maxV, Facing), CircuitBlock);

        builder.SetBlock(CircuitLayout.Map(0, 0, 0, u0 + 3, 1, 0, maxU, maxV, Facing), CircuitLayout.CreateWire(Facing, north: false, east: false, south: true, west: true));
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, u0 + 3, 1, 1, maxU, maxV, Facing), CircuitLayout.CreateWire(Facing, north: true, east: true, south: false, west: false));
        return builder;
    }
}
