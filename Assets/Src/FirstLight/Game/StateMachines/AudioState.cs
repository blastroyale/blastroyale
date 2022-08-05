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
			var disonnected = stateFactory.State("AUDIO - Disconnected");
			
			initial.Transition().Target(audioBase);
			initial.OnExit(SubscribeEvents);

			audioBase.Event(MainMenuState.MainMenuLoadedEvent).Target(mainMenu);

			mainMenu.OnEnter(PlayMainMenuMusic);
			mainMenu.Event(MainMenuState.MainMenuUnloadedEvent).Target(matchmaking);
			mainMenu.OnExit(StopMusicInstant);
			
			matchmaking.Event(MatchState.MatchUnloadedEvent).Target(audioBase);
			matchmaking.Event(GameSimulationState.SimulationStartedEvent).OnTransition(GetEntityViewUpdaterService).Target(gameModeCheck);
			matchmaking.Event(NetworkState.PhotonDisconnectedEvent).Target(disonnected);
			
			gameModeCheck.Transition().Condition(IsDeathmatch).Target(deathmatch);
			gameModeCheck.Transition().Target(battleRoyale);

			battleRoyale.Nest(_audioBrState.Setup).Target(postGame);
			battleRoyale.Event(GameSimulationState.GameCompleteExitEvent).Target(postGame);
			battleRoyale.Event(GameSimulationState.MatchEndedEvent).Target(postGame);
			battleRoyale.Event(GameSimulationState.MatchQuitEvent).OnTransition(StopMusicInstant).Target(audioBase);
			battleRoyale.Event(MatchState.MatchUnloadedEvent).Target(audioBase);
			battleRoyale.Event(NetworkState.PhotonDisconnectedEvent).Target(disonnected);
			
			deathmatch.Nest(_audioDmState.Setup).Target(postGame);
			deathmatch.Event(GameSimulationState.GameCompleteExitEvent).Target(postGame);
			deathmatch.Event(GameSimulationState.MatchEndedEvent).Target(postGame);
			deathmatch.Event(GameSimulationState.MatchQuitEvent).OnTransition(StopMusicInstant).Target(audioBase);
			deathmatch.Event(MatchState.MatchUnloadedEvent).Target(audioBase);
			deathmatch.Event(NetworkState.PhotonDisconnectedEvent).Target(disonnected);
			
			postGame.OnEnter(PlayPostGameMusic);
			postGame.Event(MatchState.MatchUnloadedEvent).Target(audioBase);
			postGame.OnExit(StopMusicInstant);
			
			disonnected.OnEnter(StopMusicInstant);
			disonnected.Event(MainMenuState.MainMenuLoadedEvent).Target(mainMenu);
			disonnected.Event(NetworkState.JoinedRoomEvent).Target(matchmaking);
			
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
			_services.AudioFxService.PlayMusic(AudioId.MusicMainMenuLoop,
			                                   GameConstants.Audio.MUSIC_SHORT_FADE_SECONDS);
		}

		private void PlayPostGameMusic()
		{
			_services.AudioFxService.PlayMusic(AudioId.MusicPostMatchLoop,
			                                   GameConstants.Audio.MUSIC_SHORT_FADE_SECONDS,
			                                   GameConstants.Audio.MUSIC_SHORT_FADE_SECONDS);
		}

		private void StopMusicInstant()
		{
			_services.AudioFxService.StopMusic();
		}

		private void StopMusicFadeOut()
		{
			_services.AudioFxService.StopMusic(GameConstants.Audio.MUSIC_SHORT_FADE_SECONDS);
		}

		private void OnPlayerAttack(EventOnPlayerAttack callback)
		{
			if (_entityViewUpdaterService == null)
			{
				return;
			}
			
			if(_entityViewUpdaterService.TryGetView(callback.PlayerEntity, out var entityView))
			{
				var weaponConfig = _services.ConfigsProvider.GetConfig<AudioWeaponConfig>((int) callback.Weapon.GameId);

				_services.AudioFxService.PlayClip3D(weaponConfig.WeaponShotId, entityView.transform.position);
			}
		}

		private void OnPlayerDamaged(EventOnPlayerDamaged callback)
		{
			if (_entityViewUpdaterService == null)
			{
				return;
			}
			
			if(_entityViewUpdaterService.TryGetView(callback.Entity, out var entityView))
			{
				var game = callback.Game;
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
					_services.AudioFxService.PlayClip3D(audio, entityView.transform.position);
				}
			}
		
		}
	}
}