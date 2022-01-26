
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
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
			_contendersLeftText.text = _gameDataProvider.AdventureDataProvider.SelectedMapConfig.PlayersLimit.ToString();
			
			QuantumEvent.Subscribe<EventOnPlayerDead>(this, OnEventOnPlayerDead);
		}
		
		private void OnDestroy()
		{
			_services?.MessageBrokerService?.UnsubscribeAll(this);
		}
		
		private void OnEventOnPlayerDead(EventOnPlayerDead callback)
		{
			_contendersLeftText.text = (callback.Game.Frames.Verified.ComponentCount<AlivePlayerCharacter>()).ToString();
			
			_animation.Rewind();
			_animation.Play();
		}
	}
}
