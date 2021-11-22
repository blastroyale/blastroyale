namespace Quantum
{
  public unsafe abstract partial class AIFunctionInt
  {
    public abstract int Execute(Frame frame, EntityRef entity = default);
  }

  [BotSDKHidden]
  [System.Serializable]
  public unsafe partial class DefaultAIFunctionInt : AIFunctionInt
  {
    public override int Execute(Frame frame, EntityRef entity = default)
    {
      return 0;
    }
  }
}
