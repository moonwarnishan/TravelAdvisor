namespace TravelAdvisor.Application.Common.Mapping;

public sealed class ApplicationMappingProfile : Profile
{
    public ApplicationMappingProfile()
    {
        CreateMap<District, RankedDistrictDto>()
            .ForMember(dest => dest.Rank, opt => opt.Ignore())
            .ForMember(dest => dest.AverageTemperatureAt2pm, opt => opt.Ignore())
            .ForMember(dest => dest.AveragePm25, opt => opt.Ignore());

        CreateMap<District, LocationComparisonDto>()
            .ForMember(dest => dest.TemperatureAt2pm, opt => opt.Ignore())
            .ForMember(dest => dest.Pm25Level, opt => opt.Ignore());
    }
}
