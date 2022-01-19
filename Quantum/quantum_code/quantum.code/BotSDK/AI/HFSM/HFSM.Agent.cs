namespace Quantum
{
	public partial struct HFSMAgent
	{
		// ========== PUBLIC MEMBERS ==================================================================================

		// Used to setup info on the Unity debugger
		public string GetRootAssetName(Frame frame) => frame.FindAsset<HFSMRoot>(Data.Root.Id).Path;
		public string GetRootAssetName(FrameThreadSafe frame) => frame.FindAsset<HFSMRoot>(Data.Root.Id).Path;

		// ========== PUBLIC METHODS ==================================================================================

		public AIConfig GetConfig(Frame frame)
		{
			return frame.FindAsset<AIConfig>(Config.Id);
		}

		public AIConfig GetConfig(FrameThreadSafe frame)
		{
			return frame.FindAsset<AIConfig>(Config.Id);
		}
	}
}
