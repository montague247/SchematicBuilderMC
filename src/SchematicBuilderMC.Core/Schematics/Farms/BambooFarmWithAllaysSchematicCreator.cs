using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;
using SchematicBuilderMC.Extensions;
using SchematicBuilderMC.Models;
using SchematicBuilderMC.Registries;
using SchematicBuilderMC.Schematics.Contraptions;
using SchematicBuilderMC.Schematics.Redstone.Clocks;
using SchematicBuilderMC.Schematics.Redstone.Internal;

namespace SchematicBuilderMC.Schematics.Farms;

/// <summary>
/// Builds a self-contained flying-machine bamboo farm collected by Allays.
/// <see cref="Width"/> and <see cref="Length"/> specify the exact dimensions of the dirt bed.
/// </summary>
public sealed class BambooFarmWithAllaysSchematicCreator : SchematicCreator
{
    private const int SoilLevel = 2;
    private const int MinDirtWidth = 4;
    private const int MaxDirtWidth = 33; // 16 * 2 + 1: max 16-block allay reach to the centered note block
    private const int MinDirtLength = 5;
    private const int MinFrontPadding = 2; // space required before the start wall for the trigger bus and clock lift
    private const int BackPadding = 8; // wallEndZ + 1 (bus/repeater) + 7 (north-facing hopper clock)

    /// <summary>
    /// Exact width (X) of the dirt bed in blocks (clamped to <c>4..33</c>).
    /// </summary>
    public int Width { get; set; } = MaxDirtWidth;

    /// <summary>
    /// Exact length (Z) of the dirt bed in blocks (minimum <c>5</c>).
    /// </summary>
    public int Length { get; set; } = 32;

    /// <summary>
    /// Minimum clear bamboo growth height inside the farm.
    /// </summary>
    public int MaxBambooHeight { get; set; } = 4;

    /// <summary>
    /// Blocks reserved before the start wall along <c>-Z</c> (minimum <c>2</c>).
    /// Increasing this value shifts the entire farm cleanly along <c>+Z</c>.
    /// </summary>
    public int FrontPadding { get; set; } = MinFrontPadding;

    public string SoilBlock { get; set; } = Minecraft.Blocks.Dirt;

    public string WallBlock { get; set; } = Minecraft.Blocks.WaxedCopperBlock;

    public string? RoofBlock { get; set; } = Minecraft.Blocks.Glass;

    public string? LightBlock { get; set; } = Minecraft.Blocks.VerdantFroglight;

    public void SetBestWidth()
    {
        // Max range of an allay to the centered note block is 16 blocks on each side.
        Width = MaxDirtWidth;
    }

    public override SchematicBuilder Create()
    {
        var layout = CreateLayout();

        var builder = SchematicBuilder.Create(layout.TotalWidth, layout.TotalHeight, layout.TotalLength, Rotation)
            .WithOffset(0, 0, 0)
            .WithName("Bamboo Farm with Allays")
            .WithAuthor("Dirk Helbig")
            .WithDescription("A bamboo farm schematic with allays");

        PlaceWallsAndCropBed(builder, layout);
        PlaceHarvestClock(builder, layout);

        // create the flying machine(s)
        var flyingMachine = new FlatFlyingMachine();
        builder.Create(
            flyingMachine,
            layout.DirtStartX,
            SoilLevel + 2,
            layout.DirtStartZ,
            layout.DirtWidth,
            out List<BlockPosition> triggerPositions,
            Minecraft.Blocks.Bamboo,
            layout.DirtEndZ);

        // connect trigger with clock
        ConnectTriggerWithClock(builder, triggerPositions);

        PlaceCollectionSystem(builder, layout);
        PlaceAllays(builder, layout);
        PlaceRoofAndLights(builder, layout);

        return builder;
    }

    private FarmLayout CreateLayout()
    {
        int dirtWidth = Math.Clamp(Width, MinDirtWidth, MaxDirtWidth);
        int dirtLength = Math.Max(Length, MinDirtLength);
        int maxBambooHeight = Math.Max(MaxBambooHeight, 2);
        int wallHeight = Math.Max(maxBambooHeight, 7);
        int frontPadding = Math.Max(FrontPadding, MinFrontPadding);

        return new FarmLayout(dirtWidth, dirtLength, wallHeight, frontPadding, RoofBlock != null || LightBlock != null);
    }

    private void PlaceWallsAndCropBed(SchematicBuilder builder, FarmLayout layout)
    {
        int wallTopY = SoilLevel + layout.WallHeight;

        builder.Fill(layout.WallStartX, SoilLevel, layout.WallStartZ, layout.WallEndX, wallTopY, layout.WallStartZ, WallBlock);
        builder.Fill(layout.WallStartX, SoilLevel, layout.WallEndZ, layout.WallEndX, wallTopY, layout.WallEndZ, WallBlock);
        builder.Fill(layout.WallStartX, SoilLevel, layout.WallStartZ, layout.WallStartX, wallTopY, layout.WallEndZ, WallBlock);
        builder.Fill(layout.WallEndX, SoilLevel, layout.WallStartZ, layout.WallEndX, wallTopY, layout.WallEndZ, WallBlock);

        // Dirt bed matches exact Width x Length dimensions inside the walls.
        builder.Fill(layout.DirtStartX, SoilLevel, layout.DirtStartZ, layout.DirtEndX, SoilLevel, layout.DirtEndZ, SoilBlock);

        // Leave the first and last dirt rows open for the flying machine stations.
        builder.Fill(
            layout.DirtStartX,
            SoilLevel + 1,
            layout.DirtStartZ + 1,
            layout.DirtEndX,
            SoilLevel + 1,
            layout.DirtEndZ - 1,
            Minecraft.Blocks.Bamboo);
    }

    private static void PlaceHarvestClock(SchematicBuilder builder, FarmLayout layout)
    {
        int clockStartZ = layout.CenterZ - 4;
        new HopperClockSchematicCreator
        {
            Period = TimeSpan.FromSeconds(30),
            TorchPlacement = HopperClockTorchPlacement.Both
        }.WithRotation(Rotation.Clockwise90).Place(builder, layout.CenterX - 1, 0, clockStartZ);
    }

    private static void ConnectTriggerWithClock(SchematicBuilder builder, List<BlockPosition> triggerPositions)
    {
        if (triggerPositions.Count < 2)
        {
            return;
        }

        var startTriggerXs = new List<int>();
        var endTriggerXs = new List<int>();

        for (int i = 0; i < triggerPositions.Count; i++)
        {
            BlockPosition triggerPos = triggerPositions[i];
            bool isStart = i % 2 == 0;
            builder.SetBlock(
                triggerPos,
                Minecraft.Blocks.Observer.WithFacing(isStart ? Direction.North : Direction.South));

            if (isStart)
            {
                startTriggerXs.Add(triggerPos.X);
            }
            else
            {
                endTriggerXs.Add(triggerPos.X);
            }
        }

        int wallStartZ = triggerPositions[0].Z;
        int wallEndZ = triggerPositions[1].Z;
        int triggerY = triggerPositions[0].Y;
        int dirtStartZ = wallStartZ + 1;
        int dirtLength = wallEndZ - dirtStartZ;
        int centerX = 1 + ((builder.Width - 2) / 2);
        int centerZ = dirtStartZ + (dirtLength / 2);
        int clockStartZ = centerZ - 4;
        int clockEndZ = clockStartZ + 7;

        // Connect north and south clock torches to their respective trigger buses and torch lifts.
        PlaceTriggerBus(builder, centerX, triggerY, clockStartZ - 1, Direction.North, wallStartZ - 1, wallStartZ - 2, Direction.South, startTriggerXs);
        PlaceTriggerBus(builder, centerX, triggerY, clockEndZ + 1, Direction.South, wallEndZ + 1, wallEndZ + 2, Direction.North, endTriggerXs);
    }

    private static void PlaceTriggerBus(
        SchematicBuilder builder,
        int centerX,
        int busY,
        int clockWireStartZ,
        Direction clockWireDirection,
        int busZ,
        int towerZ,
        Direction towerToBusDirection,
        List<int> triggerXs)
    {
        if (triggerXs.Count == 0)
        {
            return;
        }

        int wireLength = (clockWireDirection == Direction.North
            ? clockWireStartZ - busZ
            : busZ - clockWireStartZ) + 1;
        if (wireLength > 0)
        {
            builder.SetWireLine(
                centerX,
                1,
                clockWireStartZ,
                clockWireDirection,
                wireLength,
                supportBlock: Minecraft.Blocks.SmoothStone);
        }

        // Vertical torch tower lifting signal from y = 1 up to busY.
        builder.SetBlock(centerX, 1, towerZ, Minecraft.Blocks.SmoothStone);
        builder.SetBlock(centerX, 2, towerZ, Minecraft.Blocks.RedstoneTorch.WithLit(true));
        builder.SetBlock(centerX, 3, towerZ, Minecraft.Blocks.SmoothStone);
        builder.SetBlock(centerX, 4, towerZ, Minecraft.Blocks.RedstoneTorch.WithLit(false));
        builder.SetBlock(centerX, 5, towerZ, Minecraft.Blocks.SmoothStone);
        builder.SetBlock(
            centerX,
            busY,
            towerZ,
            CircuitLayout.CreateWire(
                Direction.North,
                north: towerToBusDirection == Direction.North,
                east: false,
                south: towerToBusDirection == Direction.South,
                west: false));

        int minX = Math.Min(centerX, triggerXs.Min());
        int maxX = Math.Max(centerX, triggerXs.Max());

        builder.SetBlock(centerX, busY - 1, busZ, Minecraft.Blocks.SmoothStone);
        builder.SetBlock(
            centerX,
            busY,
            busZ,
            CircuitLayout.CreateWire(
                Direction.North,
                north: towerToBusDirection == Direction.South,
                east: maxX > centerX,
                south: towerToBusDirection == Direction.North,
                west: minX < centerX));

        if (minX < centerX)
        {
            builder.SetWireLine(
                centerX - 1,
                busY,
                busZ,
                Direction.West,
                length: centerX - minX,
                dustOffset: 1,
                supportBlock: Minecraft.Blocks.SmoothStone);
        }

        if (maxX > centerX)
        {
            builder.SetWireLine(
                centerX + 1,
                busY,
                busZ,
                Direction.East,
                length: maxX - centerX,
                dustOffset: 1,
                supportBlock: Minecraft.Blocks.SmoothStone);
        }
    }

    private void PlaceCollectionSystem(SchematicBuilder builder, FarmLayout layout)
    {
        int centerX = layout.CenterX;
        int centerZ = layout.CenterZ;
        int hopperY = layout.HopperY;
        int noteBlockY = layout.NoteBlockY;

        // Solid block below the note block so air directly above the note block stays clear.
        builder.SetBlock(centerX, hopperY, centerZ, WallBlock);
        builder.SetBlock(centerX, noteBlockY, centerZ, Minecraft.Blocks.NoteBlock);
        builder.SurroundWithTrapdoors(centerX, noteBlockY + 1, centerZ, Minecraft.Blocks.BambooTrapdoor);

        // Route all surrounding hoppers into one north trunk exiting before the start wall.
        builder.Fill(centerX, hopperY, layout.WallStartZ, centerX, hopperY, centerZ - 1, Minecraft.Blocks.Hopper.FacingNorth());
        builder.SetBlock(centerX - 1, hopperY, centerZ - 1, Minecraft.Blocks.Hopper.FacingEast());
        builder.SetBlock(centerX - 1, hopperY, centerZ, Minecraft.Blocks.Hopper.FacingNorth());
        builder.SetBlock(centerX - 1, hopperY, centerZ + 1, Minecraft.Blocks.Hopper.FacingNorth());
        builder.SetBlock(centerX + 1, hopperY, centerZ - 1, Minecraft.Blocks.Hopper.FacingWest());
        builder.SetBlock(centerX + 1, hopperY, centerZ, Minecraft.Blocks.Hopper.FacingNorth());
        builder.SetBlock(centerX + 1, hopperY, centerZ + 1, Minecraft.Blocks.Hopper.FacingNorth());

        builder.SetBlock(centerX, hopperY, layout.WallStartZ - 1, Minecraft.Blocks.Chest.FacingNorth());
        builder.SetChest(centerX, hopperY, layout.WallStartZ - 1);

        // Place the note block timer behind the end wall facing north so its end torch pulses north.
        int clockStartZ = layout.WallEndZ + 2;
        new HopperClockSchematicCreator
        {
            Period = TimeSpan.FromSeconds(13),
            Facing = Direction.North,
            TorchPlacement = HopperClockTorchPlacement.End
        }.Place(builder, centerX, noteBlockY - 1, clockStartZ);

        // Repeater at wallEndZ + 1 boosts signal into the south wire trunk (solid blocks prevent locking hoppers).
        builder.SetBlock(centerX, noteBlockY - 1, layout.WallEndZ + 1, Minecraft.Blocks.SmoothStone);
        CircuitLayout.SetRepeater(builder, new BlockPosition(centerX, noteBlockY, layout.WallEndZ + 1), Direction.North);
        builder.SetWireLine(
            centerX,
            noteBlockY,
            layout.WallEndZ,
            Direction.North,
            length: layout.WallEndZ - centerZ,
            supportBlock: Minecraft.Blocks.SmoothStone);
    }

    private static void PlaceAllays(SchematicBuilder builder, FarmLayout layout)
    {
        int allayY = SoilLevel + 4;
        builder.AddEntity(layout.DirtStartX, allayY, layout.DirtStartZ, CreateAllay());
        builder.AddEntity(layout.DirtEndX, allayY, layout.DirtStartZ, CreateAllay());
        builder.AddEntity(layout.DirtEndX, allayY, layout.DirtEndZ, CreateAllay());
        builder.AddEntity(layout.DirtStartX, allayY, layout.DirtEndZ, CreateAllay());
    }

    private static Entity CreateAllay() =>
        new Entity(Minecraft.Entities.Allay)
            .WithNoGravity()
            .WithMainHand(new ItemStack(Minecraft.Items.Bamboo));

    private void PlaceRoofAndLights(SchematicBuilder builder, FarmLayout layout)
    {
        int roofY = layout.RoofY;

        if (RoofBlock != null)
        {
            builder.Fill(layout.WallStartX, roofY, layout.WallStartZ, layout.WallEndX, roofY, layout.WallEndZ, RoofBlock);
        }

        if (LightBlock != null)
        {
            const int LightSpacing = 3;
            for (int x = layout.DirtStartX + 1; x <= layout.DirtEndX - 1; x += LightSpacing)
            {
                for (int z = layout.DirtStartZ + 1; z <= layout.DirtEndZ - 1; z += LightSpacing)
                {
                    builder.SetBlock(x, roofY, z, LightBlock);
                }
            }
        }
    }

    private readonly record struct FarmLayout(int DirtWidth, int DirtLength, int WallHeight, int FrontPadding, bool HasRoofLayer)
    {
        public int WallStartX => 0;
        public int DirtStartX => WallStartX + 1;
        public int DirtEndX => DirtStartX + DirtWidth - 1;
        public int WallEndX => DirtEndX + 1;
        public int TotalWidth => WallEndX + 1;

        public int WallStartZ => FrontPadding;
        public int DirtStartZ => WallStartZ + 1;
        public int DirtEndZ => DirtStartZ + DirtLength - 1;
        public int WallEndZ => DirtEndZ + 1;
        public int TotalLength => WallEndZ + 1 + BackPadding;

        public int CenterX => DirtStartX + (DirtWidth / 2);
        public int CenterZ => DirtStartZ + (DirtLength / 2);

        public int TriggerY => SoilLevel + 4;
        public int HopperY => SoilLevel + 5;
        public int NoteBlockY => SoilLevel + 6;
        public int RoofY => SoilLevel + WallHeight + 1;
        public int TotalHeight => SoilLevel + WallHeight + 1 + (HasRoofLayer ? 1 : 0);
    }
}
