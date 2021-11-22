using System.Collections.Generic;
using Photon.Deterministic;

namespace Quantum.Systems
{
	/// <summary>
	/// This system handles all the behaviour for <see cref="DummyCharacter"/> entity
	/// </summary>
	public class DummyCharacterSystem : SystemSignalsOnly, ISignalHealthIsZero
	{
		/// <inheritdoc />
		public void HealthIsZero(Frame f, EntityRef entity, EntityRef attacker)
		{
			if (f.Has<DummyCharacter>(entity))
			{
				f.Add<EntityDestroyer>(entity);
				
				f.Events.OnDummyCharacterKilled(entity);
			}
		}
	}
}