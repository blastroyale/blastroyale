using System;
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
		private static readonly IStatechartEvent IncreaseIntensityEvent = new StatechartEvent("Increase Music Intensity Event");

		private readonly IGameServices _services;
		private readonly IGameDataProvider _dataProvider;
		private readonly Action<IStatechartEvent> _statechartTrigger;
		private bool _isHighIntensityPhase = false;
		
		private float CurrentMusicPlaybackTime => _services.AudioFxService.GetCurrentMusicPlaybackTime();

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
			var initial = stateFactory.Initial("AUDIO BR - Initial");
			var final = stateFactory.Final("AUDIO BR - Final");
			var matchStateCheck = stateFactory.Choice("AUDIO BR - Match State Check");
			var midIntensity = stateFactory.State("AUDIO BR - Mid Intensity");
			var highIntensity = stateFactory.State("AUDIO BR - High Intensity");
			
			initial.Transition().Target(matchStateCheck);
			initial.OnExit(SubscribeEvents);
			
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
		}

		private void UnsubscribeEvents()
		{
			_services?.MessageBrokerService.UnsubscribeAll(this);
			QuantumEvent.UnsubscribeListener(this);
		}

		private bool IsMidIntensityPhase()
		{
			var game = QuantumRunner.Default.Game;
			var frame = game.Frames.Verified;
			var container = frame.GetSingleton<GameContainer>();
			var playerData = container.GetPlayersMatchData(frame, out var leader);
			var leaderPlayerData = playerData[leader];

			var killsLeftForLeader = container.TargetProgress - leaderPlayerData.Data.PlayersKilledCount;
			
			return killsLeftForLeader <= GameConstants.Audio.DM_HIGH_PHASE_KILLS_LEFT_THRESHOLD;
		}
		
		private void OnEventOnPlayerKilledPlayer(EventOnPlayerKilledPlayer callback)
		{
			var frame = callback.Game.Frames.Verified;
			var container = frame.GetSingleton<GameContainer>();
			var playerData = container.GetPlayersMatchData(frame, out var leader);
			var leaderPlayerData = playerData[leader];

			var killsLeftForLeader = container.TargetProgress - leaderPlayerData.Data.PlayersKilledCount;

			if (killsLeftForLeader <= GameConstants.Audio.DM_HIGH_PHASE_KILLS_LEFT_THRESHOLD && !_isHighIntensityPhase)
			{
				_statechartTrigger(IncreaseIntensityEvent);
			}
		}

		private void PlayMidIntensityMusic()
		{
			_services.AudioFxService.PlayMusic(AudioId.DmLoop);
		}

		private void PlayHighIntensityMusic()
		{
			_isHighIntensityPhase = true;
			
			// If resync, skip fading
			var fadeInDuration = _services.NetworkService.IsJoiningNewMatch
				                     ? GameConstants.Audio.MUSIC_DEFAULT_FADE_IN_SECONDS
				                     : 0;
			
			_services.AudioFxService.PlayMusic(AudioId.DmFinalDuelLoop, fadeInDuration,
			                                   GameConstants.Audio.MUSIC_DEFAULT_FADE_OUT_SECONDS, GetInitDataForPlayback(1.025f));
		}
		
		// TODO - REVERT THE PITCH PARAM WHEN ALL PROPER MUSIC TRACKS ARE IN
		private AudioSourceInitData? GetInitDataForPlayback(float pitch = 1f)
		{
			var sourceInitData = _services.AudioFxService.GetDefaultAudioInitProps(GameConstants.Audio.SFX_2D_SPATIAL_BLEND);
			sourceInitData.StartTime = CurrentMusicPlaybackTime;
			sourceInitData.Pitch = pitch;
			
			return sourceInitData;
		}
	}
}