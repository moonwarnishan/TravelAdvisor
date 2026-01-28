namespace TravelAdvisor.Domain.Exceptions;

public abstract class DomainException : Exception
{
    public abstract int StatusCode { get; }

    protected DomainException(string message) : base(message) { }

    protected DomainException(string message, Exception innerException)
        : base(message, innerException) { }
}
