using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;
using SchematicBuilderMC.Models;
using SchematicBuilderMC.Registries;
using SchematicBuilderMC.Schematics.Redstone.Internal;

namespace SchematicBuilderMC.Schematics.Redstone.Miscellaneous;

/// <summary>
/// Builds a configurable inline multi-stage sticky piston extender (Double, Triple, or N-stage)
/// with sequenced extension/retraction timing delays.
/// </summary>
public class PistonExtenderSchematicCreator : SchematicCreator
{
    /// <summary>
    /// Number of cascaded sticky pistons (2 = Double Piston Extender, 3 = Triple, ...). Defaults to 2.
    /// </summary>
    public int Reach { get; set; } = 2;

    public Direction Facing { get; set; } = Direction.East;

    public string CircuitBlock { get; set; } = Minecraft.Blocks.SmoothStone;

    /// <summary>
    /// Payload block pushed out by the final piston. Defaults to <see cref="Minecraft.Blocks.Glass"/>.
    /// </summary>
    public string PayloadBlock { get; set; } = Minecraft.Blocks.Glass;

    public bool IncludeInputLever { get; set; }

    public override SchematicBuilder Create()
    {
        CircuitLayout.ValidateHorizontalFacing(Facing);
        if (Reach < 2 || Reach > 6)
        {
            throw new ArgumentOutOfRangeException(nameof(Reach), Reach, "Reach must be between 2 and 6 blocks.");
        }

        CircuitLayout.ValidateBlockId(CircuitBlock, nameof(CircuitBlock));
        CircuitLayout.ValidateBlockId(PayloadBlock, nameof(PayloadBlock));

        // Double/Triple+ horizontal inline piston extender with ABBA timed driver lane beside pistons
        int inputLead = IncludeInputLever ? 1 : 0;
        int coreLengthU = (Reach * 3) + 1;
        const int widthV = 3;
        int totalLengthU = inputLead + coreLengthU;
        int height = IncludeInputLever ? 3 : 2;
        int maxU = totalLengthU - 1;
        int maxV = widthV - 1;

        var builder = CircuitLayout.CreateBuilder(
            this,
            CircuitLayout.GetWidth(totalLengthU, widthV, Facing),
            height,
            CircuitLayout.GetLength(totalLengthU, widthV, Facing),
            $"{Reach}-Stage Piston Extender",
            $"{Reach}-piston horizontal sticky piston extender facing {Facing}.");

        for (int u = 0; u < totalLengthU; u++)
        {
            for (int v = 0; v < widthV; v++)
            {
                builder.SetBlock(CircuitLayout.Map(0, 0, 0, u, 0, v, maxU, maxV, Facing), CircuitBlock);
            }
        }

        if (IncludeInputLever)
        {
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, 0, 1, 2, maxU, maxV, Facing), CircuitBlock);
            builder.SetLever(CircuitLayout.Map(0, 0, 0, 0, 2, 2, maxU, maxV, Facing), BlockFace.Floor, Facing, powered: false);
        }

        Direction sideNorth = CircuitLayout.RotateDirection(Direction.North, Facing);
        int u0 = inputLead;

        // Pistons aligned along v=0 pushing forward in +U
        for (int p = 0; p < Reach; p++)
        {
            builder.SetPiston(CircuitLayout.Map(0, 0, 0, u0 + p, 1, 0, maxU, maxV, Facing), Facing, sticky: true);
        }

        builder.SetBlock(CircuitLayout.Map(0, 0, 0, u0 + Reach, 1, 0, maxU, maxV, Facing), PayloadBlock);

        // Sequenced timing driver along v=2 -> side repeaters on v=1 feeding piston positions (0..Reach-1)
        for (int u = u0; u < u0 + Reach; u++)
        {
            int p = u - u0;
            int delay = Math.Clamp(p + 1, 1, 4);
            builder.SetBlock(CircuitLayout.Map(0, 0, 0, u, 1, 2, maxU, maxV, Facing), CircuitLayout.CreateWire(Facing, north: true, east: u < u0 + Reach - 1, south: false, west: true));
            CircuitLayout.SetRepeater(builder, CircuitLayout.Map(0, 0, 0, u, 1, 1, maxU, maxV, Facing), sideNorth, delay: delay);
        }

        return builder;
    }
}
