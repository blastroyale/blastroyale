namespace Quantum
{
  public partial struct UTAgent
  {
    // Used to setup info on the Unity debugger
    public string GetRootAssetName(Frame f) => f.FindAsset<UTRoot>(UtilityReasoner.UTRoot.Id).Path;

    public AIConfig GetConfig(Frame f)
    {
      return f.FindAsset<AIConfig>(Config.Id);
    }
  }
}
