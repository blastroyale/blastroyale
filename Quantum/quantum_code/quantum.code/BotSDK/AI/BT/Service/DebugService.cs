using System;

namespace Quantum
{
  [Serializable]
  public unsafe partial class DebugService : BTService
  {
    public string Message;
    protected unsafe override void OnUpdate(BTParams p)
    {
      Log.Info($"[BT SERVICE] { Message } | Frame: {p.Frame.Number}");
    }
  }
}
