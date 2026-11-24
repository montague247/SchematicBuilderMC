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
    private const int RotatedNoteBlockClockBackPadding = 8; // wallEndZ + 1 (repeater) + 7 (north-facing clock)
    private const int FlushNoteBlockClockBackPadding = 3; // wallEndZ + 1 (repeater) + 2 (east-facing clock depth)
    private const int FlushNoteBlockClockWidth = 7;

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

    /// <summary>
    /// When <c>true</c> (default), places the note block hopper clock rotated perpendicular to the end wall along <c>+Z</c>.
    /// When <c>false</c>, places the hopper clock unrotated parallel to the end wall along <c>+X</c>.
    /// </summary>
    public bool RotateNoteBlockClock { get; set; }

    /// <summary>
    /// When <c>true</c> (default) and multiple flying machines exist, cross-connects each machine's start and end triggers
    /// to opposite rising-edge clock channels so only one flying machine runs at a time.
    /// When <c>false</c>, connects all start triggers to the north clock line and all end triggers to the south clock line.
    /// </summary>
    public bool CrossConnectTriggerWiring { get; set; } = true;

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
        int sidePadding = RotateNoteBlockClock
            ? 0
            : Math.Max(0, FlushNoteBlockClockWidth - (1 + (dirtWidth / 2)));
        int backPadding = RotateNoteBlockClock
            ? RotatedNoteBlockClockBackPadding
            : FlushNoteBlockClockBackPadding;

        return new FarmLayout(dirtWidth, dirtLength, wallHeight, sidePadding, frontPadding, backPadding, RoofBlock != null);
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

    private void ConnectTriggerWithClock(SchematicBuilder builder, List<BlockPosition> triggerPositions)
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
        int centerX = CreateLayout().CenterX;
        int centerZ = dirtStartZ + (dirtLength / 2);
        int clockStartZ = centerZ - 4;
        int clockEndZ = clockStartZ + 7;
        int startBusZ = wallStartZ - 1;
        int startTowerZ = wallStartZ - 2;
        int endBusZ = wallEndZ + 1;
        int endTowerZ = wallEndZ + 2;

        if (CrossConnectTriggerWiring &&
            startTriggerXs.Count > 1 &&
            endTriggerXs.Count > 1 &&
            clockStartZ - 1 > startBusZ &&
            endBusZ > clockEndZ + 1)
        {
            PlaceCrossConnectedTriggerWiring(
                builder,
                centerX,
                triggerY,
                clockStartZ,
                clockEndZ,
                startBusZ,
                startTowerZ,
                endBusZ,
                endTowerZ,
                startTriggerXs,
                endTriggerXs);
            return;
        }

        // Direct mode: north clock line triggers all start observers, south clock line triggers all end observers.
        PlaceTriggerBus(builder, centerX, triggerY, clockStartZ - 1, Direction.North, 0, startBusZ, startTowerZ, Direction.South, startTriggerXs);
        PlaceTriggerBus(builder, centerX, triggerY, clockEndZ + 1, Direction.South, 0, endBusZ, endTowerZ, Direction.North, endTriggerXs);
    }

    private static void PlaceCrossConnectedTriggerWiring(
        SchematicBuilder builder,
        int centerX,
        int busY,
        int clockStartZ,
        int clockEndZ,
        int startBusZ,
        int startTowerZ,
        int endBusZ,
        int endTowerZ,
        List<int> startTriggerXs,
        List<int> endTriggerXs)
    {
        // Channel 1 (Beginning torch -> CopperBulb 1 -> Comparator 1) toggles on cycles 1, 3, 5...
        // Channel 2 (End torch -> CopperBulb 2 -> Comparator 2) toggles on cycles 2, 4, 6...
        // Cross-connecting each machine's start and end triggers onto separate channels alternates machines.
        int m1TrackX = centerX - 3;
        int m1JunctionZ = clockStartZ - 1;
        CircuitLayout.SetCopperBulb(builder, new BlockPosition(centerX, 1, m1JunctionZ), lit: false, powered: false);
        builder.SetBlock(centerX - 1, 0, m1JunctionZ, Minecraft.Blocks.SmoothStone);
        CircuitLayout.SetComparator(builder, new BlockPosition(centerX - 1, 1, m1JunctionZ), Direction.West);
        builder.SetBlock(centerX - 2, 0, m1JunctionZ, Minecraft.Blocks.SmoothStone);
        builder.SetBlock(centerX - 2, 1, m1JunctionZ, CircuitLayout.CreateWire(Direction.East, north: false, east: true, south: false, west: true, power: 0));
        builder.SetBlock(m1TrackX, 0, m1JunctionZ, Minecraft.Blocks.SmoothStone);
        builder.SetBlock(m1TrackX, 1, m1JunctionZ, CircuitLayout.CreateWire(Direction.North, north: true, east: true, south: true, west: false, power: 0));

        int m2TrackX = centerX + 2;
        int m2JunctionZ = clockEndZ + 1;
        CircuitLayout.SetCopperBulb(builder, new BlockPosition(centerX, 1, m2JunctionZ), lit: false, powered: true);
        builder.SetBlock(centerX + 1, 0, m2JunctionZ, Minecraft.Blocks.SmoothStone);
        CircuitLayout.SetComparator(builder, new BlockPosition(centerX + 1, 1, m2JunctionZ), Direction.East);
        builder.SetBlock(m2TrackX, 0, m2JunctionZ, Minecraft.Blocks.SmoothStone);
        builder.SetBlock(m2TrackX, 1, m2JunctionZ, CircuitLayout.CreateWire(Direction.North, north: true, east: false, south: true, west: true, power: 0));

        var leftStartXs = startTriggerXs.Where((_, index) => index % 2 == 0).ToList();
        var leftEndXs = endTriggerXs.Where((_, index) => index % 2 == 0).ToList();
        var rightStartXs = startTriggerXs.Where((_, index) => index % 2 != 0).ToList();
        var rightEndXs = endTriggerXs.Where((_, index) => index % 2 != 0).ToList();

        PlaceTriggerBus(builder, m1TrackX, busY, m1JunctionZ - 1, Direction.North, clockDustOffset: 2, startBusZ, startTowerZ, Direction.South, leftStartXs);
        PlaceTriggerBus(builder, m1TrackX, busY, m1JunctionZ + 1, Direction.South, clockDustOffset: 2, endBusZ, endTowerZ, Direction.North, leftEndXs);
        PlaceTriggerBus(builder, m2TrackX, busY, m2JunctionZ + 1, Direction.South, clockDustOffset: 1, endBusZ, endTowerZ, Direction.North, rightEndXs);
        PlaceTriggerBus(builder, m2TrackX, busY, m2JunctionZ - 1, Direction.North, clockDustOffset: 1, startBusZ, startTowerZ, Direction.South, rightStartXs);
    }

    private static void PlaceTriggerBus(
        SchematicBuilder builder,
        int liftX,
        int busY,
        int clockWireStartZ,
        Direction clockWireDirection,
        int clockDustOffset,
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
                liftX,
                1,
                clockWireStartZ,
                clockWireDirection,
                wireLength,
                dustOffset: clockDustOffset,
                supportBlock: Minecraft.Blocks.SmoothStone);
        }

        // Vertical torch tower lifting signal from y = 1 up to busY.
        builder.SetBlock(liftX, 1, towerZ, Minecraft.Blocks.SmoothStone);
        builder.SetBlock(liftX, 2, towerZ, Minecraft.Blocks.RedstoneTorch.WithLit(true));
        builder.SetBlock(liftX, 3, towerZ, Minecraft.Blocks.SmoothStone);
        builder.SetBlock(liftX, 4, towerZ, Minecraft.Blocks.RedstoneTorch.WithLit(false));
        builder.SetBlock(liftX, 5, towerZ, Minecraft.Blocks.SmoothStone);
        builder.SetBlock(
            liftX,
            busY,
            towerZ,
            CircuitLayout.CreateWire(
                Direction.North,
                north: towerToBusDirection == Direction.North,
                east: false,
                south: towerToBusDirection == Direction.South,
                west: false));

        int minX = Math.Min(liftX, triggerXs.Min());
        int maxX = Math.Max(liftX, triggerXs.Max());

        builder.SetBlock(liftX, busY - 1, busZ, Minecraft.Blocks.SmoothStone);
        builder.SetBlock(
            liftX,
            busY,
            busZ,
            CircuitLayout.CreateWire(
                Direction.North,
                north: towerToBusDirection == Direction.South,
                east: maxX > liftX,
                south: towerToBusDirection == Direction.North,
                west: minX < liftX));

        if (minX < liftX)
        {
            builder.SetWireLine(
                liftX - 1,
                busY,
                busZ,
                Direction.West,
                length: liftX - minX,
                dustOffset: 1,
                supportBlock: Minecraft.Blocks.SmoothStone);
        }

        if (maxX > liftX)
        {
            builder.SetWireLine(
                liftX + 1,
                busY,
                busZ,
                Direction.East,
                length: maxX - liftX,
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

        // Place the note block timer behind the end wall (rotated perpendicular to the wall or flush along X).
        int clockStartZ = layout.WallEndZ + 2;
        if (RotateNoteBlockClock)
        {
            new HopperClockSchematicCreator
            {
                Period = TimeSpan.FromSeconds(13),
                Facing = Direction.North,
                TorchPlacement = HopperClockTorchPlacement.End
            }.Place(builder, centerX, noteBlockY - 1, clockStartZ);
        }
        else
        {
            new HopperClockSchematicCreator
            {
                Period = TimeSpan.FromSeconds(13),
                Facing = Direction.East,
                TorchPlacement = HopperClockTorchPlacement.End
            }.Place(builder, centerX - FlushNoteBlockClockWidth, noteBlockY - 1, clockStartZ);

            builder.SetBlock(centerX, noteBlockY - 1, clockStartZ, Minecraft.Blocks.SmoothStone);
            builder.SetBlock(
                centerX,
                noteBlockY,
                clockStartZ,
                CircuitLayout.CreateWire(Direction.North, north: true, east: false, south: false, west: true));
        }

        // Repeater at wallEndZ + 1 strongly powers the solid end wall block at wallEndZ so no gap is cut in the wall.
        builder.SetBlock(centerX, noteBlockY - 1, layout.WallEndZ + 1, Minecraft.Blocks.SmoothStone);
        CircuitLayout.SetRepeater(builder, new BlockPosition(centerX, noteBlockY, layout.WallEndZ + 1), Direction.North);
        if (string.Equals(WallBlock, Minecraft.Blocks.Glass, StringComparison.OrdinalIgnoreCase))
        {
            builder.SetBlock(centerX, noteBlockY, layout.WallEndZ, Minecraft.Blocks.SmoothStone);
        }

        builder.SetWireLine(
            centerX,
            noteBlockY,
            layout.WallEndZ - 1,
            Direction.North,
            length: layout.WallEndZ - centerZ - 1,
            supportBlock: Minecraft.Blocks.SmoothStone);

        // Complete the 3x3 collection ring behind the note block so items dropped over the wire tip are not lost on stone.
        builder.SetBlock(centerX, hopperY, centerZ + 1, Minecraft.Blocks.Hopper.FacingWest());
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
        if (RoofBlock != null)
        {
            int roofY = layout.RoofY;
            builder.Fill(layout.DirtStartX, roofY, layout.DirtStartZ, layout.DirtEndX, roofY, layout.DirtEndZ, RoofBlock);
        }

        if (LightBlock == null)
        {
            return;
        }

        // Bamboo requires block/sky light >= 9 above its shoot (y = SoilLevel + 2) and base (y = SoilLevel + 1).
        // Placing LightBlock (emission 15) at HopperY (SoilLevel + 5 = 7, dy = 4 to bamboo, 3 to growth space)
        // stays clear of the flying machine and triggers while guaranteeing light >= 9 everywhere on the dirt bed.
        int lightY = layout.HopperY;
        const int LightSpacing = 3;
        List<int> xs = GetLightAxisCoordinates(layout.DirtStartX, layout.DirtEndX, LightSpacing);
        List<int> zs = GetLightAxisCoordinates(layout.DirtStartZ, layout.DirtEndZ, LightSpacing);

        foreach (int x in xs)
        {
            foreach (int z in zs)
            {
                PlaceLightWithCollectionClearance(builder, layout, x, lightY, z);
            }
        }

        // Ensure extra lights flank the 3x3 central collection ring so (CenterX, CenterZ) receives light >= 9 for all sizes.
        TryPlaceLightIfClear(builder, layout, layout.CenterX - 2, lightY, layout.CenterZ);
        TryPlaceLightIfClear(builder, layout, layout.CenterX + 2, lightY, layout.CenterZ);
        TryPlaceLightIfClear(builder, layout, layout.CenterX - 1, lightY, layout.CenterZ - 2);
        TryPlaceLightIfClear(builder, layout, layout.CenterX + 1, lightY, layout.CenterZ - 2);
        TryPlaceLightIfClear(builder, layout, layout.CenterX - 1, lightY, layout.CenterZ + 2);
        TryPlaceLightIfClear(builder, layout, layout.CenterX + 1, lightY, layout.CenterZ + 2);
    }

    private void PlaceLightWithCollectionClearance(
        SchematicBuilder builder,
        FarmLayout layout,
        int x,
        int y,
        int z)
    {
        if (!IsOccupiedAtHopperLevel(x, z, layout))
        {
            builder.SetBlock(x, y, z, LightBlock!);
            return;
        }

        // Bypass central hopper/wire trunk with symmetric flanking lights so blocks beneath still receive light >= 9.
        int offset = Math.Abs(z - layout.CenterZ) <= 1 ? 2 : 1;
        TryPlaceLightIfClear(builder, layout, layout.CenterX - offset, y, z);
        TryPlaceLightIfClear(builder, layout, layout.CenterX + offset, y, z);
    }

    private void TryPlaceLightIfClear(SchematicBuilder builder, FarmLayout layout, int x, int y, int z)
    {
        if (x < layout.DirtStartX || x > layout.DirtEndX || z < layout.DirtStartZ || z > layout.DirtEndZ)
        {
            return;
        }

        if (!IsOccupiedAtHopperLevel(x, z, layout))
        {
            builder.SetBlock(x, y, z, LightBlock!);
        }
    }

    private static bool IsOccupiedAtHopperLevel(int x, int z, FarmLayout layout) =>
        x == layout.CenterX ||
        (Math.Abs(x - layout.CenterX) <= 1 && Math.Abs(z - layout.CenterZ) <= 1);

    private static List<int> GetLightAxisCoordinates(int start, int end, int spacing)
    {
        var coords = new List<int>();
        if (end < start)
        {
            return coords;
        }

        if (end == start)
        {
            coords.Add(start);
            return coords;
        }

        int first = Math.Min(start + 1, end);
        for (int pos = first; pos <= end - 1; pos += spacing)
        {
            coords.Add(pos);
        }

        if (coords.Count == 0 || coords[^1] < end - 1)
        {
            coords.Add(end - 1);
        }

        return coords;
    }

    private readonly record struct FarmLayout(
        int DirtWidth,
        int DirtLength,
        int WallHeight,
        int SidePadding,
        int FrontPadding,
        int BackPadding,
        bool HasRoofLayer)
    {
        public int WallStartX => SidePadding;
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
