using Backend.Game.Services;
using FirstLight.Server.SDK.Models;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using ServerCommon.CommonServices;


namespace GameLogicService;

public static class LogSetup
{
	public static void SetupLogging(this WebApplicationBuilder builder, EnvironmentVariablesConfigurationService env)
	{
		builder.Host.UseSerilog((context, services, builder) =>
		{
			if (env.Standalone || env.DevelopmentMode)
			{
				builder.MinimumLevel.Debug();
				builder.WriteTo.Console(theme: AnsiConsoleTheme.Code);
			}
			else
			{
				builder.MinimumLevel.Information();
				builder.WriteTo.Console();
			}

			builder.MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
				.Enrich.WithRequestMetadata() 
				.WriteTo.ApplicationInsights(
					services.GetRequiredService<TelemetryConfiguration>(),
					TelemetryConverter.Traces)
				.Enrich.FromLogContext();
		});
	}

	/// <summary>
	/// Sets up basic web service telemetry.
	/// This involvs sending metrics and logs.
	/// </summary>
	public static void SetupMetrics(this WebApplicationBuilder builder, EnvironmentVariablesConfigurationService env)
	{
		var insightsConnection = env.TelemetryConnectionString;
		if (insightsConnection != null)
		{
			builder.Services.AddApplicationInsightsTelemetry(o => o.ConnectionString = insightsConnection);
			builder.Services.AddSingleton<IMetricsService, AppInsightsMetrics>();
		}
		else
		{
			builder.Services.AddSingleton<IMetricsService, NoMetrics>();
		}
	}	
}