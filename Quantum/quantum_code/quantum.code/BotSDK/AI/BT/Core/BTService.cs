using Photon.Deterministic;
using System;

namespace Quantum
{
  public unsafe abstract partial class BTService
  {
    public FP IntervalInSec;

    [BotSDKHidden] public Int32 Id;

    public virtual void Init(Frame frame, BTAgent* btAgent, AIBlackboardComponent* blackboard)
    {
      var nextTicksList = frame.ResolveList<Int32>(btAgent->ServicesNextTicks);
      nextTicksList.Add(0);
    }

    public void SetNextTick(Frame frame, BTAgent* btAgent)
    {
      var ticks = IntervalInSec * frame.SessionConfig.UpdateFPS;
      var nextTicksList = frame.ResolveList<Int32>(btAgent->ServicesNextTicks);
      nextTicksList[Id] = frame.Number + FPMath.RoundToInt(ticks);
    }

    public Int32 GetNextTick(Frame frame, BTAgent* btAgent)
    {
      var nextTicks = frame.ResolveList(btAgent->ServicesNextTicks);
      return nextTicks[Id];
    }

    public virtual void RunUpdate(BTParams p)
    {
      var nextTick = GetNextTick(p.Frame, p.BtAgent);
      if (p.Frame.Number >= nextTick)
      {
        OnUpdate(p);
        SetNextTick(p.Frame, p.BtAgent);
      }
    }

    public virtual void OnEnter(BTParams p)
    {
      SetNextTick(p.Frame, p.BtAgent);
    }

    /// <summary>
    /// Called whenever the Service is part of the current subtree
    /// and its waiting time is already over
    /// </summary>
    protected abstract void OnUpdate(BTParams p);

    public static void TickServices(BTParams p)
    {
      var activeServicesList = p.Frame.ResolveList<AssetRefBTService>(p.BtAgent->ActiveServices);

      for (int i = 0; i < activeServicesList.Count; i++)
      {
        var service = p.Frame.FindAsset<BTService>(activeServicesList[i].Id);
        try
        {
          service.RunUpdate(p);
        }
        catch (Exception e)
        {
          Log.Error("Exception in Behaviour Tree service '{0}' ({1}) - setting node status to Failure", service.GetType().ToString(), service.Guid);
          Log.Exception(e);
        }
      }
    }
  }
}
