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
		
		private const int _doubleKillCount = 2;
		private const int _multiKillCount = 3;
		private const int _killingSpreeCount = 3;
		private const int _dominatingCount = 5;
		private const int _godlikeCount = 10;

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
		}

		/// <summary>
		/// Handles Double Kills, Multi Kills, Killing Sprees.
		/// </summary>
		private void OnPlayerKilledPlayer(EventOnPlayerKilledPlayer callback)
		{
			/*var leaderData = callback.PlayersMatchData.Find(data => data.Data.Player.Equals(callback.PlayerLeader));
			var killerData = callback.PlayersMatchData.Find(data => data.Data.Player.Equals(callback.PlayerKiller));
			var deadData = callback.PlayersMatchData.Find(data => data.Data.Player.Equals(callback.PlayerDead));
			
			// Check to see if we are close to ending the match.
			if(leaderData.Data.PlayersKilledCount == _killWarningLimit &&  callback.PlayerKiller == callback.PlayerLeader)
			{
				var messageData = new MessageData
				{
					TopText = string.Format(ScriptLocalization.AdventureMenu.KillsRemaining, (_killTarget - _killWarningLimit).ToString()),
					BottomText = ScriptLocalization.AdventureMenu.Remaining,
					MessageEntry = _messages[Random.Range(0, _messages.Count)]
				};
					
				EnqueueMessage(messageData);
			}
			
			CheckKillingSpree(killerData, deadData);*/
		}
		
		private void CheckKillingSpree(QuantumPlayerMatchData killerData, QuantumPlayerMatchData deadData)
		{
			/*if (_matchServices.SpectateService.SpectatedPlayer.Value.Player == killerData.Data.Player)
			{
				var message = new MessageData
				{
					MessageEntry = _messages[Random.Range(0, _messages.Count)]
				};
				
				_killCounter++;
				
				if (_killCounter == _doubleKillCount)
				{
					message.TopText = ScriptLocalization.AdventureMenu.Double;
					message.BottomText = ScriptLocalization.AdventureMenu.Kill;
				}
				else if (_killCounter == _multiKillCount)
				{
					message.TopText = ScriptLocalization.AdventureMenu.Multi;
					message.BottomText = ScriptLocalization.AdventureMenu.Kill;
				}
				else if (_killCounter > _killingSpreeCount)
				{
					message.TopText = ScriptLocalization.AdventureMenu.Killing;
					message.BottomText = ScriptLocalization.AdventureMenu.Spree;
				}

				if (_killCounter > 1)
				{
					EnqueueMessage(message);
				}
				else
				{
					message.TopText = ScriptLocalization.AdventureMenu.YouKilledPlayer;
					message.BottomText = deadData.GetPlayerName();
					
					EnqueueMessage(message);
				}
				
				StopTimerCoroutine();
				
				_killTimerCoroutine = StartCoroutine(TimeUpdateCoroutine());
			}
			
			if (_matchServices.SpectateService.SpectatedPlayer.Value.Player == deadData.Data.Player)
			{
				_killCounter = 0;

				StopTimerCoroutine();
			}*/
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