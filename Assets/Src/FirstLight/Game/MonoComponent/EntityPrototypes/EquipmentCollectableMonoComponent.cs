using FirstLight.Game.Messages;
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

		protected override void OnEntityInstantiated(QuantumGame game)
		{
			var collectable = GetComponentData<EquipmentCollectable>(game);

			_collectableView.SetEntityView(game, EntityView);

			TryShowEquipment(collectable.Item);
		}

		private async void TryShowEquipment(Equipment item)
		{
			var instance = await Services.AssetResolverService.RequestAsset<GameId, GameObject>(item.GameId);

			if (this.IsDestroyed())
			{
				Destroy(instance);
				return;
			}

			var cacheTransform = instance.transform;
			cacheTransform.SetParent(_itemTransform);
			cacheTransform.localPosition = Vector3.zero;
			cacheTransform.localScale = Vector3.one;
			cacheTransform.localRotation = Quaternion.identity;
			Services.MessageBrokerService.Publish(new EquipmentInstantiatedMessage()
			{
				Equipment = item,
				Object = instance
			});
		}

		protected override string GetName(QuantumGame game)
		{
			var collectable = GetComponentData<EquipmentCollectable>(game);
			return collectable.Item.GameId + " - " + EntityView.EntityRef;
		}

		protected override string GetGroup(QuantumGame game)
		{
			return "Equipment Collectables";
		}
	}
}