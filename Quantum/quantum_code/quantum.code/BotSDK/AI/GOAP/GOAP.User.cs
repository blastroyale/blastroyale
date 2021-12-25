namespace Quantum
{
  public unsafe partial struct GOAPAgent
  {
    public AIConfig GetConfig(Frame frame)
    {
      return frame.FindAsset<AIConfig>(Config.Id);
    }
  }
}
