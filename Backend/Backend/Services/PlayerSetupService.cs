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
		var equipmentData = new EquipmentData();
		var serverState = new ServerState();
		serverState.UpdateModel(idData);
		serverState.UpdateModel(rngData);
		serverState.UpdateModel(playerData);
		serverState.UpdateModel(equipmentData);
		return serverState;
	}

	/// <inheritdoc />
	public bool IsSetup(ServerState state)
	{
		var playerData = state.DeserializeModel<PlayerData>();
		if (playerData == null || playerData.Level == 0)
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
		playerData.Trophies = 1000;
		playerData.DeathMarker = GameId.Tombstone;
		playerData.ResourcePools.Add(GameId.CS, new ResourcePoolData(GameId.CS, csPoolConfig.PoolCapacity, DateTime.UtcNow));
		playerData.ResourcePools.Add(GameId.EquipmentXP, new ResourcePoolData(GameId.EquipmentXP, eqExpPoolConfig.PoolCapacity, DateTime.UtcNow));
		playerData.Currencies.Add(GameId.CS, 0);
		playerData.Currencies.Add(GameId.BLST, 0);
		return playerData;
	}
}