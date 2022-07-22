using System;
using System.Collections;
using System.Threading.Tasks;
using FirstLight.Game.MonoComponent.EntityViews;
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

		protected override async void OnEntityInstantiated(QuantumGame game)
		{
			_collectableView.SetEntityView(game, EntityView);
			var collectable = GetComponentData<Collectable>(game);
			var instance = await Services.AssetResolverService.RequestAsset<GameId, GameObject>(collectable.GameId);
			
			if (this.IsDestroyed())
			{
				Destroy(instance);
				return;
			}
			
			var equipmentCollectable = GetComponentData<EquipmentCollectable>(game);

			var cacheTransform = instance.transform;
			cacheTransform.SetParent(_itemTransform);
			cacheTransform.localPosition = Vector3.zero;
			cacheTransform.localScale = Vector3.one;
			cacheTransform.localRotation = Quaternion.identity;

			StartCoroutine(ShowArrowWhenPlayerReady(game, equipmentCollectable));

			await ShowRarityEffect(equipmentCollectable);
		}

		private IEnumerator ShowArrowWhenPlayerReady(QuantumGame game, EquipmentCollectable equipmentCollectable)
		{
			// We wait for 1s because this might be instantiated in the beginning of the game
			// and we might not have the player data right away
			yield return new WaitForSeconds(1);
			
			var f = game.Frames.Verified;
			var playersData = f.GetSingleton<GameContainer>().PlayersData;
			var localPlayer = playersData[game.GetLocalPlayers()[0]];
			
			ShowHigherRarityArrow(game, equipmentCollectable);
			
			QuantumEvent.Subscribe<EventOnLocalPlayerWeaponChanged>(this, HandleOnPlayerWeaponChanged);
		}

		private void HandleOnPlayerWeaponChanged(EventOnLocalPlayerWeaponChanged callback)
		{
			var game = callback.Game;
			var equipmentCollectable = GetComponentData<EquipmentCollectable>(game);
			ShowHigherRarityArrow(game, equipmentCollectable);
		}

		private void ShowHigherRarityArrow(QuantumGame game, EquipmentCollectable collectable)
		{
			if (!collectable.Item.IsWeapon())
			{
				return;
			}
			
			var f = game.Frames.Verified;
			var playersData = f.GetSingleton<GameContainer>().PlayersData;
			var localPlayer = playersData[game.GetLocalPlayers()[0]];
			var playerCharacter = f.Get<PlayerCharacter>(localPlayer.Entity);
			
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

		private async Task ShowRarityEffect(EquipmentCollectable collectable)
		{
			var rarity = collectable.Item.Rarity;
			var effect = await Services.AssetResolverService.RequestAsset<EquipmentRarity, GameObject>(rarity);
			var effectTransform = effect.transform;

			effectTransform.SetParent(transform);
			effectTransform.localPosition = Vector3.zero;
			effectTransform.localScale = Vector3.one;
			effectTransform.localRotation = Quaternion.identity;
		}
	}
	
	[Serializable]
	public class EquipmentRarityEffectDictionary : UnitySerializedDictionary<EquipmentRarity, GameObject>
	{
	}
}