using System.Net;
using FirstLight.Game.Data;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Quantum;
using ServerSDK;

namespace BlastRoyaleNFTPlugin;

/// <summary>
/// Class that encapsulates external models and functionality needed to synchronize NFT's
/// </summary>
public class NftSynchronizer
{
	private PluginContext _ctx;
	private HttpClient _client;
	private string _externalUrl;
	private string _apiKey;

	public NftSynchronizer(string nftsIndexUrl, string apiKey, PluginContext ctx)
	{
		_client = new HttpClient();
		_externalUrl = nftsIndexUrl;
		_ctx = ctx;
		_apiKey = apiKey;
	}
	
	/// <summary>
    /// Function that syncrhonizes blockchain data to game data.
    /// Will add missing NFT's and remove NFT's that are not owned anymore by the user.
    /// </summary>
    public async Task SyncAllNfts(string playfabId)
    {
	    try
	    {
		    _ctx.PlayerMutex.Lock(playfabId);
		    var serverState = _ctx.ServerState.GetPlayerState(playfabId);
		    var equipmentData = serverState.DeserializeModel<NftEquipmentData>();
		    var lastBlockchainUpdate = await RequestBlockchainLastUpdate(playfabId);
		    if (equipmentData.LastUpdateTimestamp > lastBlockchainUpdate)
		    {
			    _ctx.Log.LogDebug($"{playfabId} had up-to-date NFT's");
			    return;
		    }
		    
		    var playerData = serverState.DeserializeModel<PlayerData>();
		    var idData = serverState.DeserializeModel<IdData>();
		    var ownedNftsInBlockchain = await RequestBlockchainIndexedNfts(playfabId);
		    var ownedTokensInGame = new HashSet<string>(equipmentData.TokenIds.Values);
		    var ownedTokensInBlockchain = new HashSet<string>(ownedNftsInBlockchain.Select(nft => nft.token_id));

		    // Adding missing NFTS
		    foreach (var nft in ownedNftsInBlockchain)
		    {
			    try
			    {
				    if (!ownedTokensInGame.Contains(nft.token_id))
				    {
					    AddEquipment(nft, idData, equipmentData);
					    _ctx.Log.LogInformation($"Added item {nft.token_id}({nft.name}) to user {playfabId}");
				    }
			    }
			    catch (Exception e)
			    {
				    _ctx.Log.LogError($"Error while converting NFT to Blast-Royale Equipment: {JsonConvert.SerializeObject(nft)}");
				    _ctx.Log.LogTrace(e.StackTrace);
			    }
		    }

		    // Removing unowned NFTS
		    foreach (var ownedTokenId in ownedTokensInGame)
		    {
			    if (!ownedTokensInBlockchain.Contains(ownedTokenId))
			    {
				    _ctx.Log.LogInformation($"Removed item {ownedTokenId} from user {playfabId}");
				    RemoveEquipment(ownedTokenId, equipmentData, playerData, idData);
			    }
		    }

		    equipmentData.LastUpdateTimestamp = lastBlockchainUpdate;
		    serverState.SetModel(equipmentData);
		    serverState.SetModel(idData);
		    _ctx.ServerState.UpdatePlayerState(playfabId, serverState);
	    }
	    finally
	    {
		    _ctx.PlayerMutex.Unlock(playfabId);
	    }
    }

	/// <summary>
	/// Request for all indexed nfts for a given wallet.
	/// </summary>
	protected virtual async Task<IEnumerable<PolygonNFTMetadata>?> RequestBlockchainIndexedNfts(string playerId)
	{
		var response = await _client.GetAsync($"{_externalUrl}/indexed?key={_apiKey}&playfabId={playerId}");
		if (response.StatusCode != HttpStatusCode.OK)
		{
			_ctx.Log.LogError($"Error obtaining indexed NFTS Response {response.StatusCode.ToString()}");
			return null;
		}
		var responseString = await response.Content.ReadAsStringAsync();
		return JsonConvert.DeserializeObject<List<PolygonNFTMetadata>>(responseString);
	}

	/// <summary>
	/// Request the last time a given wallet was updated.
	/// </summary>
	protected virtual async Task<ulong> RequestBlockchainLastUpdate(string playerId)
	{
		var response = await _client.GetAsync($"{_externalUrl}/lastupdate?key={_apiKey}&playfabId={playerId}");
		if (response.StatusCode != HttpStatusCode.OK)
		{
			_ctx.Log.LogError($"Error obtaining indexed NFTS Response {response.StatusCode.ToString()}");
			return 0;
		}
		var responseString = await response.Content.ReadAsStringAsync();
		return ulong.Parse(responseString);
	}

    /// <summary>
    /// Adds NFT equipment to game data models. Perform a conversion to game data models from NFT model.
    /// </summary>
    private void AddEquipment(PolygonNFTMetadata nft, IdData idData, NftEquipmentData equipmentData)
    {
        var equipment = NftToGameObject(nft);
        var nextId = ++idData.UniqueIdCounter;
        equipmentData.Inventory.Add(nextId, equipment);
        idData.GameIds.Add(nextId, equipment.GameId);
        equipmentData.TokenIds[nextId] = nft.token_id;
        equipmentData.ImageUrls[nextId] = nft.image;
    }

    /// <summary>
    /// Removes a given token from the given player.
    /// Also removes all references from the generated internal unique id for that token.
    /// </summary>
    private void RemoveEquipment(string tokenId, NftEquipmentData nftEquipment, PlayerData playerData, IdData idData)
    {
        var uniqueIds = nftEquipment.TokenIds.Keys.ToList();
        foreach (var uniqueId in uniqueIds)
        {
            var ownedTokenId = nftEquipment.TokenIds[uniqueId];
            if (ownedTokenId == tokenId)
            {
                nftEquipment.Inventory.Remove(uniqueId);
                idData.GameIds.Remove(uniqueId);
                nftEquipment.ExpireTimestamps.Remove(uniqueId);
                nftEquipment.TokenIds.Remove(uniqueId);
                var equippedGroups = playerData.Equipped.Keys.ToList();
                foreach (var group in equippedGroups)
                {
                    var equippedUniqueId = playerData.Equipped[group];
                    if (equippedUniqueId == uniqueId)
                    {
                        playerData.Equipped.Remove(group);
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Main function responsible for converting NFT's into game objects
    /// </summary>
    private Equipment NftToGameObject(PolygonNFTMetadata nft)
    {
	    var equip = new Equipment();
	    // TODO: Review - currently we are transforming the name into GameID which might missmatch.
	    // Maybe we would want to add the gameId to the nft metadata in the future
	    var gameIdName = nft.name.Replace(" ", String.Empty); 
	    if (!Enum.TryParse(typeof(GameId), gameIdName, true, out var gameId))
	    {
		    throw new Exception($"Could not parse {gameIdName} as a GameId");
	    }

	    equip.GameId = (GameId)gameId;
	    equip.Faction = (EquipmentFaction) nft.faction;
	    equip.Adjective = (EquipmentAdjective)nft.adjective;
	    equip.MaxDurability = Convert.ToUInt32(nft.maxDurability);
	    equip.Edition = (EquipmentEdition)nft.edition;
	    equip.Generation = Convert.ToUInt32(nft.generation);
	    equip.Grade = (EquipmentGrade) nft.grade;
	    equip.Manufacturer = (EquipmentManufacturer) nft.manufacturer;
	    equip.Material = (EquipmentMaterial) nft.material;
	    equip.Rarity = (EquipmentRarity) nft.rarity;
	    equip.Tuning = Convert.ToUInt32(nft.tuning);
	    equip.InitialReplicationCounter = Convert.ToUInt32(nft.initialReplicationCounter);
		
	    // Below unfinished hard-coded equipment variable fields
	    equip.Level = Convert.ToUInt32(nft.maxLevel); // TODO
	    equip.MaxLevel = Convert.ToUInt32(nft.maxLevel); // TODO
	    equip.ReplicationCounter = Convert.ToUInt32(nft.initialReplicationCounter); // TODO
	    equip.Durability = Convert.ToUInt32(nft.maxDurability); // TODO
	    return equip;
    }
}