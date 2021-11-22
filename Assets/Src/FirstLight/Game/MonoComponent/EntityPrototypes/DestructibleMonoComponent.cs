using FirstLight.Game.Messages;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.MonoComponent.EntityPrototypes
{
	/// <summary>
	/// This Mono component controls the behaviour of the <see cref="Destructible"/>'s <see cref="Quantum.EntityPrototype"/>
	/// </summary>
	public class DestructibleMonoComponent : HealthEntityBase
	{
		protected override void OnEntityInstantiated(QuantumGame game)
		{
			var destructible = GetComponentData<Destructible>(game);
			
			Services.AssetResolverService.RequestAsset<GameId, GameObject>(destructible.GameId, true, true, OnLoaded);
			
			base.OnEntityInstantiated(game);
		}
	}
}