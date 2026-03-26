using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using WeatherApp.Models;

namespace WeatherApp.Services;

public interface IWeatherForecastService
{
    Task<WeatherForecastResult> GetForecastAsync(string location, CancellationToken cancellationToken = default);
}

public sealed class TomorrowIoForecastService : IWeatherForecastService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient;
    private readonly TomorrowIoOptions _options;

    public TomorrowIoForecastService(HttpClient httpClient, IOptions<TomorrowIoOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<WeatherForecastResult> GetForecastAsync(string location, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException("Tomorrow.io API key is missing. Add TomorrowIo:ApiKey to configuration.");
        }

        var query = new Dictionary<string, string?>
        {
            ["location"] = location,
            ["apikey"] = _options.ApiKey,
            ["units"] = _options.Units
        };

        var requestUri = QueryHelpers.AddQueryString($"{_options.BaseUrl.TrimEnd('/')}/v4/weather/forecast", query);
        using var response = await _httpClient.GetAsync(requestUri, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var details = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new TomorrowIoApiException(response.StatusCode, details);
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var forecast = await JsonSerializer.DeserializeAsync<TomorrowIoForecastResponse>(stream, JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Tomorrow.io returned an empty forecast response.");

        return new WeatherForecastResult
        {
            Location = forecast.Location is null
                ? null
                : new WeatherLocation
                {
                    Name = forecast.Location.Name,
                    Latitude = forecast.Location.Lat,
                    Longitude = forecast.Location.Lon
                },
            Units = BuildUnits(_options.Units),
            HourlyPeriods = MapPeriods(forecast.Timelines?.Hourly),
            DailyPeriods = MapPeriods(forecast.Timelines?.Daily),
            MinutelyPeriods = MapPeriods(forecast.Timelines?.Minutely)
        };
    }

    private static WeatherUnits BuildUnits(string units)
    {
        return units.ToLowerInvariant() switch
        {
            "metric" => new WeatherUnits
            {
                Label = "Metric",
                Temperature = "C",
                WindSpeed = "m/s"
            },
            _ => new WeatherUnits
            {
                Label = "Imperial",
                Temperature = "F",
                WindSpeed = "mph"
            }
        };
    }

    private static IReadOnlyList<WeatherForecastPeriod> MapPeriods(List<TomorrowIoInterval>? intervals)
    {
        if (intervals is null || intervals.Count == 0)
        {
            return [];
        }

        return intervals.Select(interval => new WeatherForecastPeriod
        {
            Time = interval.Time,
            Temperature = interval.Values?.Temperature,
            TemperatureApparent = interval.Values?.TemperatureApparent,
            TemperatureMin = interval.Values?.TemperatureMin,
            TemperatureMax = interval.Values?.TemperatureMax,
            WeatherCode = interval.Values?.WeatherCode,
            WeatherDescription = DescribeWeatherCode(interval.Values?.WeatherCode),
            Humidity = interval.Values?.Humidity,
            WindSpeed = interval.Values?.WindSpeed,
            PrecipitationProbability = interval.Values?.PrecipitationProbability
        }).ToList();
    }

    private static string DescribeWeatherCode(int? weatherCode)
    {
        return weatherCode switch
        {
            0 => "Unknown",
            1000 => "Clear",
            1001 => "Cloudy",
            1100 => "Mostly Clear",
            1101 => "Partly Cloudy",
            1102 => "Mostly Cloudy",
            2000 => "Fog",
            2100 => "Light Fog",
            3000 => "Light Wind",
            3001 => "Windy",
            3002 => "Strong Wind",
            4000 => "Drizzle",
            4001 => "Rain",
            4200 => "Light Rain",
            4201 => "Heavy Rain",
            5000 => "Snow",
            5001 => "Flurries",
            5100 => "Light Snow",
            5101 => "Heavy Snow",
            6000 => "Freezing Drizzle",
            6001 => "Freezing Rain",
            6200 => "Light Freezing Rain",
            6201 => "Heavy Freezing Rain",
            7000 => "Ice Pellets",
            7101 => "Heavy Ice Pellets",
            7102 => "Light Ice Pellets",
            8000 => "Thunderstorm",
            _ => $"Code {weatherCode}"
        };
    }
}

public sealed class TomorrowIoApiException : Exception
{
    public TomorrowIoApiException(HttpStatusCode statusCode, string responseBody)
        : base($"Tomorrow.io request failed with status {(int)statusCode} ({statusCode}).")
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }

    public HttpStatusCode StatusCode { get; }

    public string ResponseBody { get; }
}
