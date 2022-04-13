
using System;
using System.Collections.Generic;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Game.Views.AdventureHudViews;
using FirstLight.Services;
using I2.Loc;
using Quantum;
using TMPro;
using UnityEngine;

namespace FirstLight.Game.Views.MainMenuViews
{
	/// <summary>
	/// Used to display how many contenders are left within the Battle Royale via a message. IE "10 PLAYERS REMAINING".
	/// </summary>
	public class ContendersLeftHolderMessageView : MonoBehaviour
	{
		[SerializeField] private TextMeshProUGUI _contendersLeftText;
		[SerializeField] private Animation _animation;
		[SerializeField] private AnimationClip _animationClipFadeIn;
		[SerializeField] private AnimationClip _animationClipFadeOut;

		private IGameServices _services;
		private IGameDataProvider _gameDataProvider;
		private int _playersLeft;

		private void Awake()
		{
			_services.MessageBrokerService.Subscribe<MatchStartedMessage>(OnMatchStarted);
		}

		private void Start()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
			_contendersLeftText.text = "";

			QuantumEvent.Subscribe<EventOnPlayerDead>(this, OnEventOnPlayerDead);
		}

		private void OnDestroy()
		{
			_services?.MessageBrokerService?.UnsubscribeAll(this);
		}

		private void OnMatchStarted(MatchStartedMessage message)
		{
			var mapConfig = _gameDataProvider.AppDataProvider.CurrentMapConfig;
			_contendersLeftText.text = mapConfig.PlayersLimit.ToString();
		}
		
		/// <summary>
		/// The scoreboard could update whilst it's open, e.g. players killed whilst looking at it, etc.
		/// </summary>
		private void OnEventOnPlayerDead(EventOnPlayerDead callback)
		{
			_contendersLeftText.text = string.Format(ScriptLocalization.AdventureMenu.ContendersRemaining, callback.Game.Frames.Verified.ComponentCount<AlivePlayerCharacter>() );
			
			_animation.clip = _animationClipFadeIn;
			_animation.Rewind();
			_animation.Play();
			
			this.LateCall(_animation.clip.length, PlayFadeOutAnimation);
		}
		
		private void PlayFadeOutAnimation()
		{
			_animation.clip = _animationClipFadeOut;
			_animation.Rewind();
			_animation.Play();
		}
	}
}
