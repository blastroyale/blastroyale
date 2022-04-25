using System.Collections;
using System.Collections.Generic;
using FirstLight.Game.Configs;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Game.Views.AdventureHudViews;
using I2.Loc;
using Quantum;
using UnityEngine;
using Random = UnityEngine.Random;

namespace FirstLight.Game.Views.MatchHudViews
{
	/// <summary>
	/// Shows a Dynamic Message featuring Text and Graphics on screen after receiving an event from Quantum in game.
	/// We can attach listeners to the message start and end in case we want to tie any other game action to the messages.
	/// </summary>
	public class DynamicMessageView : MonoBehaviour
	{
		[SerializeField] private List<DynamicMessageEntryView> _messages;
		[SerializeField] private AudioId[] _killedEnemyAudioIds; 
		
		private const int _doubleKillCount = 2;
		private const int _multiKillCount = 3;
		private const int _killingSpreeCount = 3;
		private const int _dominatingCount = 5;
		private const int _godlikeCount = 10;

		private readonly Queue<MessageData> _queue = new Queue<MessageData>();
		
		private IGameServices _services;
		private IGameDataProvider _gameDataProvider;
		private Coroutine _killTimerCoroutine;
		private int _killCounter;
		private float _killConfigTimer;
		private int _killWarningLimit;
		private int _killTarget;
		private int[] _playerKillStreak;
		private bool[] _playerDominating;
		private bool[] _playerGodlike;
		
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
			
			var mapConfig = _services.NetworkService.CurrentRoomMapConfig.Value;
			var config = _services.ConfigsProvider.GetConfig<QuantumGameConfig>();
			
			foreach (var message in _messages)
			{
				message.gameObject.SetActive(false);
			}

			_killConfigTimer = config.DoubleKillTimeLimit;
			_killTarget = mapConfig.GameEndTarget;
			_killWarningLimit = (_killTarget / 3) * 2;
			_playerKillStreak = new int[mapConfig.PlayersLimit];
			_playerDominating = new bool[mapConfig.PlayersLimit];
			_playerGodlike = new bool[mapConfig.PlayersLimit];
			
			QuantumEvent.Subscribe<EventOnPlayerKilledPlayer>(this, OnEventOnPlayerKilledPlayer, onlyIfActiveAndEnabled: true);
			QuantumEvent.Subscribe<EventOnGameEnded>(this, OnEventGameEnd);
		}
		
		private void OnEventGameEnd(EventOnGameEnded callback)
		{
			StopTimerCoroutine();
		}
		
		/// <summary>
		/// Handles Double Kills, Multi Kills, Killing Sprees.
		/// </summary>
		private void OnEventOnPlayerKilledPlayer(EventOnPlayerKilledPlayer callback)
		{
			var leaderData = callback.PlayersMatchData[callback.PlayerLeader];
			var killerData = callback.PlayersMatchData[callback.PlayerKiller];
			var deadData = callback.PlayersMatchData[callback.PlayerDead];
			
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

			// Update Kill streaks;
			_playerKillStreak[callback.PlayerDead] = 0;
			_playerKillStreak[callback.PlayerKiller]++;

			CheckKillingSpree(killerData, deadData);

			if (!_playerDominating[callback.PlayerKiller])
			{
				CheckDominating(callback.PlayerKiller, killerData.PlayerName);
			}

			if (!_playerGodlike[callback.PlayerKiller])
			{
				CheckGodlike(callback.PlayerKiller, killerData.PlayerName);
			}
		}

		private void CheckKillingSpree(QuantumPlayerMatchData killerData, QuantumPlayerMatchData deadData)
		{
			if (killerData.IsLocalPlayer)
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
					
					_services.AudioFxService.PlayClip2D(AudioId.DoubleKill);
				}
				else if (_killCounter == _multiKillCount)
				{
					message.TopText = ScriptLocalization.AdventureMenu.Multi;
					message.BottomText = ScriptLocalization.AdventureMenu.Kill;
					
					_services.AudioFxService.PlayClip2D(AudioId.MultiKill);
				}
				else if (_killCounter > _killingSpreeCount)
				{
					message.TopText = ScriptLocalization.AdventureMenu.Killing;
					message.BottomText = ScriptLocalization.AdventureMenu.Spree;
					
					_services.AudioFxService.PlayClip2D(AudioId.KillingSpree);
				}
				else if (!deadData.IsLocalPlayer)
				{
					int randAudioClip = Random.Range(0, _killedEnemyAudioIds.Length);
					_services.AudioFxService.PlayClip2D(_killedEnemyAudioIds[randAudioClip]);
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
			
			if (deadData.IsLocalPlayer)
			{
				_killCounter = 0;
				_services.AudioFxService.PlayClip2D(AudioId.YouTasteDeath);
				
				StopTimerCoroutine();
			}
		}

		private void StopTimerCoroutine()
		{
			if (_killTimerCoroutine != null)
			{
				StopCoroutine(_killTimerCoroutine);
				_killTimerCoroutine  = null;
			} 
		}

		private void CheckDominating(int playerIndex, string playerName)
		{
			for (var i = 0; i < _playerKillStreak.Length; i++)
			{
				if (i != playerIndex && _playerKillStreak[i] + _dominatingCount >= _playerKillStreak[playerIndex])
				{
					return;
				}
			}
			
			_playerDominating[playerIndex] = true;
				
			var messageData = new MessageData
			{
				TopText = playerName,
				BottomText = ScriptLocalization.AdventureMenu.Dominating,
				MessageEntry = _messages[Random.Range(0, _messages.Count)]
			};
				
			EnqueueMessage(messageData);
		}

		private void CheckGodlike(int playerIndex, string playerName)
		{
			for (var i = 0; i < _playerKillStreak.Length; i++)
			{
				if (i != playerIndex && _playerKillStreak[i] + _godlikeCount >= _playerKillStreak[playerIndex])
				{
					return;
				}
			}
			
			_playerDominating[playerIndex] = true;
				
			var messageData = new MessageData
			{
				TopText = playerName,
				BottomText = ScriptLocalization.AdventureMenu.Godlike,
				MessageEntry = _messages[Random.Range(0, _messages.Count)]
			};
				
			EnqueueMessage(messageData);
		}
		
		
		// If the local player kills another player within the specified time period, they can 
		// rack up double kills and multi-kills.
		private IEnumerator TimeUpdateCoroutine()
		{
			yield return new WaitForSeconds(_killConfigTimer);

			_killCounter = 0;
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