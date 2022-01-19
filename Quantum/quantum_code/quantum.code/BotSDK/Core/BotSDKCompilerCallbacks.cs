namespace Quantum
{
	public static class BotSDKCompilerCallbacks
	{
		public static System.Action<string, AssetRefHFSMRoot, AssetRefAIBlackboard, AssetRefAIBlackboardInitializer, AssetRefAIConfig> HFSMCompiled;
		public static System.Action<string, AssetRefBTRoot, AssetRefAIBlackboard, AssetRefAIBlackboardInitializer, AssetRefAIConfig> BTCompiled;
		public static System.Action<string, AssetRefUTRoot, AssetRefAIBlackboard, AssetRefAIBlackboardInitializer, AssetRefAIConfig> UTCompiled;
		public static System.Action<string, AssetRefGOAPRoot, AssetRefAIBlackboard, AssetRefAIBlackboardInitializer, AssetRefAIConfig> GOAPCompiled;
	}
}
