using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;
using SchematicBuilderMC.Models;
using SchematicBuilderMC.Registries;
using SchematicBuilderMC.Schematics.Redstone.Internal;

namespace SchematicBuilderMC.Schematics.Redstone.Miscellaneous;

/// <summary>
/// Builds a configurable ABBA circuit that activates outputs in forward order (<c>A -&gt; B -&gt; C ...</c>)
/// and deactivates them in exact reverse order (<c>... C -&gt; B -&gt; A</c>).
/// Supports configurable <see cref="OutputCount"/> and <see cref="StepDelay"/> / <see cref="StepDelayTicks"/>.
/// </summary>
public class AbbaCircuitSchematicCreator : SchematicCreator
{
    public int OutputCount { get; set; } = 3;

    public TimeSpan? StepDelay { get; set; }

    public int? StepDelayTicks { get; set; }

    public Direction Facing { get; set; } = Direction.East;

    public string CircuitBlock { get; set; } = Minecraft.Blocks.SmoothStone;

    public bool IncludeInputLever { get; set; }

    public bool IncludeOutputLamps { get; set; } = true;

    public int GetActualStepTicks() =>
        CircuitLayout.ResolveGameTicks(StepDelay, StepDelayTicks, defaultGameTicks: 4, minGameTicks: 2, stepGameTicks: 2);

    public override SchematicBuilder Create()
    {
        CircuitLayout.ValidateHorizontalFacing(Facing);
        CircuitLayout.ValidatePositive(OutputCount, nameof(OutputCount), 2);
        CircuitLayout.ValidateBlockId(CircuitBlock, nameof(CircuitBlock));

        int stepTicks = GetActualStepTicks();
        int stepRt = stepTicks / 2;
        int repeatersPerStep = Math.Max(1, (int)Math.Ceiling(stepRt / 4.0));
        int stride = 1 + repeatersPerStep;

        int inputLead = IncludeInputLever ? 1 : 0;
        int coreLengthU = 2 + ((OutputCount - 1) * stride) + 1;
        const int widthV = 5;
        int totalLengthU = inputLead + coreLengthU;
        int height = IncludeOutputLamps || IncludeInputLever ? 3 : 2;
        int maxU = totalLengthU - 1;
        int maxV = widthV - 1;

        var builder = CircuitLayout.CreateBuilder(
            this,
            CircuitLayout.GetWidth(totalLengthU, widthV, Facing),
            height,
            CircuitLayout.GetLength(totalLengthU, widthV, Facing),
            "ABBA Sequence Circuit",
            $"ABBA {OutputCount}-output circuit ({stepTicks} gt step delay) facing {Facing}.");

        for (int u = 0; u < totalLengthU; u++)
        {
            for (int v = 0; v < widthV; v++)
            {
                builder.SetBlock(CircuitLayout.Map(0, 0, 0, u, 0, v, maxU, maxV, Facing), CircuitBlock);
            }
        }

        if (IncludeInputLever)
        {
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, 0, 1, 0, maxU, maxV, Facing), CircuitBlock);
            builder.SetLever(CircuitLayout.Map(0, 0, 0, 0, 2, 0, maxU, maxV, Facing), BlockFace.Floor, Facing, powered: false);
        }

        Direction reverse = CircuitLayout.GetOpposite(Facing);
        Direction sideSouth = CircuitLayout.RotateDirection(Direction.South, Facing);
        Direction sideNorth = CircuitLayout.RotateDirection(Direction.North, Facing);

        int startU = inputLead;
        int turnU = startU + ((OutputCount - 1) * stride) + 1;

        // Input enters v=0 forward line and loops around end corner turnU back along v=4 reverse line.
        builder.SetBlock(CircuitLayout.Map(0, 0, 0, startU, 1, 0, maxU, maxV, Facing), CircuitLayout.CreateAxisWire(Facing, true));

        for (int i = 0; i < OutputCount; i++)
        {
            int tapU = startU + 1 + (i * stride);

            // v=0: forward tap block, v=1: forward diode, v=2: shared output block/lamp, v=3: reverse diode, v=4: reverse tap block
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, tapU, 1, 0, maxU, maxV, Facing), CircuitBlock);
            builder.SetRepeater(CircuitLayout.Map(0, 0, 0, tapU, 1, 1, maxU, maxV, Facing), sideSouth, delay: 1);
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, tapU, 1, 2, maxU, maxV, Facing), CircuitBlock);
            builder.SetRepeater(CircuitLayout.Map(0, 0, 0, tapU, 1, 3, maxU, maxV, Facing), sideNorth, delay: 1);
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, tapU, 1, 4, maxU, maxV, Facing), CircuitBlock);

            if (IncludeOutputLamps)
            {
                builder.SetBlock(CircuitLayout.Map(0, 0, 0, tapU, 2, 2, maxU, maxV, Facing), Minecraft.Blocks.RedstoneLamp);
            }

            if (i < OutputCount - 1)
            {
                int remainingRt = stepRt;
                for (int r = 0; r < repeatersPerStep; r++)
                {
                    int left = repeatersPerStep - 1 - r;
                    int rt = Math.Clamp(remainingRt - left, 1, 4);
                    remainingRt -= rt;

                    int stepU = tapU + 1 + r;
                    builder.SetRepeater(CircuitLayout.Map(0, 0, 0, stepU, 1, 0, maxU, maxV, Facing), Facing, delay: rt);
                    builder.SetRepeater(CircuitLayout.Map(0, 0, 0, stepU, 1, 4, maxU, maxV, Facing), reverse, delay: rt);
                }
            }
        }

        // Loop turnaround connecting forward end of v=0 around turnU to reverse start of v=4
        int loopbackU = startU + 1 + ((OutputCount - 1) * stride) + 1;
        for (int v = 0; v < widthV; v++)
        {
            builder.SetBlock(
                CircuitLayout.Map(0, 0, 0, loopbackU, 1, v, maxU, maxV, Facing),
                CircuitLayout.CreateWire(Facing, north: v > 0, east: false, south: v < widthV - 1, west: v is 0 or 4));
        }

        return builder;
    }
}
