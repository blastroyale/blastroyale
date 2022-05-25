using FirstLight.Game.Configs;
using Quantum;

namespace FirstLight.Editor.SheetImporters
{
	public class
		EquipmentStatsConfigImporter : GoogleSheetQuantumConfigsImporter<QuantumEquipmentStatsConfig,
			EquipmentStatsConfigs>
	{
		public override string GoogleSheetUrl =>
			"***REMOVED***/edit#gid=805628280";
	}
}