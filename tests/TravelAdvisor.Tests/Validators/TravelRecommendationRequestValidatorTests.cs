using FluentAssertions;
using TravelAdvisor.Application.Common.Validators;
using TravelAdvisor.Application.DTOs.Requests;

namespace TravelAdvisor.Tests.Validators;

public class TravelRecommendationRequestValidatorTests
{
    private readonly TravelRecommendationRequestValidator _validator = new();

    [Fact]
    public void Validate_WithValidRequest_ShouldPass()
    {
        var request = new TravelRecommendationRequest
        {
            CurrentLatitude = 23.8103,
            CurrentLongitude = 90.4125,
            DestinationDistrict = "Cox's Bazar",
            TravelDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1))
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(20.4)]
    [InlineData(26.8)]
    [InlineData(0)]
    [InlineData(-90)]
    [InlineData(90)]
    public void Validate_WithInvalidLatitude_ShouldFail(double latitude)
    {
        var request = new TravelRecommendationRequest
        {
            CurrentLatitude = latitude,
            CurrentLongitude = 90.4125,
            DestinationDistrict = "Dhaka",
            TravelDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1))
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CurrentLatitude");
    }

    [Theory]
    [InlineData(87.9)]
    [InlineData(92.8)]
    [InlineData(0)]
    [InlineData(-180)]
    [InlineData(180)]
    public void Validate_WithInvalidLongitude_ShouldFail(double longitude)
    {
        var request = new TravelRecommendationRequest
        {
            CurrentLatitude = 23.8103,
            CurrentLongitude = longitude,
            DestinationDistrict = "Dhaka",
            TravelDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1))
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CurrentLongitude");
    }

    [Theory]
    [InlineData(20.5, 88.0)]
    [InlineData(26.7, 92.7)]
    [InlineData(23.0, 90.0)]
    public void Validate_WithBoundaryCoordinates_ShouldPass(double lat, double lon)
    {
        var request = new TravelRecommendationRequest
        {
            CurrentLatitude = lat,
            CurrentLongitude = lon,
            DestinationDistrict = "Dhaka",
            TravelDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1))
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WithEmptyDestination_ShouldFail(string? destination)
    {
        var request = new TravelRecommendationRequest
        {
            CurrentLatitude = 23.8103,
            CurrentLongitude = 90.4125,
            DestinationDistrict = destination!,
            TravelDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1))
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DestinationDistrict");
    }

    [Fact]
    public void Validate_WithDestinationTooLong_ShouldFail()
    {
        var request = new TravelRecommendationRequest
        {
            CurrentLatitude = 23.8103,
            CurrentLongitude = 90.4125,
            DestinationDistrict = new string('a', 101),
            TravelDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1))
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DestinationDistrict");
    }

    [Fact]
    public void Validate_WithPastDate_ShouldFail()
    {
        var request = new TravelRecommendationRequest
        {
            CurrentLatitude = 23.8103,
            CurrentLongitude = 90.4125,
            DestinationDistrict = "Dhaka",
            TravelDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1))
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TravelDate");
    }

    [Fact]
    public void Validate_WithDateTooFarInFuture_ShouldFail()
    {
        var request = new TravelRecommendationRequest
        {
            CurrentLatitude = 23.8103,
            CurrentLongitude = 90.4125,
            DestinationDistrict = "Dhaka",
            TravelDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(15))
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TravelDate");
    }
}
