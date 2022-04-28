using FirstLight.Game.Configs;
using Quantum;

namespace FirstLight.Editor.SheetImporters
{
	/// <inheritdoc />
	public class GearConfigsImporter : GoogleSheetQuantumConfigsImporter<QuantumGearConfig, GearConfigs>
	{
		/// <inheritdoc />
		public override string GoogleSheetUrl => "***REMOVED***/edit#gid=79837249";
	}
}