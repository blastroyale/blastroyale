namespace Quantum
{
  [System.Serializable]
  public unsafe class DebugAction : AIAction
  {
    public string Message;

    public override void Update(Frame f, EntityRef e)
    {
      Log.Info(Message);
    }
  }
}
