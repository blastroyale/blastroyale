using Photon.Deterministic;
using System;

namespace Quantum
{
  public unsafe abstract partial class BTComposite : BTNode
  {
    public AssetRefBTNode[] Children;
    public AssetRefBTService[] Services;
    [BotSDKHidden] public AssetRefBTNode TopmostDecorator;

    public BTDataIndex CurrentChildIndex;

    protected BTNode[] _childInstances;
    protected BTService[] _serviceInstances;
    protected BTNode _topmostDecoratorInstance;

    public bool IsDynamic;

    public BTNode[] ChildInstances
    {
      get
      {
        return _childInstances;
      }
    }

    public BTService[] ServiceInstances
    {
      get
      {
        return _serviceInstances;
      }
    }

    public override BTNodeType NodeType
    {
      get
      {
        return BTNodeType.Composite;
      }
    }

    internal Int32 GetCurrentChild(Frame frame, BTAgent* btAgent)
    {
      Byte currentChild = (Byte)btAgent->GetIntData(frame, CurrentChildIndex.Index);
      return currentChild;
    }

    internal void SetCurrentChild(Frame frame, Int32 currentIndex, BTAgent* btAgent)
    {
      btAgent->SetIntData(frame, currentIndex, CurrentChildIndex.Index);
    }

    /// <summary>
    /// When a Composite node is Updated, it only increase the current child updated
    /// when the child results in either FAIL/SUCCESS. So we need this callback
    /// to be used when the child was RUNNING and then had some result, to properly increase the current 
    /// child ID
    /// </summary>
    /// <param name="p"></param>
    /// <param name="childResult"></param>
    internal virtual void ChildCompletedRunning(BTParams p, BTStatus childResult)
    {
    }

    public override void Init(Frame frame, AIBlackboardComponent* bbComponent, BTAgent* btAgent)
    {
      base.Init(frame, bbComponent, btAgent);

      btAgent->AddIntData(frame, 0);

      for (Int32 i = 0; i < Services.Length; i++)
      {
        BTService service = frame.FindAsset<BTService>(Services[i].Id);
        service.Init(frame, btAgent, bbComponent);
      }
    }

    public override void OnEnter(BTParams p)
    {
      BTManager.OnNodeEnter?.Invoke(p.Entity, Guid.Value);
      SetCurrentChild(p.Frame, 0, p.BtAgent);
    }

    public override void OnEnterRunning(BTParams p)
    {
      var activeServicesList = p.Frame.ResolveList<AssetRefBTService>(p.BtAgent->ActiveServices);

      for (Int32 i = 0; i < _serviceInstances.Length; i++)
      {
        _serviceInstances[i].OnEnter(p);

        activeServicesList.Add(Services[i]);
      }

      if (IsDynamic == true)
      {
        var dynamicComposites = p.Frame.ResolveList<AssetRefBTComposite>(p.BtAgent->DynamicComposites);
        dynamicComposites.Add(this);
      }
    }

    public override void OnReset(BTParams p)
    {
      base.OnReset(p);

      OnExit(p);

      for (Int32 i = 0; i < _childInstances.Length; i++)
        _childInstances[i].OnReset(p);
    }

    public override void OnExit(BTParams p)
    {
      base.OnExit(p);

      BTManager.OnNodeExit?.Invoke(p.Entity, Guid.Value);

      var activeServicesList = p.Frame.ResolveList<AssetRefBTService>(p.BtAgent->ActiveServices);
      for (Int32 i = 0; i < _serviceInstances.Length; i++)
      {
        activeServicesList.Remove(Services[i]);
      }

      if (IsDynamic == true)
      {
        var dynamicComposites = p.Frame.ResolveList<AssetRefBTComposite>(p.BtAgent->DynamicComposites);
        dynamicComposites.Remove(this);
      }
    }

    public override bool OnDynamicRun(BTParams p)
    {
      if (_topmostDecoratorInstance != null)
      {
        return _topmostDecoratorInstance.OnDynamicRun(p);
      }

      return true;
    }

    public override void Loaded(IResourceManager resourceManager, Native.Allocator allocator)
    {
      base.Loaded(resourceManager, allocator);

      // Cache the child assets links
      _childInstances = new BTNode[Children.Length];
      for (Int32 i = 0; i < Children.Length; i++)
      {
        _childInstances[i] = (BTNode)resourceManager.GetAsset(Children[i].Id);
        _childInstances[i].Parent = this;
        _childInstances[i].ParentIndex = i;
      }

      // Cache the service assets links
      _serviceInstances = new BTService[Services.Length];
      for (Int32 i = 0; i < Services.Length; i++)
      {
        _serviceInstances[i] = (BTService)resourceManager.GetAsset(Services[i].Id);
      }

      if (TopmostDecorator != null)
      {
        _topmostDecoratorInstance = (BTDecorator)resourceManager.GetAsset(TopmostDecorator.Id);
      }
    }
  }
}