using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WeatherApp.Data;
using WeatherApp.Models;

namespace WeatherApp.Services;

public interface IWeatherForecastService
{
    Task<WeatherForecastResult> GetForecastAsync(string location, CancellationToken cancellationToken = default);
}

public sealed class TomorrowIoForecastService : IWeatherForecastService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly string[] ForecastFields =
    [
        "temperature",
        "temperatureApparent",
        "temperatureMin",
        "temperatureMax",
        "humidity",
        "humidityAvg",
        "humidityMax",
        "windSpeed",
        "windSpeedAvg",
        "windSpeedMax",
        "precipitationProbability",
        "precipitationProbabilityAvg",
        "precipitationProbabilityMax",
        "weatherCode",
        "weatherCodeFullDay",
        "weatherCodeDay",
        "weatherCodeNight",
        "weatherCodeMax",
        "weatherCodeMin"
    ];
    private readonly HttpClient _httpClient;
    private readonly TomorrowIoOptions _options;
    private readonly WeatherContext _dbContext;

    public TomorrowIoForecastService(
        HttpClient httpClient,
        IOptions<TomorrowIoOptions> options,
        WeatherContext dbContext)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _dbContext = dbContext;
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
            ["units"] = _options.Units,
            ["timesteps"] = "1h,1d",
            ["fields"] = string.Join(",", ForecastFields)
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

        var result = new WeatherForecastResult
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

        await SaveForecastAsync(location, result, cancellationToken);
        return result;
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

        return intervals.Select(interval =>
        {
            var weatherCode = GetBestWeatherCode(interval.Values);

            return new WeatherForecastPeriod
            {
                Time = interval.Time,
                Temperature = interval.Values?.Temperature,
                TemperatureApparent = interval.Values?.TemperatureApparent,
                TemperatureMin = interval.Values?.TemperatureMin,
                TemperatureMax = interval.Values?.TemperatureMax,
                WeatherCode = weatherCode,
                WeatherDescription = DescribeWeatherCode(weatherCode),
                Humidity = GetBestHumidity(interval.Values),
                WindSpeed = GetBestWindSpeed(interval.Values),
                PrecipitationProbability = GetBestPrecipitationProbability(interval.Values)
            };
        }).ToList();
    }

    private static int? GetBestWeatherCode(TomorrowIoValues? values)
    {
        return values?.WeatherCode
            ?? values?.WeatherCodeFullDay
            ?? values?.WeatherCodeDay
            ?? values?.WeatherCodeNight
            ?? values?.WeatherCodeMax
            ?? values?.WeatherCodeMin;
    }

    private static decimal? GetBestHumidity(TomorrowIoValues? values)
    {
        return values?.Humidity
            ?? values?.HumidityAvg
            ?? values?.HumidityMax;
    }

    private static decimal? GetBestWindSpeed(TomorrowIoValues? values)
    {
        return values?.WindSpeed
            ?? values?.WindSpeedAvg
            ?? values?.WindSpeedMax;
    }

    private static decimal? GetBestPrecipitationProbability(TomorrowIoValues? values)
    {
        return values?.PrecipitationProbability
            ?? values?.PrecipitationProbabilityAvg
            ?? values?.PrecipitationProbabilityMax;
    }

    private static string DescribeWeatherCode(int? weatherCode)
    {
        if (weatherCode is null)
        {
            return "Unknown";
        }

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

    private async Task SaveForecastAsync(
        string queriedLocation,
        WeatherForecastResult forecast,
        CancellationToken cancellationToken)
    {
        if (forecast.Location is null)
        {
            return;
        }

        var savedLocation = await _dbContext.SavedLocations.FirstOrDefaultAsync(
            location => location.Name == forecast.Location.Name
                && location.Latitude == forecast.Location.Latitude
                && location.Longitude == forecast.Location.Longitude,
            cancellationToken);

        if (savedLocation is null)
        {
            savedLocation = new SavedLocation
            {
                Name = forecast.Location.Name,
                Latitude = forecast.Location.Latitude,
                Longitude = forecast.Location.Longitude
            };

            _dbContext.SavedLocations.Add(savedLocation);
        }

        var snapshot = new ForecastSnapshot
        {
            SavedLocation = savedLocation,
            QueriedLocation = queriedLocation,
            FetchedAtUtc = DateTimeOffset.UtcNow,
            UnitsLabel = forecast.Units.Label,
            TemperatureUnit = forecast.Units.Temperature,
            WindSpeedUnit = forecast.Units.WindSpeed,
            Periods = BuildForecastPeriods("hourly", forecast.HourlyPeriods)
        };

        snapshot.Periods.AddRange(BuildForecastPeriods("daily", forecast.DailyPeriods));
        snapshot.Periods.AddRange(BuildForecastPeriods("minutely", forecast.MinutelyPeriods));

        _dbContext.ForecastSnapshots.Add(snapshot);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static List<ForecastPeriod> BuildForecastPeriods(
        string periodType,
        IReadOnlyList<WeatherForecastPeriod> periods)
    {
        return periods.Select(period => new ForecastPeriod
        {
            PeriodType = periodType,
            Time = period.Time,
            Temperature = period.Temperature,
            TemperatureApparent = period.TemperatureApparent,
            TemperatureMin = period.TemperatureMin,
            TemperatureMax = period.TemperatureMax,
            WeatherCode = period.WeatherCode,
            WeatherDescription = period.WeatherDescription,
            Humidity = period.Humidity,
            WindSpeed = period.WindSpeed,
            PrecipitationProbability = period.PrecipitationProbability
        }).ToList();
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
