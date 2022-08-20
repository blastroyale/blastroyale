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
				Duration = component->Duration,
				StartTime = f.Time,
				IsNegative = false
			};
			
			stats->AddModifier(f, entity, powerModifier);
		}

		/// <inheritdoc />
		public void OnRemoved(Frame f, EntityRef entity, Rage* component)
		{
			if (!f.Unsafe.TryGetPointer<Stats>(entity, out var stats))
			{
				return;
			}

			var modifiers = f.ResolveList(stats->Modifiers);

			for (var i = 0; i < modifiers.Count; i++)
			{
				if (modifiers[i].Id == component->PowerModifierId)
				{
					stats->RemoveModifier(f, entity, i);
					break;
				}
			}
		}
	}
}