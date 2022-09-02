using System;
using System.Collections.Generic;
using FirstLight;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Utils;
using Quantum;
using ServerSDK.Models;
using ServerSDK.Services;

namespace Backend.Game.Services
{
	
/// <inheritdoc />
public class BlastRoyalePlayerSetup : IPlayerSetupService
{
	private static readonly List<GameId> _initialSkins = new List<GameId>
	{
		GameId.Male01Avatar, GameId.Male02Avatar, GameId.Female01Avatar, GameId.Female02Avatar
	};
	
	private static IConfigsProvider _configsProvider;

	public BlastRoyalePlayerSetup(IConfigsProvider configsProvider)
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
		if (state.Count == 0)
			return false;
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
		var playerData = new PlayerData();
		playerData.PlayerSkinId = _initialSkins[rngSkin];
		playerData.ResourcePools.Add(GameId.CS, new ResourcePoolData(GameId.CS, csPoolConfig.PoolCapacity, DateTime.UtcNow));
		return playerData;
	}
}
}

