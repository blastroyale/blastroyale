using Photon.Deterministic;
using System.Collections.Generic;
using System;

namespace Quantum
{
	[Serializable]
	public unsafe partial class GOAPDefaultAction : GOAPAction
	{
		// PUBLIC MEMBERS

		public AIParamBool                  Validation = true;
		public AIParamFP                    Cost = FP._1;
		public AssetRefGOAPBackValidation   PlanStateValidationLink;

		public AssetRefAIAction[] OnActivateLinks;
		public AssetRefAIAction[] OnUpdateLinks;
		public AssetRefAIAction[] OnDeactivateLinks;

		public AIParamBool IsDone;
		public AIParamBool IsFailed;

		[NonSerialized]
		public GOAPBackValidation PlanStateValidation;

		[NonSerialized]
		public AIAction[] OnActivate;
		[NonSerialized]
		public AIAction[] OnUpdate;
		[NonSerialized]
		public AIAction[] OnDeactivate;

		public override bool UsePlanStateValidation => PlanStateValidation != null;

		// PUBLIC METHODS

		public override bool ValidateAction(Frame frame, GOAPEntityContext context, GOAPState startState, out FP cost)
		{
			cost = FP.MaxValue;

			if (Validation.Resolve(frame, context.Entity, context.Blackboard, context.Config) == false)
				return false;

			cost = Cost.Resolve(frame, context.Entity, context.Blackboard, context.Config);

			return cost < FP.MaxValue;
		}

		public override void ValidatePlanState(Frame frame, GOAPEntityContext context, GOAPState stateToValidate, GOAPState nextState, FP costToNextState, List<StateBackValidation> validatedStates)
		{
			PlanStateValidation.ValidatePlanState(frame, context.Entity, stateToValidate, nextState, costToNextState, validatedStates);
		}

		public override void Activate(Frame frame, GOAPEntityContext context)
		{
			ExecuteActions(frame, context.Entity, OnActivate);
		}

		public override EResult Update(Frame frame, GOAPEntityContext context)
		{
			if (IsDone.Resolve(frame, context.Entity, context.Blackboard, context.Config) == true)
				return EResult.IsDone;

			if (IsFailed.Resolve(frame, context.Entity, context.Blackboard, context.Config) == true)
				return EResult.IsFailed;

			ExecuteActions(frame, context.Entity, OnUpdate);

			return EResult.Continue;
		}

		public override void Deactivate(Frame frame, GOAPEntityContext context)
		{
			ExecuteActions(frame, context.Entity, OnDeactivate);
		}

		// AssetObject INTERFACE

		public override void Loaded(IResourceManager resourceManager, Native.Allocator allocator)
		{
			base.Loaded(resourceManager, allocator);

			PlanStateValidation = (GOAPBackValidation)resourceManager.GetAsset(PlanStateValidationLink.Id);

			OnActivate = new AIAction[OnActivateLinks == null ? 0 : OnActivateLinks.Length];
			for (int i = 0; i < OnActivate.Length; i++)
			{
				OnActivate[i] = (AIAction)resourceManager.GetAsset(OnActivateLinks[i].Id);
			}

			OnUpdate = new AIAction[OnUpdateLinks == null ? 0 : OnUpdateLinks.Length];
			for (int i = 0; i < OnUpdate.Length; i++)
			{
				OnUpdate[i] = (AIAction)resourceManager.GetAsset(OnUpdateLinks[i].Id);
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