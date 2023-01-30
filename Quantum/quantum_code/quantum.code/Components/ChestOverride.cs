using System;
using System.Collections.Generic;
using System.Linq;
using Photon.Deterministic;
using Quantum.Collections;

namespace Quantum
{
	/// <summary>
	/// This component can be added to spawners or chests to override the contents of the chest
	/// </summary>
	public unsafe partial struct ChestOverride
	{
		/// <summary>
		/// Copies the <paramref name="currentComponent"/> to the <paramref name="targetEntity"/> and removes it from <paramref name="originalEntity"/>
		/// </summary>
		internal void CopyComponent(Frame f, EntityRef targetEntity, EntityRef originalEntity, ChestOverride* currentComponent)
		{
			f.Add(targetEntity, *currentComponent, out var copy);
			copy->ContentsOverride = currentComponent->ContentsOverride;
			f.Remove<ChestOverride>(originalEntity);
		}
	}
}
