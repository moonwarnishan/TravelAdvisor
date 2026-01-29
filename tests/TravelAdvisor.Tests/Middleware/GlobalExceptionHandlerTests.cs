using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using TravelAdvisor.Api.Middleware;
using TravelAdvisor.Domain.Exceptions;

namespace TravelAdvisor.Tests.Middleware;

public class GlobalExceptionHandlerTests
{
    private readonly Mock<ILogger<GlobalExceptionHandler>> _loggerMock;
    private readonly GlobalExceptionHandler _handler;

    public GlobalExceptionHandlerTests()
    {
        _loggerMock = new Mock<ILogger<GlobalExceptionHandler>>();
        _handler = new GlobalExceptionHandler(_loggerMock.Object);
    }

    [Fact]
    public async Task TryHandleAsync_WithValidationException_ShouldReturn400()
    {
        var context = CreateHttpContext();
        var exception = new ValidationException(new[] { "Error 1", "Error 2" });

        var result = await _handler.TryHandleAsync(context, exception, CancellationToken.None);

        result.Should().BeTrue();
        context.Response.StatusCode.Should().Be(400);

        var response = await ReadResponseAsJsonAsync(context);
        response.GetProperty("success").GetBoolean().Should().BeFalse();
        response.GetProperty("statusCode").GetInt32().Should().Be(400);
    }

    [Fact]
    public async Task TryHandleAsync_WithNotFoundException_ShouldReturn404()
    {
        var context = CreateHttpContext();
        var exception = new NotFoundException("District", "InvalidId");

        var result = await _handler.TryHandleAsync(context, exception, CancellationToken.None);

        result.Should().BeTrue();
        context.Response.StatusCode.Should().Be(404);

        var response = await ReadResponseAsJsonAsync(context);
        response.GetProperty("success").GetBoolean().Should().BeFalse();
        response.GetProperty("statusCode").GetInt32().Should().Be(404);
    }

    [Fact]
    public async Task TryHandleAsync_WithBadRequestException_ShouldReturn400()
    {
        var context = CreateHttpContext();
        var exception = new BadRequestException("Invalid request data");

        var result = await _handler.TryHandleAsync(context, exception, CancellationToken.None);

        result.Should().BeTrue();
        context.Response.StatusCode.Should().Be(400);

        var response = await ReadResponseAsJsonAsync(context);
        response.GetProperty("success").GetBoolean().Should().BeFalse();
        response.GetProperty("message").GetString().Should().Be("Invalid request data");
    }

    [Fact]
    public async Task TryHandleAsync_WithArgumentException_ShouldReturn400()
    {
        var context = CreateHttpContext();
        var exception = new ArgumentException("Invalid argument provided");

        var result = await _handler.TryHandleAsync(context, exception, CancellationToken.None);

        result.Should().BeTrue();
        context.Response.StatusCode.Should().Be(400);

        var response = await ReadResponseAsJsonAsync(context);
        response.GetProperty("success").GetBoolean().Should().BeFalse();
        response.GetProperty("message").GetString().Should().Be("Invalid argument provided");
    }

    [Fact]
    public async Task TryHandleAsync_WithInvalidOperationException_ShouldReturn400()
    {
        var context = CreateHttpContext();
        var exception = new InvalidOperationException("Invalid operation");

        var result = await _handler.TryHandleAsync(context, exception, CancellationToken.None);

        result.Should().BeTrue();
        context.Response.StatusCode.Should().Be(400);

        var response = await ReadResponseAsJsonAsync(context);
        response.GetProperty("success").GetBoolean().Should().BeFalse();
        response.GetProperty("message").GetString().Should().Be("Invalid operation");
    }

    [Fact]
    public async Task TryHandleAsync_WithUnhandledException_ShouldReturn500()
    {
        var context = CreateHttpContext();
        var exception = new Exception("Unexpected error");

        var result = await _handler.TryHandleAsync(context, exception, CancellationToken.None);

        result.Should().BeTrue();
        context.Response.StatusCode.Should().Be(500);

        var response = await ReadResponseAsJsonAsync(context);
        response.GetProperty("success").GetBoolean().Should().BeFalse();
        response.GetProperty("statusCode").GetInt32().Should().Be(500);
        response.GetProperty("message").GetString().Should().Be("An unexpected error occurred.");
    }

    [Fact]
    public async Task TryHandleAsync_ShouldSetContentTypeToJson()
    {
        var context = CreateHttpContext();
        var exception = new Exception("Test");

        await _handler.TryHandleAsync(context, exception, CancellationToken.None);

        context.Response.ContentType.Should().Be("application/json");
    }

    [Fact]
    public async Task TryHandleAsync_ShouldAlwaysReturnTrue()
    {
        var context = CreateHttpContext();

        var result1 = await _handler.TryHandleAsync(context, new Exception(), CancellationToken.None);
        result1.Should().BeTrue();

        context = CreateHttpContext();
        var result2 = await _handler.TryHandleAsync(context, new ValidationException("test"), CancellationToken.None);
        result2.Should().BeTrue();

        context = CreateHttpContext();
        var result3 = await _handler.TryHandleAsync(context, new NotFoundException("test"), CancellationToken.None);
        result3.Should().BeTrue();
    }

    private static HttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<JsonElement> ReadResponseAsJsonAsync(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var body = await reader.ReadToEndAsync();
        using var doc = JsonDocument.Parse(body);
        return doc.RootElement.Clone();
    }
}
