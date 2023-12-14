using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Configs;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Services;
using FirstLight.Statechart;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.StateMachines
{
	/// <summary>
	/// This object contains the behaviour logic to control all the game audio in the <seealso cref="GameStateMachine"/>
	/// </summary>
	public class AudioState
	{
		private const string SPECTATED_PLAYER_CHANGED_EVENT = "SpectatedPlayerChangedEvent";

		private readonly IGameServices _services;
		private readonly IGameDataProvider _gameDataProvider;
		private readonly AudioBattleRoyaleState _audioBrState;
		private readonly Action<IStatechartEvent> _statechartTrigger;

		private IMatchServices _matchServices;
		private List<LoopedAudioClip> _currentClips = new List<LoopedAudioClip>();
		private DateTime _voOneKillSfxAvailabilityTime = DateTime.UtcNow;
		private DateTime _voClutchSfxAvailabilityTime = DateTime.UtcNow;
		private bool _gameRunning;
		private List<AudioId> _ambienceList = new List<AudioId>();

		public AudioState(IGameDataProvider gameLogic, IGameServices services,
						  Action<IStatechartEvent> statechartTrigger)
		{
			_services = services;
			_gameDataProvider = gameLogic;
			_statechartTrigger = statechartTrigger;
			_audioBrState = new AudioBattleRoyaleState(services, statechartTrigger);
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
			var postGame = stateFactory.State("AUDIO - Post Game");
			var disconnected = stateFactory.State("AUDIO - Disconnected");
			var postGameSpectatorCheck = stateFactory.Choice("AUDIO - Spectator Check");

			initial.Transition().Target(audioBase);
			initial.OnExit(SubscribeMessages);

			audioBase.Event(MainMenuState.MainMenuLoadedEvent).Target(mainMenu);
			audioBase.Event(NetworkState.JoinedRoomEvent).Target(matchmaking);

			mainMenu.OnEnter(StopAllSfx);
			mainMenu.OnEnter(TransitionAudioMixerMain);
			mainMenu.OnEnter(TryPlayMainMenuMusic);
			mainMenu.Event(NetworkState.JoinedRoomEvent).Target(matchmaking);
			mainMenu.Event(NetworkState.JoinedPlayfabMatchmaking).Target(matchmaking);

			matchmaking.OnEnter(TryPlayLobbyMusic);
			matchmaking.OnEnter(TransitionAudioMixerLobby);
			matchmaking.Event(MatchState.MatchUnloadedEvent).Target(audioBase);
			matchmaking.Event(GameSimulationState.SimulationStartedEvent).Target(gameModeCheck);
			matchmaking.Event(NetworkState.PhotonDisconnectedEvent).OnTransition(StopMusicInstant).Target(disconnected);
			matchmaking.OnExit(TransitionAudioMixerMain);

			gameModeCheck.OnEnter(SetMatchServices);
			gameModeCheck.OnEnter(SubscribeMatchEvents);
			gameModeCheck.OnEnter(PrepareForMatchMusic);
			gameModeCheck.Transition().Target(battleRoyale);
			gameModeCheck.OnExit(() => SetSimulationRunning(true));

			battleRoyale.Nest(_audioBrState.Setup).Target(postGameSpectatorCheck);
			battleRoyale.Event(NetworkState.PhotonDisconnectedEvent).Target(disconnected);
			battleRoyale.Event(MatchState.MatchCompleteExitEvent).Target(postGameSpectatorCheck);
			battleRoyale.Event(MatchState.MatchEndedEvent).Target(postGameSpectatorCheck);
			battleRoyale.Event(MatchState.MatchQuitEvent).OnTransition(StopAllAudio).Target(audioBase);
			battleRoyale.Event(MatchState.MatchUnloadedEvent).OnTransition(StopAllAudio).Target(audioBase);
			battleRoyale.OnExit(UnsubscribeMatchEvents);
			battleRoyale.OnExit(() => SetSimulationRunning(false));
            

			postGameSpectatorCheck.Transition().Condition(IsSpectator).OnTransition(StopMusicInstant).Target(audioBase);
			postGameSpectatorCheck.Transition().Target(postGame);
			postGameSpectatorCheck.OnExit(StopAllSfx);

			postGame.OnEnter(StopMusicInstant);
			postGame.OnEnter(PlayPostMatchMusic);
			postGame.OnEnter(PlayPostMatchAnnouncer);
			postGame.Event(MatchState.MatchStateEndingEvent).Target(audioBase);
			postGame.OnExit(StopMusicInstant);
			postGame.OnExit(StopAllSfx);

			disconnected.OnEnter(StopAllSfx);
			disconnected.OnEnter(StopMusicInstant);
			disconnected.Event(MainMenuState.MainMenuLoadedEvent).Target(mainMenu);
			disconnected.Event(NetworkState.JoinedRoomEvent).Target(matchmaking);
			disconnected.Event(NetworkState.JoinedPlayfabMatchmaking).Target(matchmaking);
		}

		private void SubscribeMessages()
		{
			_services.MessageBrokerService.Subscribe<MatchCountdownStartedMessage>(OnMatchCountdownStarted);
			_services.MessageBrokerService.Subscribe<ApplicationPausedMessage>(OnApplicationPausedMessage);
			_services.MessageBrokerService.Subscribe<PlayerEnteredAmbienceMessage>(OnPlayerEnteredAmbienceMessage);
			_services.MessageBrokerService.Subscribe<PlayerLeftAmbienceMessage>(OnPlayerLeftAmbienceMessage);
		}

		private void SubscribeMatchEvents()
		{
			_matchServices.SpectateService.SpectatedPlayer.Observe(OnSpectatedPlayerChanged);

			QuantumEvent.SubscribeManual<EventOnNewShrinkingCircle>(this, OnNewShrinkingCircle);
			QuantumEvent.SubscribeManual<EventOnPlayerSkydiveDrop>(this, OnPlayerSkydiveDrop);
			QuantumEvent.SubscribeManual<EventOnEntityDamaged>(this, OnEntityDamaged);
			QuantumEvent.SubscribeManual<EventOnPlayerAttack>(this, OnPlayerAttack);
			QuantumEvent.SubscribeManual<EventOnCollectableCollected>(this, OnCollectableCollected);
			QuantumEvent.SubscribeManual<EventOnDamageBlocked>(this, OnDamageBlocked);
			QuantumEvent.SubscribeManual<EventOnPlayerSpecialUsed>(this, OnSpecialUsed);
			QuantumEvent.SubscribeManual<EventOnRaycastShotExplosion>(this, OnEventOnRaycastShotExplosion);
			QuantumEvent.SubscribeManual<EventOnHazardLand>(this, OnEventHazardLand);
			QuantumEvent.SubscribeManual<EventOnProjectileEndOfLife>(this, OnProjectileEndOfLife);
			QuantumEvent.SubscribeManual<EventOnChestOpened>(this, OnEventOnChestOpened);
			QuantumEvent.SubscribeManual<EventOnPlayerKilledPlayer>(this, OnPlayerKilledPlayer);
			QuantumEvent.SubscribeManual<EventOnPlayerDead>(this, OnPlayerDead);
			QuantumEvent.SubscribeManual<EventOnLocalPlayerDead>(this, OnLocalPlayerDead);
			QuantumEvent.SubscribeManual<EventOnAirDropDropped>(this, OnAirdropDropped);
			QuantumEvent.SubscribeManual<EventOnAirDropLanded>(this, OnAirdropLanded);
			QuantumEvent.SubscribeManual<EventOnAirDropCollected>(this, OnAirdropCollected);
			QuantumEvent.SubscribeManual<EventOnLocalPlayerSkydiveDrop>(this, OnLocalPlayerSkydiveDrop);
			QuantumEvent.SubscribeManual<EventOnLocalPlayerSkydiveLand>(this, OnLocalSkydiveEnd);
			QuantumEvent.SubscribeManual<EventOnPlayerAlive>(this, OnPlayerAlive);
			QuantumEvent.SubscribeManual<EventOnPlayerSpawned>(this, OnPlayerSpawned);
			QuantumEvent.SubscribeManual<EventOnPlayerWeaponChanged>(this, OnPlayerWeaponChanged);
			QuantumEvent.SubscribeManual<EventOnPlayerReloadStart>(this, OnPlayerStartReload);
			QuantumEvent.SubscribeManual<EventOnPlayerMagazineReloaded>(this, OnPlayerMagazineReloaded);
		}

		private void UnsubscribeMatchEvents()
		{
			QuantumEvent.UnsubscribeListener(this);
			_matchServices?.SpectateService?.SpectatedPlayer?.StopObserving(OnSpectatedPlayerChanged);
		}

		private void OnSpectatedPlayerChanged(SpectatedPlayer previous, SpectatedPlayer next)
		{
			CheckDespawnClips(SPECTATED_PLAYER_CHANGED_EVENT, previous.Entity);
		}

		private bool IsSpectator()
		{
			return _services.RoomService.IsLocalPlayerSpectator;
		}
        

		private void SetMatchServices()
		{
			_matchServices = MainInstaller.Resolve<IMatchServices>();
		}

		private void PrepareForMatchMusic()
		{
			StopMusicInstant();
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
			if (!_services.AudioFxService.IsMusicPlaying || !IsResyncing()) return;

			_services.AudioFxService.PlayMusic(AudioId.MusicMainLoop, GameConstants.Audio.MUSIC_SHORT_FADE_SECONDS);
		}

		private void PlayPostMatchMusic()
		{
			var game = QuantumRunner.Default.Game;
			game.GeneratePlayersMatchDataLocal(out var leader, out var localWinner);

			var victoryStatusAudio = AudioId.MusicDefeatJingle;

			if (_services.RoomService.IsLocalPlayerSpectator &&
				_matchServices.SpectateService.SpectatedPlayer.Value.Player == leader)
			{
				victoryStatusAudio = AudioId.MusicVictoryJingle;
			}
			else if (localWinner)
			{
				victoryStatusAudio = AudioId.MusicVictoryJingle;
			}

			_services.AudioFxService.PlaySequentialMusicTransition(victoryStatusAudio, AudioId.MusicPostMatchLoop);
		}

		private void PlayPostMatchAnnouncer()
		{
			if (IsSpectator()) return;

			_services.AudioFxService.WipeSoundQueue();

			var game = QuantumRunner.Default.Game;
			var matchData = game.GeneratePlayersMatchDataLocal(out var leader, out var localWinner);
			var localPlayerData = matchData[game.GetLocalPlayerRef()];
			var gameMode = _services.RoomService.CurrentRoom.GameModeConfig;

			if (localWinner)
			{
				if (gameMode.CompletionStrategy == GameCompletionStrategy.KillCount &&
					localPlayerData.Data.DeathCount == 0)
				{
					_services.AudioFxService.PlayClipQueued2D(AudioId.Vo_PerfectVictory,
						GameConstants.Audio.MIXER_GROUP_DIALOGUE_ID);
				}
				else
				{
					_services.AudioFxService.PlayClipQueued2D(AudioId.Vo_Victory,
						GameConstants.Audio.MIXER_GROUP_DIALOGUE_ID);
				}
			}
			else
			{
				_services.AudioFxService.PlayClipQueued2D(AudioId.Vo_GameOver,
					GameConstants.Audio.MIXER_GROUP_DIALOGUE_ID);
			}
		}

		private void StopAllAudio()
		{
			_services.AudioFxService.StopAmbience();
			_services.AudioFxService.StopAllSfx();
			_services.AudioFxService.WipeSoundQueue();
			_currentClips.Clear();
			StopMusicInstant();
		}

		private void StopAllSfx()
		{
			_services.AudioFxService.StopAllSfx();
			_services.AudioFxService.WipeSoundQueue();
			_currentClips.Clear();
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
				GameConstants.Audio.MIXER_MUSIC_TRANSITION_SECONDS);
		}

		private void TransitionAudioMixerLobby()
		{
			if (IsResyncing()) return;

			_services.AudioFxService.TransitionAudioMixer(GameConstants.Audio.MIXER_LOBBY_SNAPSHOT_ID,
				GameConstants.Audio.MIXER_MUSIC_TRANSITION_SECONDS);
		}

		/// <summary>
		/// Stops and despawns any currently playing looped clips for given entity, if matching despawn event is called
		/// </summary>
		private void CheckDespawnClips(string currentEvent, EntityRef entity)
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

		private void OnMatchCountdownStarted(MatchCountdownStartedMessage msg)
		{
			_services.CoroutineService.StartCoroutine(MatchCountdownCoroutine());
		}

		private void OnApplicationPausedMessage(ApplicationPausedMessage message)
		{
			if (message.IsPaused)
			{
				StopAllSfx();
			}
		}

		private IEnumerator MatchCountdownCoroutine()
		{
			var waitOneSec = new WaitForSeconds(1f);

			_services.AudioFxService.PlayClip2D(AudioId.Vo_Countdown3, GameConstants.Audio.MIXER_GROUP_DIALOGUE_ID);
			yield return waitOneSec;
			_services.AudioFxService.PlayClip2D(AudioId.Vo_Countdown2, GameConstants.Audio.MIXER_GROUP_DIALOGUE_ID);
			yield return waitOneSec;
			_services.AudioFxService.PlayClip2D(AudioId.Vo_Countdown1, GameConstants.Audio.MIXER_GROUP_DIALOGUE_ID);
			yield return waitOneSec;
			_services.AudioFxService.PlayClip2D(AudioId.Vo_CountdownGo, GameConstants.Audio.MIXER_GROUP_DIALOGUE_ID);
		}

		private void OnPlayerAlive(EventOnPlayerAlive callback)
		{
			if (!_matchServices.EntityViewUpdaterService.TryGetView(callback.Entity, out var entityView)) return;

			// Respawnable game-mode
			if (_services.RoomService.CurrentRoom.GameModeConfig.Lives is 0 or > 1)
			{
				_services.AudioFxService.PlayClip3D(AudioId.PlayerRespawnLightningBolt, entityView.transform.position);
				CheckDespawnClips(nameof(EventOnPlayerAlive), callback.Entity);
			}
		}

		private void OnPlayerSpawned(EventOnPlayerSpawned callback)
		{
			if (!_matchServices.EntityViewUpdaterService.TryGetView(callback.Entity, out var entityView)) return;

			var gameModeId = _services.GameModeService.SelectedGameMode.Value.Entry.GameModeId;
			var gameModeConfig = _services.ConfigsProvider.GetConfig<QuantumGameModeConfig>(gameModeId);

			// Respawnable game-mode
			if (gameModeConfig.Lives is 0 or > 1)
			{
				var despawnEvents = new[] {nameof(EventOnPlayerAlive)};
				var audioSource =
					_services.AudioFxService.PlayClip3D(AudioId.PlayerRespawnShine, entityView.transform.position);
				_currentClips.Add(new LoopedAudioClip(audioSource, despawnEvents, callback.Entity));
			}
		}

		private void OnPlayerSkydiveDrop(EventOnPlayerSkydiveDrop callback)
		{
			if (_matchServices.SpectateService.SpectatedPlayer.Value.Player != callback.Player) return;

			_services.AudioFxService.PlayClip2D(AudioId.Vo_GameStart, GameConstants.Audio.MIXER_GROUP_DIALOGUE_ID);
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
			CheckDespawnClips(nameof(EventOnLocalPlayerSkydiveLand), callback.Entity);
			if (!_matchServices.EntityViewUpdaterService.TryGetView(callback.Entity, out var entityView)) return;
			var f = callback.Game.Frames.Verified;

			_services.AudioFxService.PlayClip3D(AudioId.SkydiveEnd, entityView.transform.position);
		}

		private void OnPlayerWeaponChanged(EventOnPlayerWeaponChanged callback)
		{
			if (!callback.Game.PlayerIsLocal(callback.Player)) return;
			CheckDespawnClips(nameof(EventOnPlayerWeaponChanged), callback.Entity);

			if (!_matchServices.EntityViewUpdaterService.TryGetView(callback.Entity, out var entityView)) return;

			if (callback.Weapon.GameId != GameId.Random)
			{
				_services.AudioFxService.PlayClip3D(AudioId.WeaponSwitch, entityView.transform.position);
			}
			//TODO: have a negative sound for trying to swap to an empty weapon slot or an unavailable weapon
		}

		private void OnAirdropCollected(EventOnAirDropCollected callback)
		{
			CheckDespawnClips(nameof(EventOnAirDropCollected), callback.Entity);
		}


		private void SetSimulationRunning(bool running)
		{
			_gameRunning = running;
		}

		private void OnNewShrinkingCircle(EventOnNewShrinkingCircle callback)
		{
			_services.CoroutineService.StartCoroutine(WaitForCircleShrinkCoroutine(callback));
		}

		private IEnumerator WaitForCircleShrinkCoroutine(EventOnNewShrinkingCircle callback)
		{
			var f = callback.Game.Frames.Verified;

			if (callback.ShrinkingCircle.Step >= f.Context.MapShrinkingCircleConfigs.Count())
			{
				yield break;
			}
			
			var config = f.Context.MapShrinkingCircleConfigs[Math.Clamp(callback.ShrinkingCircle.Step - 1,
			                                                            0,
			                                                            f.Context.MapShrinkingCircleConfigs.Count - 1)];

			var circle = f.GetSingleton<ShrinkingCircle>();

			// We don't play on the last step, so we get the previous one as the max
			var maxStepForCircleClosing = Math.Max(1, f.Context.MapShrinkingCircleConfigs.Count - 2);
			var stepForFinalCountdown = Math.Max(1, f.Context.MapShrinkingCircleConfigs.Count - 1);

			var time = (circle.ShrinkingStartTime - f.Time - config.WarningTime).AsFloat;

			yield return new WaitForSeconds(time);
			if (!_gameRunning) yield break;

			if (config.Step == stepForFinalCountdown)
			{
				_services.AudioFxService.PlayClipQueued2D(AudioId.Vo_CircleLastCountdown,
					GameConstants.Audio.MIXER_GROUP_DIALOGUE_ID);
			}

			time = (circle.ShrinkingStartTime - f.Time).AsFloat;

			yield return new WaitForSeconds(time);
			if (!_gameRunning) yield break;

			if (config.Step <= maxStepForCircleClosing)
			{
				_services.AudioFxService.PlayClipQueued2D(config.Step == maxStepForCircleClosing
					? AudioId.Vo_CircleLastClose
					: AudioId.Vo_CircleClose, GameConstants.Audio.MIXER_GROUP_DIALOGUE_ID);
			}
		}

		private void OnAirdropDropped(EventOnAirDropDropped callback)
		{
			if (!_matchServices.EntityViewUpdaterService.TryGetView(callback.Entity, out var entityView)) return;

			var position = entityView.transform.position;
			_services.AudioFxService.PlayClip3D(AudioId.AirdropDropped, position);

			var dropsFx = _services.AudioFxService.PlayClip3D(AudioId.MissileFlyLoop, position);
			var despawnEvents = new[] {nameof(EventOnAirDropLanded)};
			_currentClips.Add(new LoopedAudioClip(dropsFx, despawnEvents, callback.Entity));

			_services.AudioFxService.PlayClipQueued2D(AudioId.Vo_AirdropComing, GameConstants.Audio.MIXER_GROUP_DIALOGUE_ID);
		}

		private void OnAirdropLanded(EventOnAirDropLanded callback)
		{
			if (!_matchServices.EntityViewUpdaterService.TryGetView(callback.Entity, out var entityView)) return;

			var position = entityView.transform.position;
			_services.AudioFxService.PlayClip3D(AudioId.AirdropLanded, position);

			_services.AudioFxService.PlayClipQueued2D(AudioId.Vo_AirdropLanded,
				GameConstants.Audio.MIXER_GROUP_DIALOGUE_ID);
		}

		private void OnPlayerStartReload(EventOnPlayerReloadStart callback)
		{
			if (_matchServices.SpectateService.SpectatedPlayer.Value.Entity != callback.Entity) return;

			var audioId = AudioId.None;

			switch (_gameDataProvider.EquipmentDataProvider.GetManufacturer(callback.Weapon))
			{
				case EquipmentManufacturer.Military:
					audioId = AudioId.ReloadMmsLoop;
					break;

				case EquipmentManufacturer.Futuristic:
					audioId = AudioId.ReloadEdiLoop;
					break;

				case EquipmentManufacturer.Apocalyptic:
					audioId = AudioId.ReloadPlaLoop;
					break;
			}

			var despawnEvents = new[]
			{
				nameof(EventOnPlayerMagazineReloaded),
				nameof(EventOnPlayerAttack),
				nameof(EventOnPlayerWeaponChanged),
				nameof(EventOnPlayerDead),
				SPECTATED_PLAYER_CHANGED_EVENT
			};

			var reloadSfx = _services.AudioFxService.PlayClip2D(audioId);
			_currentClips.Add(new LoopedAudioClip(reloadSfx, despawnEvents, callback.Entity));
		}

		private void OnPlayerMagazineReloaded(EventOnPlayerMagazineReloaded callback)
		{
			CheckDespawnClips(nameof(EventOnPlayerMagazineReloaded), callback.Entity);
		}

		private void OnPlayerDead(EventOnPlayerDead callback)
		{
			CheckDespawnClips(nameof(EventOnPlayerDead), callback.Entity);

			if (!_matchServices.EntityViewUpdaterService.TryGetView(callback.Entity, out var entityView)) return;
			var f = callback.Game.Frames.Verified;

			_services.AudioFxService.PlayClip3D(AudioId.PlayerDeath, entityView.transform.position);
		}

		private void OnLocalPlayerDead(EventOnLocalPlayerDead callback)
		{
			_services.AudioFxService.PlayClipQueued2D(AudioId.Vo_OnDeath, GameConstants.Audio.MIXER_GROUP_DIALOGUE_ID);
		}

		private void OnProjectileEndOfLife(EventOnProjectileEndOfLife callback)
		{
			if (callback.SubProjectile)
			{
				return;
			}

			var weaponConfig = _services.ConfigsProvider.GetConfig<AudioWeaponConfig>((int) callback.SourceId);
			_services.AudioFxService.PlayClip3D(weaponConfig.ProjectileEndOfLife, callback.EndPosition.ToUnityVector3());
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
				_services.AudioFxService.PlayClipQueued2D(voMultiKillAudio,
					GameConstants.Audio.MIXER_GROUP_DIALOGUE_ID);
			}
			else if (callback.CurrentMultiKill <= 1 && DateTime.UtcNow >= _voOneKillSfxAvailabilityTime)
			{
				_services.AudioFxService.PlayClipQueued2D(voMultiKillAudio,
					GameConstants.Audio.MIXER_GROUP_DIALOGUE_ID);
				_voOneKillSfxAvailabilityTime =
					DateTime.UtcNow.AddSeconds(GameConstants.Audio.VO_SFX_SINGLE_KILL_PREVENTION_SECONDS);
			}

			// Killstreak announcer
			_services.AudioFxService.PlayClipQueued2D(voKillstreakAudio);

			// Clutch announcer
			if (!frame.TryGet<Stats>(callback.EntityKiller, out var stats))
			{
				return;
			}

			var maxHealth = stats.Values[(int) StatType.Health].StatValue.AsInt;
			var maxShield = stats.Values[(int) StatType.Shield].StatValue.AsInt;
			var percent = (maxHealth + maxShield) / (stats.CurrentHealth + stats.CurrentShield);

			if (percent <= GameConstants.Audio.LOW_HP_CLUTCH_THERSHOLD_PERCENT &&
				DateTime.UtcNow >= _voClutchSfxAvailabilityTime)
			{
				_services.AudioFxService.PlayClipQueued2D(AudioId.Vo_KillLowHp,
					GameConstants.Audio.MIXER_GROUP_DIALOGUE_ID);
				_voClutchSfxAvailabilityTime =
					DateTime.UtcNow.AddSeconds(GameConstants.Audio.VO_SFX_SINGLE_KILL_PREVENTION_SECONDS);
			}
		}

		private void OnEventOnChestOpened(EventOnChestOpened callback)
		{
			_services.AudioFxService.PlayClip3D(AudioId.ChestPickup, callback.ChestPosition.ToUnityVector3());
		}

		private void OnEventOnRaycastShotExplosion(EventOnRaycastShotExplosion callback)
		{
			PlayExplosionSfx(callback.sourceId, callback.EndPosition.ToUnityVector3());
		}

		private void OnEventHazardLand(EventOnHazardLand callback)
		{
			PlayExplosionSfx(callback.sourceId, callback.HitPosition.ToUnityVector3());
			CheckDespawnClips(nameof(EventOnHazardLand), callback.AttackerEntity);
		}


		private void PlayExplosionSfx(GameId sourceId, Vector3 endPosition)
		{
			var audio = AudioId.None;

			switch (sourceId)
			{
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
				case GameId.Barrel:
					audio = AudioId.ExplosionMedium;
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
			var pos = Vector3.zero;
			var followTransform = (Transform) null;

			switch (callback.Special.SpecialType)
			{
				case SpecialType.Grenade:
					audio = AudioId.Dash;
					pos = entityView.transform.position;
					break;

				case SpecialType.StunGrenade:
					audio = AudioId.Dash;
					pos = callback.HitPosition.ToUnityVector3();
					break;

				case SpecialType.ShieldedCharge:
					audio = AudioId.Dash;
					pos = entityView.transform.position;
					followTransform = entityView.transform;
					break;

				case SpecialType.Airstrike:
					audio = AudioId.MissileFlyLoop;
					pos = callback.HitPosition.ToUnityVector3();
					break;
			}

			var audioSource = _services.AudioFxService.PlayClip3D(audio, pos);

			if (audio == AudioId.MissileFlyLoop)
			{
				var despawnEvents = new[] {nameof(EventOnHazardLand)};

				_currentClips.Add(new LoopedAudioClip(audioSource, despawnEvents, callback.Entity));
			}
			else if (audio != AudioId.None)
			{
				_services.AudioFxService.PlayClip3D(audio, pos);
			}

			if (followTransform != null)
			{
				audioSource.SetFollowTarget(followTransform, Vector3.zero, Quaternion.identity);
			}
		}

		private void OnCollectableCollected(EventOnCollectableCollected callback)
		{
			if (!callback.Game.PlayerIsLocal(callback.Player)) return;
			CheckDespawnClips(nameof(EventOnCollectableCollected), callback.CollectableEntity);

			var audio = AudioId.None;
			var collectableId = callback.CollectableId;

			switch (collectableId)
			{
				case GameId.AmmoSmall:
					audio = AudioId.AmmoPickup;
					break;
				case GameId.Health:
					audio = AudioId.HealthPickup;
					break;
				case GameId.ShieldSmall:
					audio = AudioId.ShieldPickup;
					break;
				case GameId.EnergyCubeLarge:
				case GameId.EnergyCubeSmall:
					audio = AudioId.GearPickup;
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
			else if (collectableId.IsInGroup(GameIdGroup.Special))
			{
				audio = AudioId.LargeAmmoPickup;
			}

			if (_matchServices.EntityViewUpdaterService.TryGetView(callback.PlayerEntity, out var entityView) &&
				audio != AudioId.None)
			{
				_services.AudioFxService.PlayClip3D(audio, entityView.transform.position);
			}
		}

		private void OnPlayerAttack(EventOnPlayerAttack callback)
		{
			CheckDespawnClips(nameof(EventOnPlayerAttack), callback.PlayerEntity);

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

		private void OnEntityDamaged(EventOnEntityDamaged callback)
		{
			if (!_matchServices.EntityViewUpdaterService.TryGetView(callback.Entity, out var entityView) ||
				callback.Player == PlayerRef.None) // TODO: a sound for things that are not players.
			{
				return;
			}

			var audio = AudioId.None;
			var damagedPlayerIsLocal = _matchServices.SpectateService.SpectatedPlayer.Value.Player == callback.Player;
			var spectatedEntity = _matchServices.SpectateService.SpectatedPlayer.Value.Entity;

			if (FeatureFlags.NEW_SFX)
			{
				audio = AudioId.TakeHealthDamage;
			}
			else
			{
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
			}

			if (spectatedEntity == callback.Entity || spectatedEntity == callback.Attacker)
			{
				_services.AudioFxService.PlayClip2D(audio);
			}
			else
			{
				_services.AudioFxService.PlayClip3D(audio, entityView.transform.position);
			}
		}

		private void OnPlayerEnteredAmbienceMessage(PlayerEnteredAmbienceMessage msg)
		{
			_ambienceList.Add(msg.Ambience.GetAmbientAudioId());
			_services.AudioFxService.PlayAmbience(_ambienceList.Last(), GameConstants.Audio.AMBIENCE_FADE_SECONDS,
				GameConstants.Audio.AMBIENCE_FADE_SECONDS, true);
		}

		private void OnPlayerLeftAmbienceMessage(PlayerLeftAmbienceMessage msg)
		{
			// Remove top-most matching occurence of the ambience in the list
			// This is so ambience can support entering volumes of same type, and transitioning between
			// different volumes correctly
			var index = _ambienceList.FindLastIndex(x => x == msg.Ambience.GetAmbientAudioId());

			if (index != -1)
			{
				_ambienceList.RemoveAt(index);
			}

			if (_ambienceList.Count > 0)
			{
				_services.AudioFxService.PlayAmbience(_ambienceList.Last(), GameConstants.Audio.AMBIENCE_FADE_SECONDS,
					GameConstants.Audio.AMBIENCE_FADE_SECONDS, true);
			}
			else
			{
				_services.AudioFxService.StopAmbience(GameConstants.Audio.AMBIENCE_FADE_SECONDS);
			}
		}

		private bool IsResyncing()
		{
			return _services.NetworkService.JoinSource.HasResync();
		}
	}
}