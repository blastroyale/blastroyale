using FirstLight.Game.Messages;
using FirstLight.Game.MonoComponent.EntityViews;
using FirstLight.Game.Utils;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.MonoComponent.EntityPrototypes
{
	/// <summary>
	/// This Mono component controls the behaviour of the <see cref="WeaponCollectable"/>'s <see cref="Quantum.EntityPrototype"/>
	/// </summary>
	public class WeaponCollectableMonoComponent : EntityBase
	{
		[SerializeField] private Transform _itemTransform;
		[SerializeField] private CollectableViewMonoComponent _collectableView;
		
		protected override async void OnEntityInstantiated(QuantumGame game)
		{
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

			_collectableView.SetEntityView(game, EntityView);
		}
	}
}