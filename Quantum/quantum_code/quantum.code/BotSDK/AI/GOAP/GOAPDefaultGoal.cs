using System;
using Photon.Deterministic;

namespace Quantum
{
	[Serializable]
	public unsafe partial class GOAPDefaultGoal : GOAPGoal
	{
		public AssetRefAIAction[]   OnInitPlanningLinks;
		public AssetRefAIAction[]   OnActivateLinks;
		public AssetRefAIAction[]   OnDeactivateLinks;
	}
}