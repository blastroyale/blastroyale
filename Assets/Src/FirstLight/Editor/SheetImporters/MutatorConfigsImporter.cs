using FirstLight.Game.Configs;
using Quantum;

namespace FirstLight.Editor.SheetImporters
{
	public class MutatorConfigsImporter :
		GoogleSheetQuantumConfigsImporter<QuantumMutatorConfig, MutatorConfigs>
	{
		public override string GoogleSheetUrl =>
			"***REMOVED***/edit#gid=54961578";
	}
}