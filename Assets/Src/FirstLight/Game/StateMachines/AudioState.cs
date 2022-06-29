using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Statechart;
using Quantum;

namespace FirstLight.Game.StateMachines
{
	/// <summary>
	/// This object contains the behaviour logic to control all the game audio in the <seealso cref="GameStateMachine"/>
	/// </summary>
	public class AudioState
	{
		private readonly IGameServices _services; 
		private readonly IGameDataProvider _gameDataProvider;

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
			var audioListener = stateFactory.Final("AUDIO - Audio Listener");

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
			var game = callback.Game;
			if (game.PlayerIsLocal(callback.Player))
			{
				var audio = callback.ShieldDamage > 0 ? AudioId.TakeShieldDamage : AudioId.TakeHealthDamage;
				
				_services.AudioFxService.PlayClip2D(audio);
			}
			else if (game.Frames.Verified.TryGet<PlayerCharacter>(callback.Attacker, out var player) &&
			         game.PlayerIsLocal(player.Player))
			{
				var audio = callback.ShieldDamage > 0 ? AudioId.HitShieldDamage : AudioId.HitHealthDamage;
				
				_services.AudioFxService.PlayClip2D(audio);
			}
		}
	}
}