using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using I2.Loc;
using Quantum;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace FirstLight.Game.Views.MainMenuViews
{
	/// <summary>
	/// Used to display how many contenders are left within the Battle Royale via a message. IE "10 PLAYERS REMAINING".
	/// </summary>
	public class ContendersLeftView : MonoBehaviour
	{
		[SerializeField, Required] private TextMeshProUGUI _contendersLeftText;
		[SerializeField, Required] private Animation _animation;
		[SerializeField, Required] private AnimationClip _animationClipFadeInOut;
		[SerializeField] private bool _displayNumberOnly;

		private IGameServices _services;
		private IMatchServices _matchServices;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_matchServices = MainInstaller.Resolve<IMatchServices>();

			_contendersLeftText.text = "0";

			_services.MessageBrokerService.Subscribe<MatchStartedMessage>(OnMatchStarted);
			_matchServices.SpectateService.SpectatedPlayer.Observe(OnSpectatedPlayerChanged);

			QuantumEvent.Subscribe<EventOnPlayerDead>(this, OnEventOnPlayerDead);
		}

		private void OnDestroy()
		{
			_services?.MessageBrokerService?.UnsubscribeAll(this);
			_matchServices.SpectateService.SpectatedPlayer.StopObservingAll(this);
		}

		private void OnMatchStarted(MatchStartedMessage message)
		{
			UpdatePlayersAlive(QuantumRunner.Default.Game.Frames.Verified);
		}

		private void OnEventOnPlayerDead(EventOnPlayerDead callback)
		{
			_animation.clip = _animationClipFadeInOut;
			_animation.Rewind();
			_animation.Play();

			UpdatePlayersAlive(callback.Game.Frames.Verified);
		}

		private void OnSpectatedPlayerChanged(SpectatedPlayer previous, SpectatedPlayer next)
		{
			// This is here because when joining as a spectator, OnMatchStarted is called
			// sooner than we have all the necessary data to set up the first player count.
			UpdatePlayersAlive(QuantumRunner.Default.Game.Frames.Verified);
		}

		private void UpdatePlayersAlive(Frame f)
		{
			var playersLeft = f.GetSingleton<GameContainer>().TargetProgress + 1 -
			                  f.GetSingleton<GameContainer>().CurrentProgress;

			_contendersLeftText.text = _displayNumberOnly
				                           ? playersLeft.ToString()
				                           : string.Format(ScriptLocalization.AdventureMenu.ContendersRemaining,
				                                           playersLeft);
		}
	}
}