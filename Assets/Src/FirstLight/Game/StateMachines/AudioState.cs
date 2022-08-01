using System;
using System.Collections.Generic;
using FirstLight.Game.Configs;
using FirstLight.Game.Configs.AssetConfigs;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Statechart;
using Quantum;
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
		private IEntityViewUpdaterService _entityViewUpdaterService;
		private Dictionary<AudioId, AudioClipConfig> _currentAudioClipConfigs = new Dictionary<AudioId, AudioClipConfig>();

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

			initial.Transition().Target(audioBase);
			initial.OnExit(SubscribeEvents);

			audioBase.Event(MainMenuState.MainMenuLoadedEvent).Target(mainMenu);
			
			mainMenu.OnEnter(PlayMainMenuMusic);
			mainMenu.OnEnter(UpdateAudioClipConfigsMenu);
			mainMenu.Event(MainMenuState.MainMenuUnloadedEvent).Target(matchmaking);
			mainMenu.OnExit(StopMusicInstant);
			
			matchmaking.Event(GameSimulationState.SimulationStartedEvent).Target(gameModeCheck);
			matchmaking.OnExit(GetEntityViewUpdaterService);
			
			gameModeCheck.Transition().Condition(IsDeathmatch).Target(deathmatch);
			gameModeCheck.Transition().Target(battleRoyale);
			gameModeCheck.OnExit(UpdateAudioClipConfigsMatch);
			
			battleRoyale.Nest(_audioBrState.Setup).Target(postGame);
			battleRoyale.Event(GameSimulationState.MatchEndedEvent).Target(postGame);
			
			deathmatch.Nest(_audioDmState.Setup).Target(postGame);
			deathmatch.Event(GameSimulationState.MatchEndedEvent).Target(postGame);
			
			postGame.OnEnter(PlayPostGameMusic);
			postGame.Event(GameSimulationState.SimulationEndedEvent).Target(audioBase);
			mainMenu.OnExit(StopMusicInstant);
			
			final.OnEnter(UnsubscribeEvents);
		}

		private void SubscribeEvents()
		{
			QuantumEvent.SubscribeManual<EventOnPlayerDamaged>(this, OnPlayerDamaged);
			QuantumEvent.SubscribeManual<EventOnPlayerAttack>(this, OnPlayerAttack);
		}

		private void UnsubscribeEvents()
		{
			QuantumEvent.UnsubscribeListener(this);
		}

		private void UpdateAudioClipConfigsMenu()
		{
			_currentAudioClipConfigs.Clear();

			var sharedConfigs = _services.ConfigsProvider.GetConfig<AudioSharedAssetConfigs>().Configs;
			var menuConfigs = _services.ConfigsProvider.GetConfig<AudioMainMenuAssetConfigs>().Configs;

			foreach (var sharedConfig in sharedConfigs)
			{
				_currentAudioClipConfigs.Add(sharedConfig.Key, sharedConfig.Value);
			}
			
			foreach (var menuConfig in menuConfigs)
			{
				_currentAudioClipConfigs.Add(menuConfig.Key, menuConfig.Value);
			}
		}
		
		private void UpdateAudioClipConfigsMatch()
		{
			_currentAudioClipConfigs.Clear();

			var sharedConfigs = _services.ConfigsProvider.GetConfig<AudioSharedAssetConfigs>().Configs;
			var matchConfigs = _services.ConfigsProvider.GetConfig<AudioMatchAssetConfigs>().Configs;

			foreach (var sharedConfig in sharedConfigs)
			{
				_currentAudioClipConfigs.Add(sharedConfig.Key, sharedConfig.Value);
			}
			
			foreach (var matchConfig in matchConfigs)
			{
				_currentAudioClipConfigs.Add(matchConfig.Key, matchConfig.Value);
			}
		}
		
		private bool IsDeathmatch()
		{
			return _services.NetworkService.CurrentRoomMapConfig.Value.GameMode == GameMode.Deathmatch;
		}

		private void GetEntityViewUpdaterService()
		{
			_entityViewUpdaterService = MainInstaller.Resolve<IEntityViewUpdaterService>();
		}

		private void PlayMainMenuMusic()
		{
			_services.AudioFxService.PlayMusic(AudioId.MusicMainMenuLoop, GameConstants.Audio.MUSIC_SHORT_FADE_IN_SECONDS);
		}

		private void PlayPostGameMusic()
		{
			_services.AudioFxService.PlayMusic(AudioId.MusicPostMatchLoop, GameConstants.Audio.MUSIC_SHORT_FADE_IN_SECONDS,
			                                   GameConstants.Audio.MUSIC_SHORT_FADE_OUT_SECONDS);
		}
		
		private void StopMusicInstant()
		{
			_services.AudioFxService.StopMusic();
		}
		
		private void StopMusicFadeOut()
		{
			_services.AudioFxService.StopMusic(GameConstants.Audio.MUSIC_SHORT_FADE_IN_SECONDS);
		}

		private void OnPlayerAttack(EventOnPlayerAttack callback)
		{
			var weaponConfig = _services.ConfigsProvider.GetConfig<AudioWeaponConfig>((int) callback.Weapon.GameId);
			var audioConfig = _currentAudioClipConfigs[weaponConfig.WeaponShotId];

			var entityView = _entityViewUpdaterService.GetManualView(callback.PlayerEntity);
			var initProps = _services.AudioFxService.GetDefaultAudioInitProps(GameConstants.Audio.SFX_3D_SPATIAL_BLEND);

			initProps.Volume = Random.Range(audioConfig.BaseVolume - audioConfig.VolumeRandDeviation,
			                                audioConfig.BaseVolume + audioConfig.VolumeRandDeviation);

			initProps.Pitch = Random.Range(audioConfig.BasePitch - audioConfig.PitchRandDeviation,
			                               audioConfig.BasePitch + audioConfig.PitchRandDeviation);

			_services.AudioFxService.PlayClip3D(audioConfig.AudioId, entityView.transform.position,
			                                    initProps);
		}

		private void OnPlayerDamaged(EventOnPlayerDamaged callback)
		{
			var game = callback.Game;
			var entityView = _entityViewUpdaterService.GetManualView(callback.Entity);
			
			var pitch = Random.Range(GameConstants.Audio.SFX_DEFAULT_PITCH - GameConstants.Audio.SFX_DEFAULT_PITCH_DEVIATION,
			                         GameConstants.Audio.SFX_DEFAULT_PITCH + GameConstants.Audio.SFX_DEFAULT_PITCH_DEVIATION);

			var initProps = _services.AudioFxService.GetDefaultAudioInitProps(GameConstants.Audio.SFX_3D_SPATIAL_BLEND);
			
			initProps.Pitch = pitch;

			var audio = AudioId.None;

			// TODO - TAKE/SHIELD HIT DAMAGE BASED ON SPECTATED ENTITY
			if (game.PlayerIsLocal(callback.Player))
			{
				initProps.Volume = Random.Range(GameConstants.Audio.SFX_DEFAULT_TAKE_DAMAGE_VOLUME - GameConstants.Audio.SFX_DEFAULT_VOLUME_DEVIATION,
				                                GameConstants.Audio.SFX_DEFAULT_TAKE_DAMAGE_VOLUME + GameConstants.Audio.SFX_DEFAULT_VOLUME_DEVIATION);;
				audio = callback.ShieldDamage > 0 ? AudioId.TakeShieldDamage : AudioId.TakeHealthDamage;
			}
			else if (game.Frames.Verified.TryGet<PlayerCharacter>(callback.Attacker, out var player) &&
			         game.PlayerIsLocal(player.Player))
			{
				initProps.Volume = Random.Range(GameConstants.Audio.SFX_DEFAULT_HIT_DAMAGE_VOLUME - GameConstants.Audio.SFX_DEFAULT_VOLUME_DEVIATION,
				                                GameConstants.Audio.SFX_DEFAULT_HIT_DAMAGE_VOLUME + GameConstants.Audio.SFX_DEFAULT_VOLUME_DEVIATION);;
				audio = callback.ShieldDamage > 0 ? AudioId.HitShieldDamage : AudioId.HitHealthDamage;
			}

			if (audio != AudioId.None)
			{
				_services.AudioFxService.PlayClip3D(audio, entityView.transform.position, initProps);
			}
		}
	}
}