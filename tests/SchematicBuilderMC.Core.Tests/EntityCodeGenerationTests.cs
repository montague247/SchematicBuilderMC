using fNbt;
using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Generators;
using SchematicBuilderMC.Models;
using SchematicBuilderMC.Registries;
using Xunit;

namespace SchematicBuilderMC.Core.Tests;

public class EntityCodeGenerationTests
{
    [Fact]
    public void EnchantmentConstants_MatchExpectedIdentifiers()
    {
        Assert.Equal("minecraft:sharpness", Minecraft.Enchantments.Sharpness);
        Assert.Equal("minecraft:mending", Minecraft.Enchantments.Mending);
        Assert.Equal("minecraft:unbreaking", Minecraft.Enchantments.Unbreaking);
        Assert.Equal("minecraft:looting", Minecraft.Enchantments.Looting);
        Assert.Equal("minecraft:aqua_affinity", Minecraft.Enchantments.AquaAffinity);
        Assert.Equal("minecraft:protection", Minecraft.Enchantments.Protection);

        Assert.True(Minecraft.Enchantments.IsValid("minecraft:sharpness"));
        Assert.True(Minecraft.Enchantments.IsValid("sharpness"));
        Assert.True(Minecraft.Enchantments.IsValid("mending"));
        Assert.False(Minecraft.Enchantments.IsValid("non_existent_enchantment"));
    }

    [Fact]
    public void AddDroppedItem_AddsEntityToSchematic()
    {
        var schematic = SchematicBuilder.Create(4, 4, 4)
            .AddDroppedItem(2.5, 1.0, 1.5, new ItemStack(Minecraft.Items.Snowball, count: 16))
            .Build();

        Assert.Single(schematic.Entities);
        var ent = schematic.Entities[0];
        Assert.True(ent.IsItemEntity);
        Assert.Equal("minecraft:item", ent.Id);
        Assert.Equal(2.5, ent.X);
        Assert.Equal(1.0, ent.Y);
        Assert.Equal(1.5, ent.Z);

        var (item, age, pickupDelay) = ent.GetItemEntityData();
        Assert.NotNull(item);
        Assert.Equal(Minecraft.Items.Snowball, item.Id);
        Assert.Equal(16, item.Count);
        Assert.Equal(0, age);
        Assert.Equal(0, pickupDelay);
    }

    [Fact]
    public void FluentBuilderCodeGenerator_DroppedItem_EmitsCleanCall()
    {
        var itemCompound = new NbtCompound
        {
            new NbtString("id", "minecraft:item"),
            new NbtList("Pos", NbtTagType.Double) { new NbtDouble(2.875), new NbtDouble(1.0), new NbtDouble(1.644) },
            new NbtList("Motion", NbtTagType.Double) { new NbtDouble(0), new NbtDouble(0), new NbtDouble(2.533E-97) },
            new NbtShort("Health", 5),
            new NbtList("Rotation", NbtTagType.Float) { new NbtFloat(239.8f), new NbtFloat(0f) },
            new NbtCompound("Item")
            {
                new NbtInt("count", 14),
                new NbtString("id", "minecraft:snowball")
            },
            new NbtShort("PickupDelay", 0),
            new NbtShort("Age", 1641)
        };

        var ent = Entity.FromNbt(itemCompound);
        var schematic = SchematicBuilder.Create(4, 4, 4)
            .AddEntity(ent)
            .Build();

        string code = FluentBuilderCodeGenerator.Generate(schematic);

        Assert.Contains(".AddDroppedItem(2.875, 1.0, 1.644, new ItemStack(Minecraft.Items.Snowball, count: 14))", code);
        Assert.DoesNotContain("new NbtCompound", code);
        Assert.DoesNotContain("using fNbt;", code);
    }

    [Fact]
    public void FluentBuilderCodeGenerator_EntityWithEquipmentAndEnchantments_EmitsStronglyTypedConstants()
    {
        var spear = new ItemStack(Minecraft.Items.NetheriteSpear)
            .WithEnchantment(Minecraft.Enchantments.Sharpness, 5)
            .WithEnchantment(Minecraft.Enchantments.Mending, 1)
            .WithRepairCost(15);

        var helmet = new ItemStack(Minecraft.Items.GoldenHelmet);
        var leggings = new ItemStack(Minecraft.Items.GoldenLeggings).WithDamage(101);

        var mob = new Entity(Minecraft.Entities.ZombifiedPiglin, 2.5, 1.0, 2.5)
            .WithMainHand(spear)
            .WithHead(helmet)
            .WithLegs(leggings)
            .WithDropChance("mainhand", 2f)
            .WithRotation(270f, -3.75f)
            .WithCustomName("Chucky", visible: false);

        var schematic = SchematicBuilder.Create(5, 5, 5)
            .AddEntity(mob)
            .Build();

        string code = FluentBuilderCodeGenerator.Generate(schematic);

        // Strongly typed constants should be used throughout
        Assert.Contains("Minecraft.Entities.ZombifiedPiglin", code);
        Assert.Contains("Minecraft.Items.NetheriteSpear", code);
        Assert.Contains("Minecraft.Enchantments.Sharpness, 5", code);
        Assert.Contains("Minecraft.Enchantments.Mending", code);
        Assert.Contains("WithRepairCost(15)", code);
        Assert.Contains("Minecraft.Items.GoldenHelmet", code);
        Assert.Contains("Minecraft.Items.GoldenLeggings", code);
        Assert.Contains("WithDamage(101)", code);
        Assert.Contains("WithDropChance(\"mainhand\", 2f)", code);
        Assert.Contains("WithCustomName(\"Chucky\", visible: false)", code);

        // No raw string constants for items or enchantments
        Assert.DoesNotContain("\"minecraft:sharpness\"", code);
        Assert.DoesNotContain("\"minecraft:netherite_spear\"", code);
        Assert.DoesNotContain("\"minecraft:golden_helmet\"", code);
        Assert.DoesNotContain("\"minecraft:golden_leggings\"", code);
    }

    [Fact]
    public void FluentBuilderCodeGenerator_LivingEntityDefaults_OmittedFromCode()
    {
        // Cow with default max health (10f) and resting gravity motion
        var cowCompound = new NbtCompound
        {
            new NbtString("id", "minecraft:cow"),
            new NbtList("Pos", NbtTagType.Double) { new NbtDouble(1.0), new NbtDouble(1.0), new NbtDouble(1.0) },
            new NbtList("Motion", NbtTagType.Double) { new NbtDouble(0.005), new NbtDouble(-0.0784000015258789), new NbtDouble(0) },
            new NbtFloat("Health", 10f), // default max health for cow
            new NbtByte("PersistenceRequired", 0),
            new NbtByte("CanPickUpLoot", 0),
            new NbtInt("Age", 0),
            new NbtByte("AgeLocked", 0),
            new NbtInt("ForcedAge", 0),
            new NbtInt("InLove", 0),
            new NbtShort("HurtTime", 8),
            new NbtString("variant", "minecraft:warm"),
            new NbtList("attributes", NbtTagType.Compound)
            {
                new NbtCompound
                {
                    new NbtString("id", "minecraft:movement_speed"),
                    new NbtDouble("base", 0.2)
                },
                new NbtCompound
                {
                    new NbtString("id", "minecraft:follow_range"),
                    new NbtDouble("base", 16.0),
                    new NbtList("modifiers", NbtTagType.Compound)
                    {
                        new NbtCompound
                        {
                            new NbtString("id", "minecraft:random_spawn_bonus"),
                            new NbtDouble("amount", 0.034),
                            new NbtString("operation", "add_multiplied_base")
                        }
                    }
                }
            }
        };

        var cow = Entity.FromNbt(cowCompound);
        var schematic = SchematicBuilder.Create(5, 5, 5)
            .AddEntity(cow)
            .Build();

        string code = FluentBuilderCodeGenerator.Generate(schematic);

        // Variant should be kept
        Assert.Contains("variant", code);
        Assert.Contains("minecraft:warm", code);

        // Defaults should be stripped
        Assert.DoesNotContain("PersistenceRequired", code);
        Assert.DoesNotContain("CanPickUpLoot", code);
        Assert.DoesNotContain("HurtTime", code);
        Assert.DoesNotContain("random_spawn_bonus", code);
        Assert.DoesNotContain("attributes", code);
        Assert.DoesNotContain("WithHealth", code); // Cow max health is 10f, so default!
    }

    [Fact]
    public void FluentBuilderCodeGenerator_DamagedHealth_IsPreserved()
    {
        // Iron Golem max health is 100f. Damaged to 86f.
        var golem = new Entity(Minecraft.Entities.IronGolem, 0.5, 1.0, 0.5)
            .WithHealth(86f)
            .WithRotation(180f);

        var schematic = SchematicBuilder.Create(3, 3, 3)
            .AddEntity(golem)
            .Build();

        string code = FluentBuilderCodeGenerator.Generate(schematic);

        Assert.Contains(".WithHealth(86f)", code);
        Assert.Contains(".WithRotation(180f)", code);
    }

    [Fact]
    public void Entity_NoGravity_Property_And_FluentMethod()
    {
        var allay = new Entity(Minecraft.Entities.Allay, 0, 0, 0);
        Assert.False(allay.NoGravity);

        allay.WithNoGravity();
        Assert.True(allay.NoGravity);
        Assert.Equal((byte)1, allay.Data["NoGravity"]?.ByteValue);

        allay.WithNoGravity(false);
        Assert.False(allay.NoGravity);
        Assert.Equal((byte)0, allay.Data["NoGravity"]?.ByteValue);
    }

    [Fact]
    public void Entity_EquipmentConvenienceOverloads_WorkWithItemIdentifiers()
    {
        var entity = new Entity(Minecraft.Entities.Zombie, 0, 0, 0)
            .WithMainHand(Minecraft.Items.DiamondSword)
            .WithOffHand(Minecraft.Items.Shield)
            .WithHead(Minecraft.Items.DiamondHelmet)
            .WithChest(Minecraft.Items.DiamondChestplate)
            .WithLegs(Minecraft.Items.DiamondLeggings)
            .WithFeet(Minecraft.Items.DiamondBoots);

        var eq = entity.Data["equipment"] as NbtCompound;
        Assert.NotNull(eq);
        Assert.Equal("minecraft:diamond_sword", eq["mainhand"]?["id"]?.StringValue);
        Assert.Equal("minecraft:shield", eq["offhand"]?["id"]?.StringValue);
        Assert.Equal("minecraft:diamond_helmet", eq["head"]?["id"]?.StringValue);
        Assert.Equal("minecraft:diamond_chestplate", eq["chest"]?["id"]?.StringValue);
        Assert.Equal("minecraft:diamond_leggings", eq["legs"]?["id"]?.StringValue);
        Assert.Equal("minecraft:diamond_boots", eq["feet"]?["id"]?.StringValue);
    }

    [Fact]
    public void FluentBuilderCodeGenerator_AllayDefaults_AreOmitted_And_WithNoGravity_IsGenerated()
    {
        var allayCompound = new NbtCompound
        {
            new NbtString("id", "minecraft:allay"),
            new NbtList("Pos", NbtTagType.Double) { new NbtDouble(0.5709921046658906), new NbtDouble(2.0729235545797877), new NbtDouble(1.871308390241694) },
            new NbtByte("NoGravity", 1),
            new NbtCompound("Brain")
            {
                new NbtCompound("memories")
                {
                    new NbtCompound("minecraft:liked_player")
                    {
                        new NbtIntArray("value", new int[] { -1652937059, 304301920, -1954908319, 973468245 })
                    }
                }
            },
            new NbtCompound("listener")
            {
                new NbtCompound("selector") { new NbtLong("tick", -1L) },
                new NbtInt("event_delay", 0)
            },
            new NbtLong("DuplicationCooldown", 0L),
            new NbtList("Motion", NbtTagType.Double) { new NbtDouble(-0.029462814417454672d), new NbtDouble(0.030660835759548154d), new NbtDouble(0.048707494398240396d) },
            new NbtByte("CanPickUpLoot", 1),
            new NbtList("Inventory", NbtTagType.End),
            new NbtList("Rotation", NbtTagType.Float) { new NbtFloat(2.2070389f), new NbtFloat(0f) },
            new NbtCompound("equipment")
            {
                new NbtCompound("mainhand")
                {
                    new NbtInt("count", 1),
                    new NbtString("id", "minecraft:bamboo")
                }
            }
        };

        var allay = Entity.FromNbt(allayCompound);
        var schematic = SchematicBuilder.Create(3, 3, 3)
            .AddEntity(allay)
            .Build();

        string code = FluentBuilderCodeGenerator.Generate(schematic);

        Assert.Contains("Minecraft.Entities.Allay", code);
        Assert.Contains(".WithNoGravity()", code);
        Assert.Contains(".WithMainHand(new ItemStack(Minecraft.Items.Bamboo))", code);
        Assert.Contains(".WithRotation(2.2070389f)", code);

        // All defaults and transient tags should be stripped completely
        Assert.DoesNotContain("new NbtCompound", code);
        Assert.DoesNotContain("Brain", code);
        Assert.DoesNotContain("liked_player", code);
        Assert.DoesNotContain("listener", code);
        Assert.DoesNotContain("DuplicationCooldown", code);
        Assert.DoesNotContain("CanPickUpLoot", code);
        Assert.DoesNotContain("Inventory", code);
        Assert.DoesNotContain("Motion", code);
    }

    [Fact]
    public void FluentBuilderCodeGenerator_AllayNonDefaults_ArePreserved()
    {
        // If Allay has active DuplicationCooldown and CanPickUpLoot = 0 (non-default for allay), preserve them
        var allayCompound = new NbtCompound
        {
            new NbtString("id", "minecraft:allay"),
            new NbtList("Pos", NbtTagType.Double) { new NbtDouble(1.0), new NbtDouble(1.0), new NbtDouble(1.0) },
            new NbtLong("DuplicationCooldown", 6000L),
            new NbtByte("CanPickUpLoot", 0) // default for allay is 1, so 0 is non-default!
        };

        var allay = Entity.FromNbt(allayCompound);
        var schematic = SchematicBuilder.Create(3, 3, 3)
            .AddEntity(allay)
            .Build();

        string code = FluentBuilderCodeGenerator.Generate(schematic);

        Assert.Contains("DuplicationCooldown", code);
        Assert.Contains("CanPickUpLoot", code);
    }
}
