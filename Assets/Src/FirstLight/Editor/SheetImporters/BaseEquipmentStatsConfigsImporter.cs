using FirstLight.Game.Configs;
using Quantum;

namespace FirstLight.Editor.SheetImporters
{
	/// <inheritdoc />
	public class BaseEquipmentStatsConfigsImporter : GoogleSheetQuantumConfigsImporter<QuantumBaseEquipmentStatsConfig,
		BaseEquipmentStatsConfigs>
	{
		/// <inheritdoc />
		public override string GoogleSheetUrl =>
			"***REMOVED***/edit#gid=2033534642";
	}
}