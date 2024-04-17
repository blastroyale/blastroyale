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
using SRDebugger;
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

	private static uint _currencyValue = 100;

	private static void AddCurrencyCheats()
	{
		var category = "Currencies";
		var sort = 10;
		var container = new DynamicOptionContainer();
		// Create a mutable option
		var input = OptionDefinition.Create(
			"Currency Amount",
			() => _currencyValue,
			(newValue) => _currencyValue = newValue,
			category,
			sort
		);
		container.AddOption(input);

		var values = new Dictionary<GameId, Action<IGameLogic>>()
		{
			{GameId.COIN, null},
			{GameId.BlastBuck, null},
			{GameId.NOOB, null},
			{GameId.BPP, l => l.BattlePassLogic.AddBPP(_currencyValue)},
			{GameId.XP, l => l.PlayerLogic.AddXP(_currencyValue)},
			{GameId.Trophies, l => l.PlayerLogic.UpdateTrophies((int) _currencyValue)},
		};
		foreach (var kv in values)
		{
			container.AddOption(OptionDefinition.FromMethod("Give " + kv.Key, () =>
				{
					var gameLogic = MainInstaller.Resolve<IGameDataProvider>() as IGameLogic;
					var services = MainInstaller.Resolve<IGameServices>();
					if (kv.Value != null)
					{
						kv.Value(gameLogic);
					}
					else
					{
						gameLogic.CurrencyLogic.AddCurrency(kv.Key, _currencyValue);
					}

					((GameCommandService) services.CommandService).ForceServerDataUpdate();
				}, category,
				sort));
		}

		SRDebug.Instance.AddOptionContainer(container);
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
	[Sort(5)]
	public void UnlockAllCosmetics()
	{
		var gameLogic = MainInstaller.Resolve<IGameDataProvider>() as IGameLogic;
		var services = MainInstaller.Resolve<IGameServices>();

		foreach (var glider in gameLogic.CollectionLogic.GetCollectionsCategories().SelectMany(category => category.Id.GetIds()))
		{
			if (glider.IsInGroup(GameIdGroup.GenericCollectionItem)) continue;
			UnlockCollectionItem(glider, gameLogic, services);
		}


		((GameCommandService) services.CommandService).ForceServerDataUpdate();
	}


	[Sort(30)]
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