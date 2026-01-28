namespace TravelAdvisor.Infrastructure.Mapping;

public sealed class InfrastructureMappingProfile : Profile
{
    public InfrastructureMappingProfile()
    {
        CreateMap<DistrictApiModel, District>()
            .ForMember(dest => dest.Latitude, opt => opt.MapFrom(src => ParseCoordinate(src.Lat)))
            .ForMember(dest => dest.Longitude, opt => opt.MapFrom(src => ParseCoordinate(src.Long)));
    }

    private static double ParseCoordinate(string value)
    {
        return double.TryParse(value, out var result) ? result : 0;
    }
}
