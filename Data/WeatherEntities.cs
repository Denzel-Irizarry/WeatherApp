namespace WeatherApp.Data;

public sealed class SavedLocation
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public List<ForecastSnapshot> Snapshots { get; set; } = [];
}

public sealed class ForecastSnapshot
{
    public int Id { get; set; }
    public int SavedLocationId { get; set; }
    public SavedLocation? SavedLocation { get; set; }
    public string QueriedLocation { get; set; } = string.Empty;
    public DateTimeOffset FetchedAtUtc { get; set; }
    public string UnitsLabel { get; set; } = string.Empty;
    public string TemperatureUnit { get; set; } = string.Empty;
    public string WindSpeedUnit { get; set; } = string.Empty;
    public List<ForecastPeriod> Periods { get; set; } = [];
}

public sealed class ForecastPeriod
{
    public int Id { get; set; }
    public int ForecastSnapshotId { get; set; }
    public ForecastSnapshot? ForecastSnapshot { get; set; }
    public string PeriodType { get; set; } = string.Empty;
    public DateTimeOffset Time { get; set; }
    public decimal? Temperature { get; set; }
    public decimal? TemperatureApparent { get; set; }
    public decimal? TemperatureMin { get; set; }
    public decimal? TemperatureMax { get; set; }
    public int? WeatherCode { get; set; }
    public string WeatherDescription { get; set; } = "Unknown";
    public decimal? Humidity { get; set; }
    public decimal? WindSpeed { get; set; }
    public decimal? PrecipitationProbability { get; set; }
}
