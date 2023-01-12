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
				info.Equipment.Edition.ToString().ToUpperInvariant(),
				info.Equipment.Material.ToString().ToUpperInvariant(),
				info.Equipment.Faction.ToString().ToUpperInvariant(),
				info.Equipment.Grade.ToString().ToUpperInvariant(),
				info.Manufacturer.ToString().ToUpperInvariant(),
				string.Format(ScriptLocalization.UITEquipment.replicated_count, info.Equipment.ReplicationCounter,
					info.Equipment.InitialReplicationCounter)
			};

			return tags;
		}
	}
}