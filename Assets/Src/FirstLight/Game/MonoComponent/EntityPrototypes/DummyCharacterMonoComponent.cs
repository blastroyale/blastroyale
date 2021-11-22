using FirstLight.Game.Messages;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.MonoComponent.EntityPrototypes
{
	/// <summary>
	/// This Mono component controls the behaviour of the <see cref="DummyCharacter"/>'s <see cref="Quantum.EntityPrototype"/>
	/// </summary>
	public class DummyCharacterMonoComponent : HealthEntityBase
	{
		protected override void OnEntityInstantiated(QuantumGame game)
		{
			base.OnEntityInstantiated(game);
			
			Services.AssetResolverService.RequestAsset<GameId, GameObject>(GameId.DummyCharacter, true, true, OnLoaded);
		}
	}
}