using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Extensions;
using SchematicBuilderMC.Registries;

namespace SchematicBuilderMC.Schematics.Farms;

/// <summary>
/// Builds a self-contained underwater flying-machine kelp farm collected by Allays above the water surface.
/// <see cref="FlyingMachineAllayFarmSchematicCreator.Width"/> and <see cref="FlyingMachineAllayFarmSchematicCreator.Length"/>
/// specify the exact dimensions of the sand bed.
/// </summary>
public sealed class KelpFarmWithAllaysSchematicCreator : FlyingMachineAllayFarmSchematicCreator
{
    private const int MaxSubmergedHeight = 4; // Keeps water strictly below HopperY (SoilLevel + 5) and NoteBlockY (SoilLevel + 6).

    public KelpFarmWithAllaysSchematicCreator()
    {
        SoilBlock = Minecraft.Blocks.Sand;
        LightBlock = null;
    }

    /// <summary>
    /// Water depth and kelp growth height above the sand bed (clamped to 1..4 blocks for water placement so overhead
    /// hoppers and note block wiring remain dry).
    /// </summary>
    public int MaxKelpHeight { get; set; } = MaxSubmergedHeight;

    protected override int MaxCropHeight => MaxKelpHeight;

    protected override string CropBlockId => Minecraft.Blocks.Kelp;

    protected override string SchematicName => "Kelp Farm with Allays";

    protected override string SchematicDescription => "A kelp farm schematic with allays";

    protected override string AllayCustomName => "Kelp Collector";

    protected override int GetAllaySpawnY(FarmLayout layout) => GetWaterTopY() + 1;

    protected override void PlaceGrowthMedium(SchematicBuilder builder, FarmLayout layout)
    {
        int waterTopY = GetWaterTopY();
        builder.Fill(
            layout.DirtStartX,
            SoilLevel + 1,
            layout.DirtStartZ,
            layout.DirtEndX,
            waterTopY,
            layout.DirtEndZ,
            Minecraft.Blocks.Water.WithLevel(0));
    }

    private int GetWaterTopY() => SoilLevel + Math.Clamp(MaxKelpHeight, 1, MaxSubmergedHeight);
}
