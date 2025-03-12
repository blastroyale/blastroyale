using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Numerics;
using System.Threading.Tasks;
using System.Web;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Logic;
using FirstLight.Server.SDK.Models;
using FirstLight.Server.SDK.Modules;
using Microsoft.Extensions.Logging;
using Quantum;


namespace BlastRoyaleNFTPlugin
{
	
	/// <summary>
	/// Class that encapsulates external models and functionality needed to synchronize NFT's
	/// </summary>
	public class BlockchainApi
	{
		private static readonly CollectionFetchResponse _EMPTY_COLLECTION = new()
			{CollectionNFTsOwnedDict = new Dictionary<string, List<RemoteCollectionItem>>()};
		
		internal HttpClient _client;
		internal string _externalUrl;
		internal string _apiKey;

		public void Setup(string url, string apiKey)
		{
			_client = new HttpClient();
			_externalUrl = url;
			_apiKey = apiKey;
		}
		
		public BlockchainApi(string url, string api) 
		{
			Setup(
				url, 
				api);
		}
		
		public BlockchainApi() 
		{
			Setup(
				Environment.GetEnvironmentVariable("API_URL", EnvironmentVariableTarget.Process), 
				Environment.GetEnvironmentVariable("API_KEY", EnvironmentVariableTarget.Process));
		}
		

		public async Task<BigInteger> GetSpentOnShop(string wallet, string contract)
		{
			var url = $"{_externalUrl}/shop/spent?wallet={wallet}&shopContract={contract}&key={_apiKey}";
			Console.WriteLine(url);
			var response = await _client.GetAsync(url);
			var responseString = await response.Content.ReadAsStringAsync();
			if (!response.IsSuccessStatusCode)
			{
				Console.Error.WriteLine($"GetSpentOnShop Error: {response.ReasonPhrase} - {responseString}");
				return 0;
			}
			return BigInteger.Parse(responseString);
		}
		
		public async Task<IReadOnlyCollection<Web3ShopIntent>> GetPurchaseIntents(string wallet, string contract)
		{
			var url = $"{_externalUrl}/shop/purchases?wallet={wallet}&shopContract={contract}&key={_apiKey}";
			Console.WriteLine(url);
			var response = await _client.GetAsync(url);
			var responseString = await response.Content.ReadAsStringAsync();
			if (!response.IsSuccessStatusCode)
			{
				Console.Error.WriteLine($"GetPurchases Error: {response.ReasonPhrase} - {responseString}");
				return Array.Empty<Web3ShopIntent>();
			}
			Console.WriteLine(responseString);
			var purchases = ModelSerializer.Deserialize<List<ShopPurchaseInput>>(responseString);
			return purchases.Select(p => p.Format()).ToList();
		}

		/// <summary>
		/// Requests all indexed NFTs for a given player's wallet from the blockchain API.
		/// This method fetches the NFTs owned by the player for a list of collections from the blockchain indexer and returns the results.
		/// </summary>
		/// <param name="playerId">The PlayFab ID of the player whose NFT collection is being fetched.</param>
		/// <param name="collectionNames">A list of NFT collection names to retrieve for the player.</param>
		/// <returns>
		/// A <see cref="Task{CollectionFetchResponse}"/> representing the asynchronous operation. 
		/// The result contains the fetched NFTs for the specified player and collection. If an error occurs, an empty collection is returned.
		/// </returns>
		public async Task<CollectionFetchResponse> RequestCollection(string playerId, string[] collectionNames)
		{
			if (string.IsNullOrWhiteSpace(playerId))
			{
				Console.Error.WriteLine("RequestCollection called with an empty playerId.");
				return _EMPTY_COLLECTION;
			}

			var query = HttpUtility.ParseQueryString(string.Empty);
			query["key"] = _apiKey;
			query["playfabId"] = playerId;

			if (collectionNames?.Any() == true)
			{
				foreach (var collection in collectionNames)
				{
					query.Add("collectionNames", collection); // Adds multiple instances of collectionName
				}
			}
			string url = $"{_externalUrl}/nft/owned/all?{query}";
			try
			{
				var response = await _client.GetAsync(url);
				var responseString = await response.Content.ReadAsStringAsync();

				if (response.StatusCode != HttpStatusCode.OK)
				{
					Console.Error.WriteLine($"Error obtaining NFT Collection Response [{response.StatusCode}] - {responseString}");
					return _EMPTY_COLLECTION;
				}
				var collectionNFTsOwnedDict = ModelSerializer.Deserialize<Dictionary<string, List<RemoteCollectionItem>>>(responseString) ?? new Dictionary<string, List<RemoteCollectionItem>>();
				return new CollectionFetchResponse { CollectionNFTsOwnedDict = collectionNFTsOwnedDict };
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"RequestCollection failed: {ex.Message}");
				return _EMPTY_COLLECTION;
			}
		}
	}
	
	[Serializable]
	public class ShopPurchaseInput
	{
		public PackedItem PackedItem;
		public GameId Currency;
		public BigInteger Amount;

		public Web3ShopIntent Format()
		{
			return new Web3ShopIntent()
			{
				Currency = this.Currency,
				Amount = this.Amount,
				Item = Web3Logic.UnpackItem(this.PackedItem)
			};
		}
	}
	
	[Serializable]
	public class Web3ShopIntent
	{
		public ItemData Item;
		public GameId Currency;
		public BigInteger Amount;
	}


}