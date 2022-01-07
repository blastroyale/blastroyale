namespace Quantum.Systems
{
	/// <summary>
	/// This system handles all the behaviour of the <see cref="Modifier"/> active in the entities & all changes to the
	/// entities <see cref="Stats"/>
	/// </summary>
	public unsafe class StatSystem : SystemMainThreadFilter<StatSystem.StatsFilter>, 
	                                 ISignalOnComponentAdded<Stats>, ISignalOnComponentRemoved<Stats>
	{
		public struct StatsFilter
		{
			public EntityRef Entity;
			public Stats* Stats;
		}

		/// <inheritdoc />
		public void OnAdded(Frame f, EntityRef entity, Stats* component)
		{
			component->Modifiers = f.AllocateList<Modifier>(32);
		}

		/// <inheritdoc />
		public void OnRemoved(Frame f, EntityRef entity, Stats* component)
		{
			f.FreeList(component->Modifiers);

			component->Modifiers = default;
		}

		/// <inheritdoc />
		public override void Update(Frame f, ref StatsFilter filter)
		{
			var list = f.ResolveList(filter.Stats->Modifiers);

			for (var i = list.Count - 1; i > -1 ; i--)
			{
				if (f.Time > list[i].EndTime)
				{
					filter.Stats->RemoveModifier(f, list[i]);
					list.RemoveAt(i);
				}
			}

			if (filter.Stats->CurrentStatusModifierType == StatusModifierType.None)
			{
				return;
			}
			
			if (f.Time > filter.Stats->CurrentStatusModifierEndTime)
			{
				StatusModifiers.FinishCurrentStatusModifier(f, filter.Entity);
			}
		}
	}
}