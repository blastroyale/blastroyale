using Photon.Deterministic;

namespace Quantum {
  public abstract unsafe partial class BTDecorator : BTNode {
    [BotSDKHidden] public AssetRefBTNode Child;
    protected BTNode _childInstance;
    public BTAbort AbortType;

    public BTNode ChildInstance {
      get {
        return _childInstance;
      }
    }

    public override BTNodeType NodeType {
      get {
        return BTNodeType.Decorator;
      }
    }

    public override void OnReset(BTParams p) {
      base.OnReset(p);

      OnExit(p);

      if (_childInstance != null)
        _childInstance.OnReset(p);
    }

    protected override BTStatus OnUpdate(BTParams p) {

      if (DryRun(p) == true) {
        if (_childInstance != null)
          return _childInstance.RunUpdate(p);

        return BTStatus.Success;
      }

      return BTStatus.Failure;
    }

    public override bool OnDynamicRun(BTParams p)
    {
      var result = DryRun(p);
      if(result == false)
      {
        return false;
      }
      else if(ChildInstance.NodeType != BTNodeType.Decorator)
      {
        return true;
      }
      else
      {
        return ChildInstance.OnDynamicRun(p);
      }
    }

    public override void Loaded(IResourceManager resourceManager, Native.Allocator allocator)
    {
      base.Loaded(resourceManager, allocator);

      // Cache the child
      _childInstance = (BTNode)resourceManager.GetAsset(Child.Id);
      _childInstance.Parent = this;
    }
  }
}