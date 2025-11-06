namespace API.Exceptions;

public sealed class ParsingException(string message, Exception? inner = null) : Exception(message, inner)
{
    
}