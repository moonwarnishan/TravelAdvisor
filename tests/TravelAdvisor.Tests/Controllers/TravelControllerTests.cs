using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TravelAdvisor.Api.Controllers;
using TravelAdvisor.Application.Common.Models;
using TravelAdvisor.Application.DTOs.Districts;
using TravelAdvisor.Application.DTOs.Requests;
using TravelAdvisor.Application.Features.TopDistricts;
using TravelAdvisor.Application.Features.TravelRecommendation;
using TravelAdvisor.Domain.Common;
using ValidationException = TravelAdvisor.Domain.Exceptions.ValidationException;

namespace TravelAdvisor.Tests.Controllers;

public class TravelControllerTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<IValidator<GetTopDistrictsRequest>> _topDistrictsValidatorMock;
    private readonly Mock<IValidator<TravelRecommendationRequest>> _recommendationValidatorMock;
    private readonly TravelController _controller;

    public TravelControllerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _topDistrictsValidatorMock = new Mock<IValidator<GetTopDistrictsRequest>>();
        _recommendationValidatorMock = new Mock<IValidator<TravelRecommendationRequest>>();

        _controller = new TravelController(
            _mediatorMock.Object,
            _topDistrictsValidatorMock.Object,
            _recommendationValidatorMock.Object);
    }

    [Fact]
    public async Task GetTopDistricts_WithValidRequest_ShouldReturnOk()
    {
        var response = new TopDistrictsResponse
        {
            Districts = new List<RankedDistrictDto>
            {
                new() { Rank = 1, Name = "Sylhet", Latitude = 24.8949, Longitude = 91.8687 }
            },
            GeneratedAt = DateTime.UtcNow,
            ForecastPeriod = "2026-01-30 to 2026-02-05"
        };

        _topDistrictsValidatorMock
            .Setup(x => x.ValidateAsync(It.IsAny<GetTopDistrictsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<GetTopDistrictsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await _controller.GetTopDistricts(5, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<TopDistrictsResponse>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().Be(response);
    }

    [Fact]
    public async Task GetTopDistricts_WithNullCount_ShouldUseDefaultValue()
    {
        _topDistrictsValidatorMock
            .Setup(x => x.ValidateAsync(It.IsAny<GetTopDistrictsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<GetTopDistrictsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TopDistrictsResponse
            {
                Districts = new List<RankedDistrictDto>(),
                GeneratedAt = DateTime.UtcNow,
                ForecastPeriod = "test"
            });

        await _controller.GetTopDistricts(null, CancellationToken.None);

        _mediatorMock.Verify(
            x => x.Send(It.Is<GetTopDistrictsQuery>(q => q.Count == 10), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetTopDistricts_WithInvalidRequest_ShouldThrowValidationException()
    {
        var validationFailures = new List<ValidationFailure>
        {
            new("Count", "Count must be greater than 0")
        };

        _topDistrictsValidatorMock
            .Setup(x => x.ValidateAsync(It.IsAny<GetTopDistrictsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(validationFailures));

        await Assert.ThrowsAsync<ValidationException>(
            () => _controller.GetTopDistricts(-1, CancellationToken.None));
    }

    [Fact]
    public async Task GetTravelRecommendation_WithValidRequest_ShouldReturnOk()
    {
        var request = new TravelRecommendationRequest
        {
            CurrentLatitude = 23.8103,
            CurrentLongitude = 90.4125,
            DestinationDistrict = "Cox's Bazar",
            TravelDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1))
        };

        var response = new TravelRecommendationResponse
        {
            Recommendation = Constants.Recommendations.Recommended,
            Reason = "Better weather conditions",
            CurrentLocation = new LocationComparisonDto { Name = "Dhaka", Latitude = 23.8103, Longitude = 90.4125 },
            Destination = new LocationComparisonDto { Name = "Cox's Bazar", Latitude = 21.4272, Longitude = 92.0058 }
        };

        _recommendationValidatorMock
            .Setup(x => x.ValidateAsync(It.IsAny<TravelRecommendationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<GetTravelRecommendationQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await _controller.GetTravelRecommendation(request, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<TravelRecommendationResponse>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data!.Recommendation.Should().Be(Constants.Recommendations.Recommended);
    }

    [Fact]
    public async Task GetTravelRecommendation_WithInvalidRequest_ShouldThrowValidationException()
    {
        var request = new TravelRecommendationRequest
        {
            CurrentLatitude = 0,
            CurrentLongitude = 0,
            DestinationDistrict = "",
            TravelDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1))
        };

        var validationFailures = new List<ValidationFailure>
        {
            new("CurrentLatitude", "Latitude must be within Bangladesh bounds"),
            new("DestinationDistrict", "Destination is required")
        };

        _recommendationValidatorMock
            .Setup(x => x.ValidateAsync(It.IsAny<TravelRecommendationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(validationFailures));

        await Assert.ThrowsAsync<ValidationException>(
            () => _controller.GetTravelRecommendation(request, CancellationToken.None));
    }

    [Fact]
    public async Task GetTravelRecommendation_ShouldPassCorrectQueryToMediator()
    {
        var request = new TravelRecommendationRequest
        {
            CurrentLatitude = 23.8103,
            CurrentLongitude = 90.4125,
            DestinationDistrict = "Sylhet",
            TravelDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3))
        };

        _recommendationValidatorMock
            .Setup(x => x.ValidateAsync(It.IsAny<TravelRecommendationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<GetTravelRecommendationQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TravelRecommendationResponse
            {
                Recommendation = Constants.Recommendations.Recommended,
                Reason = "Better weather conditions",
                CurrentLocation = new LocationComparisonDto { Name = "Dhaka", Latitude = 23.8103, Longitude = 90.4125 },
                Destination = new LocationComparisonDto { Name = "Sylhet", Latitude = 24.8949, Longitude = 91.8687 }
            });

        await _controller.GetTravelRecommendation(request, CancellationToken.None);

        _mediatorMock.Verify(
            x => x.Send(
                It.Is<GetTravelRecommendationQuery>(q =>
                    q.CurrentLatitude == request.CurrentLatitude &&
                    q.CurrentLongitude == request.CurrentLongitude &&
                    q.DestinationDistrict == request.DestinationDistrict &&
                    q.TravelDate == request.TravelDate),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
