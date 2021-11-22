using Photon.Deterministic;
using System;
using System.Collections.Generic;

namespace Quantum {
  [AssetObjectConfig(GenerateLinkingScripts = true, GenerateAssetCreateMenu = false, GenerateAssetResetMethod = false)]
  public unsafe partial class HFSMState : AssetObject {
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

    public override void Loaded(IResourceManager resourceManager, Native.Allocator allocator)
    {
      base.Loaded(resourceManager, allocator);

      OnUpdate = new AIAction[OnUpdateLinks == null ? 0 : OnUpdateLinks.Length];
      if (OnUpdateLinks != null) {
        for (Int32 i = 0; i < OnUpdateLinks.Length; i++) {
          OnUpdate[i] = (AIAction)resourceManager.GetAsset(OnUpdateLinks[i].Id);
        }
      }
      OnEnter = new AIAction[OnEnterLinks == null ? 0 : OnEnterLinks.Length];
      if (OnEnterLinks != null) {
        for (Int32 i = 0; i < OnEnterLinks.Length; i++) {
          OnEnter[i] = (AIAction)resourceManager.GetAsset(OnEnterLinks[i].Id);
        }
      }
      OnExit = new AIAction[OnExitLinks == null ? 0 : OnExitLinks.Length];
      if (OnExitLinks != null) {
        for (Int32 i = 0; i < OnExitLinks.Length; i++) {
          OnExit[i] = (AIAction)resourceManager.GetAsset(OnExitLinks[i].Id);
        }
      }

      Children = new HFSMState[ChildrenLinks == null ? 0 : ChildrenLinks.Length];
      if (ChildrenLinks != null) {
        for (Int32 i = 0; i < ChildrenLinks.Length; i++) {
          Children[i] = (HFSMState)resourceManager.GetAsset(ChildrenLinks[i].Id);
        }
      }

      Parent = (HFSMState)resourceManager.GetAsset(ParentLink.Id);
      if (Transitions != null) {
        for (int i = 0; i < Transitions.Length; i++) {
          Transitions[i].Setup(resourceManager);
        }
      }
    }

    internal Boolean UpdateState(Frame f, FP deltaTime, HFSMData* fsm, EntityRef e)
    {
      HFSMState parent = Parent;
      Boolean transition = false;

      if (parent != null)
      {
        transition = parent.UpdateState(f, deltaTime, fsm, e);
      }

      if (transition == true)
        return true;

      *fsm->Times.GetPointer(Level) += deltaTime;

      DoUpdateActions(f, e);
      return CheckStateTransitions(f, fsm, e, 0);
    }

    internal Boolean Event(Frame f, HFSMData* fsm, EntityRef e, Int32 eventInt) {
      HFSMState p = Parent;
      Boolean transition = false;
      if (p != null) {
        transition = p.Event(f, fsm, e, eventInt);
      }

      if (transition) {
        return true;
      }

      return CheckStateTransitions(f, fsm, e, eventInt);
    }

    private void DoUpdateActions(Frame f, EntityRef e) {
      for (int i = 0; i < OnUpdate.Length; i++) {
        OnUpdate[i].Update(f, e);
        int nextAction = OnUpdate[i].NextAction(f, e);
        if (nextAction > i) {
          i = nextAction;
        }
      }
    }
    private void DoEnterActions(Frame f, EntityRef e) {
      for (int i = 0; i < OnEnter.Length; i++) {
        OnEnter[i].Update(f, e);
        int nextAction = OnEnter[i].NextAction(f, e);
        if (nextAction > i) {
          i = nextAction;
        }
      }
    }
    private void DoExitActions(Frame f, EntityRef e) {
      for (int i = 0; i < OnExit.Length; i++)
      {
        OnExit[i].Update(f, e);
        int nextAction = OnExit[i].NextAction(f, e);
        if (nextAction > i) {
          i = nextAction;
        }
      }
    }

    private bool CheckStateTransitions(Frame frame, HFSMData* fsm, EntityRef entity, Int32 eventKey = 0)
    {
      fsm->Time = *fsm->Times.GetPointer(Level);

      return CheckTransitions(frame, Transitions, fsm, entity, eventKey);
    }

    private static bool CheckTransitions(Frame frame, HFSMTransition[] transitions, HFSMData* fsm, EntityRef entity, int eventKey, int depth = 0)
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

        if (transition.Decision != null && transition.Decision.Decide(frame, entity) == false)
          continue;

        if (transition.State != null)
        {
          HFSMManager.ChangeState(transition.State, frame, fsm, entity, transition.Id);
          return true;
        }
        else if (CheckTransitions(frame, transition.TransitionSet.Transitions, fsm, entity, eventKey, depth + 1) == true)
        {
          return true;
        }
      }

      return false;
    }

    internal void EnterState(Frame f, HFSMData* fsm, EntityRef e)
    {
      *fsm->Times.GetPointer(Level) = FP._0;
      
      DoEnterActions(f, e);
      if (Children != null && Children.Length > 0) {
        HFSMState child = Children[0];
        HFSMManager.ChangeState(child, f, fsm, e, "");
      }
    }

    internal void ExitState(HFSMState nextState, Frame f, HFSMData* fsm, EntityRef e)
    {
      if (nextState != null && nextState.IsChildOf(this) == true)
        return;

      DoExitActions(f, e);
      Parent?.ExitState(nextState, f, fsm, e);
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
  }
}
