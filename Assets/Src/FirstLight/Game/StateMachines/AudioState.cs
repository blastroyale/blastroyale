using System;
using FirstLight.Game.Configs;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
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
		private readonly IStatechartEvent
			_battleRoyaleStartedEvent = new StatechartEvent("Battle Royale Started Event");

		private readonly IStatechartEvent _deathmatchStartedEvent = new StatechartEvent("Deathmatch Started Event");
		private readonly IStatechartEvent _leftMatchEvent = new StatechartEvent("Left Match Event");

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
		}

		/// <summary>
		/// Setups the Adventure gameplay state
		/// </summary>
		public void Setup(IStateFactory stateFactory)
		{
			var initial = stateFactory.Initial("AUDIO - Initial");
			var final = stateFactory.Final("AUDIO - Final");
			var audioBase = stateFactory.State("AUDIO - Audio Base");
			var battleRoyale = stateFactory.Nest("AUDIO - Battle Royale");
			var deathmatch = stateFactory.Nest("AUDIO - Deathmatch");
			var postGame = stateFactory.State("AUDIO - Post Game");

			initial.Transition().Target(audioBase);
			initial.OnExit(SubscribeEvents);

			audioBase.OnEnter(PlayMainMenuMusic);
			audioBase.Event(_battleRoyaleStartedEvent).Target(battleRoyale);
			audioBase.Event(_deathmatchStartedEvent).Target(deathmatch);
			audioBase.OnExit(GetEntityViewUpdaterService);
			
			battleRoyale.Nest(_audioBrState.Setup).Target(postGame);
			deathmatch.Nest(_audioDmState.Setup).Target(postGame);

			postGame.OnEnter(PlayPostGameMusic);
			postGame.Event(_leftMatchEvent).Target(audioBase);

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

		private void GetEntityViewUpdaterService()
		{
			_entityViewUpdaterService = MainInstaller.Resolve<IEntityViewUpdaterService>();
		}

		private void PlayMainMenuMusic()
		{
			_services.AudioFxService.PlayMusic(AudioId.MainMenuLoopNew);
		}

		private void PlayPostGameMusic()
		{
			_services.AudioFxService.PlayMusic(AudioId.PostMatchLoop, GameConstants.Audio.MUSIC_SHORT_FADE_IN_SECONDS,
			                                   GameConstants.Audio.MUSIC_SHORT_FADE_OUT_SECONDS);
		}

		private void OnPlayerAttack(EventOnPlayerAttack callback)
		{
			var audioConfig = _services.ConfigsProvider.GetConfig<AudioWeaponConfig>((int) callback.Weapon.GameId);
			var entityView = _entityViewUpdaterService.GetManualView(callback.PlayerEntity);
			var initProps = _services.AudioFxService.GetDefaultAudioInitProps(GameConstants.Audio.SFX_3D_SPATIAL_BLEND);

			initProps.Volume = Random.Range(audioConfig.BaseVolume - audioConfig.VolumeRandDeviation,
			                                audioConfig.BaseVolume + audioConfig.VolumeRandDeviation);

			initProps.Pitch = Random.Range(audioConfig.BasePitch - audioConfig.PitchRandDeviation,
			                               audioConfig.BasePitch + audioConfig.PitchRandDeviation);

			_services.AudioFxService.PlayClip3D(audioConfig.WeaponShotAudioId, entityView.transform.position,
			                                    initProps);
		}

		private void OnPlayerDamaged(EventOnPlayerDamaged callback)
		{
			var game = callback.Game;
			var entityView = _entityViewUpdaterService.GetManualView(callback.Entity);

			var randomVol =
				Random.Range(GameConstants.Audio.SFX_DEFAULT_VOLUME - GameConstants.Audio.SFX_DEAULT_VOLUME_DEVIATION,
				             GameConstants.Audio.SFX_DEFAULT_VOLUME + GameConstants.Audio.SFX_DEAULT_VOLUME_DEVIATION);
			var randomPitch =
				Random.Range(GameConstants.Audio.SFX_DEFAULT_PITCH - GameConstants.Audio.SFX_DEFAULT_PITCH_DEVIATION,
				             GameConstants.Audio.SFX_DEFAULT_PITCH + GameConstants.Audio.SFX_DEFAULT_PITCH_DEVIATION);

			var initProps = _services.AudioFxService.GetDefaultAudioInitProps(GameConstants.Audio.SFX_3D_SPATIAL_BLEND);
			initProps.Volume = randomVol;
			initProps.Pitch = randomPitch;

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
				_services.AudioFxService.PlayClip3D(audio, entityView.transform.position, initProps);
			}
		}
	}
}