using System.Text.Json.Serialization;

namespace WeatherApp.Models;

public sealed class WeatherForecastResult
{
    public WeatherLocation? Location { get; init; }

    public WeatherUnits Units { get; init; } = new();

    public IReadOnlyList<WeatherForecastPeriod> HourlyPeriods { get; init; } = [];

    public IReadOnlyList<WeatherForecastPeriod> DailyPeriods { get; init; } = [];

    public IReadOnlyList<WeatherForecastPeriod> MinutelyPeriods { get; init; } = [];
}

public sealed class WeatherLocation
{
    public string? Name { get; init; }

    public double Latitude { get; init; }

    public double Longitude { get; init; }
}

public sealed class WeatherUnits
{
    public string Label { get; init; } = "Imperial";

    public string Temperature { get; init; } = "F";

    public string WindSpeed { get; init; } = "mph";
}

public sealed class WeatherForecastPeriod
{
    public DateTimeOffset Time { get; init; }

    public decimal? Temperature { get; init; }

    public decimal? TemperatureApparent { get; init; }

    public decimal? TemperatureMin { get; init; }

    public decimal? TemperatureMax { get; init; }

    public int? WeatherCode { get; init; }

    public string WeatherDescription { get; init; } = "Unknown";

    public decimal? Humidity { get; init; }

    public decimal? WindSpeed { get; init; }

    public decimal? PrecipitationProbability { get; init; }
}

public sealed class TomorrowIoForecastResponse
{
    [JsonPropertyName("timelines")]
    public TomorrowIoTimelines? Timelines { get; set; }

    [JsonPropertyName("location")]
    public TomorrowIoLocation? Location { get; set; }
}

public sealed class TomorrowIoTimelines
{
    [JsonPropertyName("hourly")]
    public List<TomorrowIoInterval>? Hourly { get; set; }

    [JsonPropertyName("daily")]
    public List<TomorrowIoInterval>? Daily { get; set; }

    [JsonPropertyName("minutely")]
    public List<TomorrowIoInterval>? Minutely { get; set; }
}

public sealed class TomorrowIoInterval
{
    [JsonPropertyName("time")]
    public DateTimeOffset Time { get; set; }

    [JsonPropertyName("values")]
    public TomorrowIoValues? Values { get; set; }
}

public sealed class TomorrowIoValues
{
    [JsonPropertyName("temperature")]
    public decimal? Temperature { get; set; }

    [JsonPropertyName("temperatureApparent")]
    public decimal? TemperatureApparent { get; set; }

    [JsonPropertyName("weatherCode")]
    public int? WeatherCode { get; set; }

    [JsonPropertyName("humidity")]
    public decimal? Humidity { get; set; }

    [JsonPropertyName("windSpeed")]
    public decimal? WindSpeed { get; set; }

    [JsonPropertyName("precipitationProbability")]
    public decimal? PrecipitationProbability { get; set; }

    [JsonPropertyName("temperatureMax")]
    public decimal? TemperatureMax { get; set; }

    [JsonPropertyName("temperatureMin")]
    public decimal? TemperatureMin { get; set; }
}

public sealed class TomorrowIoLocation
{
    [JsonPropertyName("lat")]
    public double Lat { get; set; }

    [JsonPropertyName("lon")]
    public double Lon { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }
}
