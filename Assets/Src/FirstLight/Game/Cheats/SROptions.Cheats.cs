using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using FirstLight.FLogger;
using FirstLight.Game;
using FirstLight.Game.Data;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Logic.RPC;
using FirstLight.Game.Presenters;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Server.SDK.Modules;
using FirstLight.Services;
using PlayFab;
using Quantum;
using UnityEngine;
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
		var services = MainInstaller.Resolve<IGameServices>();
		var update = new PlayFab.AdminModels.UpdateUserDataRequest()
		{
			KeysToRemove = new List<string>()
			{
				typeof(PlayerData).FullName,
				typeof(IdData).FullName,
				typeof(RngData).FullName,
				typeof(EquipmentData).FullName,
			},
			PlayFabId = player.PlayFabId
		};

		FLog.Verbose($"Wiping data for account {player.PlayFabId}");
		PlayFabAdminAPI.UpdateUserReadOnlyData(update, Result, services.PlayfabService.HandleError);
		PlayerPrefs.DeleteAll();

		var deletionUrl =
			$"***REMOVED***/accounts/admin/unlink?key=devkey&playfabId={player.PlayFabId}";
		var task = new HttpClient().DeleteAsync(deletionUrl);
		task.Wait();
		FLog.Info("Wallet unlinked from marketplace");
		void Result(PlayFab.AdminModels.UpdateUserDataResult result)
		{
			FLog.Verbose("Server Data Wiped. Re-login to re-build your game-data.");
#if UNITY_EDITOR
			if(UnityEditor.EditorApplication.isPlaying) 
			{
				UnityEditor.EditorApplication.isPlaying = false;
			}
#endif
		}
	}
#endif
	/// <summary>
	/// This cheat can be be used to validate resource pool calculations, by receiving equipment used in the resource pool calculator:
	/// https://docs.google.com/spreadsheets/d/1LrHGwlNi2tbb7I8xmQVNCKKbc9YgEJjYyA8EFsIFarw/edit#gid=1028779545
	///
	/// Make sure to use RemoveAllEquipment cheat before using this one, if you want to test RP calculations.
	/// </summary>
	[Category("Equipment")]
	public void UnlockTestEquipment()
	{
		var services = MainInstaller.Resolve<IGameServices>();
		var gameLogic = MainInstaller.Resolve<IGameDataProvider>() as IGameLogic;
		var equipmentConfigs = services.ConfigsProvider.GetConfigsList<QuantumBaseEquipmentStatConfig>();

		gameLogic.EquipmentLogic.AddToInventory(new Equipment(equipmentConfigs[0].Id, rarity: EquipmentRarity.RarePlus,
		                                                      adjective: EquipmentAdjective.Regular,
		                                                      grade: EquipmentGrade.GradeV,
		                                                      level: 3));
		
		gameLogic.EquipmentLogic.AddToInventory(new Equipment(equipmentConfigs[5].Id, rarity: EquipmentRarity.Rare,
		                                                      adjective: EquipmentAdjective.Exquisite,
		                                                      grade: EquipmentGrade.GradeIII,
		                                                      level: 3));
		
		gameLogic.EquipmentLogic.AddToInventory(new Equipment(equipmentConfigs[12].Id, rarity: EquipmentRarity.Uncommon,
		                                                      adjective: EquipmentAdjective.Cool,
		                                                      grade: EquipmentGrade.GradeIII,
		                                                      level: 3));
		
		gameLogic.EquipmentLogic.AddToInventory(new Equipment(equipmentConfigs[17].Id, rarity: EquipmentRarity.Legendary,
		                                                      adjective: EquipmentAdjective.Royal,
		                                                      grade: EquipmentGrade.GradeI,
		                                                      level: 3));
		
		gameLogic.EquipmentLogic.AddToInventory(new Equipment(equipmentConfigs[40].Id,
		                                                      rarity: EquipmentRarity.LegendaryPlus,
		                                                      adjective: EquipmentAdjective.Divine,
		                                                      grade: EquipmentGrade.GradeIV,
		                                                      level: 3));
		
		((GameCommandService) services.CommandService).ForceServerDataUpdate();
	}

	/// <summary>
	/// This cheat helps to test all 2D sprites and 3D models for all equipment in the game
	/// </summary>
	[Category("Equipment")]
	public void UnlockAllEquipment()
	{
		var services = MainInstaller.Resolve<IGameServices>();
		var gameLogic = MainInstaller.Resolve<IGameDataProvider>() as IGameLogic;
		var equipmentConfigs = services.ConfigsProvider.GetConfigsList<QuantumBaseEquipmentStatConfig>();

		for (var i = 0; i < equipmentConfigs.Count; i++)
		{
			gameLogic.EquipmentLogic.AddToInventory(new Equipment(equipmentConfigs[i].Id,
			                                                      rarity: EquipmentRarity.Epic,
			                                                      adjective: EquipmentAdjective.Exquisite,
			                                                      grade: EquipmentGrade.GradeIII, maxDurability:100,
			                                                      level: 3));
		}

		((GameCommandService) services.CommandService).ForceServerDataUpdate();
	}

	[Category("Equipment")]
	public void GiveMaxArmourBuildEquipment()
	{
		var services = MainInstaller.Resolve<IGameServices>();
		var gameLogic = MainInstaller.Resolve<IGameDataProvider>() as IGameLogic;

		gameLogic!.EquipmentLogic.AddToInventory(new Equipment(GameId.BaseballHelmet,
		                                                       material: EquipmentMaterial.Golden,
		                                                       faction: EquipmentFaction.Celestial,
		                                                       adjective: EquipmentAdjective.Divine,
		                                                       rarity: EquipmentRarity.LegendaryPlus,
		                                                       level: 35,
		                                                       grade: EquipmentGrade.GradeI));
		gameLogic!.EquipmentLogic.AddToInventory(new Equipment(GameId.SoldierArmor,
		                                                       material: EquipmentMaterial.Golden,
		                                                       faction: EquipmentFaction.Celestial,
		                                                       adjective: EquipmentAdjective.Divine,
		                                                       rarity: EquipmentRarity.LegendaryPlus,
		                                                       level: 35,
		                                                       grade: EquipmentGrade.GradeI));
		gameLogic!.EquipmentLogic.AddToInventory(new Equipment(GameId.SoldierShield,
		                                                       material: EquipmentMaterial.Golden,
		                                                       faction: EquipmentFaction.Celestial,
		                                                       adjective: EquipmentAdjective.Divine,
		                                                       rarity: EquipmentRarity.LegendaryPlus,
		                                                       level: 35,
		                                                       grade: EquipmentGrade.GradeI));
		gameLogic!.EquipmentLogic.AddToInventory(new Equipment(GameId.SoldierAmulet,
		                                                       material: EquipmentMaterial.Golden,
		                                                       faction: EquipmentFaction.Celestial,
		                                                       adjective: EquipmentAdjective.Divine,
		                                                       rarity: EquipmentRarity.LegendaryPlus,
		                                                       level: 35,
		                                                       grade: EquipmentGrade.GradeI));

		((GameCommandService) services.CommandService).ForceServerDataUpdate();
	}

	[Category("Equipment")]
	public void GiveMaxHealthBuildEquipment()
	{
		var services = MainInstaller.Resolve<IGameServices>();
		var gameLogic = MainInstaller.Resolve<IGameDataProvider>() as IGameLogic;

		gameLogic!.EquipmentLogic.AddToInventory(new Equipment(GameId.MausHelmet,
															   material: EquipmentMaterial.Golden,
															   faction: EquipmentFaction.Organic,
															   adjective: EquipmentAdjective.Divine,
															   rarity: EquipmentRarity.LegendaryPlus,
															   level: 35,
															   grade: EquipmentGrade.GradeI));
		gameLogic!.EquipmentLogic.AddToInventory(new Equipment(GameId.SoldierArmor,
															   material: EquipmentMaterial.Golden,
															   faction: EquipmentFaction.Organic,
															   adjective: EquipmentAdjective.Divine,
															   rarity: EquipmentRarity.LegendaryPlus,
															   level: 35,
															   grade: EquipmentGrade.GradeI));
		gameLogic!.EquipmentLogic.AddToInventory(new Equipment(GameId.SoldierShield,
															   material: EquipmentMaterial.Golden,
															   faction: EquipmentFaction.Organic,
															   adjective: EquipmentAdjective.Divine,
															   rarity: EquipmentRarity.LegendaryPlus,
															   level: 35,
															   grade: EquipmentGrade.GradeI));
		gameLogic!.EquipmentLogic.AddToInventory(new Equipment(GameId.MouseAmulet,
															   material: EquipmentMaterial.Golden,
															   faction: EquipmentFaction.Organic,
															   adjective: EquipmentAdjective.Divine,
															   rarity: EquipmentRarity.LegendaryPlus,
															   level: 35,
															   grade: EquipmentGrade.GradeI));

		((GameCommandService)services.CommandService).ForceServerDataUpdate();
	}

	[Category("Equipment")]
	public void GiveMaxShieldsBuildEquipment()
	{
		var services = MainInstaller.Resolve<IGameServices>();
		var gameLogic = MainInstaller.Resolve<IGameDataProvider>() as IGameLogic;

		gameLogic!.EquipmentLogic.AddToInventory(new Equipment(GameId.MausHelmet,
															   material: EquipmentMaterial.Golden,
															   faction: EquipmentFaction.Shadow,
															   adjective: EquipmentAdjective.Divine,
															   rarity: EquipmentRarity.LegendaryPlus,
															   level: 35,
															   grade: EquipmentGrade.GradeI));
		gameLogic!.EquipmentLogic.AddToInventory(new Equipment(GameId.SoldierArmor,
															   material: EquipmentMaterial.Golden,
															   faction: EquipmentFaction.Shadow,
															   adjective: EquipmentAdjective.Divine,
															   rarity: EquipmentRarity.LegendaryPlus,
															   level: 35,
															   grade: EquipmentGrade.GradeI));
		gameLogic!.EquipmentLogic.AddToInventory(new Equipment(GameId.SoldierShield,
															   material: EquipmentMaterial.Golden,
															   faction: EquipmentFaction.Shadow,
															   adjective: EquipmentAdjective.Divine,
															   rarity: EquipmentRarity.LegendaryPlus,
															   level: 35,
															   grade: EquipmentGrade.GradeI));
		gameLogic!.EquipmentLogic.AddToInventory(new Equipment(GameId.WarriorAmulet,
															   material: EquipmentMaterial.Golden,
															   faction: EquipmentFaction.Shadow,
															   adjective: EquipmentAdjective.Divine,
															   rarity: EquipmentRarity.LegendaryPlus,
															   level: 35,
															   grade: EquipmentGrade.GradeI));

		((GameCommandService)services.CommandService).ForceServerDataUpdate();
	}

	[Category("Equipment")]
	public void GiveMaxSpeedBuildEquipment()
	{
		var services = MainInstaller.Resolve<IGameServices>();
		var gameLogic = MainInstaller.Resolve<IGameDataProvider>() as IGameLogic;

		gameLogic!.EquipmentLogic.AddToInventory(new Equipment(GameId.FootballHelmet,
															   material: EquipmentMaterial.Golden,
															   faction: EquipmentFaction.Chaos,
															   adjective: EquipmentAdjective.Divine,
															   rarity: EquipmentRarity.LegendaryPlus,
															   level: 35,
															   grade: EquipmentGrade.GradeI));
		gameLogic!.EquipmentLogic.AddToInventory(new Equipment(GameId.FootballArmor,
															   material: EquipmentMaterial.Golden,
															   faction: EquipmentFaction.Chaos,
															   adjective: EquipmentAdjective.Divine,
															   rarity: EquipmentRarity.LegendaryPlus,
															   level: 35,
															   grade: EquipmentGrade.GradeI));
		gameLogic!.EquipmentLogic.AddToInventory(new Equipment(GameId.RoadShield,
															   material: EquipmentMaterial.Golden,
															   faction: EquipmentFaction.Chaos,
															   adjective: EquipmentAdjective.Divine,
															   rarity: EquipmentRarity.LegendaryPlus,
															   level: 35,
															   grade: EquipmentGrade.GradeI));
		gameLogic!.EquipmentLogic.AddToInventory(new Equipment(GameId.TikTokAmulet,
															   material: EquipmentMaterial.Golden,
															   faction: EquipmentFaction.Chaos,
															   adjective: EquipmentAdjective.Divine,
															   rarity: EquipmentRarity.LegendaryPlus,
															   level: 35,
															   grade: EquipmentGrade.GradeI));

		((GameCommandService)services.CommandService).ForceServerDataUpdate();
	}

	[Category("Equipment")]
	public void GiveMaxAttackBuildEquipment()
	{
		var services = MainInstaller.Resolve<IGameServices>();
		var gameLogic = MainInstaller.Resolve<IGameDataProvider>() as IGameLogic;

		gameLogic!.EquipmentLogic.AddToInventory(new Equipment(GameId.HockeyHelmet,
		                                                       material: EquipmentMaterial.Golden,
		                                                       faction: EquipmentFaction.Dimensional,
		                                                       adjective: EquipmentAdjective.Divine,
		                                                       rarity: EquipmentRarity.LegendaryPlus,
		                                                       level: 35,
		                                                       grade: EquipmentGrade.GradeI));
		gameLogic!.EquipmentLogic.AddToInventory(new Equipment(GameId.BaseballArmor,
		                                                       material: EquipmentMaterial.Golden,
		                                                       faction: EquipmentFaction.Dimensional,
		                                                       adjective: EquipmentAdjective.Divine,
		                                                       rarity: EquipmentRarity.LegendaryPlus,
		                                                       level: 35,
		                                                       grade: EquipmentGrade.GradeI));
		gameLogic!.EquipmentLogic.AddToInventory(new Equipment(GameId.RiotShield,
		                                                       material: EquipmentMaterial.Golden,
		                                                       faction: EquipmentFaction.Dimensional,
		                                                       adjective: EquipmentAdjective.Divine,
		                                                       rarity: EquipmentRarity.LegendaryPlus,
		                                                       level: 35,
		                                                       grade: EquipmentGrade.GradeI));
		gameLogic!.EquipmentLogic.AddToInventory(new Equipment(GameId.RiotAmulet,
		                                                       material: EquipmentMaterial.Golden,
		                                                       faction: EquipmentFaction.Organic,
		                                                       adjective: EquipmentAdjective.Divine,
		                                                       rarity: EquipmentRarity.LegendaryPlus,
		                                                       level: 35,
		                                                       grade: EquipmentGrade.GradeI));

		((GameCommandService) services.CommandService).ForceServerDataUpdate();
	}

	[Category("Equipment")]
	public void GiveMaxAttackRangeBuildEquipment()
	{
		var services = MainInstaller.Resolve<IGameServices>();
		var gameLogic = MainInstaller.Resolve<IGameDataProvider>() as IGameLogic;

		gameLogic!.EquipmentLogic.AddToInventory(new Equipment(GameId.BaseballHelmet,
		                                                       material: EquipmentMaterial.Golden,
		                                                       faction: EquipmentFaction.Shadow,
		                                                       adjective: EquipmentAdjective.Divine,
		                                                       rarity: EquipmentRarity.LegendaryPlus,
		                                                       level: 35,
		                                                       grade: EquipmentGrade.GradeI));
		gameLogic!.EquipmentLogic.AddToInventory(new Equipment(GameId.BaseballArmor,
		                                                       material: EquipmentMaterial.Golden,
		                                                       faction: EquipmentFaction.Organic,
		                                                       adjective: EquipmentAdjective.Divine,
		                                                       rarity: EquipmentRarity.LegendaryPlus,
		                                                       level: 35,
		                                                       grade: EquipmentGrade.GradeI));
		gameLogic!.EquipmentLogic.AddToInventory(new Equipment(GameId.MouseShield,
		                                                       material: EquipmentMaterial.Golden,
		                                                       faction: EquipmentFaction.Shadow,
		                                                       adjective: EquipmentAdjective.Divine,
		                                                       rarity: EquipmentRarity.LegendaryPlus,
		                                                       level: 35,
		                                                       grade: EquipmentGrade.GradeI));
		gameLogic!.EquipmentLogic.AddToInventory(new Equipment(GameId.WarriorAmulet,
		                                                       material: EquipmentMaterial.Golden,
		                                                       faction: EquipmentFaction.Shadow,
		                                                       adjective: EquipmentAdjective.Divine,
		                                                       rarity: EquipmentRarity.LegendaryPlus,
		                                                       level: 35,
		                                                       grade: EquipmentGrade.GradeI));

		((GameCommandService) services.CommandService).ForceServerDataUpdate();
	}

	[Category("Equipment")]
	public void GiveMaxPickupSpeedBuildEquipment()
	{
		var services = MainInstaller.Resolve<IGameServices>();
		var gameLogic = MainInstaller.Resolve<IGameDataProvider>() as IGameLogic;

		gameLogic!.EquipmentLogic.AddToInventory(new Equipment(GameId.RoadHelmet,
															   material: EquipmentMaterial.Golden,
															   faction: EquipmentFaction.Dimensional,
															   adjective: EquipmentAdjective.Divine,
															   rarity: EquipmentRarity.LegendaryPlus,
															   level: 35,
															   grade: EquipmentGrade.GradeI));
		gameLogic!.EquipmentLogic.AddToInventory(new Equipment(GameId.MouseArmor,
															   material: EquipmentMaterial.Golden,
															   faction: EquipmentFaction.Chaos,
															   adjective: EquipmentAdjective.Divine,
															   rarity: EquipmentRarity.LegendaryPlus,
															   level: 35,
															   grade: EquipmentGrade.GradeI));
		gameLogic!.EquipmentLogic.AddToInventory(new Equipment(GameId.RoadShield,
															   material: EquipmentMaterial.Golden,
															   faction: EquipmentFaction.Dimensional,
															   adjective: EquipmentAdjective.Divine,
															   rarity: EquipmentRarity.LegendaryPlus,
															   level: 35,
															   grade: EquipmentGrade.GradeI));
		gameLogic!.EquipmentLogic.AddToInventory(new Equipment(GameId.TikTokAmulet,
															   material: EquipmentMaterial.Golden,
															   faction: EquipmentFaction.Dimensional,
															   adjective: EquipmentAdjective.Divine,
															   rarity: EquipmentRarity.LegendaryPlus,
															   level: 35,
															   grade: EquipmentGrade.GradeI));

		((GameCommandService)services.CommandService).ForceServerDataUpdate();
	}

	[Category("Equipment")]
	public void UnlockOneEquipment()
	{
		var services = MainInstaller.Resolve<IGameServices>();
		var gameLogic = MainInstaller.Resolve<IGameDataProvider>() as IGameLogic;
		var equipmentConfigs = services.ConfigsProvider.GetConfigsList<QuantumBaseEquipmentStatConfig>();

		var rand = Random.Range(0, equipmentConfigs.Count);

		gameLogic.EquipmentLogic.AddToInventory(new Equipment(equipmentConfigs[rand].Id,
		                                                      rarity: EquipmentRarity.Epic,
		                                                      adjective: EquipmentAdjective.Exquisite,
		                                                      grade: EquipmentGrade.GradeIII, maxDurability:100,
		                                                      level: 3));

		((GameCommandService) services.CommandService).ForceServerDataUpdate();
	}

	[Category("Equipment")]
	public void SetAllEquipmentNew()
	{
		var gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();

		gameDataProvider.UniqueIdDataProvider.NewIds.Clear();
		foreach (var (id, _) in gameDataProvider.EquipmentDataProvider.Inventory)
		{
			gameDataProvider.UniqueIdDataProvider.NewIds.Add(id);
		}
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
		var services = MainInstaller.Resolve<IGameServices>();
		
		// TODO: Remove Logic outside command
		gameLogic.PlayerLogic.AddXp(amount);

		var data = new Dictionary<string, string>();
		ModelSerializer.SerializeToData(data, dataProvider.GetData<PlayerData>());
		services.PlayfabService.CallFunction("ExecuteCommand", null, null,new LogicRequest
		{
			Command = "CheatAddXpCommand",
			Data = data
		});
	}
	
	[Category("Progression")]
	public void Add5CS()
	{
		var gameLogic = MainInstaller.Resolve<IGameDataProvider>() as IGameLogic;
		var services = MainInstaller.Resolve<IGameServices>();
		
		gameLogic.CurrencyLogic.AddCurrency(GameId.CS, 5);

		//((GameCommandService) services.CommandService).ForceServerDataUpdate();
	}
	
	[Category("Progression")]
	public void Add5BLST()
	{
		var gameLogic = MainInstaller.Resolve<IGameDataProvider>() as IGameLogic;
		var services = MainInstaller.Resolve<IGameServices>();
		
		gameLogic.CurrencyLogic.AddCurrency(GameId.BLST, 5);

		//((GameCommandService) services.CommandService).ForceServerDataUpdate();
	}
	
	[Category("Progression")]
	public void Add5BPP()
	{
		var gameLogic = MainInstaller.Resolve<IGameDataProvider>() as IGameLogic;
		var services = MainInstaller.Resolve<IGameServices>();
		
		gameLogic.BattlePassLogic.AddBPP(5);

		((GameCommandService) services.CommandService).ForceServerDataUpdate();
	}
	
	[Category("Progression")]
	public void Add25BPP()
	{
		var gameLogic = MainInstaller.Resolve<IGameDataProvider>() as IGameLogic;
		var services = MainInstaller.Resolve<IGameServices>();
		
		gameLogic.BattlePassLogic.AddBPP(25);

		((GameCommandService) services.CommandService).ForceServerDataUpdate();
	}
	
	[Category("Progression")]
	public void RedeemBpp()
	{
		var gameLogic = MainInstaller.Resolve<IGameDataProvider>() as IGameLogic;
		var services = MainInstaller.Resolve<IGameServices>();

		gameLogic.BattlePassLogic.RedeemBPP(out var _r, out var _l);

		((GameCommandService) services.CommandService).ForceServerDataUpdate();
	}
	
	[Category("Progression")]
	public void ResetBpp()
	{
		var services = MainInstaller.Resolve<IGameServices>();
		var dataProvider = services.DataSaver as IDataService;
		var playerData = dataProvider.GetData<PlayerData>();

		playerData.BPLevel = 0;
		playerData.BPPoints = 0;
		
		dataProvider.SaveData<PlayerData>();

		((GameCommandService) services.CommandService).ForceServerDataUpdate();
	}

	[Category("Progression")]
	public bool IsCollectedOverride
	{
		get => DebugUtils.DebugFlags.OverrideCurrencyChangedIsCollecting;
		set => DebugUtils.DebugFlags.OverrideCurrencyChangedIsCollecting = value;
	}
	
	[Category("Logging")]
	public void LogCurrentRoomInfo()
	{
		var services = MainInstaller.Resolve<IGameServices>();
		var room = services.NetworkService.QuantumClient.CurrentRoom;
		
		if (room == null)
		{
			return;
		}
		
		var roomProps = (string) "";

		foreach (var prop in room.CustomProperties)
		{
			roomProps += $"{prop.Key}: {prop.Value}\n";
		}
		
		Debug.Log($"-NETWORK INFO-\n" +
		          $"Lobby Name: {services.NetworkService.QuantumClient.CurrentLobby?.Name}\n" +
		          $"Room Name: {room.Name}\n" +
		          $"Player Count: {room.Players.Count}\n" +
		          $"Is Open: {room.IsOpen}\n" +
		          $"Is Visible: {room.IsVisible}\n" + 
		          $"-----\n" + 
		          $"Custom Props:\n" + roomProps +
		          $"-----\n");
	}
#endif
}