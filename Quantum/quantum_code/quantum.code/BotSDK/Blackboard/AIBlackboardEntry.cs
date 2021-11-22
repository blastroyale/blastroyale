using System;

namespace Quantum
{
  // This struct is stored on the blackboard asset
  // It is NOT the one used on the Blackboard component
  [Serializable]
  public struct AIBlackboardEntry
  {
    public AIBlackboardValueType Type;
    public AIBlackboardValueKey Key;
  }
}
