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
    private const int WaterDepth = 2; // Fixed 2-block water column (y = SoilLevel + 1..2) covering kelp and flying-machine sweep layer.

    public KelpFarmWithAllaysSchematicCreator()
    {
        SoilBlock = Minecraft.Blocks.Sand;
        LightBlock = null;
    }

    protected override string CropBlockId => Minecraft.Blocks.Kelp;

    protected override string SchematicName => "Kelp Farm with Allays";

    protected override string SchematicDescription => "A kelp farm schematic with allays";

    protected override string AllayCustomName => "Kelp Collector";

    protected override int GetAllaySpawnY(FarmLayout layout) => WaterTopY + 1;

    protected override void PlaceGrowthMedium(SchematicBuilder builder, FarmLayout layout)
    {
        builder.Fill(
            layout.DirtStartX,
            SoilLevel + 1,
            layout.DirtStartZ,
            layout.DirtEndX,
            WaterTopY,
            layout.DirtEndZ,
            Minecraft.Blocks.Water.WithLevel(0));
    }

    private static int WaterTopY => SoilLevel + WaterDepth;
}
