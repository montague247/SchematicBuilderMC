using SchematicBuilderMC.Builder;
using SchematicBuilderMC.Enums;
using SchematicBuilderMC.IO;
using SchematicBuilderMC.Models;
using SchematicBuilderMC.Registries;
using SchematicBuilderMC.Schematics.Redstone.Logic;

namespace SchematicBuilderMC.Core.Tests.Schematics.Redstone;

public sealed class LogicCircuitsTests
{
    [Theory]
    [InlineData(LogicGateType.Not, 4, 2, 1)]
    [InlineData(LogicGateType.Or, 3, 2, 3)]
    [InlineData(LogicGateType.Nor, 5, 2, 3)]
    [InlineData(LogicGateType.And, 4, 3, 3)]
    [InlineData(LogicGateType.Nand, 4, 2, 3)]
    [InlineData(LogicGateType.Xor, 10, 3, 3)]
    [InlineData(LogicGateType.Xnor, 8, 3, 3)]
    [InlineData(LogicGateType.Imply, 4, 2, 3)]
    [InlineData(LogicGateType.Nimply, 4, 2, 3)]
    public void LogicGate_AllNineGates_HaveExpectedBoundsAndRoundTrip(
        LogicGateType gateType,
        int expectedWidth,
        int expectedHeight,
        int expectedLength)
    {
        var schematic = new LogicGateSchematicCreator
        {
            GateType = gateType,
            Facing = Direction.East
        }.Build();

        Assert.Equal(expectedWidth, schematic.Width);
        Assert.Equal(expectedHeight, schematic.Height);
        Assert.Equal(expectedLength, schematic.Length);

        string serializedPath = Path.Combine(SchematicCreatorTests.TestOutputDirectory, $"Logic_{gateType}.schem");
        SchematicFile.Write(schematic, serializedPath, SchematicFormat.SpongeV2);
        var readBack = SchematicFile.Read(serializedPath, SchematicFormat.SpongeV2);
        Assert.Equal(expectedWidth, readBack.Width);
        Assert.Equal(expectedHeight, readBack.Height);
        Assert.Equal(expectedLength, readBack.Length);
    }

    [Fact]
    public void DedicatedLogicGateCreatorsAndIoOptions_PlaceLeversAndLamps()
    {
        LogicGateSchematicCreator[] creators =
        [
            new NotGateSchematicCreator(),
            new OrGateSchematicCreator(),
            new NorGateSchematicCreator(),
            new AndGateSchematicCreator(),
            new NandGateSchematicCreator(),
            new XorGateSchematicCreator(),
            new XnorGateSchematicCreator(),
            new ImplyGateSchematicCreator(),
            new NimplyGateSchematicCreator()
        ];

        foreach (var creator in creators)
        {
            var gate = (LogicGateSchematicCreator)creator;
            gate.Facing = Direction.North;
            gate.IncludeInputLevers = true;
            gate.IncludeOutputLamp = true;
            var built = gate.Build();

            int leverCount = 0;
            int lampCount = 0;
            for (int x = 0; x < built.Width; x++)
            {
                for (int y = 0; y < built.Height; y++)
                {
                    for (int z = 0; z < built.Length; z++)
                    {
                        if (built[x, y, z].Identifier == Minecraft.Blocks.Lever) leverCount++;
                        if (built[x, y, z].Identifier == Minecraft.Blocks.RedstoneLamp) lampCount++;
                    }
                }
            }

            Assert.Equal(gate.GateType == LogicGateType.Not ? 1 : 2, leverCount);
            Assert.Equal(1, lampCount);
        }
    }

    [Fact]
    public void LogicExtensions_PlacesGatesIntoSharedBuilder()
    {
        var schematic = SchematicBuilder.Create(32, 6, 32)
            .AddNotGate(0, 0, 0, includeInputLever: true, includeOutputLamp: true)
            .AddOrGate(0, 0, 2)
            .AddAndGate(0, 0, 6)
            .AddXorGate(8, 0, 0, includeInputLevers: true, includeOutputLamp: true)
            .AddNimplyGate(8, 0, 6)
            .Build();

        Assert.Equal(Minecraft.Blocks.Comparator, schematic[10, 1, 6].Identifier);
        Assert.Equal("subtract", schematic[10, 1, 6].Properties["mode"]);
    }

    [Fact]
    public void LogicGate_InvalidArguments_Throw()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new LogicGateSchematicCreator { Facing = Direction.Down }.Build());
        Assert.Throws<ArgumentOutOfRangeException>(() => new LogicGateSchematicCreator { GateType = (LogicGateType)999 }.Build());
    }
}
