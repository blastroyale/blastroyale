using System;
using System.Collections;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Presenters;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Services;
using FirstLight.Statechart;
using I2.Loc;
using PlayFab;
using PlayFab.ClientModels;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.StateMachines
{
	/// <summary>
	/// This object contains music layering logic for the deathmatch game mode
	/// </summary>
	public class AudioDeathmatchState
	{
		private static readonly IStatechartEvent IncreaseIntensityEvent =
			new StatechartEvent("Increase Music Intensity Event");

		private readonly IGameServices _services;
		private readonly IGameDataProvider _dataProvider;
		private readonly Action<IStatechartEvent> _statechartTrigger;
		private bool _isHighIntensityPhase = false;
		private DateTime _voLeaderboardSfxAvailabilityTime = DateTime.UtcNow;
		private uint _localPlayerLastRank;
		private bool _killsLeftSfxPlayed3;
		private bool _killsLeftSfxPlayed1;
		
		public AudioDeathmatchState(IGameServices services, IGameDataProvider gameLogic,
		                            Action<IStatechartEvent> statechartTrigger)
		{
			_services = services;
			_dataProvider = gameLogic;
			_statechartTrigger = statechartTrigger;
		}

		/// <summary>
		/// Setups the Adventure gameplay state
		/// </summary>
		public void Setup(IStateFactory stateFactory)
		{
			var initial = stateFactory.Initial("AUDIO DM - Initial");
			var final = stateFactory.Final("AUDIO DM - Final");
			var resyncCheck = stateFactory.Choice("AUDIO DM - Resync Check");
			var matchStateCheck = stateFactory.Choice("AUDIO DM - Match State Check");
			var midIntensity = stateFactory.State("AUDIO DM - Mid Intensity");
			var highIntensity = stateFactory.State("AUDIO DM - High Intensity");

			initial.Transition().Target(resyncCheck);
			initial.OnExit(SubscribeEvents);

			resyncCheck.Transition().Condition(IsResyncing).Target(matchStateCheck);
			resyncCheck.Transition().Target(midIntensity);

			matchStateCheck.Transition().Condition(IsMidIntensityPhase).Target(midIntensity);
			matchStateCheck.Transition().Target(highIntensity);

			midIntensity.OnEnter(PlayMidIntensityMusic);
			midIntensity.Event(IncreaseIntensityEvent).Target(highIntensity);

			highIntensity.OnEnter(PlayHighIntensityMusic);
			highIntensity.Event(IncreaseIntensityEvent).Target(final);

			final.OnEnter(UnsubscribeEvents);
		}

		private void SubscribeEvents()
		{
			QuantumEvent.SubscribeManual<EventOnPlayerKilledPlayer>(this, OnEventOnPlayerKilledPlayer);

			_localPlayerLastRank = 0;
			_killsLeftSfxPlayed3 = false;
			_killsLeftSfxPlayed1 = false;
		}

		private void UnsubscribeEvents()
		{
			_services?.MessageBrokerService.UnsubscribeAll(this);
			QuantumEvent.UnsubscribeListener(this);
		}

		private bool IsResyncing()
		{
			return !_services.NetworkService.IsJoiningNewMatch;
		}

		private bool IsMidIntensityPhase()
		{
			var game = QuantumRunner.Default.Game;
			var frame = game.Frames.Verified;
			var container = frame.GetSingleton<GameContainer>();
			var killsLeftForLeader = container.TargetProgress - container.CurrentProgress;

			return killsLeftForLeader <= GameConstants.Audio.DM_HIGH_PHASE_KILLS_LEFT_THRESHOLD;
		}

		private void OnEventOnPlayerKilledPlayer(EventOnPlayerKilledPlayer callback)
		{
			var game = callback.Game;
			var frame = game.Frames.Verified;
			var container = frame.GetSingleton<GameContainer>();
			var killsLeftForLeader = container.TargetProgress - container.CurrentProgress;
			var matchData = container.GetPlayersMatchData(frame, out var leader);
			
			// Kills left announcer
			if (!_killsLeftSfxPlayed3 && killsLeftForLeader == 3)
			{
				_killsLeftSfxPlayed3 = true;
				_services.AudioFxService.PlayClipQueued2D(AudioId.Vo_KillsLeft3, GameConstants.Audio.MIXER_GROUP_DIALOGUE_ID);
			}
			else if (!_killsLeftSfxPlayed1 && killsLeftForLeader == 1)
			{
				_killsLeftSfxPlayed1 = true;
				_services.AudioFxService.PlayClipQueued2D(AudioId.Vo_KillsLeft1, GameConstants.Audio.MIXER_GROUP_DIALOGUE_ID);
			}

			if (!IsSpectator())
			{
				var localPlayerData = matchData[game.GetLocalPlayers()[0]];
				
				// Leaderboard announcer
				if (_localPlayerLastRank != 1 && localPlayerData.PlayerRank == 1
				                              && DateTime.UtcNow >= _voLeaderboardSfxAvailabilityTime)
				{
					_services.AudioFxService.PlayClipQueued2D(AudioId.Vo_LeaderboardTop, GameConstants.Audio.MIXER_GROUP_DIALOGUE_ID);
					_voLeaderboardSfxAvailabilityTime = DateTime.UtcNow.AddSeconds(GameConstants.Audio.VO_SFX_LEADERBOARD_PREVENTION_SECONDS);
				}
				else if (_localPlayerLastRank == 1 && localPlayerData.PlayerRank != 1
				                                   && DateTime.UtcNow >= _voLeaderboardSfxAvailabilityTime)
				{
					_services.AudioFxService.PlayClipQueued2D(AudioId.Vo_LeaderboardOvertaken, GameConstants.Audio.MIXER_GROUP_DIALOGUE_ID);
					_voLeaderboardSfxAvailabilityTime = DateTime.UtcNow.AddSeconds(GameConstants.Audio.VO_SFX_LEADERBOARD_PREVENTION_SECONDS);
				}

				_localPlayerLastRank = localPlayerData.PlayerRank;
			}

			if (killsLeftForLeader <= GameConstants.Audio.DM_HIGH_PHASE_KILLS_LEFT_THRESHOLD && !_isHighIntensityPhase)
			{
				_statechartTrigger(IncreaseIntensityEvent);
			}
		}

		private bool IsSpectator()
		{
			return _services.NetworkService.QuantumClient.LocalPlayer.IsSpectator();
		}

		private void PlayMidIntensityMusic()
		{
			_services.AudioFxService.PlayMusic(AudioId.MusicDmLoop);
		}

		private void PlayHighIntensityMusic()
		{
			_isHighIntensityPhase = true;

			_services.AudioFxService.PlayClip2D(AudioId.MusicHighTransitionJingleDm,
			                                    GameConstants.Audio.MIXER_GROUP_MUSIC_ID);

			_services.CoroutineService.StartCoroutine(PlayDmHighLoopCoroutine());
		}

		private IEnumerator PlayDmHighLoopCoroutine()
		{
			yield return new WaitForSeconds(GameConstants.Audio.HIGH_LOOP_TRANSITION_DELAY);
			_services.AudioFxService.PlayMusic(AudioId.MusicDmHighLoop);
		}
	}
}