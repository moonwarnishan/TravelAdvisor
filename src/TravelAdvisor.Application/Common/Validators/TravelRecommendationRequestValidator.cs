namespace TravelAdvisor.Application.Common.Validators;

public sealed class TravelRecommendationRequestValidator : AbstractValidator<TravelRecommendationRequest>
{
    public TravelRecommendationRequestValidator()
    {
        RuleFor(x => x.CurrentLatitude)
            .InclusiveBetween(-90, 90)
            .WithMessage("Latitude must be between -90 and 90");

        RuleFor(x => x.CurrentLongitude)
            .InclusiveBetween(-180, 180)
            .WithMessage("Longitude must be between -180 and 180");

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
