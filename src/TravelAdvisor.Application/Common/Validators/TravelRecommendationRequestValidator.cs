namespace TravelAdvisor.Application.Common.Validators;

public sealed class TravelRecommendationRequestValidator : AbstractValidator<TravelRecommendationRequest>
{
    // Bangladesh geographic boundaries
    private const double MinLatitude = 20.5;
    private const double MaxLatitude = 26.7;
    private const double MinLongitude = 88.0;
    private const double MaxLongitude = 92.7;

    public TravelRecommendationRequestValidator()
    {
        RuleFor(x => x.CurrentLatitude)
            .InclusiveBetween(MinLatitude, MaxLatitude)
            .WithMessage($"Latitude must be within Bangladesh bounds ({MinLatitude} to {MaxLatitude})");

        RuleFor(x => x.CurrentLongitude)
            .InclusiveBetween(MinLongitude, MaxLongitude)
            .WithMessage($"Longitude must be within Bangladesh bounds ({MinLongitude} to {MaxLongitude})");

        RuleFor(x => x.DestinationDistrict)
            .NotEmpty()
            .WithMessage("Destination district is required")
            .MaximumLength(100)
            .WithMessage("Destination district name is too long");

        RuleFor(x => x.TravelDate)
            .Must(BeWithinForecastRange)
            .WithMessage($"Travel date must be within the next {Constants.ApiParameters.ForecastDays} days");
    }

    private static bool BeWithinForecastRange(DateOnly date)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var maxDate = today.AddDays(Constants.ApiParameters.ForecastDays);
        return date >= today && date <= maxDate;
    }
}
