using Photon.Deterministic;

namespace Quantum
{
	public unsafe partial struct Weapon
	{
		/// <summary>
		/// Gives the given ammo <paramref name="amount"/> to this <paramref name="entity"/> and notifies the change.
		/// </summary>
		internal void GainAmmo(uint amount)
		{
			var consumablePower = amount / FP._100;
			var updatedCapacity = (uint) (Capacity + FPMath.CeilToInt(MaxCapacity * consumablePower));
			
			Capacity = updatedCapacity > MaxCapacity ? MaxCapacity : updatedCapacity;
			
			if (Emptied && Capacity >= MinCapacityToShoot)
			{
				Emptied = false;
			}
		}
	}
}