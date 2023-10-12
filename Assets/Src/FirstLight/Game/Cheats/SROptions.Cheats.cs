using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using FirstLight.FLogger;
using FirstLight.Game;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Logic.RPC;
using FirstLight.Game.Messages;
using FirstLight.Game.Presenters;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Server.SDK.Modules;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using FirstLight.Services;
using Photon.Realtime;
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
		PlayFabAdminAPI.UpdateUserReadOnlyData(update, Result, null);
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
			if (UnityEditor.EditorApplication.isPlaying)
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
			level: 3,
			lastRepairTimestamp: DateTime.UtcNow.Ticks));

		gameLogic.EquipmentLogic.AddToInventory(new Equipment(equipmentConfigs[5].Id, rarity: EquipmentRarity.Rare,
			adjective: EquipmentAdjective.Exquisite,
			grade: EquipmentGrade.GradeIII,
			level: 3,
			lastRepairTimestamp: DateTime.UtcNow.Ticks));

		gameLogic.EquipmentLogic.AddToInventory(new Equipment(equipmentConfigs[12].Id, rarity: EquipmentRarity.Uncommon,
			adjective: EquipmentAdjective.Cool,
			grade: EquipmentGrade.GradeIII,
			level: 3,
			lastRepairTimestamp: DateTime.UtcNow.Ticks));

		gameLogic.EquipmentLogic.AddToInventory(new Equipment(equipmentConfigs[17].Id, rarity: EquipmentRarity.Legendary,
			adjective: EquipmentAdjective.Royal,
			grade: EquipmentGrade.GradeI,
			level: 3,
			lastRepairTimestamp: DateTime.UtcNow.Ticks));

		gameLogic.EquipmentLogic.AddToInventory(new Equipment(equipmentConfigs[40].Id,
			rarity: EquipmentRarity.LegendaryPlus,
			adjective: EquipmentAdjective.Divine,
			grade: EquipmentGrade.GradeIV,
			level: 3,
			lastRepairTimestamp: DateTime.UtcNow.Ticks));

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
			if (equipmentConfigs[i].Id == Equipment.DefaultWeapon)
			{
				continue;
			}

			gameLogic.EquipmentLogic.AddToInventory(new Equipment(equipmentConfigs[i].Id,
				rarity: EquipmentRarity.Epic,
				adjective: EquipmentAdjective.Exquisite,
				grade: EquipmentGrade.GradeIII, maxDurability: 100,
				level: 3,
				lastRepairTimestamp: DateTime.UtcNow.Ticks));
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
			faction: EquipmentFaction.Order,
			adjective: EquipmentAdjective.Divine,
			rarity: EquipmentRarity.LegendaryPlus,
			level: 35,
			grade: EquipmentGrade.GradeI,
			lastRepairTimestamp: DateTime.UtcNow.Ticks));
		gameLogic!.EquipmentLogic.AddToInventory(new Equipment(GameId.SoldierArmor,
			material: EquipmentMaterial.Golden,
			faction: EquipmentFaction.Order,
			adjective: EquipmentAdjective.Divine,
			rarity: EquipmentRarity.LegendaryPlus,
			level: 35,
			grade: EquipmentGrade.GradeI,
			lastRepairTimestamp: DateTime.UtcNow.Ticks));
		gameLogic!.EquipmentLogic.AddToInventory(new Equipment(GameId.SoldierShield,
			material: EquipmentMaterial.Golden,
			faction: EquipmentFaction.Order,
			adjective: EquipmentAdjective.Divine,
			rarity: EquipmentRarity.LegendaryPlus,
			level: 35,
			grade: EquipmentGrade.GradeI,
			lastRepairTimestamp: DateTime.UtcNow.Ticks));
		gameLogic!.EquipmentLogic.AddToInventory(new Equipment(GameId.SoldierAmulet,
			material: EquipmentMaterial.Golden,
			faction: EquipmentFaction.Order,
			adjective: EquipmentAdjective.Divine,
			rarity: EquipmentRarity.LegendaryPlus,
			level: 35,
			grade: EquipmentGrade.GradeI,
			lastRepairTimestamp: DateTime.UtcNow.Ticks));

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
			grade: EquipmentGrade.GradeI,
			lastRepairTimestamp: DateTime.UtcNow.Ticks));
		gameLogic!.EquipmentLogic.AddToInventory(new Equipment(GameId.MouseArmor,
			material: EquipmentMaterial.Golden,
			faction: EquipmentFaction.Organic,
			adjective: EquipmentAdjective.Divine,
			rarity: EquipmentRarity.LegendaryPlus,
			level: 35,
			grade: EquipmentGrade.GradeI,
			lastRepairTimestamp: DateTime.UtcNow.Ticks));
		gameLogic!.EquipmentLogic.AddToInventory(new Equipment(GameId.MouseShield,
			material: EquipmentMaterial.Golden,
			faction: EquipmentFaction.Organic,
			adjective: EquipmentAdjective.Divine,
			rarity: EquipmentRarity.LegendaryPlus,
			level: 35,
			grade: EquipmentGrade.GradeI,
			lastRepairTimestamp: DateTime.UtcNow.Ticks));
		gameLogic!.EquipmentLogic.AddToInventory(new Equipment(GameId.MouseAmulet,
			material: EquipmentMaterial.Golden,
			faction: EquipmentFaction.Organic,
			adjective: EquipmentAdjective.Divine,
			rarity: EquipmentRarity.LegendaryPlus,
			level: 35,
			grade: EquipmentGrade.GradeI,
			lastRepairTimestamp: DateTime.UtcNow.Ticks));

		((GameCommandService) services.CommandService).ForceServerDataUpdate();
	}

	[Category("Equipment")]
	public void GiveMaxShieldsBuildEquipment()
	{
		var services = MainInstaller.Resolve<IGameServices>();
		var gameLogic = MainInstaller.Resolve<IGameDataProvider>() as IGameLogic;

		gameLogic!.EquipmentLogic.AddToInventory(new Equipment(GameId.HockeyHelmet,
			material: EquipmentMaterial.Golden,
			faction: EquipmentFaction.Shadow,
			adjective: EquipmentAdjective.Divine,
			rarity: EquipmentRarity.LegendaryPlus,
			level: 35,
			grade: EquipmentGrade.GradeI,
			lastRepairTimestamp: DateTime.UtcNow.Ticks));
		gameLogic!.EquipmentLogic.AddToInventory(new Equipment(GameId.SoldierArmor,
			material: EquipmentMaterial.Golden,
			faction: EquipmentFaction.Shadow,
			adjective: EquipmentAdjective.Divine,
			rarity: EquipmentRarity.LegendaryPlus,
			level: 35,
			grade: EquipmentGrade.GradeI,
			lastRepairTimestamp: DateTime.UtcNow.Ticks));
		gameLogic!.EquipmentLogic.AddToInventory(new Equipment(GameId.SoldierShield,
			material: EquipmentMaterial.Golden,
			faction: EquipmentFaction.Shadow,
			adjective: EquipmentAdjective.Divine,
			rarity: EquipmentRarity.LegendaryPlus,
			level: 35,
			grade: EquipmentGrade.GradeI,
			lastRepairTimestamp: DateTime.UtcNow.Ticks));
		gameLogic!.EquipmentLogic.AddToInventory(new Equipment(GameId.SoldierAmulet,
			material: EquipmentMaterial.Golden,
			faction: EquipmentFaction.Shadow,
			adjective: EquipmentAdjective.Divine,
			rarity: EquipmentRarity.LegendaryPlus,
			level: 35,
			grade: EquipmentGrade.GradeI,
			lastRepairTimestamp: DateTime.UtcNow.Ticks));

		((GameCommandService) services.CommandService).ForceServerDataUpdate();
	}

	[Category("Equipment")]
	public void GiveMaxSpeedBuildEquipment()
	{
		var services = MainInstaller.Resolve<IGameServices>();
		var gameLogic = MainInstaller.Resolve<IGameDataProvider>() as IGameLogic;

		gameLogic!.EquipmentLogic.AddToInventory(new Equipment(GameId.FootballHelmet,
			material: EquipmentMaterial.Golden,
			faction: EquipmentFaction.Dimensional,
			adjective: EquipmentAdjective.Divine,
			rarity: EquipmentRarity.LegendaryPlus,
			level: 35,
			grade: EquipmentGrade.GradeI,
			lastRepairTimestamp: DateTime.UtcNow.Ticks));
		gameLogic!.EquipmentLogic.AddToInventory(new Equipment(GameId.FootballArmor,
			material: EquipmentMaterial.Golden,
			faction: EquipmentFaction.Dimensional,
			adjective: EquipmentAdjective.Divine,
			rarity: EquipmentRarity.LegendaryPlus,
			level: 35,
			grade: EquipmentGrade.GradeI,
			lastRepairTimestamp: DateTime.UtcNow.Ticks));
		gameLogic!.EquipmentLogic.AddToInventory(new Equipment(GameId.RoadShield,
			material: EquipmentMaterial.Golden,
			faction: EquipmentFaction.Dimensional,
			adjective: EquipmentAdjective.Divine,
			rarity: EquipmentRarity.LegendaryPlus,
			level: 35,
			grade: EquipmentGrade.GradeI,
			lastRepairTimestamp: DateTime.UtcNow.Ticks));
		gameLogic!.EquipmentLogic.AddToInventory(new Equipment(GameId.TikTokAmulet,
			material: EquipmentMaterial.Golden,
			faction: EquipmentFaction.Dimensional,
			adjective: EquipmentAdjective.Divine,
			rarity: EquipmentRarity.LegendaryPlus,
			level: 35,
			grade: EquipmentGrade.GradeI,
			lastRepairTimestamp: DateTime.UtcNow.Ticks));

		((GameCommandService) services.CommandService).ForceServerDataUpdate();
	}

	[Category("Equipment")]
	public void GiveMaxAttackBuildEquipment()
	{
		var services = MainInstaller.Resolve<IGameServices>();
		var gameLogic = MainInstaller.Resolve<IGameDataProvider>() as IGameLogic;

		gameLogic!.EquipmentLogic.AddToInventory(new Equipment(GameId.RiotHelmet,
			material: EquipmentMaterial.Golden,
			faction: EquipmentFaction.Dimensional,
			adjective: EquipmentAdjective.Divine,
			rarity: EquipmentRarity.LegendaryPlus,
			level: 35,
			grade: EquipmentGrade.GradeI,
			lastRepairTimestamp: DateTime.UtcNow.Ticks));
		gameLogic!.EquipmentLogic.AddToInventory(new Equipment(GameId.RiotArmor,
			material: EquipmentMaterial.Golden,
			faction: EquipmentFaction.Dimensional,
			adjective: EquipmentAdjective.Divine,
			rarity: EquipmentRarity.LegendaryPlus,
			level: 35,
			grade: EquipmentGrade.GradeI,
			lastRepairTimestamp: DateTime.UtcNow.Ticks));
		gameLogic!.EquipmentLogic.AddToInventory(new Equipment(GameId.RiotShield,
			material: EquipmentMaterial.Golden,
			faction: EquipmentFaction.Dimensional,
			adjective: EquipmentAdjective.Divine,
			rarity: EquipmentRarity.LegendaryPlus,
			level: 35,
			grade: EquipmentGrade.GradeI,
			lastRepairTimestamp: DateTime.UtcNow.Ticks));
		gameLogic!.EquipmentLogic.AddToInventory(new Equipment(GameId.RiotAmulet,
			material: EquipmentMaterial.Golden,
			faction: EquipmentFaction.Organic,
			adjective: EquipmentAdjective.Divine,
			rarity: EquipmentRarity.LegendaryPlus,
			level: 35,
			grade: EquipmentGrade.GradeI,
			lastRepairTimestamp: DateTime.UtcNow.Ticks));

		((GameCommandService) services.CommandService).ForceServerDataUpdate();
	}

	[Category("Equipment")]
	public void GiveMaxAttackRangeBuildEquipment()
	{
		var services = MainInstaller.Resolve<IGameServices>();
		var gameLogic = MainInstaller.Resolve<IGameDataProvider>() as IGameLogic;

		gameLogic!.EquipmentLogic.AddToInventory(new Equipment(GameId.BaseballHelmet,
			material: EquipmentMaterial.Golden,
			faction: EquipmentFaction.Organic,
			adjective: EquipmentAdjective.Divine,
			rarity: EquipmentRarity.LegendaryPlus,
			level: 35,
			grade: EquipmentGrade.GradeI,
			lastRepairTimestamp: DateTime.UtcNow.Ticks));
		gameLogic!.EquipmentLogic.AddToInventory(new Equipment(GameId.BaseballArmor,
			material: EquipmentMaterial.Golden,
			faction: EquipmentFaction.Organic,
			adjective: EquipmentAdjective.Divine,
			rarity: EquipmentRarity.LegendaryPlus,
			level: 35,
			grade: EquipmentGrade.GradeI,
			lastRepairTimestamp: DateTime.UtcNow.Ticks));
		gameLogic!.EquipmentLogic.AddToInventory(new Equipment(GameId.MouseShield,
			material: EquipmentMaterial.Golden,
			faction: EquipmentFaction.Organic,
			adjective: EquipmentAdjective.Divine,
			rarity: EquipmentRarity.LegendaryPlus,
			level: 35,
			grade: EquipmentGrade.GradeI,
			lastRepairTimestamp: DateTime.UtcNow.Ticks));
		gameLogic!.EquipmentLogic.AddToInventory(new Equipment(GameId.WarriorAmulet,
			material: EquipmentMaterial.Golden,
			faction: EquipmentFaction.Organic,
			adjective: EquipmentAdjective.Divine,
			rarity: EquipmentRarity.LegendaryPlus,
			level: 35,
			grade: EquipmentGrade.GradeI,
			lastRepairTimestamp: DateTime.UtcNow.Ticks));

		((GameCommandService) services.CommandService).ForceServerDataUpdate();
	}

	[Category("Equipment")]
	public void GiveMaxPickupSpeedBuildEquipment()
	{
		var services = MainInstaller.Resolve<IGameServices>();
		var gameLogic = MainInstaller.Resolve<IGameDataProvider>() as IGameLogic;

		gameLogic!.EquipmentLogic.AddToInventory(new Equipment(GameId.RoadHelmet,
			material: EquipmentMaterial.Golden,
			faction: EquipmentFaction.Shadow,
			adjective: EquipmentAdjective.Divine,
			rarity: EquipmentRarity.LegendaryPlus,
			level: 35,
			grade: EquipmentGrade.GradeI,
			lastRepairTimestamp: DateTime.UtcNow.Ticks));
		gameLogic!.EquipmentLogic.AddToInventory(new Equipment(GameId.MouseArmor,
			material: EquipmentMaterial.Golden,
			faction: EquipmentFaction.Shadow,
			adjective: EquipmentAdjective.Divine,
			rarity: EquipmentRarity.LegendaryPlus,
			level: 35,
			grade: EquipmentGrade.GradeI,
			lastRepairTimestamp: DateTime.UtcNow.Ticks));
		gameLogic!.EquipmentLogic.AddToInventory(new Equipment(GameId.RoadShield,
			material: EquipmentMaterial.Golden,
			faction: EquipmentFaction.Shadow,
			adjective: EquipmentAdjective.Divine,
			rarity: EquipmentRarity.LegendaryPlus,
			level: 35,
			grade: EquipmentGrade.GradeI,
			lastRepairTimestamp: DateTime.UtcNow.Ticks));
		gameLogic!.EquipmentLogic.AddToInventory(new Equipment(GameId.TikTokAmulet,
			material: EquipmentMaterial.Golden,
			faction: EquipmentFaction.Shadow,
			adjective: EquipmentAdjective.Divine,
			rarity: EquipmentRarity.LegendaryPlus,
			level: 35,
			grade: EquipmentGrade.GradeI,
			lastRepairTimestamp: DateTime.UtcNow.Ticks));

		((GameCommandService) services.CommandService).ForceServerDataUpdate();
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
			grade: EquipmentGrade.GradeIII, maxDurability: 100,
			level: 3,
			lastRepairTimestamp: DateTime.UtcNow.Ticks));

		((GameCommandService) services.CommandService).ForceServerDataUpdate();
	}

	private void UnlockCollectionItem(GameId item, IGameLogic gameLogic, IGameServices services)
	{
		var newCollectionItem = ItemFactory.Collection(item);
		if (!gameLogic.CollectionLogic.IsItemOwned(newCollectionItem))
		{
			gameLogic.CollectionLogic.UnlockCollectionItem(newCollectionItem);

			services.MessageBrokerService.Publish(new CollectionItemUnlockedMessage()
			{
				Source = CollectionUnlockSource.ServerGift,
				EquippedItem = newCollectionItem
			});
		}
	}

	[Category("Cosmetics")]
	public void UnlockAllCosmetics()
	{
		var gameLogic = MainInstaller.Resolve<IGameDataProvider>() as IGameLogic;
		var services = MainInstaller.Resolve<IGameServices>();

		foreach (var glider in gameLogic.CollectionLogic.GetCollectionsCategories().SelectMany(category => category.Id.GetIds()))
		{
			if(glider.IsInGroup(GameIdGroup.GenericCollectionItem))continue;
			UnlockCollectionItem(glider, gameLogic, services);
		}


		((GameCommandService) services.CommandService).ForceServerDataUpdate();
	}

	[Category("Progression")]
	private void AddXp(uint amount)
	{
		var dataProvider = MainInstaller.Resolve<IGameServices>().DataSaver as IDataService;
		var gameLogic = MainInstaller.Resolve<IGameDataProvider>() as IGameLogic;
		var services = MainInstaller.Resolve<IGameServices>();

		// TODO: Remove Logic outside command
		gameLogic.PlayerLogic.AddXP(amount);

		var data = new Dictionary<string, string>();
		ModelSerializer.SerializeToData(data, dataProvider.GetData<PlayerData>());
		services.GameBackendService.CallFunction("ExecuteCommand", null, null, new LogicRequest
		{
			Command = "CheatAddXpCommand",
			Data = data
		});
	}

	[Category("Progression")]
	private void PrintLevelXP()
	{
		var dataProvider = MainInstaller.Resolve<IGameDataProvider>();

		FLog.Info("PACO", $"Level: {dataProvider.PlayerDataProvider.Level}");
		FLog.Info("PACO", $"XP: {dataProvider.PlayerDataProvider.XP}");
	}

	[Category("Progression")]
	public void Add5000Coins()
	{
		var gameLogic = MainInstaller.Resolve<IGameDataProvider>() as IGameLogic;
		var services = MainInstaller.Resolve<IGameServices>();

		gameLogic.CurrencyLogic.AddCurrency(GameId.COIN, 5000);
		((GameCommandService) services.CommandService).ForceServerDataUpdate();
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
	public void Add250BPP()
	{
		var gameLogic = MainInstaller.Resolve<IGameDataProvider>() as IGameLogic;
		var services = MainInstaller.Resolve<IGameServices>();

		gameLogic.BattlePassLogic.AddBPP(250);

		((GameCommandService) services.CommandService).ForceServerDataUpdate();
	}

	[Category("Progression")]
	public void Add100000BPP()
	{
		var gameLogic = MainInstaller.Resolve<IGameDataProvider>() as IGameLogic;
		var services = MainInstaller.Resolve<IGameServices>();

		gameLogic.BattlePassLogic.AddBPP(100000);

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
	public void Add200Trophies()
	{
		var gameLogic = MainInstaller.Resolve<IGameDataProvider>() as IGameLogic;
		var services = MainInstaller.Resolve<IGameServices>();

		gameLogic.PlayerLogic.UpdateTrophies(200);

		((GameCommandService) services.CommandService).ForceServerDataUpdate();
	}

	[Category("Progression")]
	public void Add50XP()
	{
		var gameLogic = (IGameLogic) MainInstaller.Resolve<IGameDataProvider>();
		var services = MainInstaller.Resolve<IGameServices>();

		gameLogic.PlayerLogic.AddXP(50);

		((GameCommandService) services.CommandService).ForceServerDataUpdate();
	}

	[Category("Progression")]
	public void LevelFameUp()
	{
		var gameLogic = (IGameLogic) MainInstaller.Resolve<IGameDataProvider>();
		var services = MainInstaller.Resolve<IGameServices>();
		var level = gameLogic.PlayerLogic.Level.Value;
		var xp = gameLogic.PlayerLogic.XP.Value;
		var finalXpNeeded = gameLogic.PlayerLogic.GetXpNeededForLevel(level) - xp;
		gameLogic.PlayerLogic.AddXP(finalXpNeeded);
		((GameCommandService) services.CommandService).ForceServerDataUpdate();
	}

	[Category("Progression")]
	public void ResetLevelAndXP()
	{
		var gameLogic = (IGameLogic) MainInstaller.Resolve<IGameDataProvider>();
		var services = MainInstaller.Resolve<IGameServices>();

		gameLogic.PlayerLogic.ResetLevelAndXP();

		((GameCommandService) services.CommandService).ForceServerDataUpdate();
	}

	[Category("Progression")]
	public void Add10SecTime()
	{
		var services = MainInstaller.Resolve<IGameServices>();
		var timeManipulator = services.TimeService as ITimeManipulator;

		timeManipulator.AddTime(10);
	}


	[Category("Progression")]
	public void Add1MinTime()
	{
		var services = MainInstaller.Resolve<IGameServices>();
		var timeManipulator = services.TimeService as ITimeManipulator;

		timeManipulator.AddTime(60);
	}


	[Category("Progression")]
	public void Add10MinTime()
	{
		var services = MainInstaller.Resolve<IGameServices>();
		var timeManipulator = services.TimeService as ITimeManipulator;

		timeManipulator.AddTime(60 * 10);
	}

	[Category("Progression")]
	public void Add1HourTime()
	{
		var services = MainInstaller.Resolve<IGameServices>();
		var timeManipulator = services.TimeService as ITimeManipulator;

		timeManipulator.AddTime(60 * 60);
	}

	[Category("Progression")]
	public void Add1DayTime()
	{
		var services = MainInstaller.Resolve<IGameServices>();
		var timeManipulator = services.TimeService as ITimeManipulator;

		timeManipulator.AddTime(60 * 60 * 24);
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
			$"Player TTL: {room.PlayerTtl}\n" +
			$"Room TTL: {room.EmptyRoomTtl}\n" +
			$"-----\n" +
			$"Custom Props:\n" + roomProps +
			$"-----\n");
	}

	[Category("Logging")]
	public void LogCurrentRoomReadyStatus()
	{
		var services = MainInstaller.Resolve<IGameServices>();
		var room = services.NetworkService.QuantumClient.CurrentRoom;
		if (room == null)
		{
			return;
		}

		var str = room.GetRoomDebugString();
#if UNITY_EDITOR
		UnityEditor.EditorUtility.DisplayDialog("Room debug", str, "close");
#else
	FLog.Info(str);
#endif
	}


#endif
}