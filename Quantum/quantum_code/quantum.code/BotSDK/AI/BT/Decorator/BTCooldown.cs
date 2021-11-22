using Photon.Deterministic;
using System;

namespace Quantum
{
  [Serializable]
  public unsafe partial class BTCooldown : BTDecorator
  {
    // How many time should we wait
    public FP CooldownTime;

    // An indexer so we know when the time started counting
    public BTDataIndex StartTimeIndex;

    public override void Init(Frame frame, AIBlackboardComponent* bbComponent, BTAgent* btAgent)
    {
      base.Init(frame, bbComponent, btAgent);

      // We allocate space on the BTAgent so we can store the Start Time
      btAgent->AddFPData(frame, 0);
    }

    protected override BTStatus OnUpdate(BTParams p)
    {
      var result = base.OnUpdate(p);

      // We let the time check, which happens on the DryRun, happen
      // If it results in success, then we store on the BTAgent the time value of the moment that it happened
      if (result == BTStatus.Success)
      {
        var currentTime = p.Frame.DeltaTime * p.Frame.Number;

        var frame = p.Frame;
        var entity = p.Entity;
        p.BtAgent->SetFPData(frame, currentTime, StartTimeIndex.Index);
      }

      return result;
    }

    // We get the Start Time stored on the BTAgent, then we check if the time + cooldown is already over
    // If it is not over, then we return False, blocking the execution of the children nodes
    public override Boolean DryRun(BTParams p)
    {
      var frame = p.Frame;
      var entity = p.Entity;
      FP startTime = p.BtAgent->GetFPData(frame, StartTimeIndex.Index);

      var currentTime = p.Frame.DeltaTime * p.Frame.Number;

      return currentTime >= startTime + CooldownTime;
    }
  }
}
