using Quantum;
using UnityEngine;


namespace FirstLight.Game.MonoComponent.EntityPrototypes
{
	/// <summary>
	/// This Mono component controls the behaviour of the <see cref="Projectile"/>'s <see cref="Quantum.EntityPrototype"/>
	/// </summary>
	public class ProjectileMonoComponent : EntityBase
	{
		protected override void OnEntityInstantiated(QuantumGame game)
		{
			var projectile = game.Frames.Predicted.Get<Projectile>(EntityView.EntityRef);
			
			Services.AssetResolverService.RequestAsset<GameId, GameObject>(projectile.Data.ProjectileId, true, true, OnLoaded);

		}
	}
}