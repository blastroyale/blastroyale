namespace Quantum
{
	public partial struct UTAgent
	{
		// ========== PUBLIC MEMBERS ==================================================================================

		// Used to setup info on the Unity debugger
		public string GetRootAssetName(Frame frame) => frame.FindAsset<UTRoot>(UtilityReasoner.UTRoot.Id).Path;
		public string GetRootAssetName(FrameThreadSafe frame) => frame.FindAsset<UTRoot>(UtilityReasoner.UTRoot.Id).Path;

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
