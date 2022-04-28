using Photon.Deterministic;
using System;
using System.Collections.Generic;

namespace Quantum
{
	[Serializable]
	public unsafe partial class BTRoot : BTDecorator
	{
		// ========== PUBLIC MEMBERS ==================================================================================

		[BotSDKHidden] public Int32 NodesAmount;

		// ========== BTDecorator INTERFACE ===========================================================================

		public override BTNodeType NodeType
		{
			get
			{
				return BTNodeType.Root;
			}
		}

		protected unsafe override BTStatus OnUpdate(BTParams btParams)
		{
			btParams.Agent->Current = this;

			if (_childInstance != null)
			{
				return _childInstance.RunUpdate(btParams);
			}

			return BTStatus.Success;
		}

		// ========== PUBLIC METHODS ==================================================================================

		public void InitializeTree(Frame frame, BTAgent* agent, AIBlackboardComponent* blackboard)
		{
			InitNodesRecursively(frame, this, agent, blackboard);
		}

		// ========== PRIVATE METHODS =================================================================================

		private static void InitNodesRecursively(Frame frame, BTNode node, BTAgent* agent, AIBlackboardComponent* blackboard)
		{
			node.Init(frame, blackboard, agent);

			if (node is BTDecorator decoratorNode)
			{
				BTNode childNode = frame.FindAsset<BTNode>(decoratorNode.Child.Id);
				InitNodesRecursively(frame, childNode, agent, blackboard);
			}

			if (node is BTComposite compositeNode)
			{
				foreach (var child in compositeNode.Children)
				{
					BTNode childNode = frame.FindAsset<BTNode>(child.Id);
					InitNodesRecursively(frame, childNode, agent, blackboard);
				}
			}
		}
	}
}