using System;
using Serilog;
using Serilog.Configuration;

namespace GameLogicService;

public static class LoggingExtensions
{
	/// <summary>
	/// Add request id property to the logging
	/// </summary>
	public static LoggerConfiguration WithRequestMetadata(this LoggerEnrichmentConfiguration enrichmentConfiguration)
	{
		if (enrichmentConfiguration is null)
			throw new ArgumentNullException(nameof(enrichmentConfiguration));
		return enrichmentConfiguration.With<ServerLogEnricher>();
	}
}