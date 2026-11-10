namespace SchematicBuilderMC.Core.Exceptions;

/// <summary>
/// Base exception for schematic errors.
/// </summary>
public class SchematicException : Exception
{
    public SchematicException() { }

    public SchematicException(string message) : base(message) { }

    public SchematicException(string message, Exception innerException) : base(message, innerException) { }
}
