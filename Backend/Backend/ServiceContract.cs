using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Backend;


/// <summary>
/// Interface for our blockchain service.
/// </summary>
public interface IBlockchainService
{
	/// <summary>
	/// Syncs user NFT's to user's inventory
	/// </summary>
	Task<HttpResponseMessage> SyncNfts(string playerId);
}


/// <summary>
/// This object serves as a microservice contract middleman.
/// This is a minimal implementation to be extended later.
/// This will be shared across our services soon.
/// </summary>
public class ServiceContract : IBlockchainService
{
	private readonly string? _baseUrl;
	private readonly string? _blockchainUrl;
	private readonly string? _apiSecret;
	private readonly ILogger _log;
	
	private HttpClient _client;
	
	public ServiceContract(ILogger log)
	{
		_baseUrl = ReadConfiguredUrl("API_URL");
		_blockchainUrl = ReadConfiguredUrl("API_BLOCKCHAIN_SERVICE");
		_apiSecret = ReadConfiguredUrl("API_SECRET");
		_client = new HttpClient();
		_log = log;
	}

	private string ReadConfiguredUrl(string path)
	{
		var url = Environment.GetEnvironmentVariable(path, EnvironmentVariableTarget.Process);
		if (url == null)
			throw new Exception($"{path} Environment Config for API contracts not set.");
		return url;
	}

	public async Task<HttpResponseMessage> SyncNfts(string playerId)
	{
		var url = $"{_baseUrl}/{_blockchainUrl}/sync?key={_apiSecret}&playfabId={playerId}";
		_log.LogDebug($"Nft Sync: {url}");
		return await _client.GetAsync(url);
	}
	
}
