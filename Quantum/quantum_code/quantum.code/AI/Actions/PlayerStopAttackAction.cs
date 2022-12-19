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
		public unsafe override void Update(Frame f, EntityRef e, ref AIContext aiContext)
		{
			var player = f.Get<PlayerCharacter>(e).Player;
			var bb = f.Unsafe.GetPointer<AIBlackboardComponent>(e);
			bb->Set(f, Constants.IsShootingKey, false);
			f.Events.OnPlayerStopAttack(player, e);
		}
	}
}