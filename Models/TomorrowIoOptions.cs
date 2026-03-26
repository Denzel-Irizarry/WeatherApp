namespace WeatherApp.Models;

public sealed class TomorrowIoOptions
{
    public const string SectionName = "TomorrowIo";

    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.tomorrow.io";
    public string Units { get; set; } = "imperial";
}
