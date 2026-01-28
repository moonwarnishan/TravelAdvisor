namespace TravelAdvisor.Domain.Exceptions;

public sealed class BadRequestException : DomainException
{
    public override int StatusCode => 400;

    public BadRequestException(string message) : base(message) { }
}
