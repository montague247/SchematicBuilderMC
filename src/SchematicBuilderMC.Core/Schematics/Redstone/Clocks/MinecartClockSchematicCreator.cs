using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;
using SchematicBuilderMC.Extensions;
using SchematicBuilderMC.Models;
using SchematicBuilderMC.Registries;
using SchematicBuilderMC.Schematics.Redstone.Internal;

namespace SchematicBuilderMC.Schematics.Redstone.Clocks;

/// <summary>
/// Builds a closed minecart rail clock with booster powered rails, a detector rail tap, and a minecart entity.
/// Supports configurable track loop length or approximate cycle period via <see cref="Period"/> or <see cref="PeriodTicks"/>.
/// </summary>
public class MinecartClockSchematicCreator : SchematicCreator
{
    /// <summary>
    /// Rectangular rail loop length along <see cref="Facing"/> (minimum 3).
    /// </summary>
    public int TrackLength { get; set; } = 5;

    /// <summary>
    /// Optional target cycle period; overrides <see cref="TrackLength"/> using ~8 blocks/sec minecart speed (~2.5 gt/block).
    /// </summary>
    public TimeSpan? Period { get; set; }

    public int? PeriodTicks { get; set; }

    public Direction Facing { get; set; } = Direction.East;

    public string CircuitBlock { get; set; } = Minecraft.Blocks.SmoothStone;

    public bool IncludeOutputLamp { get; set; }

    public int GetResolvedTrackLength()
    {
        if (Period.HasValue || PeriodTicks.HasValue)
        {
            int ticks = CircuitLayout.ResolveGameTicks(Period, PeriodTicks, defaultGameTicks: 30, minGameTicks: 20, stepGameTicks: 2);
            // Closed loop length along 3-wide track: perimeter = 2*L + 2*(W-2) ~= 2*L + 2 blocks; at ~2.5 gt/block -> L ~= (ticks / 5) - 1
            int computed = Math.Max(3, (int)Math.Round((ticks / 5.0) - 1));
            return computed;
        }

        return Math.Max(3, TrackLength);
    }

    public override SchematicBuilder Create()
    {
        CircuitLayout.ValidateHorizontalFacing(Facing);
        CircuitLayout.ValidatePositive(TrackLength, nameof(TrackLength), 3);
        CircuitLayout.ValidateBlockId(CircuitBlock, nameof(CircuitBlock));

        int loopU = GetResolvedTrackLength();
        const int widthV = 3;
        int totalLengthU = loopU + (IncludeOutputLamp ? 1 : 0);
        int maxU = totalLengthU - 1;
        int maxV = widthV - 1;

        var builder = CircuitLayout.CreateBuilder(
            this,
            CircuitLayout.GetWidth(totalLengthU, widthV, Facing),
            2,
            CircuitLayout.GetLength(totalLengthU, widthV, Facing),
            "Minecart Clock",
            $"Closed minecart track clock ({loopU}x{widthV} loop) facing {Facing}.");

        for (int u = 0; u < loopU; u++)
        {
            for (int v = 0; v < widthV; v++)
            {
                if (u > 0 && u < loopU - 1 && v == 1)
                {
                    builder.SetBlock(CircuitLayout.Map(0, 0, 0, u, 0, v, maxU, maxV, Facing), Minecraft.Blocks.RedstoneBlock);
                    continue;
                }

                builder.SetBlock(CircuitLayout.Map(0, 0, 0, u, 0, v, maxU, maxV, Facing), CircuitBlock);
            }
        }

        // Straight/corner rail shapes in rotated world orientation
        bool axisEastWest = Facing is Direction.East or Direction.West;
        string shapeStraightU = axisEastWest ? "east_west" : "north_south";
        string shapeStraightV = axisEastWest ? "north_south" : "east_west";

        // Outer ring perimeter (u=0..loopU-1, v=0..2)
        for (int u = 1; u < loopU - 1; u++)
        {
            var topPos = CircuitLayout.Map(0, 0, 0, u, 1, 0, maxU, maxV, Facing);
            var botPos = CircuitLayout.Map(0, 0, 0, u, 1, 2, maxU, maxV, Facing);

            if (u == 1)
            {
                builder.SetBlock(topPos, Minecraft.Blocks.PoweredRail.WithProperty("shape", shapeStraightU).WithPowered(true));
                builder.SetBlock(botPos, Minecraft.Blocks.PoweredRail.WithProperty("shape", shapeStraightU).WithPowered(true));
            }
            else if (u == loopU - 2)
            {
                builder.SetBlock(topPos, Minecraft.Blocks.DetectorRail.WithProperty("shape", shapeStraightU).WithPowered(false));
                builder.SetBlock(botPos, Minecraft.Blocks.Rail.WithProperty("shape", shapeStraightU));
            }
            else
            {
                builder.SetBlock(topPos, Minecraft.Blocks.Rail.WithProperty("shape", shapeStraightU));
                builder.SetBlock(botPos, Minecraft.Blocks.Rail.WithProperty("shape", shapeStraightU));
            }
        }

        builder.SetBlock(CircuitLayout.Map(0, 0, 0, 0, 1, 1, maxU, maxV, Facing), Minecraft.Blocks.Rail.WithProperty("shape", shapeStraightV));
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, loopU - 1, 1, 1, maxU, maxV, Facing), Minecraft.Blocks.Rail.WithProperty("shape", shapeStraightV));

        // 4 corners automatically align via shape curves mapped cleanly for world axis
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, 0, 1, 0, maxU, maxV, Facing), Minecraft.Blocks.Rail);
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, loopU - 1, 1, 0, maxU, maxV, Facing), Minecraft.Blocks.Rail);
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, 0, 1, 2, maxU, maxV, Facing), Minecraft.Blocks.Rail);
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, loopU - 1, 1, 2, maxU, maxV, Facing), Minecraft.Blocks.Rail);

        if (IncludeOutputLamp)
        {
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, totalLengthU - 1, 0, 0, maxU, maxV, Facing), CircuitBlock);
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, totalLengthU - 1, 1, 0, maxU, maxV, Facing), Minecraft.Blocks.RedstoneLamp);
        }

        // Place minecart entity centered on top-lane rail at u=1, y=1, v=0 with initial forward momentum
        var cartPos = CircuitLayout.Map(0, 0, 0, 1, 1, 0, maxU, maxV, Facing);
        (double vx, double vz) = Facing switch
        {
            Direction.East => (0.4, 0.0),
            Direction.West => (-0.4, 0.0),
            Direction.South => (0.0, 0.4),
            Direction.North => (0.0, -0.4),
            _ => (0.4, 0.0)
        };
        builder.AddMinecart(cartPos.X + 0.5, cartPos.Y + 0.0625, cartPos.Z + 0.5, motionX: vx, motionY: 0.0, motionZ: vz);

        return builder;
    }
}
