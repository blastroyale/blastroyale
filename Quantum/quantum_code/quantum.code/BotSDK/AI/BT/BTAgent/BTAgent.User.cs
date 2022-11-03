using Photon.Deterministic;
using System;

namespace Quantum
{
	public unsafe partial struct BTAgent
	{
		// ========== PUBLIC MEMBERS ==================================================================================

		// Used to setup info on the Unity debugger
		public string GetTreeAssetName(Frame frame) => frame.FindAsset<BTRoot>(Tree.Id).Path;
		public string GetTreeAssetName(FrameThreadSafe frame) => frame.FindAsset<BTRoot>(Tree.Id).Path;

		public bool IsAborting => AbortNodeId != 0;

		public AIConfig GetConfig(Frame frame)
		{
			return frame.FindAsset<AIConfig>(Config.Id);
		}

		public AIConfig GetConfig(FrameThreadSafe frame)
		{
			return frame.FindAsset<AIConfig>(Config.Id);
		}

		public void Initialize(Frame frame, EntityRef entityRef, BTAgent* agent, AssetRefBTNode tree, bool force = false, bool isCompound = false)
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
			ServicesEndTimes = frame.AllocateList<FP>(4);

			// Node data, such as FP for timers, Integers for IDs
			BTDataValues = frame.AllocateList<BTDataValue>(4);

			// The Services contained in the current sub-tree,
			// which should be updated considering its intervals
			ActiveServices = frame.AllocateList<AssetRefBTService>(4);

			// The Dynamic Composites contained in the current sub-tree,
			// which should be re-checked every tick
			DynamicComposites = frame.AllocateList<AssetRefBTComposite>(4);

			// -- Cache the Blackboard (if any)
			AIBlackboardComponent* blackboard = null;
			if (frame.Has<AIBlackboardComponent>(entityRef))
			{
				blackboard = frame.Unsafe.GetPointer<AIBlackboardComponent>(entityRef);
			}

			// -- Initialize the tree
			treeAsset.InitializeTree(frame, agent, blackboard);

			// -- Trigger the debugging event (mostly for the Unity side)
			BTManager.OnSetupDebugger?.Invoke(entityRef, treeAsset.Path, isCompound);
		}

		public void Free(Frame frame)
		{
			Tree = default;
			frame.FreeList<Byte>(NodesStatus);
			frame.FreeList<FP>(ServicesEndTimes);
			frame.FreeList<BTDataValue>(BTDataValues);
			frame.FreeList<AssetRefBTService>(ActiveServices);
			frame.FreeList<AssetRefBTComposite>(DynamicComposites);
		}

		public void Update(ref BTParams btParams, ref AIContext aiContext)
		{
			AssetRefBTNode tree = btParams.Agent->Tree;
			if(tree != default)
			{
				// We always load the root asset to force it's Loaded callback to be called, if it was not yet
				// The root then also the entire tree, forcing the Loaded calls
				// This is useful for late joiners who did not have the tree loaded yet, thus potentially having non-cached data
				btParams.Frame.FindAsset<BTRoot>(tree.Id);
			}

			if (btParams.Agent->Current == null)
			{
				btParams.Agent->Current = btParams.Agent->Tree;
			}

			RunDynamicComposites(btParams, ref aiContext);

			BTNode node = btParams.FrameThreadSafe.FindAsset<BTNode>(btParams.Agent->Current.Id);
			UpdateSubtree(btParams, node, ref aiContext);

			BTManager.ClearBTParams(btParams);
		}

		public unsafe void AbortLowerPriority(BTParams btParams, BTNode node)
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
				return;
			}

			var nodeAsComposite = (topNode as BTComposite);
			nodeAsComposite.AbortNodes(btParams, nodeAsComposite.GetCurrentChild(btParams.FrameThreadSafe, btParams.Agent) + 1);
		}

		// Used to react to blackboard changes which are observed by Decorators
		// This is triggered by the Blackboard Entry itself, which has a list of Decorators that observes it
		public unsafe void OnDecoratorReaction(BTParams btParams, BTNode node, BTAbort abort, out bool abortSelf, out bool abortLowerPriotity, ref AIContext aiContext)
		{
			abortSelf = false;
			abortLowerPriotity = false;

			var status = node.GetStatus(btParams.FrameThreadSafe, btParams.Agent);

			if (abort.IsSelf() && (status == BTStatus.Running || status == BTStatus.Inactive))
			{
				// Check condition again
				if (node.DryRun(btParams, ref aiContext) == false)
				{
					abortSelf = true;
					node.OnAbort(btParams);
				}
			}

			if (abort.IsLowerPriority())
			{
				AbortLowerPriority(btParams, node);
				abortLowerPriotity = true;
			}
		}

		// ========== PRIVATE METHODS =================================================================================

		// We run the dynamic composites contained on the current sub-tree (if any)
		// If any of them result in "False", we abort the current sub-tree
		// and take the execution back to the topmost decorator so the agent can choose another path
		private void RunDynamicComposites(BTParams btParams, ref AIContext aiContext)
		{
			var frame = btParams.FrameThreadSafe;
			var dynamicComposites = frame.ResolveList<AssetRefBTComposite>(DynamicComposites);

			for (int i = 0; i < dynamicComposites.Count; i++)
			{
				var compositeRef = dynamicComposites.GetPointer(i);
				var composite = frame.FindAsset<BTComposite>(compositeRef->Id);
				var dynamicResult = composite.OnDynamicRun(btParams, ref aiContext);

				if (dynamicResult == false)
				{
					btParams.Agent->Current = composite.TopmostDecorator;
					dynamicComposites.Remove(*compositeRef);
					composite.OnReset(btParams, ref aiContext);
					return;
				}
			}
		}

		private void UpdateSubtree(BTParams btParams, BTNode node, ref AIContext aiContext, bool continuingAbort = false)
		{
			// Start updating the tree from the Current agent's node
			var result = node.RunUpdate(btParams, ref aiContext, continuingAbort);

			// If the current node completes, go up in the tree until we hit a composite
			// Run that one. On success or fail continue going up.
			while (result != BTStatus.Running && node.Parent != null)
			{
				// As we are traversing the tree up, we allow nodes to remove any
				// data that is only needed locally
				node.OnExit(btParams, ref aiContext);

				node = node.Parent;
				if (node.NodeType == BTNodeType.Composite)
				{
					((BTComposite)node).ChildCompletedRunning(btParams, result);
					result = node.RunUpdate(btParams, ref aiContext, continuingAbort);
				}

				if (node.NodeType == BTNodeType.Decorator)
				{
					((BTDecorator)node).EvaluateAbortNode(btParams);
				}
			}

			BTService.TickServices(btParams, ref aiContext);

			if (result != BTStatus.Running)
			{
				BTNode tree = btParams.FrameThreadSafe.FindAsset<BTNode>(btParams.Agent->Tree.Id);
				tree.OnReset(btParams, ref aiContext);
				btParams.Agent->Current = btParams.Agent->Tree;
				BTManager.OnTreeCompleted?.Invoke(btParams.Entity, btParams.IsCompound);
				//Log.Info("Behaviour Tree completed with result '{0}'. It will re-start from '{1}'", result, btParams.Agent->Current.Id);
			}
		}
	}
}