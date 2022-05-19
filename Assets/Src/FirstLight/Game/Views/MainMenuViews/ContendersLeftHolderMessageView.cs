using FirstLight.Game.Logic;
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
	public class ContendersLeftHolderMessageView : MonoBehaviour
	{
		[SerializeField, Required] private TextMeshProUGUI _contendersLeftText;
		[SerializeField, Required] private Animation _animation;
		[SerializeField, Required] private AnimationClip _animationClipFadeIn;
		[SerializeField, Required] private AnimationClip _animationClipFadeOut;

		private IGameServices _services;
		private IGameDataProvider _gameDataProvider;
		private int _playersLeft;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
			_contendersLeftText.text = "";

			_services.MessageBrokerService.Subscribe<MatchStartedMessage>(OnMatchStarted);
			QuantumEvent.Subscribe<EventOnPlayerDead>(this, OnEventOnPlayerDead);
		}

		private void OnDestroy()
		{
			_services?.MessageBrokerService?.UnsubscribeAll(this);
		}

		private void OnMatchStarted(MatchStartedMessage message)
		{
			_contendersLeftText.text = string.Format(ScriptLocalization.AdventureMenu.ContendersRemaining,
			                                         _services.NetworkService.QuantumClient.CurrentRoom.MaxPlayers.ToString());
		}

		/// <summary>
		/// The scoreboard could update whilst it's open, e.g. players killed whilst looking at it, etc.
		/// </summary>
		private void OnEventOnPlayerDead(EventOnPlayerDead callback)
		{
			_contendersLeftText.text = string.Format(ScriptLocalization.AdventureMenu.ContendersRemaining,
			                                         callback.Game.Frames.Verified.ComponentCount<AlivePlayerCharacter>());

			_animation.clip = _animationClipFadeIn;
			_animation.Rewind();
			_animation.Play();

			if (gameObject.activeInHierarchy)
			{
				this.LateCoroutineCall(_animation.clip.length, PlayFadeOutAnimation);
			}
		}

		private void PlayFadeOutAnimation()
		{
			_animation.clip = _animationClipFadeOut;
			_animation.Rewind();
			_animation.Play();
		}
	}
}