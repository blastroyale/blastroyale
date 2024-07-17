using System;

namespace Quantum
{
	/// <summary>
	/// This action processes when the <see cref="PlayerCharacter"/> stops attacking with his <see cref="Weapon"/>
	/// </summary>
	[Serializable]
	[AssetObjectConfig(GenerateLinkingScripts = true, GenerateAssetCreateMenu = false, GenerateAssetResetMethod = false)]
	public class PlayerStopAttackAction : AIAction
	{
		/// <inheritdoc />
		public override unsafe void Update(Frame f, EntityRef e, ref AIContext aiContext)
		{
			var player = f.Unsafe.GetPointer<PlayerCharacter>(e)->Player;
			var bb = f.Unsafe.GetPointer<AIBlackboardComponent>(e);
			bb->Set(f, Constants.IS_SHOOTING_KEY, false);
			f.Events.OnPlayerStopAttack(player, e);
		}
	}
}