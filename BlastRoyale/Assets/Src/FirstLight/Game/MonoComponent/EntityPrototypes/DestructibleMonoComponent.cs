using FirstLight.Game.MonoComponent.EntityViews;
using Quantum;
using Sirenix.OdinInspector;
using UnityEngine;

namespace FirstLight.Game.MonoComponent.EntityPrototypes
{
	/// <summary>
	/// This Mono component controls the behaviour of the <see cref="Destructible"/>'s <see cref="Quantum.EntityPrototype"/>
	/// </summary>
	public class DestructibleMonoComponent : HealthEntityBase
	{
		[SerializeField, Required] private DestructibleViewMonoComponent _destructibleView;
		
		protected override void OnEntityInstantiated(QuantumGame game)
		{
			_destructibleView.SetEntityView(game, EntityView);
		}
	}
}