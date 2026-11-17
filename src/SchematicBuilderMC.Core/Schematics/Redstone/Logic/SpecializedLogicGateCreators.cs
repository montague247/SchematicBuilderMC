namespace SchematicBuilderMC.Schematics.Redstone.Logic;

/// <summary>Builds a configurable Redstone NOT gate (inverter).</summary>
public sealed class NotGateSchematicCreator : LogicGateSchematicCreator
{
    public override LogicGateType GateType
    {
        get => LogicGateType.Not;
        set { }
    }
}

/// <summary>Builds a configurable Redstone OR gate.</summary>
public sealed class OrGateSchematicCreator : LogicGateSchematicCreator
{
    public override LogicGateType GateType
    {
        get => LogicGateType.Or;
        set { }
    }
}

/// <summary>Builds a configurable Redstone NOR gate.</summary>
public sealed class NorGateSchematicCreator : LogicGateSchematicCreator
{
    public override LogicGateType GateType
    {
        get => LogicGateType.Nor;
        set { }
    }
}

/// <summary>Builds a configurable Redstone AND gate.</summary>
public sealed class AndGateSchematicCreator : LogicGateSchematicCreator
{
    public override LogicGateType GateType
    {
        get => LogicGateType.And;
        set { }
    }
}

/// <summary>Builds a configurable Redstone NAND gate.</summary>
public sealed class NandGateSchematicCreator : LogicGateSchematicCreator
{
    public override LogicGateType GateType
    {
        get => LogicGateType.Nand;
        set { }
    }
}

/// <summary>Builds a configurable Redstone XOR gate.</summary>
public sealed class XorGateSchematicCreator : LogicGateSchematicCreator
{
    public override LogicGateType GateType
    {
        get => LogicGateType.Xor;
        set { }
    }
}

/// <summary>Builds a configurable Redstone XNOR gate.</summary>
public sealed class XnorGateSchematicCreator : LogicGateSchematicCreator
{
    public override LogicGateType GateType
    {
        get => LogicGateType.Xnor;
        set { }
    }
}

/// <summary>Builds a configurable Redstone IMPLY gate.</summary>
public sealed class ImplyGateSchematicCreator : LogicGateSchematicCreator
{
    public override LogicGateType GateType
    {
        get => LogicGateType.Imply;
        set { }
    }
}

/// <summary>Builds a configurable Redstone NIMPLY gate.</summary>
public sealed class NimplyGateSchematicCreator : LogicGateSchematicCreator
{
    public override LogicGateType GateType
    {
        get => LogicGateType.Nimply;
        set { }
    }
}
