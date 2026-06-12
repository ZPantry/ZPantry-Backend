namespace Shared.Exceptions;

public class CustomException : Exception
{
    public List<string>? Errors { get; }

    public CustomException(string message, List<string>? errors = null) : base(message)
    {
        Errors = errors;
    }
}
