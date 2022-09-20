using System;

namespace Quantum
{
	/// <summary>
	/// This decision checks if we should do a Skydive drop.
	/// </summary>
	[Serializable]
	[AssetObjectConfig(GenerateLinkingScripts = true, GenerateAssetCreateMenu = false,
	                   GenerateAssetResetMethod = false)]
	public partial class ShouldPerformSkydiveDecision : HFSMDecision
	{
		/// <inheritdoc />
		public override unsafe bool Decide(Frame f, EntityRef e)
		{
			return f.Context.GameModeConfig.SkydiveSpawn;
		}
	}
}