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
			var postGameSpectatorCheck = stateFactory.Choice("AUDIO - Spectator Check");

			initial.Transition().Target(audioBase);
			initial.OnExit(SubscribeEvents);

			audioBase.Event(MainMenuState.MainMenuLoadedEvent).Target(mainMenu);
			
			mainMenu.OnEnter(TransitionAudioMixerMain);
			mainMenu.OnEnter(TryPlayMainMenuMusic);
			mainMenu.Event(NetworkState.JoinedRoomEvent).Target(matchmaking);

			matchmaking.OnEnter(TryPlayLobbyMusic);
			matchmaking.OnEnter(TransitionAudioMixerLobby);
			matchmaking.Event(MatchState.MatchUnloadedEvent).Target(audioBase);
			matchmaking.Event(GameSimulationState.SimulationStartedEvent).OnTransition(PrepareForMatchMusic).Target(gameModeCheck);
			matchmaking.Event(NetworkState.PhotonDisconnectedEvent).OnTransition(StopMusicInstant).Target(disconnected);
			matchmaking.OnExit(TransitionAudioMixerMain);

			gameModeCheck.Transition().Condition(IsDeathmatch).Target(deathmatch);
			gameModeCheck.Transition().Target(battleRoyale);

			battleRoyale.Nest(_audioBrState.Setup).Target(postGameSpectatorCheck);
			battleRoyale.Event(GameSimulationState.GameCompleteExitEvent).Target(postGameSpectatorCheck);
			battleRoyale.Event(GameSimulationState.MatchEndedEvent).Target(postGameSpectatorCheck);
			battleRoyale.Event(GameSimulationState.MatchQuitEvent).OnTransition(StopMusicInstant).Target(postGameSpectatorCheck);
			battleRoyale.Event(MatchState.MatchUnloadedEvent).Target(audioBase);
			battleRoyale.Event(NetworkState.PhotonDisconnectedEvent).Target(disconnected);

			deathmatch.Nest(_audioDmState.Setup).Target(postGameSpectatorCheck);
			deathmatch.Event(GameSimulationState.GameCompleteExitEvent).Target(postGameSpectatorCheck);
			deathmatch.Event(GameSimulationState.MatchEndedEvent).Target(postGameSpectatorCheck);
			deathmatch.Event(GameSimulationState.MatchQuitEvent).OnTransition(StopMusicInstant).Target(postGameSpectatorCheck);
			deathmatch.Event(MatchState.MatchUnloadedEvent).Target(audioBase);
			deathmatch.Event(NetworkState.PhotonDisconnectedEvent).Target(disconnected);
			
			postGameSpectatorCheck.Transition().Condition(IsSpectator).Target(audioBase);
			postGameSpectatorCheck.Transition().Target(postGame);
				
			postGame.OnEnter(StopMusicInstant);
			postGame.OnEnter(PlayPostMatchMusic);
			postGame.Event(MatchState.MatchUnloadedEvent).Target(audioBase);
			postGame.OnExit(StopMusicInstant);
			postGame.OnExit(StopAllSfx);

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
			QuantumEvent.SubscribeManual<EventOnDamageBlocked>(this, OnDamageBlocked);
			QuantumEvent.SubscribeManual<EventOnSpecialUsed>(this, OnSpecialUsed);
			QuantumEvent.SubscribeManual<EventOnAudioExplosion>(this, OnExplosionStart);
			QuantumEvent.SubscribeManual<EventOnChestOpened>(this, OnChestOpened);
			QuantumEvent.SubscribeManual<EventOnPlayerKilledPlayer>(this, OnPlayerKillPlayer);

		}

		private void UnsubscribeEvents()
		{
			QuantumEvent.UnsubscribeListener(this);
		}

		private bool IsSpectator()
		{
			return _services.NetworkService.QuantumClient.LocalPlayer.IsSpectator();
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
				_services.AudioFxService.PlaySequentialMusicTransition(AudioId.MusicMainStart, AudioId.MusicMainLoop);
			}
		}

		private void TryPlayLobbyMusic()
		{
			if (!_services.AudioFxService.IsMusicPlaying)
			{
				_services.AudioFxService.PlayMusic(AudioId.MusicMainLoop, GameConstants.Audio.MUSIC_SHORT_FADE_SECONDS);
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
			
			_services.AudioFxService.PlaySequentialMusicTransition(victoryStatusAudio, AudioId.MusicPostMatchLoop);
		}

		private void StopAllSfx()
		{
			_services.AudioFxService.StopAllSfx();
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

		private void OnPlayerKillPlayer(EventOnPlayerKilledPlayer callback)
		{
			if (_matchServices.EntityViewUpdaterService == null)
			{
				return;
			}

			var game = callback.Game;
			var audio = AudioId.None;

			if (_matchServices.EntityViewUpdaterService.TryGetView(callback.EntityKiller, out var entityView))
			{
				if (_matchServices.SpectateService.SpectatedPlayer.Value.Player.Equals(callback.EntityKiller))
				{
					audio = AudioId.PlayerKill;
				}
				else if (_matchServices.SpectateService.SpectatedPlayer.Value.Player.Equals(callback.EntityDead))
				{
					audio = AudioId.PlayerDeath;
				}
				else if (game.Frames.Verified.TryGet<PlayerCharacter>(callback.EntityKiller, out var killerPlayer) &&
						game.PlayerIsLocal(killerPlayer.Player))
				{
					audio = AudioId.PlayerKill;
				}
				else if (game.Frames.Verified.TryGet<PlayerCharacter>(callback.EntityDead, out var deadPlayer) &&
						game.PlayerIsLocal(deadPlayer.Player))
				{
					audio = AudioId.PlayerDeath;
				}

				if (audio != AudioId.None)
				{
					_services.AudioFxService.PlayClip3D(audio, entityView.transform.position);
				}
			}
		}

		private void OnChestOpened(EventOnChestOpened callback)
		{
			if (_matchServices.EntityViewUpdaterService == null)
			{
				return;
			}

			var audio = AudioId.ChestPickup;

			if (audio != AudioId.None)
			{
				var pos = new Vector3(callback.ChestPosition.X.AsFloat, callback.ChestPosition.Y.AsFloat, callback.ChestPosition.Z.AsFloat);
				_services.AudioFxService.PlayClip3D(audio, pos);
			}
		}

		private void OnExplosionStart(EventOnAudioExplosion callback)
		{
			if (_matchServices.EntityViewUpdaterService == null)
			{
				return;
			}

			var audio = AudioId.None;

			switch (callback.SourceId)
			{
				//specials
				case GameId.SpecialAimingGrenade:
					audio = AudioId.ExplosionMedium;
					break;
				case GameId.SpecialAimingAirstrike:
					audio = AudioId.ExplosionLarge;
					break;
				case GameId.SpecialAimingStunGrenade:
					audio = AudioId.ExplosionFlashBang;
					break;
				case GameId.SpecialSkyLaserBeam:
					audio = AudioId.ExplosionSciFi;
					break;
				//weapons
				case GameId.ApoRPG:
					audio = AudioId.ExplosionSmall;
					break;
				case GameId.ModLauncher:
					audio = AudioId.ExplosionSmall;
					break;
				case GameId.SciCannon:
					audio = AudioId.ExplosionSciFi;
					break;
			}

			if (audio != AudioId.None)
			{
				var pos = new Vector3(callback.LandPosition.X.AsFloat, callback.LandPosition.Y.AsFloat, callback.LandPosition.Z.AsFloat);
				_services.AudioFxService.PlayClip3D(audio, pos);
			}
		}

		private void OnSpecialUsed(EventOnSpecialUsed callback)
		{
			if (_matchServices.EntityViewUpdaterService == null)
			{
				return;
			}

			var audio = AudioId.None;

			switch (callback.SpecialType)
			{
				case SpecialType.Grenade:
					audio = AudioId.Dash;
					break;
				case SpecialType.StunGrenade:
					audio = AudioId.Dash;
					break;
				case SpecialType.ShieldedCharge:
					audio = AudioId.Dash;
					break;
			}

			if (_matchServices.EntityViewUpdaterService.TryGetView(callback.Entity, out var entityView))
			{
				if (audio != AudioId.None)
				{
					_services.AudioFxService.PlayClip3D(audio, entityView.transform.position);
				}
			}
		}

		private void OnCollectableCollected(EventOnCollectableCollected callback)
		{
			if (_matchServices.EntityViewUpdaterService == null)
			{
				return;
			}

			var audio = AudioId.None;
			var collectableId = callback.CollectableId;

			switch (collectableId)
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

			if (collectableId.IsInGroup(GameIdGroup.Weapon))
				audio = AudioId.WeaponPickup;

			if (collectableId.IsInGroup(GameIdGroup.Helmet) || collectableId.IsInGroup(GameIdGroup.Armor) || 
				collectableId.IsInGroup(GameIdGroup.Shield) || collectableId.IsInGroup(GameIdGroup.Amulet))
				audio = AudioId.GearPickup;

			if (_matchServices.EntityViewUpdaterService.TryGetView(callback.PlayerEntity, out var entityView))
			{
				if (audio != AudioId.None)
				{
					_services.AudioFxService.PlayClip3D(audio, entityView.transform.position);
				}
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

		private void OnDamageBlocked(EventOnDamageBlocked callback)
		{
			if (_matchServices.EntityViewUpdaterService == null)
			{
				return;
			}
			if (_matchServices.EntityViewUpdaterService.TryGetView(callback.Entity, out var entityView))
			{
				var audio = AudioId.DamageAbsorb;
				_services.AudioFxService.PlayClip3D(audio, entityView.transform.position);
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
				}
				else if (game.Frames.Verified.TryGet<PlayerCharacter>(callback.Attacker, out var player) &&
				         game.PlayerIsLocal(player.Player))
				{
					audio = callback.ShieldDamage > 0 ? AudioId.HitShieldDamage : AudioId.HitHealthDamage;
				}
				
				if (callback.PreviousShield > 0 && callback.CurrentShield == 0)
				{
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