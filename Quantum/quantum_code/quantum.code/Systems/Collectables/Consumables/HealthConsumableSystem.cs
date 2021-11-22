namespace Quantum.Systems.Collectables.Consumables
{
	/// <inheritdoc />
	/// <remarks>
	/// Implementation for the Health <see cref="Consumable"/> in the game.
	/// </remarks>
	public unsafe class HealthConsumableSystem : ConsumableSystemBase
	{
		protected override ConsumableType ConsumableType => ConsumableType.Health;
		
		protected override void OnConsumablePicked(Frame f, EntityRef e, EntityRef playerEntity, PlayerRef player, 
		                                           Consumable consumable)
		{
			f.Unsafe.GetPointer<Stats>(playerEntity)->GainHealth(f, playerEntity, EntityRef.None, (int) consumable.PowerAmount);
		}
	}
}