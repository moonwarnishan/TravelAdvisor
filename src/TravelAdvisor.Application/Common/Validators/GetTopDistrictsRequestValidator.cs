namespace TravelAdvisor.Application.Common.Validators;

public sealed class GetTopDistrictsRequestValidator : AbstractValidator<GetTopDistrictsRequest>
{
    public GetTopDistrictsRequestValidator()
    {
        RuleFor(x => x.Count)
            .GreaterThan(0)
            .WithMessage("Count must be greater than 0")
            .LessThanOrEqualTo(Constants.Defaults.MaxDistrictsCount)
            .WithMessage($"Count cannot exceed {Constants.Defaults.MaxDistrictsCount}");
    }
}
