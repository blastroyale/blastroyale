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
	public class ContendersLeftView : MonoBehaviour
	{
		[SerializeField, Required] private TextMeshProUGUI _contendersLeftText;
		[SerializeField, Required] private Animation _animation;
		[SerializeField, Required] private AnimationClip _animationClipFadeInOut;
		[SerializeField] private bool _displayNumberOnly;
		
		private IGameServices _services;
		private int _playerLeftAlive;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_contendersLeftText.text = "0";

			_services.MessageBrokerService.Subscribe<MatchStartedMessage>(OnMatchStarted);
			_services.MessageBrokerService.Subscribe<SpectateTargetSwitchedMessage>(OnSpectateTargetSwitchedMessage);
			
			QuantumEvent.Subscribe<EventOnPlayerDead>(this, OnEventOnPlayerDead);
		}

		private void OnDestroy()
		{
			_services?.MessageBrokerService?.UnsubscribeAll(this);
		}

		private void OnSpectateTargetSwitchedMessage(SpectateTargetSwitchedMessage msg)
		{
			if (_playerLeftAlive == 0)
			{
				UpdatePlayersAlive(QuantumRunner.Default.Game.Frames.Verified.ComponentCount<AlivePlayerCharacter>());
			}
		}

		private void OnMatchStarted(MatchStartedMessage message)
		{
			UpdatePlayersAlive(QuantumRunner.Default.Game.Frames.Verified.ComponentCount<AlivePlayerCharacter>());
		}
		
		private void OnEventOnPlayerDead(EventOnPlayerDead callback)
		{
			_animation.clip = _animationClipFadeInOut;
			_animation.Rewind();
			_animation.Play();
			
			UpdatePlayersAlive(_playerLeftAlive - 1);
		}

		private void UpdatePlayersAlive(int aliveAmount)
		{
			_playerLeftAlive = aliveAmount;
			
			if (_displayNumberOnly)
			{
				_contendersLeftText.text = aliveAmount.ToString();
			}
			else
			{
				_contendersLeftText.text = string.Format(ScriptLocalization.AdventureMenu.ContendersRemaining, aliveAmount);
			}
			
		}
	}
}