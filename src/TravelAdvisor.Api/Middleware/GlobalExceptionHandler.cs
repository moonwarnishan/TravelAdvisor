namespace TravelAdvisor.Api.Middleware;

public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(exception, "An unhandled exception occurred: {Message}", exception.Message);

        var (statusCode, response) = exception switch
        {
            ValidationException validationEx => (
                validationEx.StatusCode,
                ApiResponse.FailureResponse(validationEx.ValidationErrors, validationEx.StatusCode)),

            DomainException domainEx => (
                domainEx.StatusCode,
                ApiResponse.FailureResponse(domainEx.Message, domainEx.StatusCode)),

            ArgumentException argEx => (
                400,
                ApiResponse.FailureResponse(argEx.Message, 400)),

            InvalidOperationException invalidEx => (
                400,
                ApiResponse.FailureResponse(invalidEx.Message, 400)),

            _ => (
                500,
                ApiResponse.FailureResponse("An unexpected error occurred.", 500))
        };

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/json";

        await httpContext.Response.WriteAsync(
            JsonSerializer.Serialize(response, JsonOptions),
            cancellationToken);

        return true;
    }
}
