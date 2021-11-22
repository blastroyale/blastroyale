using Photon.Deterministic;

namespace Quantum.Systems.Collectables.Consumables
{
	/// <inheritdoc />
	/// <remarks>
	/// Implementation for the <see cref="InterimArmour"/> <see cref="Consumable"/> in the game.
	/// </remarks>
	public unsafe class InterimArmourConsumableSystem : ConsumableSystemBase
	{
		protected override ConsumableType ConsumableType => ConsumableType.InterimArmour;

		protected override void OnConsumablePicked(Frame f, EntityRef e, EntityRef playerEntity, PlayerRef player, 
		                                           Consumable consumable)
		{
			if (f.Unsafe.TryGetPointer<Stats>(playerEntity, out var stats))
			{
				stats->GainInterimArmour(f, playerEntity, e, (int) consumable.PowerAmount);
			}
		}
	}
}