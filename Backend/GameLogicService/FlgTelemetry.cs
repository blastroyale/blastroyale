using Backend.Game.Services;
using FirstLight.Server.SDK.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using ServerCommon.CommonServices;

namespace GameLogicService;

public static class FlgTelemetry
{
	/// <summary>
	/// Sets up basic web service telemetry.
	/// This involvs sending metrics and logs.
	/// </summary>
	public static void UseFlgTelemetry(this WebApplicationBuilder builder, EnvironmentVariablesConfigurationService env)
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
		
		builder.Host.UseSerilog((ctx, builder) =>
		{
			if (env.Standalone)
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
				.Enrich.FromLogContext();
		});

	}	
}