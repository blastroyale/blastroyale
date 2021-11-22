namespace Quantum.Systems
{
	/// <summary>
	/// Handles specifics for Shield status modifier
	/// </summary>
	public unsafe class ShieldSystem : SystemSignalsOnly, 
	                                   ISignalOnComponentAdded<Shield>, ISignalOnComponentRemoved<Shield>
	{
		/// <inheritdoc />
		public void OnAdded(Frame f, EntityRef entity, Shield* component)
		{
			if (!f.Unsafe.TryGetPointer<Stats>(entity, out var stats))
			{
				return;
			}
			
			stats->IsImmune = true;
		}

		/// <inheritdoc />
		public void OnRemoved(Frame f, EntityRef entity, Shield* component)
		{
			if (!f.Unsafe.TryGetPointer<Stats>(entity, out var stats))
			{
				return;
			}
			
			stats->IsImmune = false;
		}
	}
}