namespace SchematicBuilderMC.Core.Exceptions;

/// <summary>
/// Exception thrown when schematic data or file format is invalid or unsupported.
/// </summary>
public class SchematicFormatException : SchematicException
{
    public SchematicFormatException() { }

    public SchematicFormatException(string message) : base(message) { }

    public SchematicFormatException(string message, Exception innerException) : base(message, innerException) { }
}
