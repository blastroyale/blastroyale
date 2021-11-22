using System;

namespace Quantum {

  public unsafe abstract partial class BTNode {

    [BotSDKHidden] public String Label;
    [BotSDKHidden] public Int32 Id;

    [NonSerialized] internal BTNode Parent;
    [NonSerialized] internal Int32 ParentIndex;

    public abstract BTNodeType NodeType { get; }

    /// <summary>
    /// Called once, for every Node, when the BT is being initialized
    /// </summary>
    public virtual void Init(Frame frame, AIBlackboardComponent* bbComponent, BTAgent* btAgent)
    {
      var statusList = frame.ResolveList(btAgent->NodesStatus);
      statusList.Add(0);
    }

    // -- STATUS --
    public BTStatus GetStatus(Frame frame, BTAgent* bt)
    {
      var nodesAndStatus = frame.ResolveList(bt->NodesStatus);
      return (BTStatus)nodesAndStatus[Id];
    }

    public void SetStatus(Frame frame, BTStatus status, BTAgent* bt)
    {
      var nodesAndStatus = frame.ResolveList(bt->NodesStatus);
      nodesAndStatus[Id] = (Byte)status;
    }

    /// <summary>
    /// Called whenever the BT execution includes this node as part of the current context
    /// </summary>
    /// <param name="p"></param>
    public virtual void OnEnter(BTParams p) { }

    public virtual void OnEnterRunning(BTParams p) { }

    /// <summary>
    /// Called when traversing the tree upwards and the node is already finished with its job.
    /// Used by Composites and Leafs to remove their Services from the list of active services
    /// as it is not anymore part of the current subtree.
    /// Dynamic Composites also remove themselves
    /// </summary>
    /// <param name="p"></param>
    public virtual void OnExit(BTParams p) { }

    /// <summary>
    /// Called when getting out of a sub-branch and this node is being discarded
    /// </summary>
    /// <param name="p"></param>
    public unsafe virtual void OnReset(BTParams p) {
      SetStatus(p.Frame, BTStatus.Inactive, p.BtAgent);
    }

    public BTStatus RunUpdate(BTParams p) {
      var oldStatus = GetStatus(p.Frame, p.BtAgent);
      if(oldStatus == BTStatus.Success || oldStatus == BTStatus.Failure)
      {
        return oldStatus;
      }

      if (oldStatus == BTStatus.Inactive) {
        OnEnter(p);
      }

      var newStatus = BTStatus.Failure;
      try
      {
        newStatus = OnUpdate(p);
        
        // Used for debugging reasons
        if(newStatus == BTStatus.Success)
        {
          BTManager.OnNodeSuccess?.Invoke(p.Entity, Guid.Value);
          BTManager.OnNodeExit?.Invoke(p.Entity, Guid.Value);
        }

        if (newStatus == BTStatus.Failure)
        {
          BTManager.OnNodeFailure?.Invoke(p.Entity, Guid.Value);
          BTManager.OnNodeExit?.Invoke(p.Entity, Guid.Value);
        }
      }
      catch (Exception e)
      {
        Log.Error("Exception in Behaviour Tree node '{0}' ({1}) - setting node status to Failure", Label, Guid);
        Log.Exception(e);
      }

      SetStatus(p.Frame, newStatus, p.BtAgent);

      if ((newStatus == BTStatus.Running || newStatus == BTStatus.Success) && 
          (oldStatus == BTStatus.Failure || oldStatus == BTStatus.Inactive)) {
        OnEnterRunning(p);
      }

      if (newStatus == BTStatus.Running && NodeType == BTNodeType.Leaf) {
        // If we are a leaf, we can store the current node
        // We know that there is only one leaf node running at any time, no parallel branches possible
        // The Run() method also return a tuple <BTStatus, BTNode(CurrentNode)>
        p.BtAgent->Current = this;
      }

      return newStatus;
    }

    /// <summary>
    /// Used by Decorators to evaluate if a condition succeeds or not.
    /// Upon success, allow the flow to continue.
    /// Upon failure, blocks the execution so another path is taken
    /// </summary>
    /// <param name="p"></param>
    /// <returns></returns>
    public virtual Boolean DryRun(BTParams p) {
      return false;
    }

    public virtual Boolean OnDynamicRun(BTParams p)
    {
      return true;
    }

    /// <summary>
    /// Called every tick while this Node is part of the current sub-tree.
    /// Returning "Success/Failure" will make the tree continue its execution.
    /// Returning "Running" will store this Node as the Current Node and re-execute it on the next frame
    /// unless something else interrputs
    /// </summary>
    /// <param name="p"></param>
    /// <returns></returns>
    protected abstract BTStatus OnUpdate(BTParams p);
  }
}