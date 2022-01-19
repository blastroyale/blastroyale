using System;
using System.Collections;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Services;
using I2.Loc;
using Quantum;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FirstLight.Game.Views.AdventureHudViews
{
	/// <summary>
	/// Handles logic for the Weapon the player currently has equipped.
	/// </summary>
	public class CurrentWeaponView : MonoBehaviour
	{
		[SerializeField] private TextMeshProUGUI _weaponText;
		[SerializeField] private Image _currentWeaponImage;
		
		private IGameServices _services;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			QuantumEvent.Subscribe<EventOnLocalPlayerSpawned>(this, OnEventOnLocalPlayerSpawned);
			QuantumEvent.Subscribe<EventOnLocalPlayerWeaponChanged>(this, OnEventOnLocalPlayerWeaponChanged);
		}

		private void OnEventOnLocalPlayerSpawned(EventOnLocalPlayerSpawned callback)
		{
			UpdateCurrentWeapon(callback.WeaponGameId);
		}
		
		private void OnEventOnLocalPlayerWeaponChanged(EventOnLocalPlayerWeaponChanged callback)
		{
			UpdateCurrentWeapon(callback.WeaponGameId);
		}
		
		private async void UpdateCurrentWeapon(GameId weaponGameId)
		{
			_currentWeaponImage.sprite = await _services.AssetResolverService.RequestAsset<GameId, Sprite>(weaponGameId);
			_weaponText.text = weaponGameId.GetTranslation();
		}
	}
}

