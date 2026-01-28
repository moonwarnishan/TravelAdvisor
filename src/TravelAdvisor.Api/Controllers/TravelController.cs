using Asp.Versioning;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using TravelAdvisor.Application.Common.Models;
using TravelAdvisor.Application.DTOs.Districts;
using TravelAdvisor.Application.DTOs.Requests;
using TravelAdvisor.Application.Features.TopDistricts;
using TravelAdvisor.Application.Features.TravelRecommendation;
using ValidationException = TravelAdvisor.Domain.Exceptions.ValidationException;

namespace TravelAdvisor.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class TravelController(
    IMediator mediator,
    IValidator<GetTopDistrictsRequest> topDistrictsValidator,
    IValidator<TravelRecommendationRequest> recommendationValidator) : ControllerBase
{
    [HttpGet("top-districts")]
    [ProducesResponseType(typeof(ApiResponse<TopDistrictsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetTopDistricts([FromQuery] int? count, CancellationToken cancellationToken)
    {
        var request = new GetTopDistrictsRequest { Count = count ?? 10 };

        var validationResult = await topDistrictsValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors.Select(e => e.ErrorMessage));
        }

        var query = new GetTopDistrictsQuery(request.Count);
        var result = await mediator.Send(query, cancellationToken);

        return Ok(ApiResponse<TopDistrictsResponse>.SuccessResponse(result));
    }

    [HttpPost("recommendation")]
    [ProducesResponseType(typeof(ApiResponse<TravelRecommendationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetTravelRecommendation([FromBody] TravelRecommendationRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await recommendationValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors.Select(e => e.ErrorMessage));
        }

        var query = new GetTravelRecommendationQuery(
            request.CurrentLatitude,
            request.CurrentLongitude,
            request.DestinationDistrict,
            request.TravelDate);

        var result = await mediator.Send(query, cancellationToken);

        return Ok(ApiResponse<TravelRecommendationResponse>.SuccessResponse(result));
    }
}
