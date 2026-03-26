using WeatherApp.Components;
using WeatherApp.Models;
using WeatherApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace WeatherApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.Configure<TomorrowIoOptions>(
                builder.Configuration.GetSection(TomorrowIoOptions.SectionName));
            builder.Services.AddHttpClient<TomorrowIoForecastService>();
            builder.Services.AddScoped<IWeatherForecastService, TomorrowIoForecastService>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            else
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
            if (!app.Environment.IsDevelopment())
            {
                app.UseHttpsRedirection();
            }

            app.UseAntiforgery();

            app.MapStaticAssets();
            app.MapGet("/api/weather/forecast", async (
                [FromQuery] string location,
                IWeatherForecastService forecastService,
                ILogger<Program> logger,
                CancellationToken cancellationToken) =>
            {
                if (string.IsNullOrWhiteSpace(location))
                {
                    return Results.BadRequest(new { error = "The 'location' query parameter is required." });
                }

                try
                {
                    var forecast = await forecastService.GetForecastAsync(location.Trim(), cancellationToken);
                    return Results.Ok(forecast);
                }
                catch (TomorrowIoApiException ex)
                {
                    logger.LogWarning(ex, "Tomorrow.io request failed for location {Location}.", location);
                    return Results.BadRequest(new
                    {
                        error = ex.Message
                    });
                }
            })
            .WithName("GetWeatherForecast")
            .WithSummary("Gets a weather forecast for a location.")
            .WithDescription("Returns normalized hourly, daily, and minutely forecast data from the configured weather provider.");
            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            app.Run();
        }
    }
}
