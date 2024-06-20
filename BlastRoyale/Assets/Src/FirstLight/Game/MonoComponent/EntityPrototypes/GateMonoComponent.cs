using FirstLight.Game.MonoComponent.EntityViews;
using Quantum;
using Sirenix.OdinInspector;
using UnityEngine;

namespace FirstLight.Game.MonoComponent.EntityPrototypes
{
	/// <summary>
	/// This Mono component controls the behaviour of the <see cref="Gate"/>'s <see cref="Quantum.EntityPrototype"/>
	/// </summary>
	public class GateMonoComponent : EntityBase
	{
		[SerializeField, Required] private GateBarrierViewMonoComponent _gateView;
		
		protected override void OnEntityInstantiated(QuantumGame game)
		{
			_gateView.SetEntityView(game, EntityView);
		}
	}
}