using Cysharp.Threading.Tasks;
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
			if (HasRenderedView()) return;
			Services.AssetResolverService.RequestAsset<GameId, GameObject>(collectable.GameId, true, true, OnLoaded).Forget();
		}

		protected override string GetName(QuantumGame game)
		{
			var collectable = GetComponentData<Collectable>(game);
			return collectable.GameId + " - " + EntityView.EntityRef;
		}

		protected override string GetGroup(QuantumGame game)
		{
			return "Consumables";
		}
	}
}