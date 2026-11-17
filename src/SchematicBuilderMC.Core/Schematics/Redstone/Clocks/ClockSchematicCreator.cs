using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;
using SchematicBuilderMC.Registries;
using SchematicBuilderMC.Schematics.Redstone.Internal;

namespace SchematicBuilderMC.Schematics.Redstone.Clocks;

/// <summary>
/// Supported Redstone clock families on Minecraft Wiki.
/// </summary>
public enum ClockType
{
    Repeater,
    TorchRepeater,
    TorchLoop,
    Comparator,
    Hopper,
    Observer,
    Piston,
    Minecart
}

/// <summary>
/// Unified configurable Redstone clock creator delegating to any of the Wiki clock topologies with optional <see cref="Period"/> or <see cref="PeriodTicks"/>.
/// </summary>
public class ClockSchematicCreator : SchematicCreator
{
    public ClockType ClockType { get; set; } = ClockType.Repeater;

    public TimeSpan? Period { get; set; }

    public int? PeriodTicks { get; set; }

    public Direction Facing { get; set; } = Direction.East;

    public string CircuitBlock { get; set; } = Minecraft.Blocks.SmoothStone;

    public string ItemId { get; set; } = Minecraft.Items.Cobblestone;

    public bool IncludeToggleLever { get; set; }

    public bool IncludeOutputLamp { get; set; }

    public override SchematicBuilder Create()
    {
        if (!Enum.IsDefined(ClockType))
        {
            throw new ArgumentOutOfRangeException(nameof(ClockType), ClockType, "Unsupported clock type.");
        }

        CircuitLayout.ValidateHorizontalFacing(Facing);
        CircuitLayout.ValidateBlockId(CircuitBlock, nameof(CircuitBlock));

        SchematicCreator inner = ClockType switch
        {
            ClockType.Repeater => new RepeaterClockSchematicCreator
            {
                Period = Period,
                PeriodTicks = PeriodTicks,
                Facing = Facing,
                CircuitBlock = CircuitBlock,
                IncludeToggleLever = IncludeToggleLever,
                IncludeOutputLamp = IncludeOutputLamp,
                Rotation = Rotation
            },
            ClockType.TorchRepeater => new TorchClockSchematicCreator
            {
                Mode = TorchClockMode.TorchRepeater,
                Period = Period,
                PeriodTicks = PeriodTicks,
                Facing = Facing,
                CircuitBlock = CircuitBlock,
                IncludeToggleLever = IncludeToggleLever,
                IncludeOutputLamp = IncludeOutputLamp,
                Rotation = Rotation
            },
            ClockType.TorchLoop => new TorchClockSchematicCreator
            {
                Mode = TorchClockMode.TorchLoop,
                Period = Period,
                PeriodTicks = PeriodTicks,
                Facing = Facing,
                CircuitBlock = CircuitBlock,
                IncludeToggleLever = IncludeToggleLever,
                IncludeOutputLamp = IncludeOutputLamp,
                Rotation = Rotation
            },
            ClockType.Comparator => new ComparatorClockSchematicCreator
            {
                Period = Period,
                PeriodTicks = PeriodTicks,
                Facing = Facing,
                CircuitBlock = CircuitBlock,
                IncludeToggleLever = IncludeToggleLever,
                IncludeOutputLamp = IncludeOutputLamp,
                Rotation = Rotation
            },
            ClockType.Hopper => new HopperClockSchematicCreator
            {
                Period = Period,
                PeriodTicks = PeriodTicks,
                Facing = Facing,
                CircuitBlock = CircuitBlock,
                ItemId = ItemId,
                IncludeToggleLever = IncludeToggleLever,
                IncludeOutputLamp = IncludeOutputLamp,
                Rotation = Rotation
            },
            ClockType.Observer => new ObserverClockSchematicCreator
            {
                Period = Period,
                PeriodTicks = PeriodTicks,
                Facing = Facing,
                CircuitBlock = CircuitBlock,
                IncludeToggleLever = IncludeToggleLever,
                IncludeOutputLamp = IncludeOutputLamp,
                Rotation = Rotation
            },
            ClockType.Piston => new PistonClockSchematicCreator
            {
                Period = Period,
                PeriodTicks = PeriodTicks,
                Facing = Facing,
                CircuitBlock = CircuitBlock,
                IncludeToggleLever = IncludeToggleLever,
                IncludeOutputLamp = IncludeOutputLamp,
                Rotation = Rotation
            },
            ClockType.Minecart => new MinecartClockSchematicCreator
            {
                Period = Period,
                PeriodTicks = PeriodTicks,
                Facing = Facing,
                CircuitBlock = CircuitBlock,
                IncludeOutputLamp = IncludeOutputLamp,
                Rotation = Rotation
            },
            _ => throw new ArgumentOutOfRangeException(nameof(ClockType), ClockType, "Unsupported clock type.")
        };

        return inner.Create();
    }
}
