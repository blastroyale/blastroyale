using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace Scripts.Base;

public class HubService
{
	private readonly HttpClient _httpClient;
	
	private readonly string _apiKey;
	private readonly Uri _baseEndpoint;

	public HubService()
	{
		 _apiKey = ConfigRegistry.Get("ServerSecretKey");
		_baseEndpoint = new Uri(ConfigRegistry.Get("ServerBaseEndpoint"));
		
		_httpClient = new HttpClient
		{
			Timeout = TimeSpan.FromSeconds(30),
			BaseAddress = _baseEndpoint
		};
	}

	private Uri BuildUri(string path, Dictionary<string, string> queryParams = null)
	{
		var builder = new UriBuilder(_httpClient.BaseAddress)
		{
			Path = path
		};

		var query = HttpUtility.ParseQueryString(string.Empty);
		if (queryParams != null)
		{
			foreach (var param in queryParams)
			{
				query[param.Key] = param.Value;
			}
		}
		query["key"] = _apiKey;  
		builder.Query = query.ToString();
		return builder.Uri;
	}
	

	public async Task<string> FetchWalletAddressFromPlayerIdAsync(string playerId)
	{
		var queryParams = new Dictionary<string, string>
		{
			{"playfabId", playerId}
		};
		var uri = BuildUri("admin/wallet", queryParams);

		var response = await _httpClient.GetAsync(uri);
		response.EnsureSuccessStatusCode();

		var walletAddress = await response.Content.ReadAsStringAsync();
		walletAddress = walletAddress.Replace("\"", "");
		
		return walletAddress;
	}
	
	
}