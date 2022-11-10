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
			var collectable = GetComponentData<EquipmentCollectable>(game);
			
			_matchServices = MainInstaller.Resolve<IMatchServices>();
			
			_collectableView.SetEntityView(game, EntityView);

			if (!await TryShowEquipment(collectable.Item.GameId) || !await TryShowRarityEffect(collectable.Item.Rarity)) return;

			_matchServices.SpectateService.SpectatedPlayer.InvokeObserve(OnSpectatedPlayerChanged);
			QuantumEvent.Subscribe<EventOnPlayerWeaponChanged>(this, HandleOnPlayerWeaponChanged);
		}

		protected override void OnEntityDestroyed(QuantumGame game)
		{
			_matchServices?.SpectateService?.SpectatedPlayer?.StopObservingAll(this);
		}

		private async Task<bool> TryShowEquipment(GameId item)
		{
			var instance = await Services.AssetResolverService.RequestAsset<GameId, GameObject>(item);

			if (this.IsDestroyed())
			{
				Destroy(instance);
				return false;
			}

			var cacheTransform = instance.transform;
			cacheTransform.SetParent(_itemTransform);
			cacheTransform.localPosition = Vector3.zero;
			cacheTransform.localScale = Vector3.one;
			cacheTransform.localRotation = Quaternion.identity;

			return true;
		}

		private async Task<bool> TryShowRarityEffect(EquipmentRarity rarity)
		{
			var instance = await Services.AssetResolverService.RequestAsset<EquipmentRarity, GameObject>(rarity);

			if (this.IsDestroyed())
			{
				Destroy(instance);
				return false;
			}
			
			var effectTransform = instance.transform;

			effectTransform.SetParent(transform);
			effectTransform.localPosition = Vector3.zero;
			effectTransform.localScale = Vector3.one;
			effectTransform.localRotation = Quaternion.identity;

			return true;
		}

		private void OnSpectatedPlayerChanged(SpectatedPlayer previous, SpectatedPlayer next)
		{
			if (!next.Entity.IsValid) return; 

			ShowHigherRarityArrow(QuantumRunner.Default.Game.Frames.Verified, next.Entity);
		}

		private void HandleOnPlayerWeaponChanged(EventOnPlayerWeaponChanged callback)
		{
			if (callback.Entity != _matchServices.SpectateService.SpectatedPlayer.Value.Entity) return;

			ShowHigherRarityArrow(callback.Game.Frames.Verified, callback.Entity);
		}

		private void ShowHigherRarityArrow(Frame f, EntityRef playerEntity)
		{
			if (!f.TryGet<EquipmentCollectable>(EntityView.EntityRef, out var collectable) || 
			    !collectable.Item.IsWeapon() || 
			    !f.TryGet<PlayerCharacter>(playerEntity, out var playerCharacter)) return;

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
