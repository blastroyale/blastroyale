using System;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
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
		[SerializeField, TabGroup("Colors")] private float _selectedOpacity;
		[SerializeField, TabGroup("Colors")] private EquipmentRarityRarityInfoDictionary _rarityInfos;

		private IGameServices _services;
		private IGameDataProvider _dataProvider;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_dataProvider = MainInstaller.Resolve<IGameDataProvider>();

			_services.MessageBrokerService.Subscribe<MatchReadyForResyncMessage>(OnMatchReadyForResyncMessage);
			QuantumEvent.Subscribe<EventOnLocalPlayerWeaponAdded>(this, OnEventOnLocalPlayerWeaponAdded);
			QuantumEvent.Subscribe<EventOnLocalPlayerWeaponChanged>(this, OnLocalPlayerWeaponChanged);
		}

		private void OnDestroy()
		{
			QuantumEvent.UnsubscribeListener(this);
			_services.MessageBrokerService.UnsubscribeAll(this);
		}
		
		private void OnMatchReadyForResyncMessage(MatchReadyForResyncMessage msg)
		{
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
			
			UpdateWeaponSlot(playerCharacter.WeaponSlots[0].Weapon, 0);
			UpdateWeaponSlot(playerCharacter.WeaponSlots[1].Weapon, 1);
			UpdateWeaponSlot(playerCharacter.WeaponSlots[2].Weapon, 2);
			SetSelectedSlot(playerCharacter.CurrentWeaponSlot);
		}

		private void OnLocalPlayerWeaponChanged(EventOnLocalPlayerWeaponChanged callback)
		{
			SetSelectedSlot(callback.Slot);
		}

		private void Start()
		{
			UpdateWeaponSlot(new Equipment(GameId.Hammer), 0);
			SetSelectedSlot(0);
		}

		private void OnEventOnLocalPlayerWeaponAdded(EventOnLocalPlayerWeaponAdded callback)
		{
			UpdateWeaponSlot(callback.Weapon, callback.WeaponSlotNumber);
		}

		private void SetSelectedSlot(int slotIndex)
		{
			for (var i = 0; i < _slots.Length; i++)
			{
				var slot = _slots[i];

				if (i == slotIndex)
				{
					// Selected
					slot.Name.color = _selectedTextColor;
					slot.SmearShadow.enabled = false;

					var smearColor = slot.Smear.color;
					smearColor.a = _selectedOpacity;
					slot.Smear.color = smearColor;

					var weaponColor = slot.Weapon.color;
					weaponColor.a = _selectedOpacity;
					slot.Weapon.color = weaponColor;
				}
				else
				{
					// Not selected
					slot.Name.color = Color.white;
					slot.SmearShadow.enabled = true;

					var smearColor = slot.Smear.color;
					smearColor.a = 1f;
					slot.Smear.color = smearColor;

					var weaponColor = slot.Weapon.color;
					weaponColor.a = 1f;
					slot.Weapon.color = weaponColor;
				}
			}
		}

		private async void UpdateWeaponSlot(Equipment equipment, int slotIndex)
		{
			var slot = _slots[slotIndex];

			slot.Name.text = equipment.GameId.GetTranslation();
			SetRarity(slot, equipment.Rarity);

			try
			{

			
			slot.Weapon.sprite = await _services.AssetResolverService.RequestAsset<GameId, Sprite>(equipment.GameId);
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				throw;
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

		[Button]
		private void DebugSetRarity(EquipmentRarity rarity)
		{
			foreach (var slotInfo in _slots)
			{
				SetRarity(slotInfo, rarity);
			}
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
			public TextMeshProUGUI Name;
			public Image Weapon;
			public Image Smear;
			public Image SmearShadow;
			public Image SmearPattern;
			public GameObject RaysHolder;
			public Image Rays;
			public Image RaysShadow;
		}

		[Serializable]
		private class EquipmentRarityRarityInfoDictionary : UnitySerializedDictionary<EquipmentRarity, RarityInfo>
		{
		}
	}
}