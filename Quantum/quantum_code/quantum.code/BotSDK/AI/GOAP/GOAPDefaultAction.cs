using Photon.Deterministic;
using System.Collections.Generic;
using System;

namespace Quantum
{
	[Serializable]
	public unsafe partial class GOAPDefaultAction : GOAPAction
	{
		public AssetRefAIAction[] OnActivateLinks;
		public AssetRefAIAction[] OnUpdateLinks;
		public AssetRefAIAction[] OnDeactivateLinks;
	}
}