using Quantum;

namespace FirstLight.Game.Utils
{
	public static class EquipmentExtensions
	{
		/// <summary>
		/// TODO
		/// </summary>
		/// <param name="equipment"></param>
		/// <returns></returns>
		public static bool IsMaxLevel(this Equipment equipment)
		{
			return equipment.Level >= equipment.MaxLevel;
		}

		/// <summary>
		/// TODO
		/// </summary>
		/// <param name="equipment"></param>
		/// <returns></returns>
		public static bool IsWeapon(this Equipment equipment)
		{
			return equipment.GameId.IsInGroup(GameIdGroup.Weapon);
		}

		/// <summary>
		/// TODO
		/// </summary>
		/// <param name="equipment"></param>
		/// <returns></returns>
		public static bool IsDefaultItem(this Equipment equipment)
		{
			// TODO: Might need different logic
			return equipment.GameId == GameId.Hammer;
		}
	}
}