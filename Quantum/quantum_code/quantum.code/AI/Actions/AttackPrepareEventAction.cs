using System;
using System.Diagnostics;

namespace Quantum
{
	[Serializable]
	[AssetObjectConfig(GenerateLinkingScripts = true, GenerateAssetCreateMenu = false,
		GenerateAssetResetMethod = false)]
	public class AttackPrepareEventAction : AIAction
	{
		public override void Update(Frame frame, EntityRef entity, ref AIContext aiContext)
		{
			var playerCharacter = frame.Get<PlayerCharacter>(entity);
			frame.Events.OnPlayerAttackPrepare(playerCharacter.Player, entity);
		}
	}
}