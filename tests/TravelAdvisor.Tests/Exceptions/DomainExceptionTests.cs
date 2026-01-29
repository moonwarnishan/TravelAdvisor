using FluentAssertions;
using TravelAdvisor.Domain.Exceptions;

namespace TravelAdvisor.Tests.Exceptions;

public class DomainExceptionTests
{
    [Fact]
    public void ValidationException_WithMultipleErrors_ShouldStoreAllErrors()
    {
        var errors = new[] { "Error 1", "Error 2", "Error 3" };

        var exception = new ValidationException(errors);

        exception.ValidationErrors.Should().BeEquivalentTo(errors);
        exception.StatusCode.Should().Be(400);
        exception.Message.Should().Be("One or more validation errors occurred.");
    }

    [Fact]
    public void ValidationException_WithSingleError_ShouldStoreError()
    {
        var error = "Single validation error";

        var exception = new ValidationException(error);

        exception.ValidationErrors.Should().ContainSingle().Which.Should().Be(error);
        exception.StatusCode.Should().Be(400);
        exception.Message.Should().Be("Validation error occurred.");
    }

    [Fact]
    public void NotFoundException_WithMessage_ShouldStoreMessage()
    {
        var message = "Resource not found";

        var exception = new NotFoundException(message);

        exception.Message.Should().Be(message);
        exception.StatusCode.Should().Be(404);
    }

    [Fact]
    public void NotFoundException_WithEntityNameAndKey_ShouldFormatMessage()
    {
        var entityName = "District";
        var key = "123";

        var exception = new NotFoundException(entityName, key);

        exception.Message.Should().Be("District with key '123' was not found.");
        exception.StatusCode.Should().Be(404);
    }

    [Fact]
    public void BadRequestException_ShouldStoreMessage()
    {
        var message = "Invalid request";

        var exception = new BadRequestException(message);

        exception.Message.Should().Be(message);
        exception.StatusCode.Should().Be(400);
    }

    [Fact]
    public void ExternalServiceException_ShouldFormatMessage()
    {
        var serviceName = "WeatherAPI";

        var exception = new ExternalServiceException(serviceName);

        exception.Message.Should().Be("External service 'WeatherAPI' is unavailable.");
        exception.StatusCode.Should().Be(503);
    }

    [Fact]
    public void ExternalServiceException_WithInnerException_ShouldStoreInnerException()
    {
        var innerException = new HttpRequestException("Connection refused");
        var serviceName = "WeatherAPI";

        var exception = new ExternalServiceException(serviceName, innerException);

        exception.Message.Should().Be("External service 'WeatherAPI' failed.");
        exception.InnerException.Should().Be(innerException);
        exception.StatusCode.Should().Be(503);
    }

    [Fact]
    public void AllDomainExceptions_ShouldInheritFromDomainException()
    {
        var validationEx = new ValidationException("test");
        var notFoundEx = new NotFoundException("test");
        var badRequestEx = new BadRequestException("test");
        var externalServiceEx = new ExternalServiceException("test");

        validationEx.Should().BeAssignableTo<DomainException>();
        notFoundEx.Should().BeAssignableTo<DomainException>();
        badRequestEx.Should().BeAssignableTo<DomainException>();
        externalServiceEx.Should().BeAssignableTo<DomainException>();
    }

    [Fact]
    public void AllDomainExceptions_ShouldInheritFromException()
    {
        var validationEx = new ValidationException("test");
        var notFoundEx = new NotFoundException("test");
        var badRequestEx = new BadRequestException("test");
        var externalServiceEx = new ExternalServiceException("test");

        validationEx.Should().BeAssignableTo<Exception>();
        notFoundEx.Should().BeAssignableTo<Exception>();
        badRequestEx.Should().BeAssignableTo<Exception>();
        externalServiceEx.Should().BeAssignableTo<Exception>();
    }
}
