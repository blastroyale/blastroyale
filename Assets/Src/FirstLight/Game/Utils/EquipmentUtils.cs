using System.Collections.Generic;
using FirstLight.Game.Infos;
using I2.Loc;
using Quantum;

namespace FirstLight.Game.Utils
{
	/// <summary>
	/// Helpers / utils to work with the <see cref="Equipment"/> class.
	/// </summary>
	public static class EquipmentUtils
	{
		/// <summary>
		/// Returns a list of tags that should be displayed to players.
		/// </summary>
		public static IEnumerable<string> GetTags(this EquipmentInfo info)
		{
			var tags = new List<string>
			{
				info.Equipment.Edition.GetLocalization(),
				info.Equipment.Material.GetLocalization(),
				info.Equipment.Faction.GetLocalization(),
				info.Equipment.Grade.GetLocalization(),
				info.Manufacturer.GetLocalization(),
				string.Format(ScriptLocalization.UITEquipment.replicated_count, info.Equipment.ReplicationCounter,
					info.Equipment.InitialReplicationCounter)
			};

			return tags;
		}
	}
}