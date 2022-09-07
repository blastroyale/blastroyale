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
	/// This object contains music layering logic for the battle royale game mode
	/// </summary>
	public class AudioBattleRoyaleState
	{
		private static readonly IStatechartEvent IncreaseIntensityEvent = new StatechartEvent("Increase Music Intensity Event");
		private static readonly IStatechartEvent MaxIntensityEvent = new StatechartEvent("Max Music Intensity Event");

		private readonly IGameServices _services;
		private readonly IGameDataProvider _dataProvider;
		private readonly Action<IStatechartEvent> _statechartTrigger;

		private float _lastRecordedIntensityIncreaseTime = 0;
		private bool _isHighIntensityPhase = false;
		private float CurrentMatchTime => QuantumRunner.Default.Game.Frames.Predicted.Time.AsFloat;

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
			var highIntensity = stateFactory.State("AUDIO BR - High Intensity");
			
			initial.Transition().Target(matchStateCheck);
			initial.OnExit(SubscribeEvents);

			matchStateCheck.Transition().Condition(IsSkyDivePhase).Target(skydive);
			matchStateCheck.Transition().Condition(IsLowIntensityPhase).Target(lowIntensity);
			matchStateCheck.Transition().Condition(IsMidIntensityPhase).Target(midIntensity);
			matchStateCheck.Transition().Target(highIntensity);

			skydive.OnEnter(PlaySkydiveMusic);
			skydive.Event(IncreaseIntensityEvent).Target(lowIntensity);

			lowIntensity.OnEnter(PlayLowIntensityMusic);
			lowIntensity.Event(IncreaseIntensityEvent).Target(midIntensity);
			lowIntensity.Event(MaxIntensityEvent).Target(highIntensity);
			
			midIntensity.OnEnter(PlayMidIntensityMusic);
			midIntensity.Event(IncreaseIntensityEvent).Target(highIntensity);
			midIntensity.Event(MaxIntensityEvent).Target(highIntensity);
			
			highIntensity.OnEnter(StopMusicInstant);
			highIntensity.OnEnter(PlayHighIntensityMusic);
			highIntensity.Event(IncreaseIntensityEvent).Target(final);
			
			final.OnEnter(UnsubscribeEvents);
		}

		private void SubscribeEvents()
		{
			QuantumCallback.SubscribeManual<CallbackUpdateView>(this, OnQuantumUpdateView);
			QuantumEvent.SubscribeManual<EventOnPlayerKilledPlayer>(this, OnEventOnPlayerKilledPlayer);
			QuantumEvent.SubscribeManual<EventOnGameEnded>(this, OnGameEnded);
		}

		private void UnsubscribeEvents()
		{
			QuantumCallback.UnsubscribeListener(this);
			QuantumEvent.UnsubscribeListener(this);
		}

		private void OnQuantumUpdateView(CallbackUpdateView callback)
		{
			var time = callback.Game.Frames.Predicted.Time.AsFloat;
			
			if ((time > GameConstants.Audio.BR_LOW_PHASE_SECONDS_THRESHOLD &&
			     _lastRecordedIntensityIncreaseTime < GameConstants.Audio.BR_LOW_PHASE_SECONDS_THRESHOLD) ||
			    (time > GameConstants.Audio.BR_MID_PHASE_SECONDS_THRESHOLD &&
			     _lastRecordedIntensityIncreaseTime < GameConstants.Audio.BR_MID_PHASE_SECONDS_THRESHOLD) ||
			    (time > GameConstants.Audio.BR_HIGH_PHASE_SECONDS_THRESHOLD &&
						    _lastRecordedIntensityIncreaseTime < GameConstants.Audio.BR_MID_PHASE_SECONDS_THRESHOLD) &&
			    !_isHighIntensityPhase) 
			{
				_statechartTrigger(IncreaseIntensityEvent);
			}
		}
		
		private void OnEventOnPlayerKilledPlayer(EventOnPlayerKilledPlayer callback)
		{
			var frame = callback.Game.Frames.Verified;
			var container = frame.GetSingleton<GameContainer>();
			var playersLeft = container.TargetProgress - (container.CurrentProgress + 1);
			// CurrentProgress+1 because BR always has 1 player left alive at the end
			
			Debug.LogError(container.CurrentProgress + " " + playersLeft);
			
			if (playersLeft == 10)
			{
				_services.AudioFxService.PlayClipQueued2D(AudioId.Vo_Alive10);
			}
			else if (playersLeft == 2)
			{
				_services.AudioFxService.PlayClipQueued2D(AudioId.Vo_Alive2);
			}
			
			if (playersLeft <= GameConstants.Audio.BR_HIGH_PHASE_PLAYERS_LEFT_THRESHOLD && !_isHighIntensityPhase)
			{
				_statechartTrigger(MaxIntensityEvent);
			}
		}

		private void OnGameEnded(EventOnGameEnded callback)
		{
			if (IsSpectator()) return;
			
			var game = QuantumRunner.Default.Game;
			var frame = game.Frames.Verified;
			var container = frame.GetSingleton<GameContainer>();
			var matchData = container.GetPlayersMatchData(frame, out var leader);

			if (game.PlayerIsLocal(leader))
			{
				_services.AudioFxService.PlayClipQueued2D(AudioId.Vo_Victory);
			}
			else
			{
				_services.AudioFxService.PlayClipQueued2D( AudioId.Vo_GameOver);
			}
		}
		
		private bool IsSpectator()
		{
			return _services.NetworkService.QuantumClient.LocalPlayer.IsSpectator();
		}

		private bool IsSkyDivePhase()
		{
			return CurrentMatchTime < GameConstants.Audio.BR_LOW_PHASE_SECONDS_THRESHOLD;
		}

		private bool IsLowIntensityPhase()
		{
			return CurrentMatchTime < GameConstants.Audio.BR_MID_PHASE_SECONDS_THRESHOLD;
		}
		
		private bool IsMidIntensityPhase()
		{
			var frame = QuantumRunner.Default.Game.Frames.Verified;
			var container = frame.GetSingleton<GameContainer>();
			var playersLeft = container.TargetProgress - container.CurrentProgress;
			
			return CurrentMatchTime < GameConstants.Audio.BR_HIGH_PHASE_SECONDS_THRESHOLD && playersLeft > 2;
		}
		
		private void StopMusicInstant()
		{
			_services.AudioFxService.StopMusic();
		}

		private void PlaySkydiveMusic()
		{
			_lastRecordedIntensityIncreaseTime = CurrentMatchTime;

			_services.AudioFxService.PlayMusic(AudioId.MusicBrSkydiveLoop);
		}

		private void PlayLowIntensityMusic()
		{
			_lastRecordedIntensityIncreaseTime = CurrentMatchTime;

			// If resync, skip fading
			var fadeInDuration = _services.NetworkService.IsJoiningNewMatch
				                     ? GameConstants.Audio.MUSIC_SHORT_FADE_SECONDS
				                     : 0;

			_services.AudioFxService.PlayMusic(AudioId.MusicBrLowLoop, fadeInDuration,
			                                   GameConstants.Audio.MUSIC_SHORT_FADE_SECONDS, true);
		}

		private void PlayMidIntensityMusic()
		{
			_lastRecordedIntensityIncreaseTime = CurrentMatchTime;

			// If resync, skip fading
			var fadeInDuration = _services.NetworkService.IsJoiningNewMatch
				                     ? GameConstants.Audio.MUSIC_SHORT_FADE_SECONDS
				                     : 0;

			_services.AudioFxService.PlayMusic(AudioId.MusicBrMidLoop, fadeInDuration,
			                                   GameConstants.Audio.MUSIC_SHORT_FADE_SECONDS, true);
		}
		
		private void PlayHighIntensityMusic()
		{
			_lastRecordedIntensityIncreaseTime = CurrentMatchTime;
			_isHighIntensityPhase = true;
			
			_services.AudioFxService.PlayClip2D(AudioId.MusicHighTransitionJingleBr, GameConstants.Audio.MIXER_GROUP_MUSIC_ID);

			_services.CoroutineService.StartCoroutine(PlayBrHighLoopCoroutine());
		}
		
		private IEnumerator PlayBrHighLoopCoroutine()
		{
			yield return new WaitForSeconds(GameConstants.Audio.HIGH_LOOP_TRANSITION_DELAY);
			_services.AudioFxService.PlayMusic(AudioId.MusicBrHighLoop, 0,0, false);
		}
	}
}