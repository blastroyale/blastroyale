namespace Quantum
{
	[BotSDKHidden]
	[System.Serializable]
	public unsafe partial class DefaultAIFunctionAssetRef : AIFunction<AssetRef>
	{
		// ========== AIFunction INTERFACE ============================================================================

		public override AssetRef Execute(Frame frame, EntityRef entity)
		{
			return null;
		}
	}
}
