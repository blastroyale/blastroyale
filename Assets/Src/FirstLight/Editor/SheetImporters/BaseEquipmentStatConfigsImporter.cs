using FirstLight.Game.Configs;
using Quantum;

namespace FirstLight.Editor.SheetImporters
{
	/// <inheritdoc />
	public class BaseEquipmentStatConfigsImporter : GoogleSheetQuantumConfigsImporter<QuantumBaseEquipmentStatConfig,
		BaseEquipmentStatConfigs>
	{
		/// <inheritdoc />
		public override string GoogleSheetUrl =>
			"***REMOVED***/edit#gid=2033534642";
	}
}