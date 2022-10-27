using System;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using I2.Loc;
using Quantum;
using Quantum.Commands;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;

namespace FirstLight.Game.Views.MatchHudViews
{
	/// <summary>
	/// Displays a single weapon slot.
	/// </summary>
	public class WeaponSlotView : MonoBehaviour
	{
		[SerializeField, Required, TabGroup("Refs")]
		private Button _button;
		
		[SerializeField, Required, TabGroup("Refs")]
		private RectTransform _container;

		[SerializeField, Required, TabGroup("Refs")]
		private CanvasGroup _weaponGroup;

		[SerializeField, Required, TabGroup("Refs")]
		private TextMeshProUGUI _name;

		[SerializeField, Required, TabGroup("Refs")]
		private Image _nameBackground;

		[SerializeField, Required, TabGroup("Refs")]
		private Image _nameShadow;

		[SerializeField, Required, TabGroup("Refs")]
		private Image _weapon;

		[SerializeField, Required, TabGroup("Refs")]
		private Image _smear;

		[SerializeField, Required, TabGroup("Refs")]
		private Image _smearBackground;

		[SerializeField, Required, TabGroup("Refs")]
		private Image _smearShadow;

		[SerializeField, Required, TabGroup("Refs")]
		private Image _smearSelectedShadow;

		[SerializeField, Required, TabGroup("Refs")]
		private Image _smearPattern;

		[SerializeField, Required, TabGroup("Refs")]
		private GameObject _raysHolder;

		[SerializeField, Required, TabGroup("Refs")]
		private Image _rays;

		[SerializeField, Required, TabGroup("Refs")]
		private Image _raysShadow;

		[SerializeField, TabGroup("Colors")] private Color _selectedTextColor;
		[SerializeField, TabGroup("Colors")] private float _notSelectedAlpha;
		[SerializeField, TabGroup("Colors")] private Color _emptyNameBackgroundColor;
		[SerializeField, TabGroup("Colors")] private Color _emptySmearColor;
		[SerializeField, TabGroup("Colors")] private EquipmentRarityRarityInfoDictionary _rarityInfos;
		[SerializeField, TabGroup("Offsets")] private float _selectedOffset;

		private IGameServices _services;
		private Equipment _equipment;
		private bool _selected;
		private float _initialAnchoredPosY;
		private int _index;

		private void Awake()
		{
			_button.onClick.AddListener(OnWeaponSlotClicked);
		}

		/// <summary>
		/// Initializes this weapon slot
		/// </summary>
		public void Init(int index)
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_initialAnchoredPosY = _container.anchoredPosition.y;
			_index = index;
		}

		/// <summary>
		/// Sets the Equipment to be displayed by this slot.
		/// </summary>
		public void SetEquipment(Equipment equipment)
		{
			var refresh = !_equipment.Equals(equipment);
			_equipment = equipment;
			if (refresh) RefreshView(true);
		}

		/// <summary>
		/// Sets if this slot should be drawn as selected.
		/// </summary>
		public void SetSelected(bool selected)
		{
			var refresh = _selected != selected;
			_selected = selected;
			if (refresh) RefreshView(true);
		}

		private async void RefreshView(bool loadSprite)
		{
			// Rarity
			var rarityInfo = _rarityInfos[_equipment.Rarity];

			_smear.color = rarityInfo.SmearColor;
			_smearPattern.color = rarityInfo.SmearPatternColor;

			if (rarityInfo.HasRays)
			{
				_raysHolder.SetActive(true);
				_rays.color = rarityInfo.LightRaysColor;
				_raysShadow.color = rarityInfo.LightRaysShadowColor;
			}
			else
			{
				_raysHolder.SetActive(false);
			}

			if (!_equipment.IsValid())
			{
				// Empty slot
				_name.text = ScriptLocalization.AdventureMenu.Empty;
				_nameBackground.color = _emptyNameBackgroundColor;
				_smear.color = _emptySmearColor;
				_weapon.enabled = false;
				_smearShadow.enabled = false;
				_smearPattern.enabled = false;
				_nameShadow.enabled = false;
				_smearBackground.enabled = false;
				_smearSelectedShadow.enabled = false;
				_weaponGroup.alpha = 1f;

				var pos = _container.anchoredPosition;
				pos.y = _initialAnchoredPosY;
				_container.anchoredPosition = pos;

				return;
			}
			
			_name.text = _equipment.GameId.GetTranslation();
			_weapon.enabled = true;
			_smearPattern.enabled = true;

			if (_selected)
			{
				// Selected
				_nameBackground.color = _selectedTextColor;
				_smearShadow.enabled = false;
				_smearSelectedShadow.enabled = true;
				_smearBackground.enabled = false;
				_weaponGroup.alpha = 1f;
				_nameShadow.enabled = true;

				var pos = _container.anchoredPosition;
				pos.y = _initialAnchoredPosY + _selectedOffset;
				_container.anchoredPosition = pos;
			}
			else
			{
				// Not selected
				_nameBackground.color = Color.black;
				_smearShadow.enabled = true;
				_smearSelectedShadow.enabled = false;
				_smearBackground.enabled = true;
				_weaponGroup.alpha = _notSelectedAlpha;
				_nameShadow.enabled = false;

				var pos = _container.anchoredPosition;
				pos.y = _initialAnchoredPosY;
				_container.anchoredPosition = pos;
			}

			if (loadSprite && Application.isPlaying) // To allow editor debug buttons to work
			{
				_weapon.enabled = false;
				_weapon.sprite =
					await _services.AssetResolverService.RequestAsset<GameId, Sprite>(_equipment.GameId);
				_weapon.enabled = true;
			}
		}

		private void OnWeaponSlotClicked()
		{
			var data = QuantumRunner.Default.Game.GetLocalPlayerData(false, out var f);

			// Check if there is a weapon equipped in the slot. Avoid extra commands to save network message traffic $$$
			if (!f.TryGet<PlayerCharacter>(data.Entity, out var pc) ||
			    pc.CurrentWeaponSlot == _index || !pc.WeaponSlots[_index].Weapon.IsValid())
			{
				return;
			}
			
			QuantumRunner.Default.Game.SendCommand(new WeaponSlotSwitchCommand { WeaponSlotIndex = _index });
		}

		[Serializable]
		private struct RarityInfo
		{
			public Color SmearColor;
			public Color SmearPatternColor;

			[ToggleGroup("HasRays")] public bool HasRays;
			[ToggleGroup("HasRays")] public Color LightRaysColor;
			[ToggleGroup("HasRays")] public Color LightRaysShadowColor;
		}

		[Serializable]
		private class EquipmentRarityRarityInfoDictionary : UnitySerializedDictionary<EquipmentRarity, RarityInfo>
		{
		}
	}
}