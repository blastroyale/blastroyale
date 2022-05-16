using System;
using System.Collections.Generic;
using Backend.Models;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Logic;
using FirstLight.Game.Utils;
using Newtonsoft.Json;
using PlayFab.ServerModels;
using Quantum;

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
}

/// <inheritdoc />
public class PlayerSetupService : IPlayerSetupService
{
	private static readonly List<GameId> _initialSkins = new List<GameId>
	{
		GameId.Male01Avatar, GameId.Male02Avatar, GameId.Female01Avatar, GameId.Female02Avatar
	};
	
	private static readonly List<GameId> _initialWeapons = new List<GameId>
	{
		GameId.Hammer
	};

	/// <inheritdoc />
	public ServerState GetInitialState(string playFabId)
	{
		var rngData = SetupInitialRngData(playFabId.GetHashCode());
		var idData = new IdData();
		var playerData = SetupInitialPlayerData(idData, rngData);
		var serverState = new ServerState();
		serverState.SetModel(idData);
		serverState.SetModel(rngData);
		serverState.SetModel(playerData);
		return serverState;
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
		var rngWeapon = Rng.Range(0, _initialWeapons.Count, rngData.State, false);
		var playerData = new PlayerData
		{
			Level = 1,
			PlayerSkinId = _initialSkins[rngSkin],
			Trophies = 100
		};

		rngData.Count += 2;
		
		playerData.ResourcePools.Add(GameId.CS, new ResourcePoolData(GameId.CS, int.MaxValue, DateTime.UtcNow));
		playerData.ResourcePools.Add(GameId.EquipmentXP, new ResourcePoolData(GameId.EquipmentXP, int.MaxValue, DateTime.UtcNow));
		
		playerData.EquippedItems.Add(GameIdGroup.Weapon, idData.UniqueIdCounter + 1);
		idData.GameIds.Add(++idData.UniqueIdCounter, _initialWeapons[rngWeapon]);

		foreach (var id in idData.GameIds)
		{
			playerData.Inventory.Add(new EquipmentData { Id = id.Key, Rarity =ItemRarity.Common, Level = 1 });
		}

		playerData.Emoji.Add(GameId.EmojiAngry);
		playerData.Emoji.Add(GameId.EmojiLove);
		playerData.Emoji.Add(GameId.EmojiAngel);
		playerData.Emoji.Add(GameId.EmojiCool);
		playerData.Emoji.Add(GameId.EmojiSick);

		return playerData;
	}
}