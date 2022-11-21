using Photon.Deterministic;
using System;
using Quantum.Prototypes;
using Quantum.Collections;

namespace Quantum
{
	[Serializable]
	public struct ResponseCurvePack
	{
		public AssetRefAIFunction ResponseCurveRef;
		[NonSerialized] public ResponseCurve ResponseCurve;
	}

	// ============================================================================================================

	public unsafe partial class Consideration
	{
		// ========== PUBLIC MEMBERS ==================================================================================

		public string Label;

		public AssetRefAIFunction RankRef;
		public AssetRefAIFunction CommitmentRef;
		public AssetRefConsideration[] NextConsiderationsRefs;
		public AssetRefAIAction[] OnEnterActionsRefs;
		public AssetRefAIAction[] OnUpdateActionsRefs;
		public AssetRefAIAction[] OnExitActionsRefs;

		public ResponseCurvePack[] ResponseCurvePacks;

		public FP BaseScore;

		public UTMomentumData MomentumData;
		public FP Cooldown;

		public byte Depth;
	}
}
