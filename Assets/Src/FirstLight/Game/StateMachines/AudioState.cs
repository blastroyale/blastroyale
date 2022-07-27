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
		private readonly IGameServices _services;
		private readonly IGameDataProvider _gameDataProvider;
		private IEntityViewUpdaterService _entityViewUpdaterService;

		public AudioState(IGameDataProvider gameLogic, IGameServices services)
		{
			_services = services;
			_gameDataProvider = gameLogic;
		}

		/// <summary>
		/// Setups the Adventure gameplay state
		/// </summary>
		public void Setup(IStateFactory stateFactory)
		{
			var initial = stateFactory.Initial("AUDIO - Initial");
			var final = stateFactory.Final("AUDIO - Final");
			var audioListener = stateFactory.State("AUDIO - Audio Listener");

			initial.Transition().Target(audioListener);
			initial.OnExit(SubscribeEvents);

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
		
		private void OnPlayerAttack(EventOnPlayerAttack callback)
		{
			// TODO - FIND BETTER SOLUTION FOR THIS PLS
			if (_entityViewUpdaterService == null)
			{
				_entityViewUpdaterService = MainInstaller.Resolve<IEntityViewUpdaterService>();
			}

			var audioConfig = _services.ConfigsProvider.GetConfig<AudioWeaponConfig>((int)callback.Weapon.GameId);
			var entityView = _entityViewUpdaterService.GetManualView(callback.PlayerEntity);
			var initProps = _services.AudioFxService.GetDefaultAudioInitProps(GameConstants.Audio.SFX_3D_SPATIAL_BLEND);
			
			initProps.Volume = Random.Range(audioConfig.BaseVolume - audioConfig.VolumeRandDeviation,
			                                audioConfig.BaseVolume + audioConfig.VolumeRandDeviation);
			
			initProps.Pitch = Random.Range(audioConfig.BasePitch - audioConfig.PitchRandDeviation,
			                               audioConfig.BasePitch + audioConfig.PitchRandDeviation);
			
			_services.AudioFxService.PlayClip3D(audioConfig.WeaponShotAudioId, entityView.transform.position, initProps);
		}

		private void OnPlayerDamaged(EventOnPlayerDamaged callback)
		{
			// TODO - FIND BETTER SOLUTION FOR THIS PLS
			if (_entityViewUpdaterService == null)
			{
				_entityViewUpdaterService = MainInstaller.Resolve<IEntityViewUpdaterService>();
			}
			
			var game = callback.Game;
			var entityView = _entityViewUpdaterService.GetManualView(callback.Entity);

			var randomVol = Random.Range(GameConstants.Audio.SFX_DEFAULT_VOLUME - GameConstants.Audio.SFX_VOLUME_DEVIATION,
			                             GameConstants.Audio.SFX_DEFAULT_VOLUME + GameConstants.Audio.SFX_VOLUME_DEVIATION);
			var randomPitch = Random.Range(GameConstants.Audio.SFX_DEFAULT_PITCH - GameConstants.Audio.SFX_PITCH_DEVIATION,
			                               GameConstants.Audio.SFX_DEFAULT_PITCH + GameConstants.Audio.SFX_PITCH_DEVIATION);

			var initProps = _services.AudioFxService.GetDefaultAudioInitProps(GameConstants.Audio.SFX_3D_SPATIAL_BLEND);
			initProps.Volume = randomVol;
			initProps.Pitch = randomPitch;

			var audio = AudioId.None;

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