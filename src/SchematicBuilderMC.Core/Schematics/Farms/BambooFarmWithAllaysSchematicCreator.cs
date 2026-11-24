using SchematicBuilderMC.Registries;

namespace SchematicBuilderMC.Schematics.Farms;

/// <summary>
/// Builds a self-contained flying-machine bamboo farm collected by Allays.
/// <see cref="FlyingMachineAllayFarmSchematicCreator.Width"/> and <see cref="FlyingMachineAllayFarmSchematicCreator.Length"/>
/// specify the exact dimensions of the dirt bed.
/// </summary>
public sealed class BambooFarmWithAllaysSchematicCreator : FlyingMachineAllayFarmSchematicCreator
{
    public BambooFarmWithAllaysSchematicCreator()
    {
        SoilBlock = Minecraft.Blocks.Dirt;
        LightBlock = Minecraft.Blocks.VerdantFroglight;
    }

    /// <summary>
    /// Minimum clear bamboo growth height inside the farm.
    /// </summary>
    public int MaxBambooHeight { get; set; } = 4;

    protected override int MaxCropHeight => MaxBambooHeight;

    protected override string CropBlockId => Minecraft.Blocks.Bamboo;

    protected override string SchematicName => "Bamboo Farm with Allays";

    protected override string SchematicDescription => "A bamboo farm schematic with allays";

    protected override string AllayCustomName => "Bamboo Collector";
}
