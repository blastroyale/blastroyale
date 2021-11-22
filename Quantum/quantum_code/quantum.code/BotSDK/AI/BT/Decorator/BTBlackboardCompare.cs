using System;

namespace Quantum
{
  /// <summary>
  /// Reactive Decorator sample. Listens to changes on two Blackboard entries.
  /// </summary>
  [Serializable]
  public unsafe class BTBlackboardCompare : BTDecorator
  {
    // We let the user define, on the Visual Editor, which Blackboard entries
    // shall be observed by this Decorator
    public AIBlackboardValueKey BlackboardKeyA;
    public AIBlackboardValueKey BlackboardKeyB;

    public override void OnEnter(BTParams p)
    {
      base.OnEnter(p);

      // Whenever we enter this Decorator...
      // We register it as a Reactive Decorator so, whenever the entries are changed,
      // the DryRun is executed again, possibly aborting the current execution
      p.Blackboard->RegisterReactiveDecorator(p.Frame, BlackboardKeyA.Key, this);
      p.Blackboard->RegisterReactiveDecorator(p.Frame, BlackboardKeyB.Key, this);
    }

    public override void OnExit(BTParams p)
    {
      base.OnExit(p);
      
      // Whenever the execution goes higher, it means that this Decorator isn't in the current subtree anymore
      // So we unregister this Decorator from the Reactive list. This means that if the Blackboard entries
      // get changed, this Decorator will not react anymore
      p.Blackboard->UnregisterReactiveDecorator(p.Frame, BlackboardKeyA.Key, this);
      p.Blackboard->UnregisterReactiveDecorator(p.Frame, BlackboardKeyB.Key, this);
    }

    // We just check if A is greater than B. If that's the case
    // PS: this gets called in THREE possible situations:
    // 1 - When the execution is goign DOWN on the tree and this Decorator is found
    // 2 - If changes to the observed blackboard entries happen
    // 3 - If this is inside a Dynamic Composite node
    public override Boolean DryRun(BTParams p)
    {
      var blackboardComponent = p.Blackboard;
      var A = blackboardComponent->GetInteger(p.Frame, BlackboardKeyA.Key);
      var B = blackboardComponent->GetInteger(p.Frame, BlackboardKeyB.Key);

      return A > B;
    }
  }
}


