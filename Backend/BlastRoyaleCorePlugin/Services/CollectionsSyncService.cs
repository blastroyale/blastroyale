using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using BlastRoyaleNFTPlugin.Collections;
using BlastRoyaleNFTPlugin.Data;
using FirstLight.Game.Data;
using FirstLight.Server.SDK.Models;
using FirstLight.Server.SDK.Modules;

namespace BlastRoyaleNFTPlugin.Services
{
	public class CollectionsSyncService
	{
		
		private static readonly CollectionFetchResponse _EMPTY_COLLECTION = new() {NFTsOwned = new List<RemoteCollectionItem>()};

		private NFTCollectionSyncConfigData NFTCollectionSyncConfigData;
		private readonly BlockchainApi BlockchainApi;

		
		public CollectionsSyncService(BlockchainApi blockchainApi)
		{
			BlockchainApi = blockchainApi;
			NFTCollectionSyncConfigData = new NFTCollectionSyncConfigData();
		}
		
		/// <summary>
		/// Looks up and retrieves NFT collections owned for a specific player
		/// This method fetches collections by the provided player ID and the available NFT collection configurations,
		/// executing the requests in parallel. 
		/// </summary>
		/// <param name="playerId">The PlayFab ID of the player whose collections are to be fetched.</param>
		/// <returns>
		/// A list of tuples where each tuple contains:
		/// - The NFT collection configuration (NFTCollectionSyncConfigModel)
		/// - The result of the collection fetch operation (CollectionFetchResponse).
		/// If no collections are available for fetching or an error occurs, an empty list is returned.
		/// </returns>
		/// <remarks>
		/// - If a collection is not eligible for fetching (based on the CanSync flag or environment variables),
		///   it is skipped and logged.
		/// - This method executes each fetch request asynchronously and waits for all of them to complete.
		/// - No actual synchronization or modification of the collections occurs in this method — it only retrieves data.
		/// - If any exception occurs during the fetching of collections, it is logged, and an empty list is returned.
		/// </remarks>
		/// <exception cref="Exception">
		/// Thrown when an error occurs during the collection fetching process. The exception details 
		/// are logged, and the method returns an empty list.
		/// </exception>
		private async Task<List<(NFTCollectionSyncConfigModel CollectionConfig, CollectionFetchResponse CollectionItems)>> RequestNFTCollectionsForPlayer(string playerId)
		{
			var collectionSyncTasks = new List<(NFTCollectionSyncConfigModel CollectionConfig, Task<CollectionFetchResponse> CollectionFetchTask)>();
			
			foreach (var nftCollection in NFTCollectionSyncConfigData.NFTCollections)
			{
				// Skip syncing if CanSync is false and environment variables do not allow syncing
				if (!nftCollection.CanSync && !BlockchainApi.CanSyncCollection(nftCollection.CollectionName))
				{
					BlockchainApi._ctx.Log.LogInformation(
						$"Collection '{nftCollection.CollectionName}' cannot be synced. " +
						$"If you believe it should be synced, ensure the environment variable '{nftCollection.CollectionName.ToUpperInvariant()}_SYNC_ENABLED' is set to 'true'.");
					continue;
				}

				var syncTask = RequestCollection(playerId, nftCollection.CollectionName);
				collectionSyncTasks.Add((nftCollection, syncTask));
			}
			
			if (collectionSyncTasks.Any())
			{
				try
				{
					var requestResults = await Task.WhenAll(collectionSyncTasks.Select(t => t.CollectionFetchTask));
					var results = collectionSyncTasks
						.Select((task, index) => (task.Item1, requestResults[index]))
						.ToList();

					return results;
				}
				catch (Exception ex)
				{
					BlockchainApi._ctx.Log.LogError($"An error occurred while syncing NFT collections: {ex.Message}");
				}
			}
			else
			{
				BlockchainApi._ctx.Log.LogInformation($"No collections were available for syncing for player {playerId}.");
			}

			// Return an empty list in case no tasks were run
			return new List<(NFTCollectionSyncConfigModel , CollectionFetchResponse)>();
		}
		
		
		/// <summary>
		/// Synchronizes the player's in-memory NFT collections without making any PlayFab requests. 
		/// This method processes only the data available in memory, comparing the current state of the player's collection and updating it accordingly.
		/// </summary>
		/// <param name="playfabId">The PlayFab ID of the player whose collections are being synced.</param>
		/// <param name="serverState">The current state of the server containing the player's data. This object is used to deserialize the player's collection and update it if any changes are detected.</param>
		/// <returns>
		/// A <see cref="Task{TResult}"/> representing the result of the asynchronous operation, which will return <c>true</c> upon successful synchronization.
		/// </returns>
		public async Task<bool> SyncCollections(string playfabId, ServerState serverState)
		{
			var playerCollectionData = serverState.DeserializeModel<CollectionData>();
			var playerCollectionInitialHash = playerCollectionData.GetHashCode();
			var collectionsOwned = await RequestNFTCollectionsForPlayer(playfabId);
			
			//Loop through CollectionConfigs and 
			foreach (var collectionConfigItemsTuple in collectionsOwned)
			{
				if (collectionConfigItemsTuple.CollectionConfig.CollectionSyncCategories.Any(cc =>
						cc == CollectionCategories.PLAYER_SKINS))
				{
					new SkinCollectionSync().Sync(playerCollectionData, collectionConfigItemsTuple.CollectionConfig, collectionConfigItemsTuple.CollectionItems.NFTsOwned.ToList());
				}
				
				if (collectionConfigItemsTuple.CollectionConfig.CollectionSyncCategories.Any(cc =>
						cc == CollectionCategories.PROFILE_PICTURE))
				{
					new ProfilePictureCollectionSync().Sync(playerCollectionData, collectionConfigItemsTuple.CollectionConfig, collectionConfigItemsTuple.CollectionItems.NFTsOwned.ToList());
				}
			}
			
			if (playerCollectionData.GetHashCode() != playerCollectionInitialHash)
			{
				serverState.UpdateModel(playerCollectionData);
			}
			
			return true;
		}
		
		
		/// <summary>
		/// Requests all indexed NFTs for a given player's wallet from the blockchain API.
		/// This method fetches the NFTs owned by the player for a specific collection from the blockchain indexer and returns the results.
		/// </summary>
		/// <param name="playerId">The PlayFab ID of the player whose NFT collection is being fetched.</param>
		/// <param name="collectionName">The name of the NFT collection to retrieve for the player.</param>
		/// <returns>
		/// A <see cref="Task{CollectionFetchResponse}"/> representing the asynchronous operation. 
		/// The result contains the fetched NFTs for the specified player and collection. If an error occurs, an empty collection is returned.
		/// </returns>
		public async Task<CollectionFetchResponse> RequestCollection(string playerId, string collectionName)
		{
			BlockchainApi._ctx.Log.LogInformation($"Requesting NFTCollection {collectionName} for PlayerID {playerId}");
			
			var url = $"{BlockchainApi._externalUrl}/nft/owned?key={BlockchainApi._apiKey}&playfabId={playerId}&collectionName={collectionName}";
		
			var response = await BlockchainApi._client.GetAsync(url);
			var responseString = await response.Content.ReadAsStringAsync();
			
			BlockchainApi._ctx.Log.LogInformation($"Request completed for NFTCollection {collectionName} for PlayerID {playerId}");

			if (response.StatusCode != HttpStatusCode.OK)
			{
				BlockchainApi._ctx.Log.LogError($"Error obtaining NFT Collection Response {response.StatusCode.ToString()} - {responseString}");
				return _EMPTY_COLLECTION;
			}

			var list = ModelSerializer.Deserialize<List<RemoteCollectionItem>>(responseString);
			return new CollectionFetchResponse()
			{
				NFTsOwned = list
			};
		}
		
	}
}