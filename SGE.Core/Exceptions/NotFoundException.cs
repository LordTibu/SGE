namespace SGE.Core.Exceptions;

/// <summary>
/// Exception thrown when a requested resource is not found.
/// </summary>
public class NotFoundException : SgeException
{
    public NotFoundException(string message) 
        : base(message, "NOT_FOUND", 404)
    {
    }
}