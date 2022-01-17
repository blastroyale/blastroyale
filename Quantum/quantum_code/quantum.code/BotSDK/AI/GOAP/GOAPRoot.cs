using System;
using Photon.Deterministic;

namespace Quantum
{
	public partial class GOAPRoot
	{
		// PUBLIC MEMBERS

		public string               Label;
		public FP                   InterruptionCheckInterval = FP._0_50;

		public AssetRefGOAPGoal[]   GoalRefs;
		public AssetRefGOAPAction[] ActionRefs;

		[NonSerialized]
		public GOAPGoal[]           Goals;
		[NonSerialized]
		public GOAPAction[]         Actions;

		// AssetObject INTERFACE

		public override void Loaded(IResourceManager resourceManager, Native.Allocator allocator)
		{
			base.Loaded(resourceManager, allocator);

			Goals = new GOAPGoal[GoalRefs == null ? 0 : GoalRefs.Length];
			for (int i = 0; i < GoalRefs.Length; i++)
			{
				Goals[i] = (GOAPGoal)resourceManager.GetAsset(GoalRefs[i].Id);
			}

			Actions = new GOAPAction[ActionRefs == null ? 0 : ActionRefs.Length];
			for (int i = 0; i < ActionRefs.Length; i++)
			{
				Actions[i] = (GOAPAction)resourceManager.GetAsset(ActionRefs[i].Id);
			}
		}
	}
}