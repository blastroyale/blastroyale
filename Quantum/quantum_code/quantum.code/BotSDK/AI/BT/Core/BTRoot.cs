using Photon.Deterministic;
using System;
using System.Collections.Generic;

namespace Quantum {
  [Serializable]
  public unsafe partial class BTRoot : BTDecorator {
    [BotSDKHidden] public Int32 NodesAmount;

    public override BTNodeType NodeType {
      get {
        return BTNodeType.Root;
      }
    }

    protected unsafe override BTStatus OnUpdate(BTParams p) {

      p.BtAgent->Current = this;

      if (_childInstance != null) {
        return _childInstance.RunUpdate(p);
      }

      return BTStatus.Success;
    }

    public void InitializeTree(Frame frame, BTAgent* btAgent, AIBlackboardComponent* bbComponent)
    {
      InitNodesRecursively(frame, this, btAgent, bbComponent);
    }

    private static void InitNodesRecursively(Frame frame, BTNode node, BTAgent* btAgent, AIBlackboardComponent* bbComponent) {
      node.Init(frame, bbComponent, btAgent);

      if (node is BTDecorator decoratorNode) {
        BTNode childNode = frame.FindAsset<BTNode>(decoratorNode.Child.Id);
        InitNodesRecursively(frame, childNode, btAgent, bbComponent);
      }

      if (node is BTComposite compositeNode) {
        foreach (var child in compositeNode.Children) {
          BTNode childNode = frame.FindAsset<BTNode>(child.Id);
          InitNodesRecursively(frame, childNode, btAgent, bbComponent);
        }
      }
    }
  }
}