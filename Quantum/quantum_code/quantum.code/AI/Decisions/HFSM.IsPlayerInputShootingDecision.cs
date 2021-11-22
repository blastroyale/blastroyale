using System;
using Photon.Deterministic;
using Quantum.Systems;

namespace Quantum
{
	/// <summary>
	/// This decision checks if the player has shooting in the input (player holds "shoot" button)
	/// </summary>
	[Serializable]
	[AssetObjectConfig(GenerateLinkingScripts = true, GenerateAssetCreateMenu = false, GenerateAssetResetMethod = false)]
	public partial class IsPlayerInputShootingDecision : HFSMDecision
	{
		/// <inheritdoc />
		public override unsafe bool Decide(Frame f, EntityRef e)
		{
			if (!f.Unsafe.TryGetPointer<PlayerCharacter>(e, out var player))
			{
				return false;
			}
			
			// If it's a Bot then we check the Target blackboard variable instead of Input
			if (f.Has<BotCharacter>(e))
			{
				var bb = f.Get<AIBlackboardComponent>(e);
				var targetCandidate = bb.GetEntityRef(f, Constants.TARGET_BB_KEY);
				
				return QuantumHelpers.IsAttackable(f, targetCandidate);
			}
			
			var input = f.GetPlayerInput(player->Player);
			
			return input->IsShootButtonDown;
		}
	}
}