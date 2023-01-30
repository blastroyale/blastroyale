using System;
using System.Collections.Generic;
using System.Linq;
using Photon.Deterministic;
using Quantum.Collections;

namespace Quantum
{
	public unsafe partial struct ChestOverride
	{
		internal void CopyComponent(Frame f, EntityRef targetEntity, ChestOverride* currentComponent)
		{
			f.Add(targetEntity, *currentComponent, out var copy);
			copy->ContentsOverride = currentComponent->ContentsOverride;
		}
	}
}
