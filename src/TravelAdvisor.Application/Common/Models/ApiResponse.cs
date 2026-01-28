namespace TravelAdvisor.Application.Common.Models;

public sealed class ApiResponse<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public string? Message { get; init; }
    public IEnumerable<string>? Errors { get; init; }
    public int StatusCode { get; init; }

    private ApiResponse() { }

    public static ApiResponse<T> SuccessResponse(T data, string? message = null, int statusCode = 200)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Message = message,
            StatusCode = statusCode
        };
    }

    public static ApiResponse<T> FailureResponse(string message, int statusCode = 400)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            StatusCode = statusCode
        };
    }

    public static ApiResponse<T> FailureResponse(IEnumerable<string> errors, int statusCode = 400)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Errors = errors,
            StatusCode = statusCode
        };
    }
}

public sealed class ApiResponse
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public IEnumerable<string>? Errors { get; init; }
    public int StatusCode { get; init; }

    private ApiResponse() { }

    public static ApiResponse SuccessResponse(string? message = null, int statusCode = 200)
    {
        return new ApiResponse
        {
            Success = true,
            Message = message,
            StatusCode = statusCode
        };
    }

    public static ApiResponse FailureResponse(string message, int statusCode = 400)
    {
        return new ApiResponse
        {
            Success = false,
            Message = message,
            StatusCode = statusCode
        };
    }

    public static ApiResponse FailureResponse(IEnumerable<string> errors, int statusCode = 400)
    {
        return new ApiResponse
        {
            Success = false,
            Errors = errors,
            StatusCode = statusCode
        };
    }
}
