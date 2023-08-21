using FirstLight.Game.Utils;
using Quantum;

namespace FirstLight.Game.Data.DataTypes
{
	/// <summary>
	/// Equipment reward holder
	/// </summary>
	public class EquipmentReward : IReward
	{
		public Equipment Equipment => _equipment;
		public GameId GameId => _equipment.GameId;
		public uint Amount => 1;
		public string DisplayName => GameId.GetLocalization();

		private Equipment _equipment;

		public EquipmentReward(Equipment equipment)
		{
			_equipment = equipment;
		}
	}
}