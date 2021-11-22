using System;

namespace Quantum
{
  // Wrapping the blackboard value key inside a struct gives us a nice way to overload the Unity inspector.
  [Serializable]
  public struct AIBlackboardValueKey
  {
    public String Key;
  }
}
