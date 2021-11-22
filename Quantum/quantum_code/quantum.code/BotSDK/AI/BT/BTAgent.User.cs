using Photon.Deterministic;
using System;

namespace Quantum
{
  public unsafe partial struct BTAgent
  {
    // Used to setup info on the Unity debugger
    public string GetTreeAssetName(Frame f) => f.FindAsset<BTRoot>(Tree.Id).Path;

    public AIConfig GetConfig(Frame f)
    {
      return f.FindAsset<AIConfig>(Config.Id);
    }

    public void Initialize(Frame frame, EntityRef entityRef, BTAgent* btAgent, AssetRefBTNode tree, bool force = false)
    {
      if (this.Tree != default && force == false)
        return;

      // -- Cache the tree
      BTRoot treeAsset = frame.FindAsset<BTRoot>(tree.Id);
      this.Tree = treeAsset;

      // -- Allocate data
      // Success/Fail/Running
      NodesStatus = frame.AllocateList<Byte>(treeAsset.NodesAmount);

      // Next tick in which each service shall be updated
      ServicesNextTicks = frame.AllocateList<Int32>(4);

      // Node data, such as FP for timers, Integers for IDs
      BTDataValues = frame.AllocateList<BTDataValue>(4);

      // The Services contained in the current sub-tree,
      // which should be updated considering its intervals
      ActiveServices = frame.AllocateList<AssetRefBTService>(4);

      // The Dynamic Composites contained in the current sub-tree,
      // which should be re-checked every tick
      DynamicComposites = frame.AllocateList<AssetRefBTComposite>(4);

      // -- Cache the Blackboard (if any)
      AIBlackboardComponent* bbComponent = null;
      if (frame.Has<AIBlackboardComponent>(entityRef))
      {
        bbComponent = frame.Unsafe.GetPointer<AIBlackboardComponent>(entityRef);
      }

      // -- Initialize the tree
      treeAsset.InitializeTree(frame, btAgent, bbComponent);

      // -- Trigger the debugging event (mostly for the Unity side)
      BTManager.OnSetupDebugger?.Invoke(entityRef, treeAsset.Path);
    }

    public void Free(Frame frame)
    {
      Tree = default;
      frame.FreeList<Byte>(NodesStatus);
      frame.FreeList<Int32>(ServicesNextTicks);
      frame.FreeList<BTDataValue>(BTDataValues);
      frame.FreeList<AssetRefBTService>(ActiveServices);
      frame.FreeList<AssetRefBTComposite>(DynamicComposites);
    }

    #region Int and FP Data
    // Getter / Setters of node FP and Int32 data
    public void AddFPData(Frame f, FP fpValue)
    {
      var nodesDataList = f.ResolveList<BTDataValue>(BTDataValues);
      BTDataValue newDataValue = new BTDataValue();
      *newDataValue.FPValue = fpValue;
      nodesDataList.Add(newDataValue);
    }

    public void AddIntData(Frame f, Int32 intValue)
    {
      var nodesDataList = f.ResolveList<BTDataValue>(BTDataValues);
      BTDataValue newDataValue = new BTDataValue();
      *newDataValue.IntValue = intValue;
      nodesDataList.Add(newDataValue);
    }

    public void SetFPData(Frame f, FP value, Int32 index)
    {
      var nodesDataList = f.ResolveList<BTDataValue>(BTDataValues);
      *nodesDataList.GetPointer(index)->FPValue = value;
    }

    public void SetIntData(Frame f, Int32 value, Int32 index)
    {
      var nodesDataList = f.ResolveList<BTDataValue>(BTDataValues);
      *nodesDataList.GetPointer(index)->IntValue = value;
    }

    public FP GetFPData(Frame f, Int32 index)
    {
      var nodesDataList = f.ResolveList<BTDataValue>(BTDataValues);
      return *nodesDataList.GetPointer(index)->FPValue;
    }

    public Int32 GetIntData(Frame f, Int32 index)
    {
      var nodesDataList = f.ResolveList<BTDataValue>(BTDataValues);
      return *nodesDataList.GetPointer(index)->IntValue;
    }
    #endregion

    public void Update(Frame frame, BTAgent* btAgent, AIBlackboardComponent* blackboard, EntityRef entity)
    {
      var p = new BTParams
      {
        Frame = frame,
        BtAgent = btAgent,
        Blackboard = blackboard,
        Entity = entity
      };

      if (btAgent->Current == null)
      {
        btAgent->Current = btAgent->Tree;
      }

      RunDynamicComposites(p);

      BTNode node = frame.FindAsset<BTNode>(btAgent->Current.Id);
      UpdateSubtree(frame, node, p);
    }

    // We run the dynamic composites contained on the current sub-tree (if any)
    // If any of them result in "False", we abort the current sub-tree
    // and take the execution back to the topmost decorator so the agent can choose another path
    private void RunDynamicComposites(BTParams p)
    {
      var frame = p.Frame;
      var dynamicComposites = frame.ResolveList<AssetRefBTComposite>(DynamicComposites);

      for (int i = 0; i < dynamicComposites.Count; i++)
      {
        var compositeRef = dynamicComposites.GetPointer(i);
        var composite = frame.FindAsset<BTComposite>(compositeRef->Id);
        var dynamicResult = composite.OnDynamicRun(p);

        if (dynamicResult == false)
        {
          p.BtAgent->Current = composite.TopmostDecorator;
          dynamicComposites.Remove(*compositeRef);
          composite.OnReset(p);
          return;
        }
      }
    }

    private void UpdateSubtree(Frame frame, BTNode node, BTParams p)
    {
      // Start updating the tree from the Current agent's node
      var result = node.RunUpdate(p);

      // If the current node completes, go up in the tree until we hit a composite
      // Run that one. On success or fail continue going up.
      while (result != BTStatus.Running && node.Parent != null)
      {
        // As we are traversing the tree up, we allow nodes to remove any
        // data that is only needed locally
        node.OnExit(p);

        node = node.Parent;
        if (node.NodeType == BTNodeType.Composite)
        {
          ((BTComposite)node).ChildCompletedRunning(p, result);
          result = node.RunUpdate(p);
        }
      }

      BTService.TickServices(p);

      if (result != BTStatus.Running)
      {
        BTNode tree = frame.FindAsset<BTNode>(p.BtAgent->Tree.Id);
        tree.OnReset(p);
        p.BtAgent->Current = p.BtAgent->Tree;
        BTManager.OnTreeCompleted?.Invoke(p.Entity);
        //Log.Info("Behaviour Tree completed with result '{0}'", result);
      }
    }

    public unsafe void AbortSelf(Frame frame, BTNode node, BTParams p, bool updateSubtree = true)
    {
      // Go up and find the next interesting node (composite or root)
      var cancelNode = node;
      while (
        node.NodeType != BTNodeType.Composite &&
        node.NodeType != BTNodeType.Root)
      {
        cancelNode = node;
        node = node.Parent;
      }

      // Cancel the current branch (everything below the interesting node)
      cancelNode.OnReset(p);

      //Log.Info("AbortSelf success: old node " + cancelNode.Name + " new node " + node.Name);

      // Run the tree from here
      if (updateSubtree)
      {
        UpdateSubtree(frame, node, p);
      }
    }

    public unsafe void AbortLowerPriority(Frame frame, BTNode node, BTParams p)
    {
      // Go up and find the next interesting node (composite or root)
      var topNode = node;
      while (
        topNode.NodeType != BTNodeType.Composite &&
        topNode.NodeType != BTNodeType.Root)
      {
        topNode = topNode.Parent;
      }

      if (topNode.NodeType == BTNodeType.Root)
      {
        // Reset and re-run the root node
        topNode.OnReset(p);
        UpdateSubtree(frame, node, p);
        return;
      }

      // Check if there is a running branch to the right (branch with lower priority)
      var compositeNode = (topNode as BTComposite);
      var currentChildIndex = compositeNode.GetCurrentChild(p.Frame, p.BtAgent);
      if (currentChildIndex < node.ParentIndex)
      {
        return;
      }

      // Decend again and check if all decorators succeed
      var testNode = compositeNode.ChildInstances[node.ParentIndex];
      while (testNode.NodeType == BTNodeType.Decorator)
      {
        if (testNode.DryRun(p) == false)
        {
          return;
        }
        testNode = (testNode as BTDecorator).ChildInstance;
      }

      // Cancel the other branch
      compositeNode.ChildInstances[currentChildIndex].OnReset(p);

      // Set composite index to us
      compositeNode.SetCurrentChild(p.Frame, node.ParentIndex, p.BtAgent);

      //Log.Info("AbortLowerPriority success: old node " + compositeNode.ChildInstances[currentChildIndex].Name + " new node " + compositeNode.ChildInstances[node.ParentIndex].Name);

      // Run the tree from here
      UpdateSubtree(frame, compositeNode, p);
    }

    // Used to react to blackboard changes which are observed by Decorators
    // This is triggered by the Blackboard Entry itself, which has a list of Decorators that observes it
    public unsafe void OnDecoratorReaction(Frame f, EntityRef entity, AIBlackboardComponent* blackboard, BTNode node, BTAbort abort)
    {
      var btAgent = f.Unsafe.GetPointer<BTAgent>(entity);

      var status = node.GetStatus(f, btAgent);

      if (status == BTStatus.Running && abort.IsSelf())
      {
        var p = new BTParams { BtAgent = btAgent, Frame = f, Entity = entity, Blackboard = blackboard };

        // Check condition again
        if (node.DryRun(p) == true)
        {
          return;
        }

        AbortSelf(f, node, p);
      }
      else if (status != BTStatus.Running && abort.IsLowerPriority())
      {
        var p = new BTParams { BtAgent = btAgent, Frame = f, Entity = entity, Blackboard = blackboard };

        AbortLowerPriority(f, node, p);
      }
    }
  }
}