using System;

namespace Quantum
{

	public unsafe abstract partial class BTNode
	{
		// ========== PUBLIC MEMBERS ==================================================================================

		[BotSDKHidden] public String Label;
		[BotSDKHidden] public Int32 Id;

		// ========== INTERNAL MEMBERS ================================================================================

		[NonSerialized] internal BTNode Parent;
		[NonSerialized] internal Int32 ParentIndex;

		// ========== BTNode INTERFACE ================================================================================

		public abstract BTNodeType NodeType { get; }

		/// <summary>
		/// Called once, for every Node, when the BT is being initialized
		/// </summary>
		public virtual void Init(FrameThreadSafe frame, AIBlackboardComponent* blackboard, BTAgent* agent)
		{
			var statusList = frame.ResolveList(agent->NodesStatus);
			statusList.Add(0);
		}

		/// <summary>
		/// Called once, for every Node, when the BT is being initialized
		/// </summary>
		public virtual void Init(Frame frame, AIBlackboardComponent* blackboard, BTAgent* agent)
		{
			Init((FrameThreadSafe)frame, blackboard, agent);
		}

		/// <summary>
		/// Called whenever the BT execution includes this node as part of the current context
		/// </summary>
		/// <param name="btParams"></param>
		public virtual void OnEnter(BTParams btParams) { }

		public virtual void OnEnterRunning(BTParams btParams) { }

		/// <summary>
		/// Called when traversing the tree upwards and the node is already finished with its job.
		/// Used by Composites and Leafs to remove their Services from the list of active services
		/// as it is not anymore part of the current subtree.
		/// Dynamic Composites also remove themselves
		/// </summary>
		/// <param name="btParams"></param>
		public virtual void OnExit(BTParams btParams) { }

		public virtual void OnAbort(BTParams btParams)
		{
		}

		/// <summary>
		/// Called when getting out of a sub-branch and this node is being discarded
		/// </summary>
		/// <param name="btParams"></param>
		public unsafe virtual void OnReset(BTParams btParams)
		{
			SetStatus(btParams.FrameThreadSafe, BTStatus.Inactive, btParams.Agent);
		}

		/// <summary>
		/// Used by Decorators to evaluate if a condition succeeds or not.
		/// Upon success, allow the flow to continue.
		/// Upon failure, blocks the execution so another path is taken
		/// </summary>
		/// <param name="btParams"></param>
		/// <returns></returns>
		public virtual Boolean DryRun(BTParams btParams)
		{
			return false;
		}

		public virtual Boolean OnDynamicRun(BTParams btParams)
		{
			return true;
		}

		/// <summary>
		/// Called every tick while this Node is part of the current sub-tree.
		/// Returning "Success/Failure" will make the tree continue its execution.
		/// Returning "Running" will store this Node as the Current Node and re-execute it on the next frame
		/// unless something else interrputs
		/// </summary>
		/// <param name="btParams"></param>
		/// <returns></returns>
		protected abstract BTStatus OnUpdate(BTParams btParams);

		// ========== PUBLIC METHODS ==================================================================================

		// -- STATUS --
		public BTStatus GetStatus(Frame frame, BTAgent* agent)
		{
			return GetStatus((FrameThreadSafe)frame, agent);
		}

		public void SetStatus(Frame frame, BTStatus status, BTAgent* agent)
		{
			SetStatus((FrameThreadSafe)frame, status, agent);
		}

		public BTStatus GetStatus(FrameThreadSafe frame, BTAgent* agent)
		{
			var nodesAndStatus = frame.ResolveList(agent->NodesStatus);
			return (BTStatus)nodesAndStatus[Id];
		}

		public void SetStatus(FrameThreadSafe frame, BTStatus status, BTAgent* agent)
		{
			var nodesAndStatus = frame.ResolveList(agent->NodesStatus);
			nodesAndStatus[Id] = (Byte)status;
		}

		public void EvaluateAbortNode(BTParams btParams)
		{
			if (btParams.Agent->AbortNodeId == Id)
			{
				btParams.Agent->AbortNodeId = 0;
			}
		}

		public BTStatus RunUpdate(BTParams btParams, bool continuingAbort = false)
		{
			var oldStatus = GetStatus(btParams.FrameThreadSafe, btParams.Agent);

			if (oldStatus == BTStatus.Success || oldStatus == BTStatus.Failure)
			{
				return oldStatus;
			}

			if (oldStatus == BTStatus.Abort)
			{
				if (btParams.Agent->IsAborting == true)
				{
					EvaluateAbortNode(btParams);
				}
				return oldStatus;
			}

			// If this node was inactive, this means that we're entering on it for the first time, so we call OnEnter
			// An exception from this rule is when we chose this node to continue an abort process. In that case,
			// we already executed OnEnter before, so we don't repeat it
			if (oldStatus == BTStatus.Inactive && continuingAbort == false)
			{
				OnEnter(btParams);
			}

			var newStatus = BTStatus.Failure;
			try
			{
				newStatus = OnUpdate(btParams);

				if (btParams.Agent->IsAborting)
				{
					newStatus = BTStatus.Abort;
				}

				// Used for debugging purposes
				if (newStatus == BTStatus.Success)
				{
					BTManager.OnNodeSuccess?.Invoke(btParams.Entity, Guid.Value);
					BTManager.OnNodeExit?.Invoke(btParams.Entity, Guid.Value);
				}

				if (newStatus == BTStatus.Failure)
				{
					BTManager.OnNodeFailure?.Invoke(btParams.Entity, Guid.Value);
					BTManager.OnNodeExit?.Invoke(btParams.Entity, Guid.Value);
				}
			}
			catch (Exception e)
			{
				Log.Error("Exception in Behaviour Tree node '{0}' ({1}) - setting node status to Failure", Label, Guid);
				Log.Exception(e);
			}

			SetStatus(btParams.FrameThreadSafe, newStatus, btParams.Agent);

			if ((newStatus == BTStatus.Running || newStatus == BTStatus.Success) &&
					(oldStatus == BTStatus.Failure || oldStatus == BTStatus.Inactive))
			{
				OnEnterRunning(btParams);
			}

			if (newStatus == BTStatus.Running && NodeType == BTNodeType.Leaf)
			{
				// If we are a leaf, we can store the current node
				// We know that there has only one leaf node running at any time, no parallel branches possible
				// The Run() method also return a tuple <BTStatus, BTNode(CurrentNode)>
				btParams.Agent->Current = this;
			}

			return newStatus;
		}
	}
}