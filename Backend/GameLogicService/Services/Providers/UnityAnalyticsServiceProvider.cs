using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FirstLight.Server.SDK.Models;
using FirstLight.Server.SDK.Services;
using Microsoft.Extensions.Logging;

namespace GameLogicService.Services.Providers;

public class UnityAnalyticsServiceProvider : IAnalyticsProvider
{
	private const string PROJECT_ID = "***REMOVED***";
	private const string COLLECTION_ANALYTICS_URL =
		"https://collect.analytics.unity3d.com/api/analytics/collect/v1/projects/{0}/environments/{1}";
	
	
	private ILogger _log;
	private HttpClient _httpClient;
	private IBaseServiceConfiguration _configuration;
	
	public UnityAnalyticsServiceProvider(IHttpClientFactory httpClientFactory, IBaseServiceConfiguration configuration, ILogger log)
	{
		_configuration = configuration;
		_log = log;
		
		_httpClient = httpClientFactory.CreateClient(); 
	}
	
	public void EmitEvent(string eventName, AnalyticsData data)
	{
		//NOT IMPLEMENTED
		//Unity Analytics doesn't allow submitting Global Events, a userID is always needed.
	}

	public void EmitUserEvent(string id, string eventName, AnalyticsData data)
	{
		SendAnalytics(CreatePayload(id, eventName, data));
	}

	private Dictionary<string, object> CreatePayload(string id, string eventName, AnalyticsData data)
	{
		return new Dictionary<string, object>
		{
			{"eventName", eventName},
			{"userID", id},
			{"eventUUID", Guid.NewGuid()},
			{"eventParams", data}
		};
	}

	private void SendAnalytics(Dictionary<string, object> data)
	{
		var analyticsUri = string.Format(COLLECTION_ANALYTICS_URL, PROJECT_ID, _configuration.UnityCloudEnvironmentName);
		
		var jsonContent = JsonSerializer.Serialize(data);
		var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

		Task.Run(async () =>
		{
			try
			{
				var analyticsResponse = await _httpClient.PostAsync(analyticsUri, content);
				var analyticsResponseContent = await analyticsResponse.Content.ReadAsStringAsync();
            
				if (analyticsResponse.StatusCode != HttpStatusCode.NoContent)
				{
					_log.LogError($"Unity Analytics failed to register the analytics event data: {analyticsResponse.StatusCode} - {analyticsResponseContent}");
					throw new Exception($"Unity Analytics failed to register the analytics event data: {analyticsResponse.StatusCode}");
				}
			}
			catch (Exception ex)
			{
				_log.LogError($"An error occurred while sending Unity Analytics: {ex.Message}");
				throw;
			}
		});
	}
}