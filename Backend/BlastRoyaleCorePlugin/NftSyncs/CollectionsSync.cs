using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using BlastRoyaleNFTPlugin.NftSyncs;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Models.Collection;
using FirstLight.Server.SDK.Models;
using FirstLight.Server.SDK.Modules;
using Quantum;

namespace BlastRoyaleNFTPlugin
{
	public class CollectionsSync
	{
		public const string COLLECTION_CORPOS_POLYGON = "CorposLegacy";
		public const string COLLECTION_CORPOS_ETH = "Corpos";
		
		private static readonly CollectionFetchResponse _EMPTY_COLLECTION = new()
			{Owned = new List<RemoteCollectionItem>()};

		protected BlockchainApi BlockchainApi;

		public CollectionsSync(BlockchainApi blockchainApi)
		{
			BlockchainApi = blockchainApi;
		}

		public virtual async Task<IEnumerable<RemoteCollectionItem>> FetchAllCollections(string playfabId)
		{
			var c1 = RequestCollection<Corpos>(playfabId, COLLECTION_CORPOS_POLYGON);
			var c2 = RequestCollection<Corpos>(playfabId, COLLECTION_CORPOS_ETH);
			await Task.WhenAll(c1, c2);
			return c1.Result.Owned.Concat(c2.Result.Owned);
		}

		/// <summary>
		/// Should only sync in memory and not do any playfab requests
		/// </summary>
		public async Task<bool> SyncCollections(string playfabId, ServerState serverState)
		{
			var collectionData = serverState.DeserializeModel<CollectionData>();
			var initialHash = collectionData.GetHashCode();
			var owned = await FetchAllCollections(playfabId);
			var tokenIds = owned.Select(i => i.TokenId).ToList();
			SyncProfilePictures(playfabId, tokenIds, collectionData);
			SyncCharacterSkins(playfabId, owned, collectionData);
			if (collectionData.GetHashCode() != initialHash)
			{
				serverState.UpdateModel(collectionData);
			}
			return true;
		}

		private void SyncCharacterSkins(string playfabId, IEnumerable<RemoteCollectionItem> nfts, CollectionData collectionData)
		{
			var hasMaleNft = false;
			var hasFemaleNft = false;
			foreach (var nft in nfts)
			{
				var parsed = new FlgTraitParser(nft);
				var body = parsed.Traits["body"].ToLowerInvariant();
				if (body == "masculine") hasMaleNft = true;
				if (body == "feminine") hasFemaleNft = true;

				if (hasMaleNft && hasFemaleNft) break;
			}
		
			var skins = collectionData.OwnedCollectibles.TryGetValue(CollectionCategories.PLAYER_SKINS,
				out var playerSkins)
				? playerSkins
				: new List<ItemData>();
			var hasMaleSkin = false;
			var hasFemaleSkin = false;

			for (var i = skins.Count - 1; i >= 0; i--)
			{
				var skin = skins[i];
				var isCorposMetadata = skin.TryGetMetadata<CollectionMetadata>(out var meta)
					&& meta.TryGetTrait(CollectionTraits.NFT_COLLECTION, out var trait)
					&& trait == COLLECTION_CORPOS_ETH;
				if (skin.Id == GameId.FemaleCorpos)
				{
					hasFemaleSkin = true;

					if (!hasFemaleNft && isCorposMetadata)
					{
						BlockchainApi._ctx.Log.LogInformation($"Removing GameId.FemaleCorpos from " + playfabId);
						BlockchainApi._ctx.Analytics.EmitUserEvent(playfabId, "nft_remove_collection",
							new AnalyticsData()
							{
								{"id", "FemaleCorpos"}
							});
						CheckEquippedRemove(skin, collectionData);
						skins.RemoveAt(i);
					}
				}

				if (skin.Id == GameId.MaleCorpos)
				{
					hasMaleSkin = true;
					if (!hasMaleNft && isCorposMetadata)
					{
						BlockchainApi._ctx.Log.LogInformation($"Removing GameId.MaleCorpos from " + playfabId);
						BlockchainApi._ctx.Analytics.EmitUserEvent(playfabId, "nft_remove_collection",
							new AnalyticsData()
							{
								{"id", "FemaleCorpos"}
							});
						CheckEquippedRemove(skin, collectionData);
						skins.RemoveAt(i);
					}
				}
			}

			if (!hasMaleSkin && hasMaleNft)
			{
				BlockchainApi._ctx.Log.LogInformation($"Add GameId.FemaleCorpos to " + playfabId);
				BlockchainApi._ctx.Analytics.EmitUserEvent(playfabId, "nft_add_collection", new AnalyticsData()
				{
					{"id", "FemaleCorpos"}
				});
				skins.Add(ItemFactory.Collection(GameId.MaleCorpos,
					new CollectionTrait(CollectionTraits.NFT_COLLECTION, COLLECTION_CORPOS_ETH)
				));
			}

			if (!hasFemaleSkin && hasFemaleNft)
			{
				BlockchainApi._ctx.Log.LogInformation($"Add GameId.MaleCorpos to " + playfabId);
				BlockchainApi._ctx.Analytics.EmitUserEvent(playfabId, "nft_add_collection", new AnalyticsData()
				{
					{"id", "MaleCorpos"}
				});
				skins.Add(ItemFactory.Collection(GameId.FemaleCorpos,
					new CollectionTrait(CollectionTraits.NFT_COLLECTION, COLLECTION_CORPOS_ETH)
				));
			}

			collectionData.OwnedCollectibles[CollectionCategories.PLAYER_SKINS] = skins;
		}

		private void CheckEquippedRemove(ItemData item, CollectionData data)
		{
			foreach (var collectionType in data.Equipped.Keys)
			{
				var equipped = data.Equipped[collectionType];
				if (equipped.Equals(item))
				{
					data.Equipped[collectionType] = data.OwnedCollectibles[collectionType].FirstOrDefault();
				}
			}
		}

		private void SyncProfilePictures(string playfabId, ICollection<string> nfts, CollectionData collectionData)
		{
			var toAdd = new List<string>(nfts);

			var pfps = collectionData.OwnedCollectibles.TryGetValue(CollectionCategories.PROFILE_PICTURE,
				out var playerPfps)
				? playerPfps
				: new List<ItemData>();

			// First remove items player doesn't have anymore
			for (var i = pfps.Count - 1; i >= 0; i--)
			{
				var pfp = pfps[i];
				if (pfp.Id != GameId.AvatarNFTCollection || !pfp.HasMetadata<CollectionMetadata>()) continue;
				var metadata = pfp.GetMetadata<CollectionMetadata>();
				if (!metadata.TryGetTrait(CollectionTraits.NFT_COLLECTION, out var collection) ||
					!metadata.TryGetTrait(CollectionTraits.TOKEN_ID, out var token)) continue;

				if (collection != COLLECTION_CORPOS_ETH)
				{
					continue;
				}

				if (!nfts.Contains(token))
				{
					BlockchainApi._ctx.Log.LogInformation($"Removing corpo PFP " + token + " from " + playfabId);
					BlockchainApi._ctx.Analytics.EmitUserEvent(playfabId, "nft_remove_collection",
						new AnalyticsData()
						{
							{"id", "AvatarNFTCollection"},
							{"token", token}
						});
					CheckEquippedRemove(pfp, collectionData);
					pfps.RemoveAt(i);
					continue;
				}
				toAdd.Remove(token);
			}

			foreach (var tokenId in toAdd)
			{
				var item = ItemFactory.Collection(GameId.AvatarNFTCollection,
					new CollectionTrait(CollectionTraits.NFT_COLLECTION, COLLECTION_CORPOS_ETH),
					new CollectionTrait(CollectionTraits.TOKEN_ID, tokenId));
				pfps.Add(item);
				BlockchainApi._ctx.Analytics.EmitUserEvent(playfabId, "nft_add_collection", new AnalyticsData()
				{
					{"id", "AvatarNFTCollection"},
					{"token", tokenId}
				});
				BlockchainApi._ctx.Log.LogInformation($"Giving corpo PFP " + tokenId + " to " + playfabId);
			}
			collectionData.OwnedCollectibles[CollectionCategories.PROFILE_PICTURE] = pfps;
		}

		/// <summary>
		/// Request for all indexed nfts for a given wallet.
		/// </summary>
		public async Task<CollectionFetchResponse> RequestCollection<T>(string playerId, string collectionName) where T : RemoteCollectionItem
		{
			var url =
				$"{BlockchainApi._externalUrl}/nft/owned?key={BlockchainApi._apiKey}&playfabId={playerId}&collectionName={collectionName}";
			var response = await BlockchainApi._client.GetAsync(url);
			var responseString = await response.Content.ReadAsStringAsync();

			if (response.StatusCode != HttpStatusCode.OK)
			{
				BlockchainApi._ctx.Log.LogError(
					$"Error obtaining indexed Collection Response {response.StatusCode.ToString()} - {responseString}");
				return _EMPTY_COLLECTION;
			}

			var list = ModelSerializer.Deserialize<List<T>>(responseString);
			return new CollectionFetchResponse()
			{
				Owned = list
			};
		}
	}
}