using System;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Presenters;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Statechart;
using I2.Loc;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;

namespace FirstLight.Game.StateMachines
{
	/// <summary>
	/// This object contains the behaviour logic for the settings menu in the <seealso cref="MainMenuState"/>
	/// </summary>
	public class AudioBattleRoyaleState
	{
		private static readonly IStatechartEvent IncreaseIntensityEvent =
			new StatechartEvent("Increase Music Intensity Event");
		private static readonly IStatechartEvent IncreaseMaxIntensityEvent =
			new StatechartEvent("Increase Max Music Intensity Event");

		private readonly IGameServices _services;
		private readonly IGameDataProvider _dataProvider;
		private readonly Action<IStatechartEvent> _statechartTrigger;

		private float _lastRecordedIntensityIncreaseTime = 0;

		public float CurrentMatchTime => QuantumRunner.Default.Game.Frames.Verified.Time.AsFloat;
		public float CurrentMusicPlaybackTime => _services.AudioFxService.GetCurrentMusicPlaybackTime();
		
		public AudioBattleRoyaleState(IGameServices services, IGameDataProvider gameLogic,
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
			var skydive = stateFactory.State("AUDIO BR - Skydive");
			var lowIntensity = stateFactory.State("AUDIO BR - Low Intensity");
			var midIntensity = stateFactory.State("AUDIO BR - Mid Intensity");
			
			initial.Transition().Target(matchStateCheck);
			initial.OnExit(SubscribeEvents);
			
			matchStateCheck.Transition().Condition(IsSkyDivePhase).Target(skydive);
			matchStateCheck.Transition().Condition(IsLowIntensityPhase).Target(lowIntensity);
			matchStateCheck.Transition().Target(midIntensity);
			
			skydive.OnEnter(PlaySkydiveMusic);
			skydive.Event(IncreaseIntensityEvent).Target(lowIntensity);

			lowIntensity.OnEnter(PlayLowIntensityMusic);
			lowIntensity.Event(IncreaseIntensityEvent).Target(midIntensity);
			
			midIntensity.OnEnter(PlayMidIntensityMusic);
			midIntensity.Event(IncreaseIntensityEvent).Target(final);

			final.OnEnter(UnsubscribeEvents);
		}
		
		private void SubscribeEvents()
		{
			_services.TickService.SubscribeOnUpdate(TickIntensityCheck,1f);
		}

		private void UnsubscribeEvents()
		{
			_services?.MessageBrokerService.UnsubscribeAll(this);
			_services?.TickService.UnsubscribeAll(this);
		}

		private void TickIntensityCheck(float deltaTime)
		{
			if ((CurrentMatchTime > GameConstants.Audio.BR_LOW_PHASE_THRESHOLD &&
			    _lastRecordedIntensityIncreaseTime < GameConstants.Audio.BR_LOW_PHASE_THRESHOLD) || 
			    (CurrentMatchTime > GameConstants.Audio.BR_MID_PHASE_THRESHOLD &&
			     _lastRecordedIntensityIncreaseTime < GameConstants.Audio.BR_MID_PHASE_THRESHOLD))
			{
				_statechartTrigger(IncreaseIntensityEvent);
			}
		}
		
		private bool IsSkyDivePhase()
		{
			return CurrentMatchTime < GameConstants.Audio.BR_LOW_PHASE_THRESHOLD;
		}
		
		private bool IsLowIntensityPhase()
		{
			return CurrentMatchTime < GameConstants.Audio.BR_MID_PHASE_THRESHOLD;
		}

		private void PlaySkydiveMusic()
		{
			_lastRecordedIntensityIncreaseTime = CurrentMatchTime;
			
			_services.AudioFxService.PlayMusic(AudioId.BrSkydiveLoop);
		}

		private void PlayLowIntensityMusic()
		{
			_lastRecordedIntensityIncreaseTime = CurrentMatchTime;
			
			var sourceInitData = _services.AudioFxService.GetDefaultAudioInitProps(GameConstants.Audio.SFX_2D_SPATIAL_BLEND);
			sourceInitData.StartTime = CurrentMusicPlaybackTime;

			// If resync, skip fading
			var fadeInDuration = _services.NetworkService.IsJoiningNewMatch
				                     ? GameConstants.Audio.MUSIC_SHORT_FADE_IN_SECONDS
				                     : 0;
			
			_services.AudioFxService.PlayMusic(AudioId.BrLowLoop, fadeInDuration,
			                                   GameConstants.Audio.MUSIC_SHORT_FADE_OUT_SECONDS, sourceInitData);
		}
		
		private void PlayMidIntensityMusic()
		{
			_lastRecordedIntensityIncreaseTime = CurrentMatchTime;
			
			// TODO - REVERT THE PITCH CHANGE WHEN ALL PROPER MUSIC TRACKS ARE IN
			var sourceInitData = _services.AudioFxService.GetDefaultAudioInitProps(GameConstants.Audio.SFX_2D_SPATIAL_BLEND);
			sourceInitData.StartTime = CurrentMusicPlaybackTime;
			sourceInitData.Pitch = 1.025f;
			
			// If resync, skip fading
			var fadeInDuration = _services.NetworkService.IsJoiningNewMatch
				                     ? GameConstants.Audio.MUSIC_DEFAULT_FADE_IN_SECONDS
				                     : 0;
			
			_services.AudioFxService.PlayMusic(AudioId.BrLowLoop, fadeInDuration,
			                                   GameConstants.Audio.MUSIC_DEFAULT_FADE_OUT_SECONDS, sourceInitData);
		}
	}
}