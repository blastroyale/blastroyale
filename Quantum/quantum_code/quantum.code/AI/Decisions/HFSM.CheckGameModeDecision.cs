using System;

namespace Quantum
{
	/// <summary>
	/// This decision checks if we are in the BR or Deathmatch mode
	/// </summary>
	[Serializable]
	[AssetObjectConfig(GenerateLinkingScripts = true, GenerateAssetCreateMenu = false,
	                   GenerateAssetResetMethod = false)]
	public partial class CheckGameModeDecision : HFSMDecision
	{
		public GameMode GameMode;

		/// <inheritdoc />
		public override unsafe bool Decide(Frame f, EntityRef e)
		{
			return f.Context.MapConfig.GameMode == GameMode;
		}
	}
}