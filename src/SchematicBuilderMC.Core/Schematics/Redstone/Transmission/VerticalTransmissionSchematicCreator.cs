using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;
using SchematicBuilderMC.Extensions;
using SchematicBuilderMC.Models;
using SchematicBuilderMC.Registries;
using SchematicBuilderMC.Schematics.Redstone.Internal;

namespace SchematicBuilderMC.Schematics.Redstone.Transmission;

/// <summary>
/// Supported Minecraft vertical Redstone signal transmission topologies.
/// </summary>
public enum VerticalTransmissionMode
{
    /// <summary>Compact 2-wide alternating top-slab upward redstone ladder (flat repeater shelf inserted every 14 vertical steps).</summary>
    RedstoneLadder,

    /// <summary>Classic 1-wide diagonal stepped redstone staircase with automatic repeater boosts every 15 steps.</summary>
    RedstoneStaircase,

    /// <summary>1x1 alternating block and upright redstone torch upward tower.</summary>
    TorchTowerUp,

    /// <summary>Verified alternating wall-torch and wire downward tower.</summary>
    TorchTowerDown,

    /// <summary>1x1 column of observers transmitting pulses upward.</summary>
    ObserverTowerUp,

    /// <summary>1x1 column of observers transmitting pulses downward.</summary>
    ObserverTowerDown,

    /// <summary>Sticky piston driving a vertical pillar of slime blocks and a top redstone block upward.</summary>
    PistonTowerUp,

    /// <summary>Downward sticky piston driving a vertical pillar of slime blocks and a bottom redstone block downward.</summary>
    PistonTowerDown
}

/// <summary>
/// Builds vertical Redstone transmission circuits in configurable height, mode, and facing orientation.
/// </summary>
public class VerticalTransmissionSchematicCreator : SchematicCreator
{
    /// <summary>
    /// Vertical transmission topology. Defaults to <see cref="VerticalTransmissionMode.TorchTowerUp"/>.
    /// </summary>
    public VerticalTransmissionMode Mode { get; set; } = VerticalTransmissionMode.TorchTowerUp;

    /// <summary>
    /// Signal transmission height in blocks (must be at least 2).
    /// </summary>
    public int Height { get; set; } = 8;

    /// <summary>
    /// Canonical facing direction for staircase/ladder/tower I/O orientation. Must be horizontal.
    /// </summary>
    public Direction Facing { get; set; } = Direction.East;

    /// <summary>
    /// Solid opaque block used for signal supports and staircase blocks.
    /// </summary>
    public string CircuitBlock { get; set; } = Minecraft.Blocks.SmoothStone;

    /// <summary>
    /// Slab block used in top-half mode for <see cref="VerticalTransmissionMode.RedstoneLadder"/>.
    /// </summary>
    public string SlabBlock { get; set; } = Minecraft.Blocks.SmoothStoneSlab;

    /// <summary>
    /// Adds a floor lever wired to the circuit input.
    /// </summary>
    public bool IncludeInputLever { get; set; }

    /// <summary>
    /// Adds a redstone lamp at the circuit output.
    /// </summary>
    public bool IncludeOutputLamp { get; set; }

    public override SchematicBuilder Create()
    {
        Validate();
        return Mode switch
        {
            VerticalTransmissionMode.RedstoneLadder => BuildLadder(),
            VerticalTransmissionMode.RedstoneStaircase => BuildStaircase(),
            VerticalTransmissionMode.TorchTowerUp => BuildTorchTowerUp(),
            VerticalTransmissionMode.TorchTowerDown => BuildTorchTowerDown(),
            VerticalTransmissionMode.ObserverTowerUp => BuildObserverTower(upward: true),
            VerticalTransmissionMode.ObserverTowerDown => BuildObserverTower(upward: false),
            VerticalTransmissionMode.PistonTowerUp => BuildPistonTower(upward: true),
            VerticalTransmissionMode.PistonTowerDown => BuildPistonTower(upward: false),
            _ => throw new ArgumentOutOfRangeException(nameof(Mode), Mode, "Unsupported vertical transmission mode.")
        };
    }

    private void Validate()
    {
        if (!Enum.IsDefined(Mode))
        {
            throw new ArgumentOutOfRangeException(nameof(Mode), Mode, "Unsupported vertical transmission mode.");
        }

        CircuitLayout.ValidateHorizontalFacing(Facing);
        CircuitLayout.ValidatePositive(Height, nameof(Height), 2);
        CircuitLayout.ValidateBlockId(CircuitBlock, nameof(CircuitBlock));
        CircuitLayout.ValidateBlockId(SlabBlock, nameof(SlabBlock));
    }

    private SchematicBuilder BuildLadder()
    {
        // Input lead at u=0..1 if lever included, then alternating 2-column slab ladder with repeater shelves every 14 steps.
        int leadU = IncludeInputLever ? 2 : 1;
        int shelves = Math.Max(0, (Height - 2) / 14);
        int canonicalLengthU = leadU + 2 + (shelves * 2) + (IncludeOutputLamp ? 1 : 0);
        int totalHeight = Height + 1;
        int maxU = canonicalLengthU - 1;

        var builder = CircuitLayout.CreateBuilder(
            this,
            CircuitLayout.GetWidth(canonicalLengthU, 1, Facing),
            totalHeight,
            CircuitLayout.GetLength(canonicalLengthU, 1, Facing),
            "Redstone Ladder",
            $"Vertical top-slab redstone ladder ({Height} blocks tall) facing {Facing}.");

        builder.SetBlock(CircuitLayout.Map(0, 0, 0, 0, 0, 0, maxU, 0, Facing), CircuitBlock);
        if (IncludeInputLever)
        {
            builder.SetLever(CircuitLayout.Map(0, 0, 0, 0, 1, 0, maxU, 0, Facing), BlockFace.Floor, Facing, powered: false);
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, 1, 0, 0, maxU, 0, Facing), CircuitBlock);
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, 1, 1, 0, maxU, 0, Facing), CircuitLayout.CreateAxisWire(Facing, true));
        }
        else
        {
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, 0, 1, 0, maxU, 0, Facing), CircuitLayout.CreateAxisWire(Facing, true));
        }

        int baseU = leadU;
        int stepSinceRepeater = 1;
        BlockState topSlab = SlabBlock.WithSlabType("top");

        for (int y = 0; y < Height - 1; y++)
        {
            if (stepSinceRepeater >= 14)
            {
                builder.SetBlock(CircuitLayout.Map(0, 0, 0, baseU, y, 0, maxU, 0, Facing), CircuitBlock);
                builder.SetRepeater(CircuitLayout.Map(0, 0, 0, baseU, y + 1, 0, maxU, 0, Facing), Facing, delay: 1);
                builder.SetBlock(CircuitLayout.Map(0, 0, 0, baseU + 1, y, 0, maxU, 0, Facing), CircuitBlock);
                builder.SetBlock(CircuitLayout.Map(0, 0, 0, baseU + 1, y + 1, 0, maxU, 0, Facing), CircuitLayout.CreateAxisWire(Facing, true));
                baseU += 2;
                stepSinceRepeater = 0;
            }

            int slabU = baseU + (y % 2);
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, slabU, y, 0, maxU, 0, Facing), topSlab);
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, slabU, y + 1, 0, maxU, 0, Facing), CircuitLayout.CreateAxisWire(Facing, true));
            stepSinceRepeater++;
        }

        int topSlabU = baseU + ((Height - 1) % 2);
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, topSlabU, Height - 1, 0, maxU, 0, Facing), topSlab);
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, topSlabU, Height, 0, maxU, 0, Facing), CircuitLayout.CreateAxisWire(Facing, true));

        if (IncludeOutputLamp)
        {
            int lampU = Math.Min(maxU, topSlabU + 1);
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, lampU, Height, 0, maxU, 0, Facing), Minecraft.Blocks.RedstoneLamp);
        }

        return builder;
    }

    private SchematicBuilder BuildStaircase()
    {
        int leadU = IncludeInputLever ? 1 : 0;
        int canonicalLengthU = leadU + Height + (IncludeOutputLamp ? 1 : 0);
        int totalHeight = Height + 1;
        int maxU = canonicalLengthU - 1;

        var builder = CircuitLayout.CreateBuilder(
            this,
            CircuitLayout.GetWidth(canonicalLengthU, 1, Facing),
            totalHeight,
            CircuitLayout.GetLength(canonicalLengthU, 1, Facing),
            "Redstone Staircase",
            $"Vertical diagonal redstone staircase ({Height} blocks tall) facing {Facing}.");

        if (IncludeInputLever)
        {
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, 0, 0, 0, maxU, 0, Facing), CircuitBlock);
            builder.SetLever(CircuitLayout.Map(0, 0, 0, 0, 1, 0, maxU, 0, Facing), BlockFace.Floor, Facing, powered: false);
        }

        for (int step = 0; step < Height; step++)
        {
            int u = leadU + step;
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, u, step, 0, maxU, 0, Facing), CircuitBlock);
            var topPos = CircuitLayout.Map(0, 0, 0, u, step + 1, 0, maxU, 0, Facing);
            if (step > 0 && step % 15 == 0)
            {
                builder.SetRepeater(topPos, Facing, delay: 1);
            }
            else
            {
                builder.SetBlock(topPos, CircuitLayout.CreateAxisWire(Facing, true));
            }
        }

        if (IncludeOutputLamp)
        {
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, canonicalLengthU - 1, Height, 0, maxU, 0, Facing), Minecraft.Blocks.RedstoneLamp);
        }

        return builder;
    }

    private SchematicBuilder BuildTorchTowerUp()
    {
        int inputLead = IncludeInputLever ? 2 : 0;
        int canonicalLengthU = inputLead + 1;
        int maxU = canonicalLengthU - 1;
        int columnU = inputLead;
        int totalHeight = Height + (IncludeOutputLamp ? 1 : 0);

        var builder = CircuitLayout.CreateBuilder(
            this,
            CircuitLayout.GetWidth(canonicalLengthU, 1, Facing),
            totalHeight,
            CircuitLayout.GetLength(canonicalLengthU, 1, Facing),
            "Redstone Torch Tower (Up)",
            $"Upward alternating redstone torch tower ({Height} blocks tall) facing {Facing}.");

        if (IncludeInputLever)
        {
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, 0, 0, 0, maxU, 0, Facing), CircuitBlock);
            builder.SetLever(CircuitLayout.Map(0, 0, 0, 0, 1, 0, maxU, 0, Facing), BlockFace.Floor, Facing, powered: false);
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, 1, 0, 0, maxU, 0, Facing), CircuitBlock);
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, 1, 1, 0, maxU, 0, Facing), CircuitLayout.CreateAxisWire(Facing, true));
        }

        for (int y = 0; y < Height; y++)
        {
            var pos = CircuitLayout.Map(0, 0, 0, columnU, y, 0, maxU, 0, Facing);
            if (y % 2 == 0)
            {
                builder.SetBlock(pos, CircuitBlock);
            }
            else
            {
                builder.SetBlock(pos, Minecraft.Blocks.RedstoneTorch.WithLit(y % 4 == 1));
            }
        }

        if (IncludeOutputLamp)
        {
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, columnU, Height, 0, maxU, 0, Facing), Minecraft.Blocks.RedstoneLamp);
        }

        return builder;
    }

    private SchematicBuilder BuildTorchTowerDown()
    {
        // Verified MCP-1.21 layout: wall torches alternate side-to-side (3 wide in U, 1 in V), descending 1 Y per stage.
        int canonicalLengthU = 3;
        int maxU = 2;
        int stages = Height - 1;
        int totalHeight = stages + 2;

        var builder = CircuitLayout.CreateBuilder(
            this,
            CircuitLayout.GetWidth(canonicalLengthU, 1, Facing),
            totalHeight,
            CircuitLayout.GetLength(canonicalLengthU, 1, Facing),
            "Redstone Torch Tower (Down)",
            $"Downward zigzag wall-torch tower ({Height} blocks tall) facing {Facing}.");

        // Top input wire at (2, stages + 1, 0) sitting on solid block (2, stages, 0).
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, 2, stages, 0, maxU, 0, Facing), CircuitBlock);
        if (IncludeInputLever)
        {
            builder.SetLever(CircuitLayout.Map(0, 0, 0, 2, stages + 1, 0, maxU, 0, Facing), BlockFace.Floor, Facing, powered: false);
        }
        else
        {
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, 2, stages + 1, 0, maxU, 0, Facing), CircuitLayout.CreateAxisWire(Facing, true));
        }

        for (int s = 0; s < stages; s++)
        {
            int y = stages - s;
            bool torchOnEastSide = s % 2 == 1;
            if (torchOnEastSide)
            {
                // Solid block at u=0,y has wall torch facing east at u=1,y -> powers wire at u=2,y sitting on u=2,y-1
                builder.SetBlock(CircuitLayout.Map(0, 0, 0, 0, y, 0, maxU, 0, Facing), CircuitBlock);
                builder.SetBlock(
                    CircuitLayout.Map(0, 0, 0, 1, y, 0, maxU, 0, Facing),
                    Minecraft.Blocks.RedstoneWallTorch
                        .WithFacing(CircuitLayout.RotateDirection(Direction.East, Facing))
                        .WithLit(true));
                if (y > 0)
                {
                    builder.SetBlock(CircuitLayout.Map(0, 0, 0, 2, y - 1, 0, maxU, 0, Facing), CircuitBlock);
                }

                builder.SetBlock(CircuitLayout.Map(0, 0, 0, 2, y, 0, maxU, 0, Facing), CircuitLayout.CreateAxisWire(Facing, true));
            }
            else
            {
                // Solid block at u=2,y has wall torch facing west at u=1,y -> powers wire at u=0,y sitting on u=0,y-1
                builder.SetBlock(CircuitLayout.Map(0, 0, 0, 2, y, 0, maxU, 0, Facing), CircuitBlock);
                builder.SetBlock(
                    CircuitLayout.Map(0, 0, 0, 1, y, 0, maxU, 0, Facing),
                    Minecraft.Blocks.RedstoneWallTorch
                        .WithFacing(CircuitLayout.RotateDirection(Direction.West, Facing))
                        .WithLit(false));
                if (y > 0)
                {
                    builder.SetBlock(CircuitLayout.Map(0, 0, 0, 0, y - 1, 0, maxU, 0, Facing), CircuitBlock);
                }

                builder.SetBlock(CircuitLayout.Map(0, 0, 0, 0, y, 0, maxU, 0, Facing), CircuitLayout.CreateAxisWire(Facing, true));
            }
        }

        if (IncludeOutputLamp)
        {
            int bottomWireU = (stages - 1) % 2 == 1 ? 2 : 0;
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, bottomWireU, 0, 0, maxU, 0, Facing), Minecraft.Blocks.RedstoneLamp);
        }

        return builder;
    }

    private SchematicBuilder BuildObserverTower(bool upward)
    {
        int leadU = IncludeInputLever ? 1 : 0;
        int canonicalLengthU = leadU + 1;
        int maxU = canonicalLengthU - 1;
        int columnU = leadU;
        int totalHeight = Height + (IncludeOutputLamp ? 1 : 0);

        var builder = CircuitLayout.CreateBuilder(
            this,
            CircuitLayout.GetWidth(canonicalLengthU, 1, Facing),
            totalHeight,
            CircuitLayout.GetLength(canonicalLengthU, 1, Facing),
            upward ? "Observer Tower (Up)" : "Observer Tower (Down)",
            $"{(upward ? "Upward" : "Downward")} observer pulse column ({Height} blocks tall) facing {Facing}.");

        Direction observerLook = upward ? Direction.Down : Direction.Up;
        int baseOffsetY = !upward && IncludeOutputLamp ? 1 : 0;

        for (int i = 0; i < Height; i++)
        {
            CircuitLayout.SetObserver(builder, CircuitLayout.Map(0, 0, 0, columnU, baseOffsetY + i, 0, maxU, 0, Facing), observerLook);
        }

        if (IncludeInputLever)
        {
            int inputY = upward ? 0 : baseOffsetY + Height - 1;
            if (inputY > 0)
            {
                builder.SetBlock(CircuitLayout.Map(0, 0, 0, 0, inputY - 1, 0, maxU, 0, Facing), CircuitBlock);
            }

            builder.SetLever(CircuitLayout.Map(0, 0, 0, 0, inputY, 0, maxU, 0, Facing), BlockFace.Floor, Facing, powered: false);
        }

        if (IncludeOutputLamp)
        {
            int lampY = upward ? Height : 0;
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, columnU, lampY, 0, maxU, 0, Facing), Minecraft.Blocks.RedstoneLamp);
        }

        return builder;
    }

    private SchematicBuilder BuildPistonTower(bool upward)
    {
        int slimeCount = Math.Min(11, Math.Max(1, Height - 3));
        int totalHeight = Math.Max(Height, slimeCount + 3);
        int leadU = IncludeInputLever ? 1 : 0;
        int canonicalLengthU = leadU + 1;
        int maxU = canonicalLengthU - 1;
        int colU = leadU;

        var builder = CircuitLayout.CreateBuilder(
            this,
            CircuitLayout.GetWidth(canonicalLengthU, 1, Facing),
            totalHeight,
            CircuitLayout.GetLength(canonicalLengthU, 1, Facing),
            upward ? "Piston Tower (Up)" : "Piston Tower (Down)",
            $"{(upward ? "Upward" : "Downward")} sticky piston slime tower ({slimeCount} slime blocks).");

        if (upward)
        {
            builder.SetPiston(CircuitLayout.Map(0, 0, 0, colU, 0, 0, maxU, 0, Facing), Direction.Up, sticky: true);
            for (int y = 1; y <= slimeCount; y++)
            {
                builder.SetBlock(CircuitLayout.Map(0, 0, 0, colU, y, 0, maxU, 0, Facing), Minecraft.Blocks.SlimeBlock);
            }

            builder.SetBlock(CircuitLayout.Map(0, 0, 0, colU, slimeCount + 1, 0, maxU, 0, Facing), Minecraft.Blocks.RedstoneBlock);

            if (IncludeInputLever)
            {
                builder.SetLever(CircuitLayout.Map(0, 0, 0, 0, 0, 0, maxU, 0, Facing), BlockFace.Floor, Facing, powered: false);
            }

            if (IncludeOutputLamp)
            {
                builder.SetBlock(CircuitLayout.Map(0, 0, 0, colU, totalHeight - 1, 0, maxU, 0, Facing), Minecraft.Blocks.RedstoneLamp);
            }
        }
        else
        {
            int topY = totalHeight - 1;
            builder.SetPiston(CircuitLayout.Map(0, 0, 0, colU, topY, 0, maxU, 0, Facing), Direction.Down, sticky: true);
            for (int i = 1; i <= slimeCount; i++)
            {
                builder.SetBlock(CircuitLayout.Map(0, 0, 0, colU, topY - i, 0, maxU, 0, Facing), Minecraft.Blocks.SlimeBlock);
            }

            builder.SetBlock(CircuitLayout.Map(0, 0, 0, colU, topY - slimeCount - 1, 0, maxU, 0, Facing), Minecraft.Blocks.RedstoneBlock);

            if (IncludeInputLever)
            {
                builder.SetBlock(CircuitLayout.Map(0, 0, 0, 0, topY - 1, 0, maxU, 0, Facing), CircuitBlock);
                builder.SetLever(CircuitLayout.Map(0, 0, 0, 0, topY, 0, maxU, 0, Facing), BlockFace.Floor, Facing, powered: false);
            }

            if (IncludeOutputLamp)
            {
                builder.SetBlock(CircuitLayout.Map(0, 0, 0, colU, 0, 0, maxU, 0, Facing), Minecraft.Blocks.RedstoneLamp);
            }
        }

        return builder;
    }
}
