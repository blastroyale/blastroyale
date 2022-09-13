using System;
using System.Collections;
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
		private List<LoopedAudioClip> _currentClips = new List<LoopedAudioClip>();
		private DateTime _voOneKillSfxAvailabilityTime = DateTime.UtcNow;
		private DateTime _voClutchSfxAvailabilityTime = DateTime.UtcNow;
		
		public AudioState(IGameDataProvider gameLogic, IGameServices services,
		                  Action<IStatechartEvent> statechartTrigger)
		{
			_services = services;
			_gameDataProvider = gameLogic;
			_statechartTrigger = statechartTrigger;
			_audioBrState = new AudioBattleRoyaleState(services, gameLogic, statechartTrigger);
			_audioDmState = new AudioDeathmatchState(services, gameLogic, statechartTrigger);
		}

		private struct LoopedAudioClip
		{
			public AudioSourceMonoComponent audioSource;
			public string[] despawnEvent;
			public EntityRef targetEntity;

			public LoopedAudioClip(AudioSourceMonoComponent audioSource, string[] despawnEvent, EntityRef targetEntity)
			{
				this.audioSource = audioSource;
				this.despawnEvent = despawnEvent;
				this.targetEntity = targetEntity;
			}
		}

		/// <summary>
		/// Setups the audio state - root state, and then per gamemode type nested states
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
			matchmaking.Event(GameSimulationState.SimulationStartedEvent).OnTransition(PrepareForMatchMusic)
			           .Target(gameModeCheck);
			matchmaking.Event(NetworkState.PhotonDisconnectedEvent).OnTransition(StopMusicInstant).Target(disconnected);
			matchmaking.OnExit(TransitionAudioMixerMain);

			gameModeCheck.Transition().Condition(ShouldUseDeathmatchSM).Target(deathmatch);
			gameModeCheck.Transition().Condition(ShouldUseBattleRoyaleSM).Target(battleRoyale);
			gameModeCheck.Transition().Target(battleRoyale);

			battleRoyale.Nest(_audioBrState.Setup).Target(postGameSpectatorCheck);
			battleRoyale.Event(GameSimulationState.GameCompleteExitEvent).Target(postGameSpectatorCheck);
			battleRoyale.Event(GameSimulationState.MatchEndedEvent).Target(postGameSpectatorCheck);
			battleRoyale.Event(GameSimulationState.MatchQuitEvent).OnTransition(StopMusicInstant)
			            .Target(postGameSpectatorCheck);
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
			postGame.OnEnter(PlayPostMatchAnnouncer);
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
			QuantumEvent.SubscribeManual<EventOnNewShrinkingCircle>(this, OnNewShrinkingCircle);
			QuantumEvent.SubscribeManual<EventOnPlayerSkydiveDrop>(this, OnPlayerSkydiveDrop);
			QuantumEvent.SubscribeManual<EventOnPlayerDamaged>(this, OnPlayerDamaged);
			QuantumEvent.SubscribeManual<EventOnPlayerAttack>(this, OnPlayerAttack);
			QuantumEvent.SubscribeManual<EventOnCollectableCollected>(this, OnCollectableCollected);
			QuantumEvent.SubscribeManual<EventOnDamageBlocked>(this, OnDamageBlocked);
			QuantumEvent.SubscribeManual<EventOnPlayerSpecialUsed>(this, OnSpecialUsed);
			QuantumEvent.SubscribeManual<EventOnRaycastShotExplosion>(this, OnRaycastShotExplosion);
			QuantumEvent.SubscribeManual<EventOnHazardLand>(this, OnHazardExplosion);
			QuantumEvent.SubscribeManual<EventOnProjectileExplosion>(this, OnProjectileExplosion);
			QuantumEvent.SubscribeManual<EventOnChestOpened>(this, OnChestOpened);
			QuantumEvent.SubscribeManual<EventOnPlayerKilledPlayer>(this, OnPlayerKilledPlayer);
			QuantumEvent.SubscribeManual<EventOnPlayerDead>(this, OnPlayerDead);
			QuantumEvent.SubscribeManual<EventOnAirDropDropped>(this, OnAirdropDropped);
			QuantumEvent.SubscribeManual<EventOnAirDropLanded>(this, OnAirdropLanded);
			QuantumEvent.SubscribeManual<EventOnAirDropCollected>(this, OnAirdropCollected);
			QuantumEvent.SubscribeManual<EventOnStartedCollecting>(this, OnStartCollection);
			QuantumEvent.SubscribeManual<EventOnStoppedCollecting>(this, OnCollectionStopped);
			QuantumEvent.SubscribeManual<EventOnCollectableBlocked>(this, OnCollectionBlocked);
			QuantumEvent.SubscribeManual<EventOnLocalPlayerSkydiveDrop>(this, OnLocalPlayerSkydiveDrop);
			QuantumEvent.SubscribeManual<EventOnLocalPlayerSkydiveLand>(this, OnLocalSkydiveEnd);
		}

		private void UnsubscribeEvents()
		{
			QuantumEvent.UnsubscribeListener(this);
			QuantumCallback.UnsubscribeListener(this);
		}

		private bool IsSpectator()
		{
			return _services.NetworkService.QuantumClient.LocalPlayer.IsSpectator();
		}

		private bool ShouldUseDeathmatchSM()
		{
			return _services.NetworkService.CurrentRoomGameModeConfig.Value.AudioStateMachine ==
			       AudioStateMachine.Deathmatch;
		}

		private bool ShouldUseBattleRoyaleSM()
		{
			return _services.NetworkService.CurrentRoomGameModeConfig.Value.AudioStateMachine ==
			       AudioStateMachine.BattleRoyale;
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
			container.GetPlayersMatchData(frame, out var leader);

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

		private void PlayPostMatchAnnouncer()
		{
			if (IsSpectator()) return;
			
			WipeSoundQueue();
			
			var game = QuantumRunner.Default.Game;
			var frame = game.Frames.Verified;
			var container = frame.GetSingleton<GameContainer>();
			var matchData = container.GetPlayersMatchData(frame, out var leader);
			var localPlayerData = matchData[game.GetLocalPlayers()[0]];
			var gameMode = _services.NetworkService.CurrentRoomGameModeConfig.Value;
			
			if (game.PlayerIsLocal(leader))
			{
				if (gameMode.CompletionStrategy == GameCompletionStrategy.KillCount && localPlayerData.Data.DeathCount == 0)
				{
					_services.AudioFxService.PlayClipQueued2D(AudioId.Vo_PerfectVictory);
				}
				else
				{
					_services.AudioFxService.PlayClipQueued2D(AudioId.Vo_Victory);
				}
			}
			else
			{
				_services.AudioFxService.PlayClipQueued2D(AudioId.Vo_GameOver);
			}
		}
		
		private void WipeSoundQueue()
		{
			_services.AudioFxService.WipeSoundQueue();
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

		/// <summary>
		/// Removes any currently playing looped clips on the target entity if the correct event is being called
		/// </summary>
		private void CheckClips(string currentEvent, EntityRef entity)
		{
			for (var i = _currentClips.Count - 1; i > -1; i--)
			{
				var clip = _currentClips[i];

				foreach (var evnt in clip.despawnEvent)
				{
					if (evnt == currentEvent && clip.targetEntity == entity)
					{
						clip.audioSource.StopAndDespawn();
						_currentClips.RemoveAt(i);
						break;
					}
				}
			}
		}

		private void OnPlayerSkydiveDrop(EventOnPlayerSkydiveDrop callback)
		{
			if (_matchServices.SpectateService.SpectatedPlayer.Value.Player == callback.Player)
			{
				_services.AudioFxService.PlayClip2D(AudioId.Vo_GameStart, GameConstants.Audio.MIXER_GROUP_DIALOGUE_ID);
			}
		}

		private void OnLocalPlayerSkydiveDrop(EventOnLocalPlayerSkydiveDrop callback)
		{
			if (!_matchServices.EntityViewUpdaterService.TryGetView(callback.Entity, out var entityView)) return;

			var despawnEvents = new[] {nameof(EventOnLocalPlayerSkydiveLand)};
			var position = entityView.transform.position;
			_services.AudioFxService.PlayClip3D(AudioId.AirdropDropped, position);
			var skydiveLoop = _services.AudioFxService.PlayClip3D(AudioId.SkydiveJetpackDiveLoop, position);

			skydiveLoop.SetFollowTarget(entityView.transform, Vector3.zero, Quaternion.identity);
			_currentClips.Add(new LoopedAudioClip(skydiveLoop, despawnEvents, callback.Entity));
		}

		private void OnLocalSkydiveEnd(EventOnLocalPlayerSkydiveLand callback)
		{
			CheckClips(nameof(EventOnLocalPlayerSkydiveLand), callback.Entity);
			if (!_matchServices.EntityViewUpdaterService.TryGetView(callback.Entity, out var entityView)) return;

			_services.AudioFxService.PlayClip3D(AudioId.SkydiveEnd, entityView.transform.position);
		}

		private void OnCollectionBlocked(EventOnCollectableBlocked callback)
		{
			CheckClips(nameof(EventOnCollectableBlocked), callback.CollectableEntity);
			if (!_matchServices.EntityViewUpdaterService.TryGetView(callback.PlayerEntity, out var entityView)) return;

			//TODO: replace this sfx with a proper sfx for your pickup being blocked
			_services.AudioFxService.PlayClip3D(AudioId.CollectionStop, entityView.transform.position);
		}

		private void OnCollectionStopped(EventOnStoppedCollecting callback)
		{
			CheckClips(nameof(EventOnStoppedCollecting), callback.CollectableEntity);
		}

		private void OnStartCollection(EventOnStartedCollecting callback)
		{
			if (!_matchServices.EntityViewUpdaterService.TryGetView(callback.PlayerEntity, out var entityView)) return;

			//TODO: add a rising pitch setting for looping sfx
			_services.AudioFxService.PlayClip3D(AudioId.CollectionStart, entityView.transform.position);
			var collectSfx =
				_services.AudioFxService.PlayClip3D(AudioId.CollectionLoop, entityView.transform.position);
			var despawnEvents = new[]
			{
				nameof(EventOnStoppedCollecting),
				nameof(EventOnCollectableBlocked),
				nameof(EventOnCollectableCollected)
			};
			_currentClips.Add(new LoopedAudioClip(collectSfx, despawnEvents, callback.CollectableEntity));
		}

		private void OnAirdropCollected(EventOnAirDropCollected callback)
		{
			CheckClips(nameof(EventOnAirDropCollected), callback.Entity);
		}

		private void OnNewShrinkingCircle(EventOnNewShrinkingCircle callback)
		{
			_services.CoroutineService.StartCoroutine(WaitForCircleShrinkCoroutine(callback));
		}

		private IEnumerator WaitForCircleShrinkCoroutine(EventOnNewShrinkingCircle callback)
		{
			var f = callback.Game.Frames.Verified;
			var allConfigs = _services.ConfigsProvider.GetConfigsList<QuantumShrinkingCircleConfig>();
			var config = allConfigs[callback.ShrinkingCircle.Step];
			var circle = f.GetSingleton<ShrinkingCircle>();
			var maxStep = allConfigs.Count - 1;
			var time = (circle.ShrinkingStartTime - f.Time - config.WarningTime).AsFloat;

			if (config.Step == maxStep)
			{
				_services.AudioFxService.PlayClipQueued2D(AudioId.Vo_CircleLastCountdown);
			}
			
			yield return new WaitForSeconds(time);
			
			time = (circle.ShrinkingStartTime - f.Time).AsFloat;
			
			yield return new WaitForSeconds(time);

			_services.AudioFxService.PlayClipQueued2D(config.Step == maxStep
				                                          ? AudioId.Vo_CircleLastClose
				                                          : AudioId.Vo_CircleClose);
		}

		private void OnAirdropDropped(EventOnAirDropDropped callback)
		{
			if (!_matchServices.EntityViewUpdaterService.TryGetView(callback.Entity, out var entityView)) return;

			var position = entityView.transform.position;
			_services.AudioFxService.PlayClip3D(AudioId.AirdropDropped, position);

			var dropsFx = _services.AudioFxService.PlayClip3D(AudioId.MissileFlyLoop, position);
			var despawnEvents = new[] {nameof(EventOnAirDropLanded)};
			_currentClips.Add(new LoopedAudioClip(dropsFx, despawnEvents, callback.Entity));

			_services.AudioFxService.PlayClipQueued2D(AudioId.Vo_AirdropComing);
		}

		private void OnAirdropLanded(EventOnAirDropLanded callback)
		{
			if (!_matchServices.EntityViewUpdaterService.TryGetView(callback.Entity, out var entityView)) return;

			var position = entityView.transform.position;
			_services.AudioFxService.PlayClip3D(AudioId.AirdropLanded, position);

			var flareSfx = _services.AudioFxService.PlayClip3D(AudioId.AirdropFlare, position);
			var despawnEvents = new[] {nameof(EventOnAirDropCollected)};
			_currentClips.Add(new LoopedAudioClip(flareSfx, despawnEvents, callback.Entity));

			_services.AudioFxService.PlayClipQueued2D(AudioId.Vo_AirdropLanded);
		}

		private void OnPlayerDead(EventOnPlayerDead callback)
		{
			if (_matchServices.SpectateService.SpectatedPlayer.Value.Entity != callback.Entity) return;

			_services.AudioFxService.PlayClip2D(AudioId.PlayerDeath);

			if (QuantumRunner.Default.Game.PlayerIsLocal(callback.Player))
			{
				_services.AudioFxService.PlayClipQueued2D(AudioId.Vo_OnDeath);
			}
		}

		private void OnPlayerKilledPlayer(EventOnPlayerKilledPlayer callback)
		{
			// If not a kill of spectated player, or spectated player committed suicide
			if (_matchServices.SpectateService.SpectatedPlayer.Value.Entity != callback.EntityKiller || 
			    callback.EntityKiller == callback.EntityDead)
			{
				return;
			}

			var game = callback.Game;
			var frame = game.Frames.Verified;
			var container = frame.GetSingleton<GameContainer>();
			var killAudio = AudioId.None;
			var voMultiKillAudio = AudioId.None;
			var voKillstreakAudio = AudioId.None;

			// Kill SFX
			switch (callback.CurrentMultiKill)
			{
				case 1:
					killAudio = AudioId.PlayerKillLevel1;
					voMultiKillAudio = AudioId.Vo_Kills1;
					break;

				case 2:
					killAudio = AudioId.PlayerKillLevel2;
					voMultiKillAudio = AudioId.Vo_Kills2;
					break;

				case 3:
					killAudio = AudioId.PlayerKillLevel3;
					voMultiKillAudio = AudioId.Vo_Kills3;
					break;

				case 4:
					killAudio = AudioId.PlayerKillLevel4;
					voMultiKillAudio = AudioId.Vo_Kills4;
					break;

				case 5:
					killAudio = AudioId.PlayerKillLevel5;
					voMultiKillAudio = AudioId.Vo_Kills5;
					break;

				default:
					if (callback.CurrentMultiKill > 5)
					{
						killAudio = AudioId.PlayerKillLevel6;
						voMultiKillAudio = AudioId.Vo_Kills5;
					}

					break;
			}

			switch (callback.CurrentKillStreak)
			{
				case 3:
					voKillstreakAudio = AudioId.Vo_KillStreak3;
					break;

				case 5:
					voKillstreakAudio = AudioId.Vo_KillStreak5;
					break;

				case 7:
					voKillstreakAudio = AudioId.Vo_KillStreak7;
					break;

				case 9:
					voKillstreakAudio = AudioId.Vo_KillStreak9;
					break;
			}

			// Kill SFX
			_services.AudioFxService.PlayClip2D(killAudio);
			
			// Multikill announcer
			if (callback.CurrentMultiKill > 1)
			{
				_services.AudioFxService.PlayClipQueued2D(voMultiKillAudio);
			}
			else if (callback.CurrentMultiKill <= 1 && DateTime.UtcNow >= _voOneKillSfxAvailabilityTime)
			{
				_services.AudioFxService.PlayClipQueued2D(voMultiKillAudio);
				_voOneKillSfxAvailabilityTime = DateTime.UtcNow.AddSeconds(GameConstants.Audio.VO_DUPLICATE_SFX_PREVENTION_SECONDS);
			}
			
			// Clutch announcer
			var stats = frame.Get<Stats>(callback.EntityKiller);
			var maxHealth = stats.Values[(int) StatType.Health].StatValue.AsInt;
			var maxShield = stats.Values[(int) StatType.Shield].StatValue.AsInt;
			var percent = (maxHealth + maxShield) / (stats.CurrentHealth + stats.CurrentShield);
			
			if (percent <= GameConstants.Audio.LOW_HP_CLUTCH_THERSHOLD_PERCENT &&
			    DateTime.UtcNow >= _voClutchSfxAvailabilityTime) 
			{
				_services.AudioFxService.PlayClipQueued2D(AudioId.Vo_KillLowHp);
				_voClutchSfxAvailabilityTime = DateTime.UtcNow.AddSeconds(GameConstants.Audio.VO_DUPLICATE_SFX_PREVENTION_SECONDS);
			}

			// Killstreak announcer
			_services.AudioFxService.PlayClipQueued2D(voKillstreakAudio);
		}

		private void OnChestOpened(EventOnChestOpened callback)
		{
			_services.AudioFxService.PlayClip3D(AudioId.ChestPickup, callback.ChestPosition.ToUnityVector3());
		}

		private void OnRaycastShotExplosion(EventOnRaycastShotExplosion callback)
		{
			PlayExplosionSfx(callback.sourceId, callback.EndPosition.ToUnityVector3());
		}

		private void OnHazardExplosion(EventOnHazardLand callback)
		{
			PlayExplosionSfx(callback.sourceId, callback.HitPosition.ToUnityVector3());
		}

		private void OnProjectileExplosion(EventOnProjectileExplosion callback)
		{
			PlayExplosionSfx(callback.sourceId, callback.EndPosition.ToUnityVector3());
		}

		private void PlayExplosionSfx(GameId sourceId, Vector3 endPosition)
		{
			var audio = AudioId.None;

			switch (sourceId)
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
				_services.AudioFxService.PlayClip3D(audio, endPosition);
			}
		}

		private void OnSpecialUsed(EventOnPlayerSpecialUsed callback)
		{
			if (!_matchServices.EntityViewUpdaterService.TryGetView(callback.Entity, out var entityView)) return;

			var audio = AudioId.None;

			switch (callback.Special.SpecialType)
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
				case SpecialType.Airstrike:
					audio = AudioId.MissileFlyLoop;
					break;
			}

			if (audio == AudioId.MissileFlyLoop)
			{
				var missileLoop = _services.AudioFxService.PlayClip3D(audio, entityView.transform.position);
				var despawnEvents = new[] {nameof(EventOnHazardLand)};
				_currentClips.Add(new LoopedAudioClip(missileLoop, despawnEvents, callback.Entity));
			}
			else if (audio != AudioId.None)
			{
				_services.AudioFxService.PlayClip3D(audio, entityView.transform.position);
			}
		}

		private void OnCollectableCollected(EventOnCollectableCollected callback)
		{
			CheckClips(nameof(EventOnCollectableCollected), callback.CollectableEntity);

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
			{
				audio = AudioId.WeaponPickup;
			}
			else if (collectableId.IsInGroup(GameIdGroup.Equipment))
			{
				audio = AudioId.GearPickup;
			}

			if (_matchServices.EntityViewUpdaterService.TryGetView(callback.PlayerEntity, out var entityView) &&
			    audio != AudioId.None)
			{
				_services.AudioFxService.PlayClip3D(audio, entityView.transform.position);
			}
		}

		private void OnPlayerAttack(EventOnPlayerAttack callback)
		{
			if (!_matchServices.EntityViewUpdaterService.TryGetView(callback.PlayerEntity, out var entityView)) return;

			var weaponConfig = _services.ConfigsProvider.GetConfig<AudioWeaponConfig>((int) callback.Weapon.GameId);
			var audio = _services.AudioFxService.PlayClip3D(weaponConfig.WeaponShotId,
			                                                entityView.transform.position);
			audio.SetFollowTarget(entityView.transform, Vector3.zero, Quaternion.identity);
		}

		private void OnDamageBlocked(EventOnDamageBlocked callback)
		{
			if (!_matchServices.EntityViewUpdaterService.TryGetView(callback.Entity, out var entityView)) return;

			var audio = AudioId.DamageAbsorb;
			_services.AudioFxService.PlayClip3D(audio, entityView.transform.position);
		}

		private void OnPlayerDamaged(EventOnPlayerDamaged callback)
		{
			if (!_matchServices.EntityViewUpdaterService.TryGetView(callback.Entity, out var entityView)) return;

			var game = callback.Game;
			var audio = AudioId.None;
			var damagedPlayerIsLocal = _matchServices.SpectateService.SpectatedPlayer.Value.Player == callback.Player;

			if (damagedPlayerIsLocal)
			{
				audio = callback.ShieldDamage > 0 ? AudioId.TakeShieldDamage : AudioId.TakeHealthDamage;
			}
			else
			{
				audio = callback.ShieldDamage > 0 ? AudioId.HitShieldDamage : AudioId.HitHealthDamage;
			}

			if (callback.ShieldDamage > 0 && callback.HealthDamage > 0)
			{
				audio = damagedPlayerIsLocal ? AudioId.SelfShieldBreak : AudioId.ShieldBreak;
			}

			if (damagedPlayerIsLocal)
			{
				_services.AudioFxService.PlayClip2D(audio);
			}
			else
			{
				_services.AudioFxService.PlayClip3D(audio, entityView.transform.position);
			}
		}
	}
}