using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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
using UnityEngine;
using UnityEngine.UI;

public partial class SROptions
{
#if DEVELOPMENT_BUILD
	[Category("Reset Player")]
	public void ResetPlayer()
	{
		PlayerPrefs.DeleteAll();
		PlayerPrefs.Save();

		var request = new ExecuteFunctionRequest
		{
			FunctionName = "SetupPlayerCommand",
			GeneratePlayStreamEvent = true,
			AuthenticationContext = PlayFabSettings.staticPlayer,
			FunctionParameter = new LogicRequest
			{
				Command = "SetupPlayerCommand",
				Data = new Dictionary<string, string>()
			}
		};

		PlayFabCloudScriptAPI.ExecuteFunction(request, null, GameCommandService.OnPlayFabError);
	}

	[Category("Equipment")]
	public void UnlockAllEquipment()
	{
		var services = MainInstaller.Resolve<IGameServices>();
		var gameLogic = MainInstaller.Resolve<IGameDataProvider>() as IGameLogic;
		var weaponConfigs = services.ConfigsProvider.GetConfigsList<QuantumWeaponConfig>();
		var gearConfigs = services.ConfigsProvider.GetConfigsList<QuantumGearConfig>();

		foreach (var config in weaponConfigs)
		{
			gameLogic.EquipmentLogic.AddToInventory(new Equipment(config.Id, rarity: EquipmentRarity.Legendary, level: 3));
		}

		foreach (var config in gearConfigs)
		{
			gameLogic.EquipmentLogic.AddToInventory(new Equipment(config.Id, rarity: EquipmentRarity.Legendary, level: 3));
		}

		((GameCommandService)services.CommandService).ForceServerDataUpdate();
	}
	
	[Category("Equipment")]
	public void UnlockEquipmentSet()
	{
		var services = MainInstaller.Resolve<IGameServices>();
		var gameLogic = MainInstaller.Resolve<IGameDataProvider>() as IGameLogic;
		var weaponConfigs = services.ConfigsProvider.GetConfigsList<QuantumWeaponConfig>();
		var gearConfigs = services.ConfigsProvider.GetConfigsList<QuantumGearConfig>();
		
		var eq1 = new Equipment(weaponConfigs[2].Id, rarity: EquipmentRarity.RarePlus, adjective: EquipmentAdjective.Regular, grade: EquipmentGrade.GradeV, durability: 96, level: 3);
		var eq2 = new Equipment(gearConfigs[0].Id, rarity: EquipmentRarity.Rare, adjective: EquipmentAdjective.Exquisite, grade: EquipmentGrade.GradeVI, durability: 62, level: 3);
		var eq3 = new Equipment(gearConfigs[4].Id, rarity: EquipmentRarity.Uncommon, adjective: EquipmentAdjective.Cool, grade: EquipmentGrade.GradeIII, durability: 51, level: 3);
		var eq4 = new Equipment(gearConfigs[5].Id, rarity: EquipmentRarity.Legendary, adjective: EquipmentAdjective.Royal, grade: EquipmentGrade.GradeI, durability: 43, level: 3);
		var eq5 = new Equipment(gearConfigs[6].Id, rarity: EquipmentRarity.LegendaryPlus, adjective: EquipmentAdjective.Divine, grade: EquipmentGrade.GradeIV, durability: 18, level: 3);
		
		gameLogic.EquipmentLogic.AddToInventory(eq1);
		gameLogic.EquipmentLogic.AddToInventory(eq2);
		gameLogic.EquipmentLogic.AddToInventory(eq3);
		gameLogic.EquipmentLogic.AddToInventory(eq4);
		gameLogic.EquipmentLogic.AddToInventory(eq5);
		
		((GameCommandService)services.CommandService).ForceServerDataUpdate();
	}

	[Category("Equipment")]
	public void UnlockOneEquipment()
	{
		var services = MainInstaller.Resolve<IGameServices>();
		var gameLogic = MainInstaller.Resolve<IGameDataProvider>() as IGameLogic;
		var weaponConfigs = services.ConfigsProvider.GetConfigsList<QuantumWeaponConfig>();

		gameLogic.EquipmentLogic.AddToInventory(new Equipment(weaponConfigs[1].Id, rarity: EquipmentRarity.Legendary, level: 3));

		((GameCommandService)services.CommandService).ForceServerDataUpdate();
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
		
		((GameCommandService)services.CommandService).ForceServerDataUpdate();
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