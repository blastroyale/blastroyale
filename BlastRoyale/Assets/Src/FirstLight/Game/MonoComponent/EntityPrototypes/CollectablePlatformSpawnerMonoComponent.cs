using FirstLight.Game.MonoComponent.EntityViews;
using Quantum;
using Sirenix.OdinInspector;
using UnityEngine;

namespace FirstLight.Game.MonoComponent.EntityPrototypes
{
	/// <summary>
	/// Responsible for instantiating the correct consumable platform model as a child of the consumable spawner entity view.
	/// </summary>
	public class CollectablePlatformSpawnerMonoComponent : EntityBase
	{
		[SerializeField, Required] private CollectablePlatformSpawnerViewMonoComponent _collectablePlatformView;
		
		protected override void OnEntityInstantiated(QuantumGame game)
		{
			_collectablePlatformView.SetEntityView(game, EntityView);
		}
	}
}