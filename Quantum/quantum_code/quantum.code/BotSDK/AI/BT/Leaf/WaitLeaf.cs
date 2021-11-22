using Photon.Deterministic;
using System;

namespace Quantum
{
  [Serializable]
  public unsafe partial class WaitLeaf : BTLeaf
  {
    // How many time shall be waited
    public FP Duration;
    
    // Indexer for us to store the End Time value on the BTAgent itself
    public BTDataIndex EndTimeIndex;

    public override void Init(Frame frame, AIBlackboardComponent* bbComponent, BTAgent* btAgent)
    {
      base.Init(frame, bbComponent, btAgent);

      // We allocate space for the End Time on the Agent so we can change it in runtime
      btAgent->AddFPData(frame, 0);
    }

    public override void OnEnter(BTParams p)
    {
      base.OnEnter(p);

      // We get the current time to define what will be the finishing time
      var currentTime = p.Frame.DeltaTime * p.Frame.Number;
      var endTime = currentTime + Duration;

      // With the final time value defined, we just set it on the BTAgent, on the space that was pre-allocated
      // on the Init method
      p.BtAgent->SetFPData(p.Frame, endTime, EndTimeIndex.Index);
    }

    protected override BTStatus OnUpdate(BTParams p)
    {
      // We get the current time...
      var currentTime = p.Frame.DeltaTime * p.Frame.Number;

      // We get the End Time from the BTAgent. The time is only updated during OnEnter
      var endTime = p.BtAgent->GetFPData(p.Frame, EndTimeIndex.Index);

      // If waiting time isn't over yet, then we need more frames executing this Leaf
      // So we say that we're still Running
      if (currentTime < endTime)
      {
        return BTStatus.Running;
      }

      // If the waiting time is over, then we succeeded on waiting that amount of time
      // Then we return Success
      return BTStatus.Success;
    }
  }
}
