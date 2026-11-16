using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Extensions;
using SchematicBuilderMC.Models;
using SchematicBuilderMC.Registries;

namespace SchematicBuilderMC.Schematics.Farms;

public sealed class BambooFarmWithAllays
{
    public int Width { get; set; } = 35; // best (max): 35, chunk: 16

    public int Length { get; set; } = 32;

    public int MaxBambooHeight { get; set; } = 4;

    public string SoilBlock { get; set; } = Minecraft.Blocks.Dirt;

    public string WallBlock { get; set; } = Minecraft.Blocks.WaxedCopperBlock;

    public string? RoofBlock { get; set; } = Minecraft.Blocks.Glass;

    public string? LightBlock { get; set; } = Minecraft.Blocks.VerdantFroglight;

    public void SetBestWidth()
    {
        // max range of allay to noteblock is 16 blocks, so we need to make sure the width is at least 16 * 2 + 1 + 2 (for walls)
        Width = 16 * 2 + 1 + 2;
    }

    public SchematicBuilder Create()
    {
        Width = Math.Max(Width, 4); // Ensure a minimum width of 4 for the farm (2 for walls and 2 for flying machine)
        Width = Math.Min(Width, 35); // Ensure a maximum width of 35 to ensure allays can reach the noteblocks (16 blocks range, so 16 * 2 + 1 + 2 for walls)
        MaxBambooHeight = Math.Max(MaxBambooHeight, 2); // Ensure a minimum height of 2 for bamboo growth

        const int SoilLevel = 5;

        var wallHeight = Math.Max(MaxBambooHeight, 5);
        var flyingMachine = new FlatFlyingMachine();

        var builder = SchematicBuilder.Create(Width, GetMaxHeight(SoilLevel, wallHeight), Length)
                .WithOffset(-Width, -SoilLevel, 0)
                .WithName("Bamboo Farm with Allays")
                .WithAuthor("Dirk Helbig")
                .WithDescription("A bamboo farm schematic with allays");

        // layer for bamboo to grow on
        builder.Fill(1, SoilLevel, 1, Width - 2, SoilLevel, Length - 2, SoilBlock);

        // surrounding walls
        builder.Fill(0, SoilLevel, 0, Width - 1, SoilLevel + wallHeight, 0, WallBlock); // x1
        builder.Fill(0, SoilLevel, 0, 0, SoilLevel + wallHeight, Length - 1, WallBlock); // z1
        builder.Fill(0, SoilLevel, Length - 1, Width - 1, SoilLevel + wallHeight, Length - 1, WallBlock); // x2
        builder.Fill(Width - 1, SoilLevel, 0, Width - 1, SoilLevel + wallHeight, Length - 1, WallBlock); // z2

        // layer with bamboo
        builder.Fill(1, SoilLevel + 1, 2, Width - 2, SoilLevel + 1, Length - 3, Minecraft.Blocks.Bamboo);

        // create the flying machine(s)
        builder.Create(flyingMachine, 1, SoilLevel + 2, 1, Width - 2, Minecraft.Blocks.Bamboo, Length - 2);

        // create the center collection area
        var centerX = Width / 2;
        var centerZ = Length / 2;

        // set the center block
        builder.SetBlock(centerX, SoilLevel + 3, centerZ, WallBlock);

        // set the noteblock for the allay to collect the bamboo
        builder.SetBlock(centerX, SoilLevel + 4, centerZ, Minecraft.Blocks.NoteBlock);

        // add trapdoors above the noteblock to prevent the allay to drop the bamboo on top of the noteblock
        builder.SetBlock(centerX, SoilLevel + 5, centerZ + 1, Minecraft.Blocks.BambooTrapdoor.FacingSouth().WithHalf("top").WithOpen(true));
        builder.SetBlock(centerX, SoilLevel + 5, centerZ - 1, Minecraft.Blocks.BambooTrapdoor.FacingNorth().WithHalf("top").WithOpen(true));
        builder.SetBlock(centerX + 1, SoilLevel + 5, centerZ, Minecraft.Blocks.BambooTrapdoor.FacingEast().WithHalf("top").WithOpen(true));
        builder.SetBlock(centerX - 1, SoilLevel + 5, centerZ, Minecraft.Blocks.BambooTrapdoor.FacingWest().WithHalf("top").WithOpen(true)); //

        // add allays to the schematic, each with a bamboo in their main hand
        builder.AddEntity(new Entity(Minecraft.Entities.Allay, 1, SoilLevel + 4, 1)
                .WithNoGravity()
                .WithMainHand(new ItemStack(Minecraft.Items.Bamboo)));
        builder.AddEntity(new Entity(Minecraft.Entities.Allay, -1, SoilLevel + 4, -1)
                .WithNoGravity()
                .WithMainHand(new ItemStack(Minecraft.Items.Bamboo)));

        var roofHeight = SoilLevel + wallHeight + 1;

        // set the roof if specified
        if (RoofBlock != null)
        {
            builder.Fill(1, roofHeight, 1, Width - 2, roofHeight, Length - 2, RoofBlock);
        }

        // set the light in a square pattern if specified
        if (LightBlock != null)
        {
            const int LightSpacing = 3; // spacing between lights

            for (int x = 2; x < Width - 1; x += LightSpacing)
            {
                for (int z = 2; z < Length - 1; z += LightSpacing)
                {
                    builder.SetBlock(x, roofHeight, z, LightBlock);
                }
            }
        }

        return builder;
    }

    public Schematic Build()
    {
        return Create().Build();
    }

    private int GetMaxHeight(int soilLevel, int wallHeight)
    {
        var height = soilLevel + wallHeight + 1;

        if (RoofBlock != null)
        {
            height += 1; // add one more block for the roof
        }

        return height;
    }
}
