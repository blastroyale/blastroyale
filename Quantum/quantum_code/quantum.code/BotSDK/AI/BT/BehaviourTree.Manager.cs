using Photon.Deterministic;
using System;

namespace Quantum
{
  public static unsafe class BTManager
  {
    public static Action<EntityRef, string> OnSetupDebugger;

    public static Action<EntityRef, long> OnNodeEnter;
    public static Action<EntityRef, long> OnNodeExit;
    public static Action<EntityRef, long> OnNodeSuccess;
    public static Action<EntityRef, long> OnNodeFailure;
    public static Action<EntityRef> OnTreeCompleted;

    /// <summary>
    /// Call this once, to initialize the BTAgent.
    /// This method internally looks for a Blackboard Component on the entity
    /// and passes it down the pipeline.
    /// </summary>
    /// <param name="frame"></param>
    /// <param name="entity"></param>
    /// <param name="root"></param>
    public static void Init(Frame frame, EntityRef entity, BTRoot root)
    {
      if (frame.Unsafe.TryGetPointer(entity, out BTAgent* btAgent))
      {
        btAgent->Initialize(frame, entity, btAgent, root, true);
      }
      else
      {
        Log.Error("[Bot SDK] Tried to initialize an entity which has no BTAgent component");
      }
    }

    /// <summary>
    /// Call this method every frame to update your BT Agent.
    /// You can optionally pass a Blackboard Component to it, if your Agents use it
    /// </summary>
    /// <param name="frame"></param>
    /// <param name="entity"></param>
    /// <param name="blackboardComponent"></param>
    public static void Update(Frame frame, EntityRef entity, AIBlackboardComponent* blackboardComponent = null)
    {
      var btAgent = frame.Unsafe.GetPointer<BTAgent>(entity);
      btAgent->Update(frame, btAgent, blackboardComponent, entity);
    }
  }
}
