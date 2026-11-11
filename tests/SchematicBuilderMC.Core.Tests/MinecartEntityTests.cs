using fNbt;
using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Generators;
using SchematicBuilderMC.Models;
using SchematicBuilderMC.Registries;
using Xunit;

namespace SchematicBuilderMC.Core.Tests;

public class MinecartEntityTests
{
    [Fact]
    public void Entity_IsMinecart_IdentifiesMinecartTypes()
    {
        var standard = new Entity(Minecraft.Entities.Minecart, 0, 0, 0);
        var chest = new Entity(Minecraft.Entities.ChestMinecart, 0, 0, 0);
        var hopper = new Entity(Minecraft.Entities.HopperMinecart, 0, 0, 0);
        var furnace = new Entity(Minecraft.Entities.FurnaceMinecart, 0, 0, 0);
        var tnt = new Entity(Minecraft.Entities.TntMinecart, 0, 0, 0);
        var cow = new Entity(Minecraft.Entities.Cow, 0, 0, 0);

        Assert.True(standard.IsMinecart);
        Assert.True(chest.IsMinecart);
        Assert.True(hopper.IsMinecart);
        Assert.True(furnace.IsMinecart);
        Assert.True(tnt.IsMinecart);
        Assert.False(cow.IsMinecart);
    }

    [Fact]
    public void Entity_PassengerAndMotion_ConfigureCorrectly()
    {
        var pig = new Entity(Minecraft.Entities.Pig).WithCustomName("Speedy");
        var cart = Entity.CreateMinecart(1.5, 0.0625, 2.5, passenger: pig, motionX: 0.4, isFlipped: true);

        Assert.True(cart.IsMinecart);
        Assert.True(cart.HasPassengers);

        var (passengers, mx, my, mz, isFlipped, type) = cart.GetMinecartData();
        Assert.Single(passengers);
        Assert.Equal("minecraft:pig", passengers[0].Id);
        Assert.Equal("Speedy", passengers[0].CustomName);
        Assert.Equal(0.4, mx);
        Assert.Equal(0.0, my);
        Assert.Equal(0.0, mz);
        Assert.True(isFlipped);
        Assert.Equal(Minecraft.Entities.Minecart, type);
    }

    [Fact]
    public void SchematicAndBuilder_AddMinecart_BuildsSuccessfully()
    {
        var passenger = new Entity(Minecraft.Entities.Villager).WithCustomName("Driver");

        var schematic = SchematicBuilder.Create(5, 5, 5)
            .AddMinecart(2.5, 1.0625, 2.5, passenger, motionX: 0.2, isFlipped: false)
            .Build();

        Assert.Single(schematic.Entities);
        var minecart = schematic.Entities[0];
        Assert.True(minecart.IsMinecart);

        var passengers = minecart.GetPassengers();
        Assert.Single(passengers);
        Assert.Equal("minecraft:villager", passengers[0].Id);
        Assert.Equal("Driver", passengers[0].CustomName);
    }

    [Fact]
    public void FluentBuilderCodeGenerator_SimpleMinecart_EmitsAddMinecart()
    {
        var schematic = SchematicBuilder.Create(3, 3, 3)
            .AddMinecart(1.5, 0.0625, 1.5)
            .Build();

        string code = FluentBuilderCodeGenerator.Generate(schematic);

        Assert.Contains(".AddMinecart(1.5, 0.0625, 1.5)", code);
    }

    [Fact]
    public void FluentBuilderCodeGenerator_MinecartWithPassengerAndMotion_EmitsCleanCall()
    {
        var passenger = new Entity(Minecraft.Entities.Zombie)
            .WithCustomName("Rider");

        var schematic = SchematicBuilder.Create(5, 5, 5)
            .AddMinecart(2.5, 0.0625, 1.5, passenger, motionX: 0.35, isFlipped: true)
            .Build();

        string code = FluentBuilderCodeGenerator.Generate(schematic);

        Assert.Contains(".AddMinecart(2.5, 0.0625, 1.5,", code);
        Assert.Contains("new Entity(Minecraft.Entities.Zombie).WithCustomName(\"Rider\")", code);
        Assert.Contains("motionX: 0.35", code);
        Assert.Contains("isFlipped: true", code);
        Assert.DoesNotContain(".AddEntity", code);
    }

    [Fact]
    public void FluentBuilderCodeGenerator_LivingEntityDefaults_AreOmittedFromNbt()
    {
        // Passenger entity loaded with lots of vanilla default tags
        var piglinCompound = new NbtCompound
        {
            new NbtString("id", "minecraft:zombified_piglin"),
            new NbtString("CustomName", "Chucky"),
            new NbtByte("CustomNameVisible", 0),
            new NbtByte("PersistenceRequired", 1),
            new NbtByte("IsBaby", 0),
            new NbtByte("Invulnerable", 0),
            new NbtByte("FallFlying", 0),
            new NbtShort("Air", 300),
            new NbtShort("Fire", 0),
            new NbtShort("DeathTime", 0),
            new NbtShort("HurtTime", 0),
            new NbtInt("PortalCooldown", 0),
            new NbtInt("InWaterTime", -1),
            new NbtFloat("Health", 20f),
            new NbtFloat("AbsorptionAmount", 0f),
            new NbtByte("LeftHanded", 0),
            new NbtByte("OnGround", 0),
            new NbtDouble("fall_distance", 0d),
            new NbtCompound("Brain") { new NbtCompound("memories") },
            new NbtIntArray("last_hurt_by_mob", new int[] { 1, 2, 3, 4 }),
            new NbtIntArray("angry_at", new int[] { 1, 2, 3, 4 }),
            new NbtLong("anger_end_time", 12345L),
            new NbtInt("ticks_since_last_hurt_by_mob", 42),
            new NbtIntArray("UUID", new int[] { 10, 20, 30, 40 }),
            new NbtList("Pos", NbtTagType.Double) { new NbtDouble(10d), new NbtDouble(20d), new NbtDouble(30d) },
            new NbtList("Motion", NbtTagType.Double) { new NbtDouble(0.01d), new NbtDouble(-0.07d), new NbtDouble(0d) },
            new NbtList("attributes", NbtTagType.Compound)
            {
                new NbtCompound
                {
                    new NbtString("id", "minecraft:armor_toughness"),
                    new NbtDouble("base", 0d)
                },
                new NbtCompound
                {
                    new NbtString("id", "minecraft:armor"),
                    new NbtDouble("base", 2d)
                },
                new NbtCompound
                {
                    new NbtString("id", "minecraft:attack_damage"),
                    new NbtDouble("base", 5d)
                },
                new NbtCompound
                {
                    new NbtString("id", "minecraft:movement_speed"),
                    new NbtDouble("base", 0.23d)
                }
            },
            new NbtByte("CanPickUpLoot", 1) // Non-default: should be preserved!
        };

        var passenger = Entity.FromNbt(piglinCompound);

        var schematic = SchematicBuilder.Create(5, 5, 5)
            .AddMinecart(3.25, 0.0625, 1.5, passenger, motionX: 0.38, isFlipped: true)
            .Build();

        string code = FluentBuilderCodeGenerator.Generate(schematic);

        // Meaningful tags should be preserved
        Assert.Contains(".AddMinecart(3.25, 0.0625, 1.5,", code);
        Assert.Contains("WithCustomName(\"Chucky\", visible: false)", code);
        Assert.Contains("new NbtByte(\"CanPickUpLoot\", 1)", code);
        Assert.Contains("motionX: 0.38", code);
        Assert.Contains("isFlipped: true", code);

        // Vanilla defaults & transient tags should be omitted
        Assert.DoesNotContain("Brain", code);
        Assert.DoesNotContain("memories", code);
        Assert.DoesNotContain("IsBaby", code);
        Assert.DoesNotContain("Invulnerable", code);
        Assert.DoesNotContain("FallFlying", code);
        Assert.DoesNotContain("DeathTime", code);
        Assert.DoesNotContain("HurtTime", code);
        Assert.DoesNotContain("PortalCooldown", code);
        Assert.DoesNotContain("InWaterTime", code);
        Assert.DoesNotContain("AbsorptionAmount", code);
        Assert.DoesNotContain("LeftHanded", code);
        Assert.DoesNotContain("last_hurt_by_mob", code);
        Assert.DoesNotContain("angry_at", code);
        Assert.DoesNotContain("anger_end_time", code);
        Assert.DoesNotContain("ticks_since_last_hurt_by_mob", code);
        Assert.DoesNotContain("UUID", code);
        Assert.DoesNotContain("attributes", code);
    }
}
