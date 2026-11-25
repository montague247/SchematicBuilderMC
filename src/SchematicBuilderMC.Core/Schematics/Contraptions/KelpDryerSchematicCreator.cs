using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;
using SchematicBuilderMC.Models;
using SchematicBuilderMC.Registries;

namespace SchematicBuilderMC.Schematics.Contraptions;

/// <summary>
/// Generates a 32-smoker automatic kelp dryer with dual dropper-line distribution,
/// double chests for kelp input, dried kelp block fuel, and dried kelp output,
/// a fast 4-comparator fader clock, and optional decorative hopper caps and indicator lamp.
/// </summary>
public sealed class KelpDryerSchematicCreator : SchematicCreator
{
    /// <summary>
    /// Horizontal direction the front input, fuel, and output double chests face
    /// (<see cref="Direction.North"/>, <see cref="Direction.South"/>, <see cref="Direction.East"/>, or <see cref="Direction.West"/>).
    /// Defaults to <see cref="Direction.South"/>.
    /// </summary>
    public Direction Facing { get; set; } = Direction.South;

    /// <summary>
    /// When <c>true</c> (default), includes decorative <see cref="Minecraft.Blocks.Composter"/> hopper caps
    /// and the <see cref="Minecraft.Blocks.RedstoneLamp"/> indicator.
    /// When <c>false</c>, omits the composters and substitutes redstone wire for the lamp so all observer triggers remain functional.
    /// </summary>
    public bool IncludeDecoration { get; set; } = true;

    /// <summary>
    /// Block identifier used for solid redstone circuit blocks (default <see cref="Minecraft.Blocks.GrayWool"/>).
    /// </summary>
    public string CircuitBlock { get; set; } = Minecraft.Blocks.GrayWool;

    /// <summary>
    /// Transparent block identifier used for redstone wire and diode supports (default <see cref="Minecraft.Blocks.GrayStainedGlass"/>).
    /// </summary>
    public string GlassBlock { get; set; } = Minecraft.Blocks.GrayStainedGlass;

    /// <summary>
    /// Optional initial kelp items placed in the top input double chest (slots 0..53, counts 1..64).
    /// </summary>
    public IReadOnlyList<ItemStack>? InputItems { get; set; }

    /// <summary>
    /// Optional initial fuel items placed in the middle fuel double chest (slots 0..53, counts 1..64).
    /// </summary>
    public IReadOnlyList<ItemStack>? FuelItems { get; set; }

    /// <summary>
    /// Optional initial dried kelp items placed in the bottom output double chest (slots 0..53, counts 1..64).
    /// </summary>
    public IReadOnlyList<ItemStack>? OutputItems { get; set; }

    /// <summary>
    /// Total schematic width along the X axis.
    /// </summary>
    public int Width => CreateConfiguredSupersmelter().Width;

    /// <summary>
    /// Total schematic height along the Y axis.
    /// </summary>
    public int Height => CreateConfiguredSupersmelter().Height;

    /// <summary>
    /// Total schematic length along the Z axis.
    /// </summary>
    public int Length => CreateConfiguredSupersmelter().Length;

    /// <summary>
    /// Places the kelp dryer into an existing <see cref="SchematicBuilder"/> starting at (startX, startY, startZ),
    /// extending its dimensions automatically if needed.
    /// </summary>
    public override SchematicBuilder Place(SchematicBuilder builder, int startX = 0, int startY = 0, int startZ = 0)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return CreateConfiguredSupersmelter().Place(builder, startX, startY, startZ);
    }

    /// <inheritdoc />
    public override SchematicBuilder Create() =>
        CreateConfiguredSupersmelter()
            .Create()
            .WithName("Kelp Dryer")
            .WithDescription("A 32-smoker automatic kelp dryer with dropper distribution and fast comparator fader clock.");

    private SupersmelterSchematicCreator CreateConfiguredSupersmelter() => new()
    {
        MachineType = SmelterType.Smoker,
        Facing = Facing,
        IncludeDecoration = IncludeDecoration,
        CircuitBlock = CircuitBlock,
        GlassBlock = GlassBlock,
        UseFastClock = true,
        InputItems = InputItems,
        FuelItems = FuelItems,
        OutputItems = OutputItems,
        Rotation = Rotation
    };
}
