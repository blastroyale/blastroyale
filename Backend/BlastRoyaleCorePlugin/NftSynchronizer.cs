using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Ids;
using FirstLight.Game.Views.MainMenuViews;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Quantum;
using ServerSDK;

namespace BlastRoyaleNFTPlugin
{
	/// <summary>
/// Class that encapsulates external models and functionality needed to synchronize NFT's
/// </summary>
public class NftSynchronizer
{
	private PluginContext _ctx;
	private HttpClient _client;
	private string _externalUrl;
	private string _apiKey;
	private static readonly IEnumerable<PolygonNFTMetadata> _EMPTY_LIST = new List<PolygonNFTMetadata>();
	
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
		    var serverState = await _ctx.ServerState.GetPlayerState(playfabId);
		    var equipmentData = serverState.DeserializeModel<EquipmentData>();
		    var lastBlockchainUpdate = await RequestBlockchainLastUpdate(playfabId);
		    if (equipmentData.LastUpdateTimestamp > lastBlockchainUpdate)
		    {
			    _ctx.Log.LogDebug($"{playfabId} had up-to-date NFT's");
			    return;
		    }
		    
		    var playerData = serverState.DeserializeModel<PlayerData>();
		    var idData = serverState.DeserializeModel<IdData>();
		    var ownedNftsInBlockchain = await RequestBlockchainIndexedNfts(playfabId);
		    var ownedNftsInGame = new Dictionary<string, UniqueId>();
		    var ownedTokensInBlockchain = new HashSet<string>(ownedNftsInBlockchain.Select(nft => nft.token_id));

		    foreach (var (id, nftEquipmentData) in equipmentData.NftInventory)
		    {
			    ownedNftsInGame.Add(nftEquipmentData.TokenId, id);
		    }

		    // Adding missing NFTS
		    foreach (var nft in ownedNftsInBlockchain)
		    {
			    try
			    {
				    if (!ownedNftsInGame.ContainsKey(nft.token_id))
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
		    foreach (var ownedTokenId in ownedNftsInGame)
		    {
			    if (!ownedTokensInBlockchain.Contains(ownedTokenId.Key))
			    {
				    _ctx.Log.LogInformation($"Removed item {ownedTokenId} from user {playfabId}");
				    RemoveEquipment(ownedTokenId.Value, equipmentData, playerData, idData);
			    }
		    }

		    equipmentData.LastUpdateTimestamp = lastBlockchainUpdate;
		    serverState.UpdateModel(equipmentData);
		    serverState.UpdateModel(idData);
		    serverState.UpdateModel(playerData);
		    await _ctx.ServerState.UpdatePlayerState(playfabId, serverState);
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
		string url = $"{_externalUrl}/indexed?key={_apiKey}&playfabId={playerId}";
		var response = await _client.GetAsync($"{_externalUrl}/indexed?key={_apiKey}&playfabId={playerId}");
		var responseString = await response.Content.ReadAsStringAsync();
		
		if (response.StatusCode != HttpStatusCode.OK)
		{
			_ctx.Log.LogError($"Error obtaining indexed NFTS Response {response.StatusCode.ToString()} - {responseString}");
			return _EMPTY_LIST;
		}
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
    private void AddEquipment(PolygonNFTMetadata nft, IdData idData, EquipmentData equipmentData)
    {
	    var equipment = NftToGameEquipment(nft);
	    var nftEquipment = NftToGameNftEquipment(nft);
        var nextId = ++idData.UniqueIdCounter;
        
        nftEquipment.InsertionTimestamp = DateTime.UtcNow.Ticks;
        
        equipmentData.Inventory.Add(nextId, equipment);
        equipmentData.NftInventory.Add(nextId, nftEquipment);
        idData.GameIds.Add(nextId, equipment.GameId);
    }

    /// <summary>
    /// Removes a given token from the given player.
    /// Also removes all references from the generated internal unique id for that token.
    /// </summary>
    private void RemoveEquipment(UniqueId uniqueId, EquipmentData nftEquipment, PlayerData playerData, IdData idData)
    {
	    nftEquipment.Inventory.Remove(uniqueId);
	    nftEquipment.NftInventory.Remove(uniqueId);
	    idData.GameIds.Remove(uniqueId);
	    
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
    
    /// <summary>
    /// Main function responsible for converting NFT's into game <see cref="Equipment"/>
    /// </summary>
    private Equipment NftToGameEquipment(PolygonNFTMetadata nft)
    {
	    object? gameId;
	    try
	    {
		    gameId = (GameId)nft.subCategory;
	    }
	    catch (Exception e)
	    {
		    // trying for name for backwards compatibility
		    if (!Enum.TryParse(typeof(GameId), nft.name.Replace(" ", String.Empty), true, out gameId))
		    {
			    throw new Exception($"Could not parse {nft.subCategory} or {nft.name} as a GameId");
		    }
	    }
	    var equip = new Equipment();
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
	    equip.Level = Convert.ToUInt32(nft.level);
	    equip.MaxLevel = Convert.ToUInt32(nft.maxLevel);
	    equip.ReplicationCounter = Convert.ToUInt32(nft.replicationCount);
	    equip.Durability = Convert.ToUInt32(nft.maxDurability);
	    return equip;
    }
    
    /// <summary>
    /// Main function responsible for converting NFT's into game <see cref="NftEquipmentData"/>
    /// </summary>
    private NftEquipmentData NftToGameNftEquipment(PolygonNFTMetadata nft)
    {
	    return new NftEquipmentData
	    {
		    TokenId = nft.token_id,
		    ImageUrl = nft.image
	    };
    }
}
}

