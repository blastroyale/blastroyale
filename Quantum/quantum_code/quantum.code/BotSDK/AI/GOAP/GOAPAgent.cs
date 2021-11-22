using System;
using Photon.Deterministic;

namespace Quantum
{
  public unsafe partial struct GOAPAgent
  {
    public void SetState(GOAPWorldState newState)
    {
      var state = (Int64)newState;
      if (!state.Equals(CurrentState))
      {
        CurrentTaskIndex = -1;
        CurrentState = state;
      }
    }

    public void SetGoal(GOAPState goal)
    {
      if (!goal.Equals(Goal))
      {
        CurrentTaskIndex = -1;
        Goal = goal;
      }
    }

    public AIConfig GetConfig(Frame f)
    {
      return f.FindAsset<AIConfig>(Config.Id);
    }
  }
}
