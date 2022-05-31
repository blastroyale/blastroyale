using System;
using System.Collections.Generic;
using FirstLight;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Utils;
using Quantum;
using ServerSDK.Models;

namespace Backend.Game.Services;


/// <summary>
/// Service responsible for generating initial player state.
/// </summary>
public interface IPlayerSetupService
{
	/// <summary>
	/// Generates initial player state and returns as server data.
	/// </summary>
	public ServerState GetInitialState(string playFabId);

	/// <summary>
	/// Checks if a given state is already setup
	/// </summary>
	/// <param name="state"></param>
	/// <returns></returns>
	public bool IsSetup(ServerState state);
}

/// <inheritdoc />
public class PlayerSetupService : IPlayerSetupService
{
	private static readonly List<GameId> _initialSkins = new List<GameId>
	{
		GameId.Male01Avatar, GameId.Male02Avatar, GameId.Female01Avatar, GameId.Female02Avatar
	};
	
	private static IConfigsProvider _configsProvider;

	public PlayerSetupService(IConfigsProvider configsProvider)
	{
		_configsProvider = configsProvider;
	}

	/// <inheritdoc />
	public ServerState GetInitialState(string playFabId)
	{
		var rngData = SetupInitialRngData(playFabId.GetHashCode());
		var idData = new IdData();
		var playerData = SetupInitialPlayerData(idData, rngData);
		var equipmentData = SetupInitialNftEquipments(idData);
		var serverState = new ServerState();
		serverState.SetModel(idData);
		serverState.SetModel(rngData);
		serverState.SetModel(playerData);
		serverState.SetModel(equipmentData);
		return serverState;
	}

	/// <inheritdoc />
	public bool IsSetup(ServerState state)
	{
		var playerData = state.DeserializeModel<PlayerData>();
		if (playerData == null || playerData.Emoji.Count == 0)
			return false;
		return true;
	}

	/// <summary>
	/// Initializes player RngData
	/// </summary>
	private static RngData SetupInitialRngData(int seed)
	{
		return new RngData
		{
			Count = 0,
			Seed = seed,
			State = RngUtils.GenerateRngState(seed)
		};
	}

	/// <summary>
	/// Sets up initial player NFT equipment data.
	/// This is for testing purposes and should be removed soon.
	/// </summary>
	private static NftEquipmentData SetupInitialNftEquipments(IdData idData)
	{
		var nftEquipsData = new NftEquipmentData();
		var nextId = ++idData.UniqueIdCounter;
		idData.GameIds.Add(nextId, GameId.Hammer);
		nftEquipsData.Inventory.Add(nextId, new Equipment(GameId.Hammer, level: 1));
		return nftEquipsData;
	}

	/// <summary>
	/// Setup initial player data contents.
	/// </summary>
	private static PlayerData SetupInitialPlayerData(IdData idData, RngData rngData)
	{
		var rngSkin = Rng.Range(0, _initialSkins.Count, rngData.State, false);
		var csPoolConfig = _configsProvider.GetConfig<ResourcePoolConfig>((int)GameId.CS);
		var eqExpPoolConfig = _configsProvider.GetConfig<ResourcePoolConfig>((int)GameId.EquipmentXP);
		var playerData = new PlayerData();
		playerData.Level = 1;
		playerData.PlayerSkinId = _initialSkins[rngSkin];
		playerData.Trophies = 100;
		playerData.ResourcePools.Add(GameId.CS, new ResourcePoolData(GameId.CS, csPoolConfig.PoolCapacity, DateTime.UtcNow));
		playerData.ResourcePools.Add(GameId.EquipmentXP, new ResourcePoolData(GameId.EquipmentXP, eqExpPoolConfig.PoolCapacity, DateTime.UtcNow));
		playerData.Currencies.Add(GameId.CS, 0);
		playerData.Emoji.Add(GameId.EmojiAngry);
		playerData.Emoji.Add(GameId.EmojiLove);
		playerData.Emoji.Add(GameId.EmojiAngel);
		playerData.Emoji.Add(GameId.EmojiCool);
		playerData.Emoji.Add(GameId.EmojiSick);
		return playerData;
	}
}