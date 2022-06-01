using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using FirstLight.FLogger;
using FirstLight.Game;
using FirstLight.Game.Commands;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Presenters;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using PlayFab;
using PlayFab.CloudScriptModels;
using Quantum;
using UnityEditor;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

public partial class SROptions
{
#if DEVELOPMENT_BUILD
#if ENABLE_PLAYFABADMIN_API
	[Category("Reset Player")]
	public void ResetPlayer()
	{
		var player = PlayFabSettings.staticPlayer;
		if (player == null || player.PlayFabId == null)
		{
			throw new Exception("Not logged in");
		}

		var update = new PlayFab.AdminModels.UpdateUserDataRequest()
		{
			KeysToRemove = new List<string>()
			{
				typeof(PlayerData).FullName,
				typeof(IdData).FullName,
				typeof(RngData).FullName,
				typeof(NftEquipmentData).FullName,
			},
			PlayFabId = player.PlayFabId
		};

		FLog.Verbose($"Wiping data for account {player.PlayFabId}");
		PlayFabAdminAPI.UpdateUserReadOnlyData(update, Result, GameCommandService.OnPlayFabError);

		void Result(PlayFab.AdminModels.UpdateUserDataResult result)
		{
			FLog.Verbose("Server Data Wiped. Re-login to re-build your game-data.");
#if UNITY_EDITOR
			if(EditorApplication.isPlaying) 
			{
				UnityEditor.EditorApplication.isPlaying = false;
			}
#endif
		}
	}
#endif

	[Category("Equipment")]
	public void GiveMaxedEquipment()
	{
		var services = MainInstaller.Resolve<IGameServices>();
		var gameLogic = (IGameLogic) MainInstaller.Resolve<IGameDataProvider>();

		var equipmentConfigs = services.ConfigsProvider.GetConfigsList<QuantumBaseEquipmentStatsConfig>();
		
		foreach (var config in equipmentConfigs)
		{
			if (config.Id == GameId.Hammer)
			{
				continue;
			}

			gameLogic.EquipmentLogic.AddToInventory(new Equipment(config.Id,
			                                                      EquipmentEdition.Genesis,
			                                                      EquipmentRarity.LegendaryPlus,
			                                                      EquipmentGrade.GradeI,
			                                                      EquipmentFaction.Dimensional,
			                                                      EquipmentAdjective.Divine,
			                                                      EquipmentMaterial.Golden,
			                                                      EquipmentManufacturer.Military,
			                                                      100,
			                                                      10,
			                                                      0,
			                                                      0,
			                                                      10
			                                                     ));
		}

		((GameCommandService) services.CommandService).ForceServerDataUpdate();
	}

	[Category("Equipment")]
	public void GiveBadEquipment()
	{
		var services = MainInstaller.Resolve<IGameServices>();
		var gameLogic = (IGameLogic) MainInstaller.Resolve<IGameDataProvider>();

		var equipmentConfigs = services.ConfigsProvider.GetConfigsList<QuantumBaseEquipmentStatsConfig>();
		
		foreach (var config in equipmentConfigs)
		{
			if (config.Id == GameId.Hammer)
			{
				continue;
			}

			gameLogic.EquipmentLogic.AddToInventory(new Equipment(config.Id));
		}

		((GameCommandService) services.CommandService).ForceServerDataUpdate();
	}

	/// <summary>
	/// This cheat can be be used to validate resource pool calculations, by receiving equipment used in the resource pool calculator:
	/// https://docs.google.com/spreadsheets/d/1LrHGwlNi2tbb7I8xmQVNCKKbc9YgEJjYyA8EFsIFarw/edit#gid=1028779545
	///
	/// Make sure to use RemoveAllEquipment cheat before using this one, if you want to test RP calculations.
	/// </summary>
	[Category("Equipment")]
	public void UnlockEquipmentSet()
	{
		var services = MainInstaller.Resolve<IGameServices>();
		var gameLogic = MainInstaller.Resolve<IGameDataProvider>() as IGameLogic;
		var equipmentConfigs = services.ConfigsProvider.GetConfigsList<QuantumBaseEquipmentStatsConfig>();

		gameLogic.EquipmentLogic.AddToInventory(new Equipment(equipmentConfigs[0].Id,
		                                                      rarity: EquipmentRarity.LegendaryPlus,
		                                                      adjective: EquipmentAdjective.Divine,
		                                                      grade: EquipmentGrade.GradeIV, durability: 18,
		                                                      level: 3));
		gameLogic.EquipmentLogic.AddToInventory(new Equipment(equipmentConfigs[4].Id, rarity: EquipmentRarity.Legendary,
		                                                      adjective: EquipmentAdjective.Royal,
		                                                      grade: EquipmentGrade.GradeI, durability: 43,
		                                                      level: 3));
		gameLogic.EquipmentLogic.AddToInventory(new Equipment(equipmentConfigs[5].Id, rarity: EquipmentRarity.Uncommon,
		                                                      adjective: EquipmentAdjective.Cool,
		                                                      grade: EquipmentGrade.GradeIII, durability: 51,
		                                                      level: 3));
		gameLogic.EquipmentLogic.AddToInventory(new Equipment(equipmentConfigs[6].Id, rarity: EquipmentRarity.Rare,
		                                                      adjective: EquipmentAdjective.Exquisite,
		                                                      grade: EquipmentGrade.GradeIII, durability: 62,
		                                                      level: 3));
		gameLogic.EquipmentLogic.AddToInventory(new Equipment(equipmentConfigs[26].Id, rarity: EquipmentRarity.RarePlus,
		                                                      adjective: EquipmentAdjective.Regular,
		                                                      grade: EquipmentGrade.GradeV, durability: 96,
		                                                      level: 3));

		((GameCommandService) services.CommandService).ForceServerDataUpdate();
	}

	[Category("Equipment")]
	public void UnlockOneEquipment()
	{
		var services = MainInstaller.Resolve<IGameServices>();
		var gameLogic = MainInstaller.Resolve<IGameDataProvider>() as IGameLogic;
		var equipmentConfigs = services.ConfigsProvider.GetConfigsList<QuantumBaseEquipmentStatsConfig>();

		var rand = Random.Range(0, equipmentConfigs.Count);

		gameLogic.EquipmentLogic.AddToInventory(new Equipment(equipmentConfigs[rand].Id,
		                                                      rarity: EquipmentRarity.Rare,
		                                                      adjective: EquipmentAdjective.Exquisite,
		                                                      grade: EquipmentGrade.GradeIII, durability: 75,
		                                                      level: 3));

		((GameCommandService) services.CommandService).ForceServerDataUpdate();
	}

	[Category("Equipment")]
	public void RemoveAllEquipment()
	{
		var services = MainInstaller.Resolve<IGameServices>();
		var gameLogic = MainInstaller.Resolve<IGameDataProvider>() as IGameLogic;

		var deletionKeys = new List<UniqueId>();

		deletionKeys.AddRange(gameLogic.EquipmentLogic.Inventory.ReadOnlyDictionary.Keys);

		foreach (var key in deletionKeys)
		{
			gameLogic.EquipmentLogic.RemoveFromInventory(key);
		}

		((GameCommandService) services.CommandService).ForceServerDataUpdate();
	}

	[Category("Marketing")]
	public void ToggleControllerGameUI()
	{
		var uiService = Object.FindObjectOfType<Main>().UiService;

		if (uiService.GetUi<MatchHudPresenter>().IsOpen)
		{
			uiService.CloseUi<MatchHudPresenter>();

			foreach (var renderer in uiService.GetUi<MatchControlsHudPresenter>().GetComponentsInChildren<Image>(true))
			{
				renderer.enabled = false;
			}
		}
		else
		{
			uiService.OpenUi<MatchHudPresenter>();

			foreach (var renderer in uiService.GetUi<MatchControlsHudPresenter>().GetComponentsInChildren<Image>(true))
			{
				renderer.enabled = true;
			}
		}
	}

	[Category("Progression")]
	private void AddXp(uint amount)
	{
		var dataProvider = MainInstaller.Resolve<IGameServices>().DataSaver as IDataService;
		var gameLogic = MainInstaller.Resolve<IGameDataProvider>() as IGameLogic;

		// TODO: Remove Logic outside command
		gameLogic.PlayerLogic.AddXp(amount);

		var data = new Dictionary<string, string>();
		ModelSerializer.SerializeToData(data, dataProvider.GetData<PlayerData>());

		var request = new ExecuteFunctionRequest
		{
			FunctionName = "ExecuteCommand",
			GeneratePlayStreamEvent = true,
			FunctionParameter = new LogicRequest
			{
				Command = "CheatAddXpCommand",
				Data = data
			},
			AuthenticationContext = PlayFabSettings.staticPlayer
		};

		PlayFabCloudScriptAPI.ExecuteFunction(request, null, GameCommandService.OnPlayFabError);
	}
#endif
}