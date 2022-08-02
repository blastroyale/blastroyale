using System;
using System.Threading.Tasks;
using FirstLight.Game.MonoComponent.EntityViews;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Quantum;
using Sirenix.OdinInspector;
using UnityEngine;

namespace FirstLight.Game.MonoComponent.EntityPrototypes
{
	/// <summary>
	/// This Mono component controls the behaviour of the <see cref="EquipmentCollectable"/>'s <see cref="Quantum.EntityPrototype"/>
	/// </summary>
	public class EquipmentCollectableMonoComponent : EntityBase
	{
		[SerializeField, Required] private Transform _itemTransform;
		[SerializeField, Required] private CollectableViewMonoComponent _collectableView;
		[SerializeField, Required] private GameObject _higherRarityArrow;

		private IMatchServices _matchServices;

		protected override async void OnEntityInstantiated(QuantumGame game)
		{
			_collectableView.SetEntityView(game, EntityView);

			_matchServices = MainInstaller.Resolve<IMatchServices>();

			if (await ShowEquipment(game)) return;

			await ShowRarityEffect(game);

			_matchServices.SpectateService.SpectatedPlayer.InvokeObserve(OnSpectatedPlayerChanged);
			QuantumEvent.Subscribe<EventOnPlayerWeaponChanged>(this, HandleOnPlayerWeaponChanged);
		}

		protected override void OnEntityDestroyed(QuantumGame game)
		{
			_matchServices.SpectateService.SpectatedPlayer.StopObservingAll(this);
			QuantumEvent.UnsubscribeListener(this);
		}

		private async Task<bool> ShowEquipment(QuantumGame game)
		{
			var collectable = GetComponentData<Collectable>(game);
			var instance = await Services.AssetResolverService.RequestAsset<GameId, GameObject>(collectable.GameId);

			if (this.IsDestroyed())
			{
				Destroy(instance);
				return true;
			}

			var cacheTransform = instance.transform;
			cacheTransform.SetParent(_itemTransform);
			cacheTransform.localPosition = Vector3.zero;
			cacheTransform.localScale = Vector3.one;
			cacheTransform.localRotation = Quaternion.identity;

			return false;
		}

		private async Task ShowRarityEffect(QuantumGame game)
		{
			var equipmentCollectable = GetComponentData<EquipmentCollectable>(game);

			var rarity = equipmentCollectable.Item.Rarity;
			var effect = await Services.AssetResolverService.RequestAsset<EquipmentRarity, GameObject>(rarity);
			var effectTransform = effect.transform;

			effectTransform.SetParent(transform);
			effectTransform.localPosition = Vector3.zero;
			effectTransform.localScale = Vector3.one;
			effectTransform.localRotation = Quaternion.identity;
		}

		private void OnSpectatedPlayerChanged(SpectatedPlayer previous, SpectatedPlayer next)
		{
			if (!next.Entity.IsValid) return; // In case where we spawn Equipment Collectables with the map

			RefreshArrow(next.Entity);
		}

		private void HandleOnPlayerWeaponChanged(EventOnPlayerWeaponChanged callback)
		{
			if (callback.Entity != _matchServices.SpectateService.SpectatedPlayer.Value.Entity) return;

			RefreshArrow(callback.Entity);
		}

		private void RefreshArrow(EntityRef playerEntity)
		{
			var game = QuantumRunner.Default.Game;
			ShowHigherRarityArrow(game, playerEntity, GetComponentData<EquipmentCollectable>(game));
		}

		private void ShowHigherRarityArrow(QuantumGame game, EntityRef playerEntity, EquipmentCollectable collectable)
		{
			if (!collectable.Item.IsWeapon() || !game.Frames.Verified.Exists(playerEntity)) return;

			var playerCharacter = game.Frames.Verified.Get<PlayerCharacter>(playerEntity);

			var numOfSlots = playerCharacter.WeaponSlots.Length;
			for (int slotIndex = 0; slotIndex < numOfSlots; slotIndex++)
			{
				var weaponSlot = playerCharacter.WeaponSlots[slotIndex];

				// If we already have the weapon, we would use that slot
				if (weaponSlot.Weapon.GameId == collectable.Item.GameId)
				{
					_higherRarityArrow.SetActive(weaponSlot.Weapon.Rarity < collectable.Item.Rarity);
					return;
				}

				// If there's an empty slot starting on a lower index, we would use that slot
				if (!weaponSlot.Weapon.IsValid())
				{
					_higherRarityArrow.SetActive(true);
					return;
				}
			}

			// If no other criteria selected a slot, we use the secondary slot
			var secondaryWeaponSlot = playerCharacter.WeaponSlots[Constants.WEAPON_INDEX_SECONDARY];
			_higherRarityArrow.SetActive(secondaryWeaponSlot.Weapon.Rarity < collectable.Item.Rarity);
		}
	}

	[Serializable]
	public class EquipmentRarityEffectDictionary : UnitySerializedDictionary<EquipmentRarity, GameObject>
	{
	}
}
