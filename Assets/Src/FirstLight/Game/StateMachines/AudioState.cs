using System;
using System.Collections.Generic;
using FirstLight.Game.Configs;
using FirstLight.Game.Configs.AssetConfigs;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Services;
using FirstLight.Statechart;
using Quantum;
using UnityEngine;
using Random = UnityEngine.Random;

namespace FirstLight.Game.StateMachines
{
	/// <summary>
	/// This object contains the behaviour logic to control all the game audio in the <seealso cref="GameStateMachine"/>
	/// </summary>
	public class AudioState
	{
		private readonly IGameServices _services;
		private readonly IGameDataProvider _gameDataProvider;
		private readonly AudioBattleRoyaleState _audioBrState;
		private readonly AudioDeathmatchState _audioDmState;
		private readonly Action<IStatechartEvent> _statechartTrigger;
		private IMatchServices _matchServices;

		public AudioState(IGameDataProvider gameLogic, IGameServices services,
		                  Action<IStatechartEvent> statechartTrigger)
		{
			_services = services;
			_gameDataProvider = gameLogic;
			_statechartTrigger = statechartTrigger;
			_audioBrState = new AudioBattleRoyaleState(services, gameLogic, statechartTrigger);
			_audioDmState = new AudioDeathmatchState(services, gameLogic, statechartTrigger);
		}

		/// <summary>
		/// Setups the Adventure gameplay state
		/// </summary>
		public void Setup(IStateFactory stateFactory)
		{
			var initial = stateFactory.Initial("AUDIO - Initial");
			var final = stateFactory.Final("AUDIO - Final");
			var audioBase = stateFactory.State("AUDIO - Audio Base");
			var mainMenu = stateFactory.State("AUDIO - Main Menu");
			var matchmaking = stateFactory.State("AUDIO - Matchmaking");
			var gameModeCheck = stateFactory.Choice("AUDIO - Game Mode Check");
			var battleRoyale = stateFactory.Nest("AUDIO - Battle Royale");
			var deathmatch = stateFactory.Nest("AUDIO - Deathmatch");
			var postGame = stateFactory.State("AUDIO - Post Game");
			var disconnected = stateFactory.State("AUDIO - Disconnected");

			initial.Transition().Target(audioBase);
			initial.OnExit(SubscribeEvents);

			audioBase.Event(MainMenuState.MainMenuLoadedEvent).Target(mainMenu);

			mainMenu.OnEnter(TryPlayMainMenuMusic);
			mainMenu.OnEnter(TransitionAudioMixerMain);
			mainMenu.Event(NetworkState.JoinedRoomEvent).Target(matchmaking);
			
			matchmaking.OnEnter(TryPlayLobbyMusic);
			matchmaking.OnEnter(TransitionAudioMixerLobby);
			matchmaking.Event(MatchState.MatchUnloadedEvent).Target(audioBase);
			matchmaking.Event(GameSimulationState.SimulationStartedEvent).OnTransition(PrepareForMatchMusic).Target(gameModeCheck);
			matchmaking.Event(NetworkState.PhotonDisconnectedEvent).OnTransition(StopMusicInstant).Target(disconnected);
			matchmaking.OnExit(TransitionAudioMixerMain);

			gameModeCheck.Transition().Condition(IsDeathmatch).Target(deathmatch);
			gameModeCheck.Transition().Target(battleRoyale);

			battleRoyale.Nest(_audioBrState.Setup).Target(postGame);
			battleRoyale.Event(GameSimulationState.GameCompleteExitEvent).Target(postGame);
			battleRoyale.Event(GameSimulationState.MatchEndedEvent).Target(postGame);
			battleRoyale.Event(GameSimulationState.MatchQuitEvent).OnTransition(StopMusicInstant).Target(audioBase);
			battleRoyale.Event(MatchState.MatchUnloadedEvent).Target(audioBase);
			battleRoyale.Event(NetworkState.PhotonDisconnectedEvent).Target(disconnected);

			deathmatch.Nest(_audioDmState.Setup).Target(postGame);
			deathmatch.Event(GameSimulationState.GameCompleteExitEvent).Target(postGame);
			deathmatch.Event(GameSimulationState.MatchEndedEvent).Target(postGame);
			deathmatch.Event(GameSimulationState.MatchQuitEvent).OnTransition(StopMusicInstant).Target(audioBase);
			deathmatch.Event(MatchState.MatchUnloadedEvent).Target(audioBase);
			deathmatch.Event(NetworkState.PhotonDisconnectedEvent).Target(disconnected);

			postGame.OnEnter(PlayPostMatchMusic);
			postGame.Event(MatchState.MatchUnloadedEvent).Target(audioBase);
			postGame.OnExit(StopMusicInstant);

			disconnected.OnEnter(StopMusicInstant);
			disconnected.Event(MainMenuState.MainMenuLoadedEvent).Target(mainMenu);
			disconnected.Event(NetworkState.JoinedRoomEvent).Target(matchmaking);

			final.OnEnter(UnsubscribeEvents);
		}

		private void SubscribeEvents()
		{
			QuantumEvent.SubscribeManual<EventOnPlayerDamaged>(this, OnPlayerDamaged);
			QuantumEvent.SubscribeManual<EventOnPlayerAttack>(this, OnPlayerAttack);
			QuantumEvent.SubscribeManual<EventOnCollectableCollected>(this, OnCollectableCollected);
			
		}

		private void UnsubscribeEvents()
		{
			QuantumEvent.UnsubscribeListener(this);
		}

		private bool IsDeathmatch()
		{
			return _services.NetworkService.CurrentRoomMapConfig.Value.GameMode == GameMode.Deathmatch;
		}

		private void PrepareForMatchMusic()
		{
			StopMusicInstant();
			_matchServices = MainInstaller.Resolve<IMatchServices>();
		}

		private void TryPlayMainMenuMusic()
		{
			if (!_services.AudioFxService.IsMusicPlaying)
			{
				_services.AudioFxService.PlayClip2D(AudioId.MusicMainStart, null, PlayMainLoop,
				                                    GameConstants.Audio.MIXER_GROUP_MUSIC_ID);
			}
		}

		private void PlayMainLoop(AudioSourceMonoComponent source)
		{
			_services.AudioFxService.PlayMusic(AudioId.MusicMainLoop);
		}

		private void TryPlayLobbyMusic()
		{
			if (!_services.AudioFxService.IsMusicPlaying)
			{
				_services.AudioFxService.PlayMusic(AudioId.MusicMainLoop,
				                                   GameConstants.Audio.MUSIC_SHORT_FADE_SECONDS);
			}
		}

		private void PlayPostMatchMusic()
		{
			var game = QuantumRunner.Default.Game;
			var frame = game.Frames.Verified;
			var container = frame.GetSingleton<GameContainer>();
			var playerData = container.GetPlayersMatchData(frame, out var leader);
			var playerWinner = playerData[leader];
			var victoryStatusAudio = AudioId.MusicDefeatJingle;
			
			if (_services.NetworkService.QuantumClient.LocalPlayer.IsSpectator() && 
			    _matchServices.SpectateService.SpectatedPlayer.Value.Player == leader)
			{
				victoryStatusAudio = AudioId.MusicVictoryJingle;
			}
			else if (game.PlayerIsLocal(leader))
			{
				victoryStatusAudio = AudioId.MusicVictoryJingle;
			}

			_services.AudioFxService.PlayClip2D(victoryStatusAudio, null, PlayPostMatchLoop,
			                                    GameConstants.Audio.MIXER_GROUP_MUSIC_ID);
		}

		private void PlayPostMatchLoop(AudioSourceMonoComponent source)
		{
			_services.AudioFxService.PlayMusic(AudioId.MusicPostMatchLoop);
		}

		private void StopMusicInstant()
		{
			_services.AudioFxService.StopMusic();
		}

		private void StopMusicFadeOut()
		{
			_services.AudioFxService.StopMusic(GameConstants.Audio.MUSIC_SHORT_FADE_SECONDS);
		}

		private void TransitionAudioMixerMain()
		{
			_services.AudioFxService.TransitionAudioMixer(GameConstants.Audio.MIXER_MAIN_SNAPSHOT_ID,
			                                              GameConstants.Audio.MIXER_SNAPSHOT_TRANSITION_SECONDS);
		}

		private void TransitionAudioMixerLobby()
		{
			_services.AudioFxService.TransitionAudioMixer(GameConstants.Audio.MIXER_LOBBY_SNAPSHOT_ID,
			                                              GameConstants.Audio.MIXER_SNAPSHOT_TRANSITION_SECONDS);
		}

		private void OnCollectableCollected(EventOnCollectableCollected callback)
		{
			if (_matchServices.EntityViewUpdaterService == null)
			{
				return;
			}
			var audio = AudioId.None;

			switch (callback.CollectableId)
			{
				case GameId.AmmoLarge:
					audio = AudioId.LargeAmmoPickup;
					break;
				case GameId.AmmoSmall:
					audio = AudioId.AmmoPickup;
					break;
				case GameId.Health:
					audio = AudioId.HealthPickup;
					break;
				case GameId.ShieldCapacityLarge:
					audio = AudioId.GearPickup;
					break;
				case GameId.ShieldCapacitySmall:
					audio = AudioId.GearPickup;
					break;
				case GameId.ShieldLarge:
					audio = AudioId.LargeShieldPickup;
					break;
				case GameId.ShieldSmall:
					audio = AudioId.ShieldPickup;
					break;
			}

			_matchServices.EntityViewUpdaterService.TryGetView(callback.PlayerEntity, out var entityView);

			if (audio != AudioId.None)
			{
				Log.Warn(audio);
				_services.AudioFxService.PlayClip3D(audio, entityView.transform.position);
			}
		}

		private void OnPlayerAttack(EventOnPlayerAttack callback)
		{
			if (_matchServices.EntityViewUpdaterService == null)
			{
				return;
			}

			if (_matchServices.EntityViewUpdaterService.TryGetView(callback.PlayerEntity, out var entityView))
			{
				var weaponConfig = _services.ConfigsProvider.GetConfig<AudioWeaponConfig>((int) callback.Weapon.GameId);

				var audio = _services.AudioFxService.PlayClip3D(weaponConfig.WeaponShotId,
				                                                entityView.transform.position);
				audio.SetFollowTarget(entityView.transform, Vector3.zero, Quaternion.identity);
			}
		}

		private void OnPlayerDamaged(EventOnPlayerDamaged callback)
		{
			if (_matchServices.EntityViewUpdaterService == null)
			{
				return;
			}

			if (_matchServices.EntityViewUpdaterService.TryGetView(callback.Entity, out var entityView))
			{
				var game = callback.Game;
				var audio = AudioId.None;

				if (_matchServices.SpectateService.SpectatedPlayer.Value.Player.Equals(callback.Player))
				{
					audio = callback.ShieldDamage > 0 ? AudioId.TakeShieldDamage : AudioId.TakeHealthDamage;
					if (callback.PreviousShield > 0 && callback.CurrentShield == 0)
						audio = AudioId.SelfShieldBreak;
				}
				else if (game.Frames.Verified.TryGet<PlayerCharacter>(callback.Attacker, out var player) &&
				         game.PlayerIsLocal(player.Player))
				{
					audio = callback.ShieldDamage > 0 ? AudioId.HitShieldDamage : AudioId.HitHealthDamage;
					if (callback.PreviousShield > 0 && callback.CurrentShield == 0)
						audio = AudioId.ShieldBreak;
				}

				if (audio != AudioId.None)
				{
					_services.AudioFxService.PlayClip3D(audio, entityView.transform.position);
				}
			}
		}
	}
}