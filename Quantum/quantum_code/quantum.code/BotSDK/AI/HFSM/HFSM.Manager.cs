using Photon.Deterministic;
using System;

namespace Quantum {
  public static unsafe class HFSMManager
  {
    public static Action<EntityRef, long, string> StateChanged;

    /// <summary>
    /// Initializes the HFSM, making the current state to be equals the initial state
    /// </summary>
    public static unsafe void Init(Frame f, EntityRef e, HFSMRoot root)
    {
      if (f.Unsafe.TryGetPointer(e, out HFSMAgent* hfsmAgent))
      {
        HFSMData* hfsmData = &hfsmAgent->Data;
        Init(f, hfsmData, e, root);
      }
      else
      {
        Log.Error("[Bot SDK] Tried to initialize an entity which has no HfsmAgent component");
      }
    }

    /// <summary>
    /// Initializes the HFSM, making the current state to be equals the initial state
    /// </summary>
    public static unsafe void Init(Frame f, HFSMData* hfsm, EntityRef e, HFSMRoot root) {
      hfsm->Root = root;
      if (hfsm->Root.Equals(default) == false)
      {
        HFSMState initialState = f.FindAsset<HFSMState>(root.InitialState.Id);
        ChangeState(initialState, f, hfsm, e, "");
      }
    }


    /// <summary>
    /// Update the state of the HFSM.
    /// </summary>
    /// <param name="deltaTime">Usually the current deltaTime so the HFSM accumulates the time stood on the current state</param>
    public static void Update(Frame f, FP deltaTime, EntityRef e)
    {
      if(f.Unsafe.TryGetPointer(e, out HFSMAgent* hfsmAgent)){ 
        HFSMData* hfsmData = &hfsmAgent->Data;
        Update(f, deltaTime, hfsmData, e);
      }
      else
      {
        Log.Error("[Bot SDK] Tried to update an entity which has no HFSMAgent component");
      }
    }

    /// <summary>
    /// Update the state of the HFSM.
    /// </summary>
    /// <param name="deltaTime">Usually the current deltaTime so the HFSM accumulates the time stood on the current state</param>
    public static void Update(Frame f, FP deltaTime, HFSMData* hfsmData, EntityRef e) {
      HFSMState curentState = f.FindAsset<HFSMState>(hfsmData->CurrentState.Id);
      curentState.UpdateState(f, deltaTime, hfsmData, e);
    }


    /// <summary>
    /// Triggers an event if the target HFSM listens to it
    /// </summary>
    public static unsafe void TriggerEvent(Frame f, EntityRef e, string eventName)
    {
      if (f.Unsafe.TryGetPointer(e, out HFSMAgent* hfsmAgent))
      {
        HFSMData* hfsmData = &hfsmAgent->Data;
        TriggerEvent(f, hfsmData, e, eventName);
      }
      else
      {
        Log.Error("[Bot SDK] Tried to trigger an event to an entity which has no HFSMAgent component");
      }
    }

    /// <summary>
    /// Triggers an event if the target HFSM listens to it
    /// </summary>
    public static unsafe void TriggerEvent(Frame f, HFSMData* hfsmData, EntityRef e, string eventName) {
      Int32 eventInt = 0;

      HFSMRoot hfsmRoot = f.FindAsset<HFSMRoot>(hfsmData->Root.Id);
      if(hfsmRoot.RegisteredEvents.TryGetValue(eventName, out eventInt)) {
        if (hfsmData->CurrentState.Equals(default) == false)
        {
          HFSMState currentState = f.FindAsset<HFSMState>(hfsmData->CurrentState.Id);
          currentState.Event(f, hfsmData, e, eventInt);
        }
      }
    }

    /// <summary>
    /// Triggers an event if the target HFSM listens to it
    /// </summary>
    public static unsafe void TriggerEventNumber(Frame f, HFSMData* hfsmData, EntityRef e, Int32 eventInt)
    {
      if (hfsmData->CurrentState.Equals(default) == false)
      {
        HFSMState currentState = f.FindAsset<HFSMState>(hfsmData->CurrentState.Id);
        currentState.Event(f, hfsmData, e, eventInt);
      }
    }

    /// <summary>
    /// Executes the On Exit actions for the current state, then changes the current state
    /// </summary>
    internal static void ChangeState(HFSMState nextState, Frame f, HFSMData* hfsmData, EntityRef e, string transitionId)
    {
      Assert.Check(nextState != null, "Tried to change HFSM to a null state");

      HFSMState currentState = f.FindAsset<HFSMState>(hfsmData->CurrentState.Id);
      currentState?.ExitState(nextState, f, hfsmData, e);
      
      hfsmData->CurrentState = nextState;

      if (f.IsVerified == true && e != default(EntityRef))
      {
        StateChanged?.Invoke(e, hfsmData->CurrentState.Id.Value, transitionId);
      }

      nextState.EnterState(f, hfsmData, e);
    }
  }
}
