namespace Shared.Exceptions;

public class NotFoundException : CustomException
{
    public NotFoundException(string entityName, object key) 
        : base($"Entity \"{entityName}\" ({key}) was not found.")
    {
    }
}
