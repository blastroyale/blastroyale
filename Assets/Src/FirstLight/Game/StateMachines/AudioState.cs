using System;
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
		}

		private void UnsubscribeEvents()
		{
			QuantumEvent.UnsubscribeListener(this);
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

			var randomVol = Random.Range(GameConstants.Audio.SFX_RAND_VOLUME_MIN,
			                             GameConstants.Audio.SFX_RAND_VOLUME_MAX);
			var randomPitch = Random.Range(GameConstants.Audio.SFX_RAND_PITCH_MIN,
			                               GameConstants.Audio.SFX_RAND_PITCH_MAX);

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