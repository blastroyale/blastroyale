using Photon.Deterministic;
using System;

namespace Quantum {
  public unsafe abstract partial class BTLeaf : BTNode {
    public AssetRefBTService[] Services;
    protected BTService[] _serviceInstances;
    public BTService[] ServiceInstances
    {
      get
      {
        return _serviceInstances;
      }
    }

    public override BTNodeType NodeType {
      get {
        return BTNodeType.Leaf;
      }
    }

    public override unsafe void Init(Frame frame, AIBlackboardComponent* bbComponent, BTAgent* btAgent)
    {
      base.Init(frame, bbComponent, btAgent);

      for (int i = 0; i < Services.Length; i++)
      {
        BTService service = frame.FindAsset<BTService>(Services[i].Id);
        service.Init(frame, btAgent, bbComponent);
      }
    }

    public override void OnEnterRunning(BTParams p)
    {
      var activeServicesList = p.Frame.ResolveList<AssetRefBTService>(p.BtAgent->ActiveServices);
      for (int i = 0; i < _serviceInstances.Length; i++)
      {
        _serviceInstances[i].OnEnter(p);
        activeServicesList.Add(Services[i]);
      }
    }

    public override void OnEnter(BTParams p)
    {
      base.OnEnter(p);
      BTManager.OnNodeEnter?.Invoke(p.Entity, Guid.Value);
    }

    public override void OnExit(BTParams p)
    {
      var activeServicesList = p.Frame.ResolveList<AssetRefBTService>(p.BtAgent->ActiveServices);
      for (Int32 i = 0; i < _serviceInstances.Length; i++)
      {
        activeServicesList.Remove(Services[i]);
      }

      BTManager.OnNodeExit?.Invoke(p.Entity, Guid.Value);
    }

    public override void OnReset(BTParams p)
    {
      base.OnReset(p);
      OnExit(p);
    }

    public override void Loaded(IResourceManager resourceManager, Native.Allocator allocator)
    {
      base.Loaded(resourceManager, allocator);

      // Cache the service assets links
      _serviceInstances = new BTService[Services.Length];
      for (int i = 0; i < Services.Length; i++)
      {
        _serviceInstances[i] = (BTService)resourceManager.GetAsset(Services[i].Id);
      }
    }
  }
}