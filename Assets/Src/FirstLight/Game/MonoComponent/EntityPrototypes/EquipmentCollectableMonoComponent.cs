using System;
using System.Threading.Tasks;
using FirstLight.Game.MonoComponent.EntityViews;
using FirstLight.Game.Utils;
using Quantum;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FirstLight.Game.MonoComponent.EntityPrototypes
{
	/// <summary>
	/// This Mono component controls the behaviour of the <see cref="EquipmentCollectable"/>'s <see cref="Quantum.EntityPrototype"/>
	/// </summary>
	public class EquipmentCollectableMonoComponent : EntityBase
	{
		[SerializeField, Required] private Transform _itemTransform;
		[SerializeField, Required] private CollectableViewMonoComponent _collectableView;
		[SerializeField, Required] private EquipmentRarityEffectDictionary _rarityEffects;
		
		protected override async void OnEntityInstantiated(QuantumGame game)
		{
			_collectableView.SetEntityView(game, EntityView);

			var collectable = GetComponentData<Collectable>(game);
			var instance = await Services.AssetResolverService.RequestAsset<GameId, GameObject>(collectable.GameId);
			var cacheTransform = instance.transform;

			if (this.IsDestroyed())
			{
				Destroy(instance);
				return;
			}

			cacheTransform.SetParent(_itemTransform);

			cacheTransform.localPosition = Vector3.zero;
			cacheTransform.localScale = Vector3.one;
			cacheTransform.localRotation = Quaternion.identity;
			
			await ShowRarityEffect(game);
		}

		private async Task ShowRarityEffect(QuantumGame game)
		{
			var rarity = GetComponentData<EquipmentCollectable>(game).Item.Rarity;
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