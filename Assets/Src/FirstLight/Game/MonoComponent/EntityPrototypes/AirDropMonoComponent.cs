using FirstLight.Game.MonoComponent.EntityViews;
using Quantum;
using Sirenix.OdinInspector;
using UnityEngine;

namespace FirstLight.Game.MonoComponent.EntityPrototypes
{
	public class AirDropMonoComponent: EntityBase
	{
		[SerializeField, Required] private CollectableViewMonoComponent _collectableView;

		protected override void OnEntityInstantiated(QuantumGame game)
		{
			var airDrop = GetComponentData<AirDrop>(game);
			
			_collectableView.SetEntityView(game, EntityView);

			Services.AssetResolverService.RequestAsset<GameId, GameObject>(airDrop.Chest, true, true, OnLoaded);
		}
	}
}