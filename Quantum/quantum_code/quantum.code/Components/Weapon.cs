using Photon.Deterministic;

namespace Quantum
{
	public unsafe partial struct Weapon
	{
		/// <summary>
		/// Requests the ammo state of the <see cref="Weapon"/>
		/// </summary>
		public bool IsEmpty => Ammo == 0;
		
		/// <summary>
		/// Gives the given ammo <paramref name="amount"/> to this <paramref name="entity"/> and notifies the change.
		/// </summary>
		internal void GainAmmo(uint amount)
		{
			// Do not do "gain" for infinite ammo weapons
			if (Ammo < 0)
			{
				return;
			}
			
			var consumablePower = amount / FP._100;
			var updatedAmmo = Ammo + FPMath.CeilToInt(MaxAmmo * consumablePower);
			
			Ammo = updatedAmmo > MaxAmmo ? MaxAmmo : updatedAmmo;
		}
	}
}