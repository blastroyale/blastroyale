using System;
using Photon.Deterministic;

namespace Quantum
{
	public abstract partial class HFSMLogicalDecision : HFSMDecision
	{
		// ========== PUBLIC MEMBERS ==================================================================================

		public AssetRefHFSMDecision[] Decisions;

		// ========== PROTECTED MEMBERS ===============================================================================

		protected HFSMDecision[] _decisions;

		// ========== AssetObject INTERFACE ===========================================================================

		public override void Loaded(IResourceManager resourceManager, Native.Allocator allocator)
		{
			base.Loaded(resourceManager, allocator);

			_decisions = new HFSMDecision[Decisions == null ? 0 : Decisions.Length];
			if (Decisions != null)
			{
				for (Int32 i = 0; i < Decisions.Length; i++)
				{
					_decisions[i] = (HFSMDecision)resourceManager.GetAsset(Decisions[i].Id);
				}
			}
		}
	}

	// ============================================================================================================

	[Serializable]
	[AssetObjectConfig(GenerateLinkingScripts = true, GenerateAssetCreateMenu = false, GenerateAssetResetMethod = false)]
	public partial class HFSMOrDecision : HFSMLogicalDecision
	{
		public override bool DecideThreadSafe(FrameThreadSafe frame, EntityRef entity)
		{
			return CheckDecisions(frame, entity);
		}

		private bool CheckDecisions(FrameThreadSafe frame, EntityRef entity)
		{
			foreach (var decision in _decisions)
			{
				if (decision.DecideThreadSafe(frame, entity) == true)
					return true;
			}
			return false;
		}
	}

	// ============================================================================================================

	[Serializable]
	[AssetObjectConfig(GenerateLinkingScripts = true, GenerateAssetCreateMenu = false, GenerateAssetResetMethod = false)]
	public partial class HFSMAndDecision : HFSMLogicalDecision
	{
		public override bool DecideThreadSafe(FrameThreadSafe frame, EntityRef entity)
		{
			return CheckDecisions(frame, entity);
		}

		public bool CheckDecisions(FrameThreadSafe frame, EntityRef entity)
		{
			foreach (var decision in _decisions)
			{
				if (decision.DecideThreadSafe(frame, entity) == false)
					return false;
			}
			return true;
		}
	}

	// ============================================================================================================

	[Serializable]
	[AssetObjectConfig(GenerateLinkingScripts = true, GenerateAssetCreateMenu = false, GenerateAssetResetMethod = false)]
	public partial class HFSMNotDecision : HFSMLogicalDecision
	{

		public override bool DecideThreadSafe(FrameThreadSafe frame, EntityRef entity)
		{
			return CheckDecisions(frame, entity);
		}

		public bool CheckDecisions(FrameThreadSafe frame, EntityRef entity)
		{
			return !_decisions[0].DecideThreadSafe(frame, entity);
		}
	}
}
