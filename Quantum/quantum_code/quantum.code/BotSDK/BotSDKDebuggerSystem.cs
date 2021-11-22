using System;

namespace Quantum
{
  /// <summary>
  /// Using this system is optional. It is only used to aim the Debugger on the Unity side.
  /// It is also safe to copy logic from this system into your own systems, if it better suits your architecture.
  /// </summary>
  public class BotSDKDebuggerSystem : SystemMainThread
  {
    // Used for DEBUGGING purposes only
    public static Action<Frame> OnVerifiedFrame;
    public static Action<EntityRef, string> SetEntityDebugLabel;

    public override void Update(Frame f)
    {
      if (f.IsVerified)
      {
        OnVerifiedFrame?.Invoke(f);
      }
    }
  }
}
