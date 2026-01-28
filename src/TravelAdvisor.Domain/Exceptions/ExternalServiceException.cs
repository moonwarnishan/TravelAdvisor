namespace TravelAdvisor.Domain.Exceptions;

public sealed class ExternalServiceException : DomainException
{
    public override int StatusCode => 503;

    public ExternalServiceException(string serviceName)
        : base($"External service '{serviceName}' is unavailable.") { }

    public ExternalServiceException(string serviceName, Exception innerException)
        : base($"External service '{serviceName}' failed.", innerException) { }
}
