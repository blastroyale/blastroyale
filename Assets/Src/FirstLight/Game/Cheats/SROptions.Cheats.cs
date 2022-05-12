using System.Collections.Generic;
using System.ComponentModel;
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
	[Category("Cheats")]
	public void Add100Sc()
	{
		var currencyLogic = MainInstaller.Resolve<IGameDataProvider>().CurrencyDataProvider as CurrencyLogic;
		
		currencyLogic.AddCurrency(GameId.SC, 100);
	}

	[Category("Cheats")]
	public void Add1000Sc()
	{
		var currencyLogic = MainInstaller.Resolve<IGameDataProvider>().CurrencyDataProvider as CurrencyLogic;
		
		currencyLogic.AddCurrency(GameId.SC, 1000);
	}

	[Category("Cheats")]
	public void Add100Hc()
	{
		var currencyLogic = MainInstaller.Resolve<IGameDataProvider>().CurrencyDataProvider as CurrencyLogic;
		
		currencyLogic.AddCurrency(GameId.HC, 100);
	}
	
	[Category("Cheats")]
	public void Add100Xp()
	{
		AddXp(100);
	}
	
	[Category("Cheats")]
	public void Add1000Xp()
	{
		AddXp(1000);
	}
	
	[Category("Cheats")]
	public void Add10000Xp()
	{
		AddXp(10000);
	}

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

	[Category("Cheats")]
	public void Add1Day()
	{
		var timeManipulator = MainInstaller.Resolve<IGameServices>().TimeService as ITimeManipulator;
		var timeNow = timeManipulator.DateTimeUtcNow;
			
		timeManipulator.AddTime((float) (timeNow.AddDays(1) - timeNow).TotalSeconds);
	}
		
	[Category("Cheats")]
	public void Add1Hour()
	{
		var timeManipulator = MainInstaller.Resolve<IGameServices>().TimeService as ITimeManipulator;
		var timeNow = timeManipulator.DateTimeUtcNow;
			
		timeManipulator.AddTime((float) (timeNow.AddHours(1) - timeNow).TotalSeconds);
	}
		
	[Category("Cheats")]
	public void Add1Minute()
	{
		var timeManipulator = MainInstaller.Resolve<IGameServices>().TimeService as ITimeManipulator;
		var timeNow = timeManipulator.DateTimeUtcNow;
			
		timeManipulator.AddTime((float) (timeNow.AddMinutes(1) - timeNow).TotalSeconds);
	}
		
	[Category("Cheats")]
	public void UnlockAllEquipment()
	{
		var services = MainInstaller.Resolve<IGameServices>();
		var gameLogic = MainInstaller.Resolve<IGameDataProvider>() as IGameLogic;
		var dataProvider = services.DataSaver as IDataService;
		var weaponConfigs = services.ConfigsProvider.GetConfigsList<QuantumWeaponConfig>();
		var gearConfigs = services.ConfigsProvider.GetConfigsList<QuantumGearConfig>();
		var converter = new StringEnumConverter();

		foreach (var config in weaponConfigs)
		{
			gameLogic.EquipmentLogic.AddToInventory(config.Id, ItemRarity.Common, 1);
		}
		
		foreach (var config in gearConfigs)
		{
			gameLogic.EquipmentLogic.AddToInventory(config.Id, config.StartingRarity, 1);
		}

		var data = new Dictionary<string, string>();
		ModelSerializer.SerializeToData(data, dataProvider.GetData<IdData>());
		ModelSerializer.SerializeToData(data, dataProvider.GetData<RngData>());
		ModelSerializer.SerializeToData(data, dataProvider.GetData<PlayerData>());
		var request = new ExecuteFunctionRequest
		{
			FunctionName = "ExecuteCommand",
			GeneratePlayStreamEvent = true,
			FunctionParameter = new LogicRequest
			{
				Command = "CheatUnlockAllEquipments",
				Data = data
			},
			AuthenticationContext = PlayFabSettings.staticPlayer
		};

		PlayFabCloudScriptAPI.ExecuteFunction(request, null, GameCommandService.OnPlayFabError);
	}
		
	[Category("Cheats")]
	public void AddManyLootBoxReward()
	{
		var lootCollected = new List<uint>();
		var services = MainInstaller.Resolve<IGameServices>();
		var configs = services.ConfigsProvider.GetConfigsList<LootBoxConfig>();
		var count = 10;//configs.Count;

		/*
		lootCollected.Add((uint) configs[1].Id);
		lootCollected.Add((uint) configs[2].Id);
		lootCollected.Add((uint) configs[3].Id);
		lootCollected.Add((uint) configs[4].Id);
		lootCollected.Add((uint) configs[5].Id);
		lootCollected.Add((uint) configs[1].Id);
		lootCollected.Add((uint) configs[2].Id);
		*/
		
		lootCollected.Add((uint) configs[4].Id);
		
		for (var i = 0; i < count; i++)
		{
			lootCollected.Add((uint) configs[0].Id);
		}
		
		services.CommandService.ExecuteCommand(new GameCompleteRewardsCommand
		{
			PlayerMatchData = new QuantumPlayerMatchData(),
		});
	}
	
	[Category("Cheats")]
	public void AddTimedLootBoxReward()
	{
		var lootCollected = new List<uint>();
		var services = MainInstaller.Resolve<IGameServices>();
		var configs = services.ConfigsProvider.GetConfigsList<LootBoxConfig>();

		lootCollected.Add((uint) configs[10].Id);
		
		services.CommandService.ExecuteCommand(new GameCompleteRewardsCommand
		{
			PlayerMatchData = new QuantumPlayerMatchData(),
		});
	}
	
	[Category("Cheats")]
	public void AddRelicLootBoxReward()
	{
		var lootCollected = new List<uint>();
		var services = MainInstaller.Resolve<IGameServices>();
		var configs = services.ConfigsProvider.GetConfigsList<LootBoxConfig>();

		lootCollected.Add((uint) configs[configs.Count - 6].Id);
		
		services.CommandService.ExecuteCommand(new GameCompleteRewardsCommand
		{
			PlayerMatchData = new QuantumPlayerMatchData(),
		});
	}
	
		
	[Category("Cheats")]
	public void AddCoinXpReward()
	{
		MainInstaller.Resolve<IGameServices>().CommandService.ExecuteCommand(new GameCompleteRewardsCommand
		{
			PlayerMatchData = new QuantumPlayerMatchData()// { EnemiesKilledCount = 10 },
		});
	}

	[Category("Cheats")]
	public void AwardCsFromPool()
	{
		IGameServices services = MainInstaller.Resolve<IGameServices>();
		ResourcePoolConfig poolConfig = services.ConfigsProvider.GetConfigsList<ResourcePoolConfig>()
		                                         .FirstOrDefault(x => x.Id == GameId.CS);
		
		services.CommandService.ExecuteCommand(new AwardFromResourcePoolCommand
		{
			PoolId = GameId.CS,
			PoolConfig = poolConfig,
			AmountToAward = 125
		});
	}
	
	[Category("Cheats")]
	public void RestockCsPool()
	{
		IGameServices services = MainInstaller.Resolve<IGameServices>();
		
		ResourcePoolConfig poolConfig = services.ConfigsProvider.GetConfigsList<ResourcePoolConfig>()
		                                        .FirstOrDefault(x => x.Id == GameId.CS);
		
		services.CommandService.ExecuteCommand(new RestockResourcePoolCommand
		{
			PoolId = GameId.CS,
			PoolConfig = poolConfig,
			ForceRestock = true
		});
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