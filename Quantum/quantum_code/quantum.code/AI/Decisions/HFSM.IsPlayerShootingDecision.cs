using System;

namespace Quantum
{
	/// <summary>
	/// This decision checks if the player has shooting in the input (player holds "shoot" button)
	/// </summary>
	[Serializable]
	[AssetObjectConfig(GenerateLinkingScripts = true, GenerateAssetCreateMenu = false, GenerateAssetResetMethod = false)]
	public partial class IsPlayerShootingDecision : HFSMDecision
	{
		public AIBlackboardValueKey TimeToTrueState;
		
		/// <inheritdoc />
		public override unsafe bool Decide(Frame f, EntityRef e)
		{
			return f.Get<AIBlackboardComponent>(e).GetBoolean(f, Constants.IsShootingKey);
		}
	}
}