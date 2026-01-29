using FluentAssertions;
using TravelAdvisor.Application.Common.Validators;
using TravelAdvisor.Application.DTOs.Requests;
using TravelAdvisor.Domain.Common;

namespace TravelAdvisor.Tests.Validators;

public class GetTopDistrictsRequestValidatorTests
{
    private readonly GetTopDistrictsRequestValidator _validator = new();

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public void Validate_WithValidCount_ShouldPass(int count)
    {
        var request = new GetTopDistrictsRequest { Count = count };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Validate_WithZeroOrNegativeCount_ShouldFail(int count)
    {
        var request = new GetTopDistrictsRequest { Count = count };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Count");
    }

    [Fact]
    public void Validate_WithCountExceedingMax_ShouldFail()
    {
        var request = new GetTopDistrictsRequest { Count = Constants.Defaults.TopDistrictsCount + 1 };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Count");
    }

    [Fact]
    public void Validate_WithMaxCount_ShouldPass()
    {
        var request = new GetTopDistrictsRequest { Count = Constants.Defaults.TopDistrictsCount };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }
}
