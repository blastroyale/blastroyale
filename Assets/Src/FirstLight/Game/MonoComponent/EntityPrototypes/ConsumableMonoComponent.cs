using FirstLight.Game.Configs;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.MonoComponent.EntityPrototypes
{
	/// <summary>
	/// Responsible for instantiating the correct consumable model as a child of the consumable entity view.
	/// </summary>
	public class ConsumableMonoComponent : EntityBase
	{
		protected override void OnEntityInstantiated(QuantumGame game)
		{
			var collectable = GetComponentData<Collectable>(game);
			
			Services.AssetResolverService.RequestAsset<GameId, GameObject>(collectable.GameId, true, true, OnLoaded);
		}
	}
}