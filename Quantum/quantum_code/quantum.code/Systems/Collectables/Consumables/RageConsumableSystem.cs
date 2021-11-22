using Photon.Deterministic;

namespace Quantum.Systems.Collectables.Consumables
{
	/// <summary>
	/// This system handles all the behaviour the player picks a <seealso cref="Rage"/>
	/// </summary>
	public unsafe class RageConsumableSystem : ConsumableSystemBase
	{
		protected override ConsumableType ConsumableType => ConsumableType.Rage;
		
		protected override void OnConsumablePicked(Frame f, EntityRef e, EntityRef playerEntity, PlayerRef player, 
		                                           Consumable consumable)
		{
			StatusModifiers.AddStatusModifierToEntity(f, playerEntity, StatusModifierType.Rage, (int) consumable.PowerAmount);
		}
	}
}