using Photon.Deterministic;
using System;
using System.Collections.Generic;

namespace Quantum
{
	[AssetObjectConfig(GenerateLinkingScripts = true, GenerateAssetCreateMenu = false, GenerateAssetResetMethod = false)]
	public unsafe partial class HFSMState : AssetObject
	{
		// ========== PUBLIC MEMBERS ==================================================================================

		public string Label;
		public AssetRefAIAction[] OnUpdateLinks;
		public AssetRefAIAction[] OnEnterLinks;
		public AssetRefAIAction[] OnExitLinks;
		public HFSMTransition[] Transitions;

		public AssetRefHFSMState[] ChildrenLinks;
		public AssetRefHFSMState ParentLink;
		public int Level;

		[NonSerialized]
		public AIAction[] OnUpdate;
		[NonSerialized]
		public AIAction[] OnEnter;
		[NonSerialized]
		public AIAction[] OnExit;
		[NonSerialized]
		public HFSMState[] Children;
		[NonSerialized]
		public HFSMState Parent;

		// ========== AssetObject INTERFACE ===========================================================================

		public override void Loaded(IResourceManager resourceManager, Native.Allocator allocator)
		{
			base.Loaded(resourceManager, allocator);

			OnUpdate = new AIAction[OnUpdateLinks == null ? 0 : OnUpdateLinks.Length];
			if (OnUpdateLinks != null)
			{
				for (Int32 i = 0; i < OnUpdateLinks.Length; i++)
				{
					OnUpdate[i] = (AIAction)resourceManager.GetAsset(OnUpdateLinks[i].Id);
				}
			}
			OnEnter = new AIAction[OnEnterLinks == null ? 0 : OnEnterLinks.Length];
			if (OnEnterLinks != null)
			{
				for (Int32 i = 0; i < OnEnterLinks.Length; i++)
				{
					OnEnter[i] = (AIAction)resourceManager.GetAsset(OnEnterLinks[i].Id);
				}
			}
			OnExit = new AIAction[OnExitLinks == null ? 0 : OnExitLinks.Length];
			if (OnExitLinks != null)
			{
				for (Int32 i = 0; i < OnExitLinks.Length; i++)
				{
					OnExit[i] = (AIAction)resourceManager.GetAsset(OnExitLinks[i].Id);
				}
			}

			Children = new HFSMState[ChildrenLinks == null ? 0 : ChildrenLinks.Length];
			if (ChildrenLinks != null)
			{
				for (Int32 i = 0; i < ChildrenLinks.Length; i++)
				{
					Children[i] = (HFSMState)resourceManager.GetAsset(ChildrenLinks[i].Id);
				}
			}

			Parent = (HFSMState)resourceManager.GetAsset(ParentLink.Id);
			if (Transitions != null)
			{
				for (int i = 0; i < Transitions.Length; i++)
				{
					Transitions[i].Setup(resourceManager);
				}
			}
		}

		// ========== INTERNAL METHODS ================================================================================

		internal Boolean UpdateState(FrameThreadSafe frame, FP deltaTime, HFSMData* hfsmData, EntityRef entity)
		{
			HFSMState parent = Parent;
			Boolean transition = false;

			if (parent != null)
			{
				transition = parent.UpdateState(frame, deltaTime, hfsmData, entity);
			}

			if (transition == true)
				return true;

			*hfsmData->Times.GetPointer(Level) += deltaTime;

			DoUpdateActions(frame, entity);
			return CheckStateTransitions(frame, hfsmData, entity, 0);
		}

		internal Boolean Event(FrameThreadSafe frame, HFSMData* hfsmData, EntityRef entity, Int32 eventInt)
		{
			HFSMState p = Parent;
			Boolean transition = false;
			if (p != null)
			{
				transition = p.Event(frame, hfsmData, entity, eventInt);
			}

			if (transition)
			{
				return true;
			}

			return CheckStateTransitions(frame, hfsmData, entity, eventInt);
		}

		internal void EnterState(FrameThreadSafe frame, HFSMData* hfsmData, EntityRef entity)
		{
			*hfsmData->Times.GetPointer(Level) = FP._0;
			DoEnterActions(frame, entity);
			if (Children != null && Children.Length > 0)
			{
				HFSMState child = Children[0];
				HFSMManager.ThreadSafe.ChangeState(child, frame, hfsmData, entity, "");
			}
		}

		internal void ExitState(HFSMState nextState, FrameThreadSafe frame, HFSMData* hfsmData, EntityRef entity)
		{
			if (nextState != null && nextState.IsChildOf(this) == true)
				return;

			DoExitActions(frame, entity);
			Parent?.ExitState(nextState, frame, hfsmData, entity);
		}

		internal bool IsChildOf(HFSMState state)
		{
			HFSMState parent = Parent;

			if (parent == null)
				return false;

			if (parent == state)
				return true;

			return parent.IsChildOf(state);
		}

		// ========== PRIVATE METHODS =================================================================================

		private void DoUpdateActions(FrameThreadSafe frame, EntityRef entity)
		{
			for (int i = 0; i < OnUpdate.Length; i++)
			{
				OnUpdate[i].Update(frame, entity);
				int nextAction = OnUpdate[i].NextActionThreadSafe(frame, entity);
				if (nextAction > i)
				{
					i = nextAction;
				}
			}
		}

		private void DoEnterActions(FrameThreadSafe frame, EntityRef entity)
		{
			for (int i = 0; i < OnEnter.Length; i++)
			{
				OnEnter[i].Update(frame, entity);
				int nextAction = OnEnter[i].NextActionThreadSafe(frame, entity);
				if (nextAction > i)
				{
					i = nextAction;
				}
			}
		}

		private void DoExitActions(FrameThreadSafe frame, EntityRef entity)
		{
			for (int i = 0; i < OnExit.Length; i++)
			{
				OnExit[i].Update(frame, entity);
				int nextAction = OnExit[i].NextActionThreadSafe(frame, entity);
				if (nextAction > i)
				{
					i = nextAction;
				}
			}
		}

		private bool CheckStateTransitions(FrameThreadSafe frame, HFSMData* hfsmData, EntityRef entity, Int32 eventKey = 0)
		{
			hfsmData->Time = *hfsmData->Times.GetPointer(Level);

			return CheckTransitions(frame, Transitions, hfsmData, entity, eventKey);
		}

		private static bool CheckTransitions(FrameThreadSafe frame, HFSMTransition[] transitions, HFSMData* hfsmData, EntityRef entity, int eventKey, int depth = 0)
		{
			// Just to avoid accidental loops
			if (depth == 10)
				return false;

			if (transitions == null)
				return false;

			for (int i = 0; i < transitions.Length; i++)
			{
				var transition = transitions[i];

				if (transition.State == null && transition.TransitionSet == null)
					continue;

				// Only consider evaluating the event if this transition HAS a event as requisite (EventKey != 0)
				if (transition.EventKey != 0 && transition.EventKey != eventKey)
					continue;

				if (transition.Decision != null && transition.Decision.DecideThreadSafe(frame, entity) == false)
					continue;

				if (transition.State != null)
				{
					HFSMManager.ThreadSafe.ChangeState(transition.State, frame, hfsmData, entity, transition.Id);
					return true;
				}
				else if (CheckTransitions(frame, transition.TransitionSet.Transitions, hfsmData, entity, eventKey, depth + 1) == true)
				{
					return true;
				}
			}

			return false;
		}
	}
}
