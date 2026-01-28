namespace TravelAdvisor.Domain.Exceptions;

public sealed class NotFoundException : DomainException
{
    public override int StatusCode => 404;

    public NotFoundException(string message) : base(message) { }

    public NotFoundException(string entityName, object key)
        : base($"{entityName} with key '{key}' was not found.") { }
}
