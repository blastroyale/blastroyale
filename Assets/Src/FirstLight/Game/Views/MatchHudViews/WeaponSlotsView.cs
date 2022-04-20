using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Quantum;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FirstLight.Game.Views.AdventureHudViews
{
	/// <summary>
	/// Handles logic for Weapon slots UI
	/// </summary>
	public class WeaponSlotsView : MonoBehaviour
	{
		[SerializeField, Required] private TextMeshProUGUI[] _weaponText;
		[SerializeField, Required] private Image[] _weaponImage;
		
		private IGameServices _services;
		private IGameDataProvider _dataProvider;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_dataProvider = MainInstaller.Resolve<IGameDataProvider>();
			
			QuantumEvent.Subscribe<EventOnLocalPlayerWeaponAdded>(this, OnEventOnLocalPlayerWeaponAdded);
		}

		private void Start()
		{
			UpdateWeaponSlot(GameId.Hammer, 0);
		}
		
		private void OnEventOnLocalPlayerWeaponAdded(EventOnLocalPlayerWeaponAdded callback)
		{
			UpdateWeaponSlot(callback.Weapon.GameId, callback.WeaponSlotNumber);
		}
		
		private async void UpdateWeaponSlot(GameId weaponGameId, int weaponSlotNumber)
		{
			_weaponImage[weaponSlotNumber].sprite = await _services.AssetResolverService.RequestAsset<GameId, Sprite>(weaponGameId);
			_weaponText[weaponSlotNumber].text = weaponGameId.GetTranslation();
		}
	}
}

