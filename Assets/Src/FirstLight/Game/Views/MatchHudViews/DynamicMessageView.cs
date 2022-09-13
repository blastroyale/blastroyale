using System.Collections;
using System.Collections.Generic;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Game.Views.AdventureHudViews;
using I2.Loc;
using Quantum;
using Sirenix.OdinInspector;
using UnityEngine;

namespace FirstLight.Game.Views.MatchHudViews
{
	/// <summary>
	/// Shows a Dynamic Message featuring Text and Graphics on screen after receiving an event from Quantum in game.
	/// We can attach listeners to the message start and end in case we want to tie any other game action to the messages.
	/// </summary>
	public class DynamicMessageView : MonoBehaviour
	{
		[SerializeField] private List<DynamicMessageEntryView> _messages;

		private readonly Queue<MessageData> _queue = new Queue<MessageData>();
		
		private IGameServices _services;
		private IMatchServices _matchServices;
		private IGameDataProvider _gameDataProvider;

		private struct MessageData
		{
			public string TopText;
			public string BottomText;
			public DynamicMessageEntryView MessageEntry;
		}

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
			_matchServices = MainInstaller.Resolve<IMatchServices>();
			
			var mapConfig = _services.NetworkService.CurrentRoomMapConfig.Value;
			var gameModeConfig = _services.NetworkService.CurrentRoomGameModeConfig.Value;
			var config = _services.ConfigsProvider.GetConfig<QuantumGameConfig>();
			var maxPlayers = NetworkUtils.GetMaxPlayers(gameModeConfig, mapConfig);
			
			foreach (var message in _messages)
			{
				message.gameObject.SetActive(false);
			}
			
			QuantumEvent.Subscribe<EventOnPlayerKilledPlayer>(this, OnPlayerKilledPlayer, onlyIfActiveAndEnabled: true);
			QuantumEvent.Subscribe<EventOnAirDropDropped>(this, OnAirDropDropped);
			QuantumEvent.Subscribe<EventOnPlayerAlive>(this, OnPlayerAlive);
		}

		private void OnPlayerAlive(EventOnPlayerAlive callback)
		{
			if (_matchServices.SpectateService.SpectatedPlayer.Value.Player != callback.Player) return;
			
			_queue.Clear();
		}
		
		private void OnPlayerKilledPlayer(EventOnPlayerKilledPlayer callback)
		{
			if (_matchServices.SpectateService.SpectatedPlayer.Value.Entity != callback.EntityKiller) return;
			
			if (callback.CurrentMultiKill == 1)
			{
				var game = QuantumRunner.Default.Game;
				var frame = game.Frames.Verified;
				var container = frame.GetSingleton<GameContainer>();
				var playerData = container.GetPlayersMatchData(frame, out _);
				var deadName = playerData[callback.PlayerDead].GetPlayerName();
				
				var messageData = new MessageData
				{
					TopText = ScriptLocalization.AdventureMenu.Kill,
					BottomText = deadName,
					MessageEntry = _messages[Random.Range(0, _messages.Count)]
				};
					
				EnqueueMessage(messageData);
			}
			else
			{
				var messageData = new MessageData
				{
					TopText = ScriptLocalization.AdventureMenu.MultikillMessage,
					BottomText = string.Format(ScriptLocalization.AdventureMenu.KillMessageAmount, callback.CurrentMultiKill),
					MessageEntry = _messages[Random.Range(0, _messages.Count)]
				};
					
				EnqueueMessage(messageData);
			}
			
			switch (callback.CurrentKillStreak)
			{
				case 3:
				case 5:
				case 7:
				case 9:
					var messageData = new MessageData
					{
						TopText = ScriptLocalization.AdventureMenu.KillingSpreeMessage,
						BottomText = string.Format(ScriptLocalization.AdventureMenu.KillMessageAmount, callback.CurrentKillStreak),
						MessageEntry = _messages[Random.Range(0, _messages.Count)]
					};
					
					EnqueueMessage(messageData);
					break;
			}
		}

		private void OnAirDropDropped(EventOnAirDropDropped callback)
		{
			var messageData = new MessageData
			{
				TopText = ScriptLocalization.AdventureMenu.AirDropIncomingLine1,
				BottomText = ScriptLocalization.AdventureMenu.AirDropIncomingLine2,
				MessageEntry = _messages[Random.Range(0, _messages.Count)]
			};
					
			EnqueueMessage(messageData);
		}
		
		private void EnqueueMessage(MessageData message)
		{
			_queue.Enqueue(message);
			
			if (_queue.Count == 1)
			{
				ShowQueueMessage();
			}
		}

		private async void ShowQueueMessage()
		{
			var message = _queue.Peek();

			message.MessageEntry.gameObject.SetActive(true);
			
			await message.MessageEntry.DisplayMessage(message.TopText, message.BottomText);

			if (this.IsDestroyed())
			{
				return;
			}

			_queue.Dequeue();

			if (_queue.Count > 0)
			{
				ShowQueueMessage();
			}
		}
	}
}