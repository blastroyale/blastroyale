using Photon.Deterministic;

namespace Quantum.Systems.Collectables.Consumables
{
	/// <inheritdoc />
	/// <remarks>
	/// Implementation for the Ammo <see cref="Consumable"/> in the game.
	/// </remarks>
	public class AmmoConsumableSystem : ConsumableSystemBase
	{
		protected override ConsumableType ConsumableType => ConsumableType.Ammo;
		
		protected override unsafe void OnConsumablePicked(Frame f, EntityRef e, EntityRef playerEntity, PlayerRef player, 
		                                           Consumable consumable)
		{
			if (!f.Unsafe.TryGetPointer<Weapon>(playerEntity, out var weapon))
			{
				return;
			}
			
			var consumablePower = consumable.PowerAmount / FP._100;
			var updatedCapacity = (uint) (weapon->Capacity + FPMath.CeilToInt(weapon->MaxCapacity * consumablePower));
			
			weapon->Capacity = updatedCapacity > weapon->MaxCapacity ? weapon->MaxCapacity : updatedCapacity;
			
			if (weapon->Emptied && weapon->Capacity >= weapon->MinCapacityToShoot)
			{
				weapon->Emptied = false;
			}
		}
	}
}