namespace SGE.Core.Exceptions;

/// <summary>
/// Exception thrown when a date validation fails (e.g., past date not allowed).
/// </summary>
public class InvalidDateException : SgeException
{
    public InvalidDateException(string message) 
        : base(message, "INVALID_DATE", 400)
    {
    }
}