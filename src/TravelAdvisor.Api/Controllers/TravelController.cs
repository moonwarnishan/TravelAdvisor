using Swashbuckle.AspNetCore.Annotations;

namespace TravelAdvisor.Api.Controllers;

/// <summary>
/// Travel Advisory API endpoints for Bangladesh districts.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
public class TravelController(
    IMediator mediator,
    IValidator<GetTopDistrictsRequest> topDistrictsValidator,
    IValidator<TravelRecommendationRequest> recommendationValidator) : ControllerBase
{
    /// <summary>
    /// Get top districts for travel based on weather and air quality.
    /// </summary>
    /// <remarks>
    /// Returns a ranked list of Bangladesh districts sorted by optimal travel conditions.
    /// Rankings are based on temperature at 2pm and PM2.5 air quality levels.
    ///
    /// Sample request:
    ///
    ///     GET /api/v1/travel/top-districts?count=5
    ///
    /// </remarks>
    /// <param name="count">Number of top districts to return (1-64). Default is 10.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of top-ranked districts with weather and air quality data</returns>
    [HttpGet("top-districts")]
    [SwaggerOperation(
        Summary = "Get top districts for travel",
        Description = "Returns ranked list of Bangladesh districts based on optimal weather and air quality conditions",
        OperationId = "GetTopDistricts",
        Tags = ["Travel"])]
    [SwaggerResponse(200, "Successfully retrieved top districts", typeof(ApiResponse<TopDistrictsResponse>))]
    [SwaggerResponse(400, "Invalid request parameters", typeof(ApiResponse))]
    [SwaggerResponse(500, "Internal server error", typeof(ApiResponse))]
    [ProducesResponseType(typeof(ApiResponse<TopDistrictsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetTopDistricts(
        [FromQuery, SwaggerParameter("Number of districts to return (1-64)", Required = false)] int? count,
        CancellationToken cancellationToken)
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

    /// <summary>
    /// Get travel recommendation for a specific destination.
    /// </summary>
    /// <remarks>
    /// Compares weather and air quality between your current location and destination district.
    /// Returns a recommendation based on the comparison.
    ///
    /// Sample request:
    ///
    ///     POST /api/v1/travel/recommendation
    ///     {
    ///         "currentLatitude": 23.8103,
    ///         "currentLongitude": 90.4125,
    ///         "destinationDistrict": "Cox's Bazar",
    ///         "travelDate": "2026-02-15"
    ///     }
    ///
    /// </remarks>
    /// <param name="request">Travel recommendation request with current location and destination</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Travel recommendation with weather comparison</returns>
    [HttpPost("recommendation")]
    [SwaggerOperation(
        Summary = "Get travel recommendation",
        Description = "Compares weather and air quality between current location and destination to provide a travel recommendation",
        OperationId = "GetTravelRecommendation",
        Tags = ["Travel"])]
    [SwaggerResponse(200, "Successfully generated travel recommendation", typeof(ApiResponse<TravelRecommendationResponse>))]
    [SwaggerResponse(400, "Invalid request parameters or district not found", typeof(ApiResponse))]
    [SwaggerResponse(500, "Internal server error", typeof(ApiResponse))]
    [ProducesResponseType(typeof(ApiResponse<TravelRecommendationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetTravelRecommendation(
        [FromBody, SwaggerRequestBody("Travel recommendation request", Required = true)] TravelRecommendationRequest request,
        CancellationToken cancellationToken)
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
