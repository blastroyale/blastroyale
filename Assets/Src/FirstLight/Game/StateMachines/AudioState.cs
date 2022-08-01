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
		private Dictionary<AudioId, AudioClipConfig> _audioClipConfigs = new Dictionary<AudioId, AudioClipConfig>();

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
			initial.OnExit(UnpackAudioClipConfigs);

			audioBase.Event(MainMenuState.MainMenuLoadedEvent).Target(mainMenu);
			
			mainMenu.OnEnter(PlayMainMenuMusic);
			mainMenu.Event(MainMenuState.MainMenuUnloadedEvent).Target(matchmaking);
			mainMenu.OnExit(StopMusicInstant);
			
			matchmaking.Event(GameSimulationState.SimulationStartedEvent).Target(gameModeCheck);
			matchmaking.OnExit(GetEntityViewUpdaterService);
			
			gameModeCheck.Transition().Condition(IsDeathmatch).Target(deathmatch);
			gameModeCheck.Transition().Target(battleRoyale);
			
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
		
		private void UnpackAudioClipConfigs()
		{
			_audioClipConfigs.Clear();

			var sharedConfigs = _services.ConfigsProvider.GetConfig<AudioSharedAssetConfigs>().Configs;
			var menuConfigs = _services.ConfigsProvider.GetConfig<AudioMainMenuAssetConfigs>().Configs;
			var matchConfigs = _services.ConfigsProvider.GetConfig<AudioMatchAssetConfigs>().Configs;

			foreach (var config in sharedConfigs)
			{
				_audioClipConfigs.Add(config.Key, config.Value);
			}
			
			foreach (var config in menuConfigs)
			{
				_audioClipConfigs.Add(config.Key, config.Value);
			}
			
			foreach (var config in matchConfigs)
			{
				_audioClipConfigs.Add(config.Key, config.Value);
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
			var entityView = _entityViewUpdaterService.GetManualView(callback.PlayerEntity);
			var weaponConfig = _services.ConfigsProvider.GetConfig<AudioWeaponConfig>((int) callback.Weapon.GameId);
			var audioConfig = _audioClipConfigs[weaponConfig.WeaponShotId];
			var initProps = _services.AudioFxService.GetDefaultAudioInitProps(GameConstants.Audio.SFX_3D_SPATIAL_BLEND);

			initProps.ClipIndex = audioConfig.PlaybackClipIndex;
			initProps.Volume = audioConfig.PlaybackVolume;
			initProps.Pitch = audioConfig.PlaybackPitch;

			_services.AudioFxService.PlayClip3D(audioConfig.AudioId, entityView.transform.position,
			                                    initProps);
		}

		private void OnPlayerDamaged(EventOnPlayerDamaged callback)
		{
			var game = callback.Game;
			var entityView = _entityViewUpdaterService.GetManualView(callback.Entity);
			var initProps = _services.AudioFxService.GetDefaultAudioInitProps(GameConstants.Audio.SFX_3D_SPATIAL_BLEND);
			var audio = AudioId.None;

			// TODO - TAKE/SHIELD HIT DAMAGE BASED ON SPECTATED ENTITY
			if (game.PlayerIsLocal(callback.Player))
			{
				audio = callback.ShieldDamage > 0 ? AudioId.TakeShieldDamage : AudioId.TakeHealthDamage;
			}
			else if (game.Frames.Verified.TryGet<PlayerCharacter>(callback.Attacker, out var player) &&
			         game.PlayerIsLocal(player.Player))
			{
				audio = callback.ShieldDamage > 0 ? AudioId.HitShieldDamage : AudioId.HitHealthDamage;
			}

			if (audio != AudioId.None)
			{
				var audioConfig = _audioClipConfigs[audio];
				initProps.ClipIndex = audioConfig.PlaybackClipIndex;
				initProps.Volume = audioConfig.PlaybackVolume;
				initProps.Pitch = audioConfig.PlaybackPitch;
				_services.AudioFxService.PlayClip3D(audio, entityView.transform.position, initProps);
			}
		}
	}
}