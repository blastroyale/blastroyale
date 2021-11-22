using Photon.Deterministic;
using System;

namespace Quantum
{
  [Serializable]
  public unsafe partial class BTLoop : BTDecorator
  {
    public Int32 LoopIterations;
    public Boolean LoopForever;
    public FP LoopTimeout = -FP._1;

    public BTDataIndex StartTimeIndex;
    public BTDataIndex IterationCountIndex;

    public override void Init(Frame frame, AIBlackboardComponent* bbComponent, BTAgent* btAgent)
    {
      base.Init(frame, bbComponent, btAgent);

      btAgent->AddFPData(frame, 0);
      btAgent->AddIntData(frame, 0);
    }

    public override void OnEnter(BTParams p)
    {
      base.OnEnter(p);

      var frame = p.Frame;
      var currentTime = frame.DeltaTime * frame.Number;

      p.BtAgent->SetFPData(frame, currentTime, StartTimeIndex.Index);
      p.BtAgent->SetIntData(frame, 0, IterationCountIndex.Index);
    }

    protected override BTStatus OnUpdate(BTParams p)
    {
      var frame = p.Frame;

      int iteration = p.BtAgent->GetIntData(frame, IterationCountIndex.Index) + 1;
      p.BtAgent->SetIntData(frame, iteration, IterationCountIndex.Index);

      if (DryRun(p) == false)
      {
        return BTStatus.Success;
      }

      var childResult = BTStatus.Failure;
      if (_childInstance != null)
      {
        _childInstance.SetStatus(p.Frame, BTStatus.Inactive, p.BtAgent);
        childResult = _childInstance.RunUpdate(p);
      }

      return childResult;
    }

    public override Boolean DryRun(BTParams p)
    {
      if (LoopForever && LoopTimeout < FP._0)
      {
        return true;
      }
      else if (LoopForever)
      {
        var frame = p.Frame;
        FP startTime = p.BtAgent->GetFPData(frame, StartTimeIndex.Index);

        var currentTime = frame.DeltaTime * frame.Number;
        if (currentTime < startTime + LoopTimeout)
        {
          return true;
        }
      }
      else
      {
        var frame = p.Frame;
        int iteration = p.BtAgent->GetIntData(frame, IterationCountIndex.Index);
        if (iteration <= LoopIterations)
        {
          return true;
        }
      }

      return false;
    }
  }
}
