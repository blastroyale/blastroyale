using Photon.Deterministic;

namespace Quantum.Systems
{
	/// <summary>
	/// Handles specifics for Rage status modifier
	/// </summary>
	public unsafe class RageSystem : SystemSignalsOnly, ISignalOnComponentAdded<Rage>,
	                                 ISignalOnComponentRemoved<Rage>
	{
		/// <inheritdoc />
		public void OnAdded(Frame f, EntityRef entity, Rage* component)
		{
			if (!f.Unsafe.TryGetPointer<Stats>(entity, out var stats))
			{
				return;
			}
			
			component->PowerModifierId = ++f.Global->ModifierIdCount;
			
			var powerModifier = new Modifier
				{
					Id = component->PowerModifierId,
					Type = StatType.Power,
					Power = component->Power,
					Duration = FP.MaxValue,
					EndTime = FP.MaxValue,
					IsNegative = false
				};
			
			stats->AddModifier(f, powerModifier);
		}

		/// <inheritdoc />
		public void OnRemoved(Frame f, EntityRef entity, Rage* component)
		{
			if (!f.Unsafe.TryGetPointer<Stats>(entity, out var stats))
			{
				return;
			}
			
			stats->RemoveModifier(f, component->PowerModifierId);
		}
	}
}