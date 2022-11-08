namespace Quantum
{
	public unsafe partial struct WeaponDropPool
	{
		/// <summary>
		/// Check if the weapon pool of drops is empty
		/// </summary>
		public bool IsPoolEmpty => !WeaponPool[0].IsValid();
	}
}