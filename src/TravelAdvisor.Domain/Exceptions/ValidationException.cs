namespace TravelAdvisor.Domain.Exceptions;

public sealed class ValidationException : DomainException
{
    public override int StatusCode => 400;
    public IEnumerable<string> ValidationErrors { get; }

    public ValidationException(IEnumerable<string> errors)
        : base("One or more validation errors occurred.")
    {
        ValidationErrors = errors;
    }

    public ValidationException(string error)
        : base("Validation error occurred.")
    {
        ValidationErrors = new[] { error };
    }
}
