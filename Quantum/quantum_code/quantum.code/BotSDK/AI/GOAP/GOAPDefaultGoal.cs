using System;
using Photon.Deterministic;

namespace Quantum
{
	[Serializable]
	public unsafe partial class GOAPDefaultGoal : GOAPGoal
	{
		// PUBLIC MEMBERS

		public AIParamBool          Validation = true;
		public AIParamFP            Relevancy = FP._1;
		public AIParamFP            DisableTime;
		public AssetRefAIAction[]   OnInitPlanningLinks;
		public AssetRefAIAction[]   OnActivateLinks;
		public AssetRefAIAction[]   OnDeactivateLinks;
		public AIParamBool          IsFinished;

		[NonSerialized]
		public AIAction[] OnInitPlanning;
		[NonSerialized]
		public AIAction[] OnActivate;
		[NonSerialized]
		public AIAction[] OnDeactivate;

		// PUBLIC METHODS

		public override FP GetRelevancy(Frame frame, GOAPEntityContext context)
		{
			if (Validation.Resolve(frame, context.Entity, context.Blackboard, context.Config) == false)
				return 0;

			return Relevancy.Resolve(frame, context.Entity, context.Blackboard, context.Config);
		}

		public override void InitPlanning(Frame frame, GOAPEntityContext context, ref GOAPState startState, ref GOAPState targetState)
		{
			base.InitPlanning(frame, context, ref startState, ref targetState);

			ExecuteActions(frame, context.Entity, OnInitPlanning);
		}

		public override void Activate(Frame frame, GOAPEntityContext context)
		{
			base.Activate(frame, context);

			ExecuteActions(frame, context.Entity, OnActivate);
		}

		public override void Deactivate(Frame frame, GOAPEntityContext context)
		{
			ExecuteActions(frame, context.Entity, OnDeactivate);

			base.Deactivate(frame, context);
		}

		public override bool HasFinished(Frame frame, GOAPEntityContext context)
		{
			if (base.HasFinished(frame, context) == true)
				return true;

			return IsFinished.Resolve(frame, context.Entity, context.Blackboard, context.Config);
		}

		public override FP GetDisableTime(Frame frame, GOAPEntityContext context)
		{
			return DisableTime.Resolve(frame, context.Entity, context.Blackboard, context.Config);
		}

		// AssetObject INTERFACE

		public override void Loaded(IResourceManager resourceManager, Native.Allocator allocator)
		{
			base.Loaded(resourceManager, allocator);

			OnInitPlanning = new AIAction[OnInitPlanningLinks == null ? 0 : OnInitPlanningLinks.Length];
			for (int i = 0; i < OnInitPlanning.Length; i++)
			{
				OnInitPlanning[i] = (AIAction)resourceManager.GetAsset(OnInitPlanningLinks[i].Id);
			}

			OnActivate = new AIAction[OnActivateLinks == null ? 0 : OnActivateLinks.Length];
			for (int i = 0; i < OnActivate.Length; i++)
			{
				OnActivate[i] = (AIAction)resourceManager.GetAsset(OnActivateLinks[i].Id);
			}

			OnDeactivate = new AIAction[OnDeactivateLinks == null ? 0 : OnDeactivateLinks.Length];
			for (int i = 0; i < OnDeactivate.Length; i++)
			{
				OnDeactivate[i] = (AIAction)resourceManager.GetAsset(OnDeactivateLinks[i].Id);
			}
		}

		// PRIVATE METHODS

		private static void ExecuteActions(Frame frame, EntityRef entity, AIAction[] actions)
		{
			for (int i = 0; i < actions.Length; i++)
			{
				var action = actions[i];

				action.Update(frame, entity);

				int nextAction = action.NextAction(frame, entity);
				if (nextAction > i)
				{
					i = nextAction;
				}
			}
		}
	}
}