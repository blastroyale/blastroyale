namespace Quantum
{
  public partial struct HFSMAgent
  {
    // Used to setup info on the Unity debugger
    public string GetRootAssetName(Frame f) => f.FindAsset<HFSMRoot>(Data.Root.Id).Path;

    public AIConfig GetConfig(Frame f)
    {
      return f.FindAsset<AIConfig>(Config.Id);
    }
  }
}
