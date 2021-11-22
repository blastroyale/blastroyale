using Quantum.Collections;

namespace Quantum
{
  public unsafe partial struct BlackboardEntry
  {
    /// <summary>
    /// Iterate through all Decorators that watches this Blackboard entry
    /// Re-check the Decorators so it can check if an abort is needed
    /// </summary>
    /// <param name="p"></param>
    public void TriggerDecorators(BTParams p)
    {
      var frame = p.Frame;

      // If the reactive decorators list was already allocated...
      if (ReactiveDecorators.Ptr != default)
      {
        // Solve it and trigger the decorators checks
        var reactiveDecorators = frame.ResolveList(ReactiveDecorators);
        for (int i = 0; i < reactiveDecorators.Count; i++)
        {
          var decoratorInstance = frame.FindAsset<BTDecorator>(reactiveDecorators[i].Id);
          p.BtAgent->OnDecoratorReaction(p.Frame, p.Entity, p.Blackboard, decoratorInstance, decoratorInstance.AbortType);
        }
      }
    }
  }
}
