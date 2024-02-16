using Microsoft.AspNetCore.Http;
using Serilog.Core;
using Serilog.Events;


namespace GameLogicService;

/// <summary>
/// Enriches server logs with data from server context
/// </summary>
public class ServerLogEnricher : ILogEventEnricher
{
	private const string RequestIdProperty = "RequestId";
	
	private readonly IHttpContextAccessor _contextAccessor;
	
	public ServerLogEnricher() : this(new HttpContextAccessor())
	{
	}
	
	public ServerLogEnricher(IHttpContextAccessor contextAccessor)
	{
		_contextAccessor = contextAccessor;
	}
	
	public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
	{
		if (_contextAccessor.HttpContext == null)
			return;
		
		var correlationIdProperty = new LogEventProperty(RequestIdProperty, new ScalarValue(_contextAccessor.HttpContext.TraceIdentifier));
		logEvent.AddOrUpdateProperty(correlationIdProperty);
	}
}