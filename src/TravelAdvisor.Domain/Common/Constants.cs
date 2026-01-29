namespace TravelAdvisor.Domain.Common;

public static class Constants
{
    public static class CacheKeys
    {
        public const string AllDistricts = "districts:all";
        public const string TopDistrictsRanking = "ranking:top10";
        public const string WeatherPrefix = "weather";
        public const string AirQualityPrefix = "airquality";
    }

    public static class TimeFormats
    {
        public const string TwoPmSuffix = "T14:00";
        public const string DateFormat = "yyyy-MM-dd";
        public const string DateTimeFormat = "yyyy-MM-dd'T'HH:mm";
    }

    public static class ApiParameters
    {
        public const string HourlyTemperature = "temperature_2m";
        public const string HourlyPm25 = "pm2_5";
        public const int ForecastDays = 7;
        public const string Timezone = "Asia/Dhaka";
    }

    public static class Defaults
    {
        public const int TopDistrictsCount = 10;
        public const int MaxDistrictsCount = 64;
        public const int CoordinatePrecision = 4;
    }

    public static class Recommendations
    {
        public const string Recommended = "Recommended";
        public const string NotRecommended = "Not Recommended";
        public const string AlreadyNearDestination = "You are already at or near your destination. No travel needed!";
        public const string CoolerAndBetterAirTemplate = "Your destination is {0}°C cooler and has significantly better air quality. Enjoy your trip!";
        public const string HotterAndWorseAir = "Your destination is hotter and has worse air quality than your current location. It's better to stay where you are.";
    }
}
