using System;
using FirstLight.FLogger;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using I2.Loc;
using Quantum;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace FirstLight.Game.Views.AdventureHudViews
{
	/// <summary>
	/// Handles logic for Weapon slots UI
	/// </summary>
	public class WeaponSlotsView : MonoBehaviour
	{
		[SerializeField, TabGroup("Slots")] private SlotInfo[] _slots;
		[SerializeField, TabGroup("Colors")] private Color _selectedTextColor;
		[SerializeField, TabGroup("Colors")] private Color _selectedSmearColor;
		[SerializeField, TabGroup("Colors")] private Color _emptyNameBackgroundColor;
		[SerializeField, TabGroup("Colors")] private float _notSelectedOpacity;
		[SerializeField, TabGroup("Colors")] private float _emptyOpacity;
		[SerializeField, TabGroup("Colors")] private EquipmentRarityRarityInfoDictionary _rarityInfos;
		[SerializeField, TabGroup("Offsets")] private float _selectedOffset;

		private IGameServices _services;
		private IGameDataProvider _dataProvider;

		private Equipment[] _equippedWeapons = new Equipment[Constants.MAX_WEAPONS];

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_dataProvider = MainInstaller.Resolve<IGameDataProvider>();

			_services.MessageBrokerService.Subscribe<MatchStartedMessage>(OnMatchStartedMessage);
			QuantumEvent.Subscribe<EventOnLocalPlayerWeaponAdded>(this, OnEventOnLocalPlayerWeaponAdded);
			QuantumEvent.Subscribe<EventOnLocalPlayerWeaponChanged>(this, OnLocalPlayerWeaponChanged);

			UpdateWeaponSlot(new Equipment(GameId.Hammer), 0);
			UpdateWeaponSlot(Equipment.None, 1);
			UpdateWeaponSlot(Equipment.None, 2);
			SetSelectedSlot(0);
		}

		private void OnDestroy()
		{
			QuantumEvent.UnsubscribeListener(this);
			_services.MessageBrokerService.UnsubscribeAll(this);
		}

		private void OnMatchStartedMessage(MatchStartedMessage msg)
		{
			if (!msg.IsResync || _services.NetworkService.QuantumClient.LocalPlayer.IsSpectator())
			{
				return;
			}

			var game = QuantumRunner.Default.Game;
			var f = game.Frames.Verified;
			var gameContainer = f.GetSingleton<GameContainer>();
			var playersData = gameContainer.PlayersData;
			var localPlayer = playersData[game.GetLocalPlayers()[0]];

			if (!localPlayer.Entity.IsAlive(f))
			{
				return;
			}

			var playerCharacter = f.Get<PlayerCharacter>(localPlayer.Entity);

			UpdateWeaponSlot(playerCharacter.WeaponSlots[Constants.WEAPON_INDEX_DEFAULT].Weapon,
			                 Constants.WEAPON_INDEX_DEFAULT);
			UpdateWeaponSlot(playerCharacter.WeaponSlots[Constants.WEAPON_INDEX_PRIMARY].Weapon,
			                 Constants.WEAPON_INDEX_PRIMARY);
			UpdateWeaponSlot(playerCharacter.WeaponSlots[Constants.WEAPON_INDEX_SECONDARY].Weapon,
			                 Constants.WEAPON_INDEX_SECONDARY);
			SetSelectedSlot(playerCharacter.CurrentWeaponSlot);
		}

		private void OnLocalPlayerWeaponChanged(EventOnLocalPlayerWeaponChanged callback)
		{
			SetSelectedSlot(callback.Slot);
		}

		private void OnEventOnLocalPlayerWeaponAdded(EventOnLocalPlayerWeaponAdded callback)
		{
			UpdateWeaponSlot(callback.Weapon, callback.WeaponSlotNumber);
		}

		private void SetSelectedSlot(int slotIndex, bool force = false)
		{
			for (var i = 0; i < _slots.Length; i++)
			{
				var slot = _slots[i];
				var item = _equippedWeapons[i];

				if (!force && !item.IsValid()) continue;

				if (i == slotIndex)
				{
					// Selected
					slot.NameBackground.color = _selectedTextColor;
					slot.SmearShadow.color = _selectedSmearColor;

					var pos = slot.Container.anchoredPosition;
					pos.y = _selectedOffset;
					slot.Container.anchoredPosition = pos;

					var smearColor = slot.Smear.color;
					smearColor.a = 1f;
					slot.Smear.color = smearColor;

					var weaponColor = slot.Weapon.color;
					weaponColor.a = 1f;
					slot.Weapon.color = weaponColor;
				}
				else
				{
					// Not selected
					slot.NameBackground.color = Color.black;
					slot.SmearShadow.color = Color.black;

					var pos = slot.Container.anchoredPosition;
					pos.y = 0f;
					slot.Container.anchoredPosition = pos;

					var smearColor = slot.Smear.color;
					smearColor.a = _notSelectedOpacity;
					slot.Smear.color = smearColor;

					var weaponColor = slot.Weapon.color;
					weaponColor.a = _notSelectedOpacity;
					slot.Weapon.color = weaponColor;
				}
			}
		}

		private async void UpdateWeaponSlot(Equipment equipment, int slotIndex)
		{
			_equippedWeapons[slotIndex] = equipment;

			var slot = _slots[slotIndex];

			if (equipment.IsValid())
			{
				slot.Name.text = equipment.GameId.GetTranslation();

				slot.Weapon.enabled = true;
				slot.SmearShadow.enabled = true;
				slot.SmearPattern.enabled = true;
				slot.NameBackground.color = Color.black;

				SetRarity(slot, equipment.Rarity);
				
				if (Application.isPlaying) // To allow editor debug buttons to work
				{
					slot.Weapon.enabled = false;
					slot.Weapon.sprite =
						await _services.AssetResolverService.RequestAsset<GameId, Sprite>(equipment.GameId);
					slot.Weapon.enabled = true;
				}

			}
			else
			{
				slot.Name.text = ScriptLocalization.AdventureMenu.Empty;
				slot.NameBackground.color = _emptyNameBackgroundColor;
				slot.Smear.color = new Color(0f, 0f, 0f, _emptyOpacity);
				slot.Weapon.enabled = false;
				slot.SmearShadow.enabled = false;
				slot.SmearPattern.enabled = false;
			}
		}

		private void SetRarity(SlotInfo slot, EquipmentRarity rarity)
		{
			var rarityInfo = _rarityInfos[rarity];

			slot.Smear.color = rarityInfo.SmearColor;
			slot.SmearPattern.color = rarityInfo.SmearPatternColor;

			if (rarityInfo.HasRays)
			{
				slot.RaysHolder.SetActive(true);
				slot.Rays.color = rarityInfo.LightRaysColor;
				slot.RaysShadow.color = rarityInfo.LightRaysShadowColor;
			}
			else
			{
				slot.RaysHolder.SetActive(false);
			}
		}

		[Button, FoldoutGroup("Debug")]
		private void DebugSetRarity(EquipmentRarity rarity)
		{
			for (var i = 0; i < _slots.Length; i++)
			{
				UpdateWeaponSlot(new Equipment(GameId.Hammer, rarity: rarity), i);
			}
		}

		[InfoBox("Use -1 to deselect all of them.")]
		[Button, FoldoutGroup("Debug")]
		private void DebugSetSelected(int slot)
		{
			SetSelectedSlot(slot, true);
		}

		[Button, FoldoutGroup("Debug")]
		private void DebugReset()
		{
			for (int i = 0; i < _slots.Length; i++)
			{
				UpdateWeaponSlot(Equipment.None, i);
			}

			SetSelectedSlot(-1, true);
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
		private struct SlotInfo
		{
			[Required] public RectTransform Container;
			[Required] public TextMeshProUGUI Name;
			[Required] public Image NameBackground;
			[Required] public Image Weapon;
			[Required] public Image Smear;
			[Required] public Image SmearShadow;
			[Required] public Image SmearPattern;
			[Required] public GameObject RaysHolder;
			[Required] public Image Rays;
			[Required] public Image RaysShadow;
		}

		[Serializable]
		private class EquipmentRarityRarityInfoDictionary : UnitySerializedDictionary<EquipmentRarity, RarityInfo>
		{
		}
	}
}