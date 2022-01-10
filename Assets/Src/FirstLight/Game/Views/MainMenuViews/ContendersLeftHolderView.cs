
using System;
using System.Collections.Generic;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Game.Views.AdventureHudViews;
using FirstLight.Services;
using Quantum;
using TMPro;
using UnityEngine;

namespace FirstLight.Game.Views.MainMenuViews
{
	/// <summary>
	/// Used to display how many contenders are left within the Battle Royale.
	/// </summary>
	public class ContendersLeftHolderView : MonoBehaviour
	{
		[SerializeField] private TextMeshProUGUI _contendersLeftText;
		[SerializeField] private Animation _animation;

		private IGameServices _services;
		private IGameDataProvider _gameDataProvider;
		private int _playersLeft;
		
		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_services.MessageBrokerService.Subscribe<MatchStartedMessage>(OnMatchStarted);
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
			QuantumEvent.Subscribe<EventOnPlayerDead>(this, OnEventOnPlayerDead);
			
			var mapConfig = _gameDataProvider.AdventureDataProvider.SelectedMapConfig;
			_contendersLeftText.text = mapConfig.PlayersLimit.ToString();
		}
		
		private void OnDestroy()
		{
			_services?.MessageBrokerService?.UnsubscribeAll(this);
		}

		private void OnMatchStarted(MatchStartedMessage message)
		{
			var mapConfig = _gameDataProvider.AdventureDataProvider.SelectedMapConfig;
			_contendersLeftText.text = mapConfig.PlayersLimit.ToString();
		}
		
		/// <summary>
		/// The scoreboard could update whilst it's open, e.g. players killed whilst looking at it, etc.
		/// </summary>
		private void OnEventOnPlayerDead(EventOnPlayerDead callback)
		{
			_contendersLeftText.text = (callback.Game.Frames.Verified.ComponentCount<AlivePlayerCharacter>()).ToString();
			
			_animation.Rewind();
			_animation.Play();
		}
	}
}
