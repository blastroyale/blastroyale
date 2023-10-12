using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Models.Collection;
using FirstLight.Server.SDK.Models;
using Quantum;

namespace BlastRoyaleNFTPlugin;

public class CorpoSync
{
	private static readonly string COLLECTION_NAME = "corpos";
	private static readonly CollectionFetchResponse _EMPTY_COLLECTION = new() { Owned = new List<RemoteCollectionItem>() };

	private NftSynchronizer _nftSynchronizer;

	public CorpoSync(NftSynchronizer nftSynchronizer)
	{
		_nftSynchronizer = nftSynchronizer;
	}

	internal async Task<bool> SyncCorpos(string playfabId, ServerState serverState, ulong lastBlockchainUpdate)
	{
		var collectionData = serverState.DeserializeModel<CollectionData>();
		if (collectionData.LastUpdateTimestamp >= lastBlockchainUpdate)
		{
			return false;
		}

		var owned = (await RequestCollection(playfabId, COLLECTION_NAME)).Owned.ToList();
		var casted = owned.Cast<Corpos>().ToList();
		var tokenIds = owned.Select(i => i.Identifier).ToList();
		SyncProfilePictures(playfabId, tokenIds, collectionData);
		SyncCharacterSkins(playfabId, casted, collectionData);
		collectionData.LastUpdateTimestamp = lastBlockchainUpdate;
		serverState.UpdateModel(collectionData);
		return true;
	}

	private void SyncCharacterSkins(string playfabId, IList<Corpos> nfts, CollectionData collectionData)
	{
		var hasMaleNFT = nfts.Any(nft => nft.MasculineBody);
		var hasFemaleNFT = nfts.Any(nft => !nft.MasculineBody);


		var skins = collectionData.OwnedCollectibles.TryGetValue(CollectionCategories.PLAYER_SKINS, out var playerSkins) ? playerSkins : new List<ItemData>();
		var hasMaleSkin = false;
		var hasFemaleSkin = false;

		for (var i = skins.Count - 1; i >= 0; i--)
		{
			var skin = skins[i];
			var isCorposMetadata = skin.TryGetMetadata<CollectionMetadata>(out var meta)
				&& meta.TryGetTrait(CollectionTraits.NFT_COLLECTION, out var trait)
				&& trait == COLLECTION_NAME;
			if (skin.Id == GameId.FemaleCorpos)
			{
				hasFemaleSkin = true;

				if (!hasFemaleNFT && isCorposMetadata)
				{
					_nftSynchronizer._ctx.Log.LogInformation($"Removing GameId.FemaleCorpos from " + playfabId);
					_nftSynchronizer._ctx.Analytics.EmitUserEvent(playfabId, "nft_remove_collection", new AnalyticsData()
					{
						{ "id", "FemaleCorpos" }
					});
					CheckEquippedRemove(skin, collectionData);
					skins.RemoveAt(i);
				}
			}

			if (skin.Id == GameId.MaleCorpos)
			{
				hasMaleSkin = true;
				if (!hasMaleNFT && isCorposMetadata)
				{
					_nftSynchronizer._ctx.Log.LogInformation($"Removing GameId.MaleCorpos from " + playfabId);
					_nftSynchronizer._ctx.Analytics.EmitUserEvent(playfabId, "nft_remove_collection", new AnalyticsData()
					{
						{ "id", "FemaleCorpos" }
					});
					CheckEquippedRemove(skin, collectionData);
					skins.RemoveAt(i);
				}
			}
		}

		if (!hasMaleSkin && hasMaleNFT)
		{
			_nftSynchronizer._ctx.Log.LogInformation($"Add GameId.FemaleCorpos to " + playfabId);
			_nftSynchronizer._ctx.Analytics.EmitUserEvent(playfabId, "nft_add_collection", new AnalyticsData()
			{
				{ "id", "FemaleCorpos" }
			});
			skins.Add(ItemFactory.Collection(GameId.MaleCorpos,
				new CollectionTrait(CollectionTraits.NFT_COLLECTION, COLLECTION_NAME)
			));
		}

		if (!hasFemaleSkin && hasFemaleNFT)
		{
			_nftSynchronizer._ctx.Log.LogInformation($"Add GameId.MaleCorpos to " + playfabId);
			_nftSynchronizer._ctx.Analytics.EmitUserEvent(playfabId, "nft_add_collection", new AnalyticsData()
			{
				{ "id", "MaleCorpos" }
			});
			skins.Add(ItemFactory.Collection(GameId.FemaleCorpos,
				new CollectionTrait(CollectionTraits.NFT_COLLECTION, COLLECTION_NAME)
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

		var pfps = collectionData.OwnedCollectibles.TryGetValue(CollectionCategories.PROFILE_PICTURE, out var playerPfps) ? playerPfps : new List<ItemData>();

		// First remove items player doesn't have anymore
		for (var i = pfps.Count - 1; i >= 0; i--)
		{
			var pfp = pfps[i];
			if (pfp.Id != GameId.AvatarNFTCollection || !pfp.HasMetadata<CollectionMetadata>()) continue;
			var metadata = pfp.GetMetadata<CollectionMetadata>();
			if (!metadata.TryGetTrait(CollectionTraits.NFT_COLLECTION, out var collection) ||
				!metadata.TryGetTrait(CollectionTraits.TOKEN_ID, out var token)) continue;

			if (collection != COLLECTION_NAME)
			{
				continue;
			}

			if (!nfts.Contains(token))
			{
				_nftSynchronizer._ctx.Log.LogInformation($"Removing corpo PFP " + token + " from " + playfabId);
				_nftSynchronizer._ctx.Analytics.EmitUserEvent(playfabId, "nft_remove_collection", new AnalyticsData()
				{
					{ "id", "AvatarNFTCollection" },
					{ "token", token }
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
				new CollectionTrait(CollectionTraits.NFT_COLLECTION, COLLECTION_NAME),
				new CollectionTrait(CollectionTraits.TOKEN_ID, tokenId));
			pfps.Add(item);
			_nftSynchronizer._ctx.Analytics.EmitUserEvent(playfabId, "nft_add_collection", new AnalyticsData()
			{
				{ "id", "AvatarNFTCollection" },
				{ "token", tokenId }
			});
			_nftSynchronizer._ctx.Log.LogInformation($"Giving corpo PFP " + tokenId + " to " + playfabId);
		}

		collectionData.OwnedCollectibles[CollectionCategories.PROFILE_PICTURE] = pfps;
	}

	/// <summary>
	/// Request for all indexed nfts for a given wallet.
	/// </summary>
	public virtual async Task<CollectionFetchResponse> RequestCollection(string playerId, string collectionName)
	{
		var url = $"{_nftSynchronizer._externalUrl}/collection/owned?key={_nftSynchronizer._apiKey}&playfabId={playerId}&collectionName={collectionName}";
		var response = await _nftSynchronizer._client.GetAsync(url);
		var responseString = await response.Content.ReadAsStringAsync();

		if (response.StatusCode != HttpStatusCode.OK)
		{
			_nftSynchronizer._ctx.Log.LogError(
				$"Error obtaining indexed Collection Response {response.StatusCode.ToString()} - {responseString}");
			return _EMPTY_COLLECTION;
		}

		return CollectionSerializer.Deserialize(responseString);
	}
}