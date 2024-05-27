using System.Collections.Generic;
using FirstLight.Editor.EditorTools;
using FirstLight.Game.Configs;
using FirstLight.Game.Configs.AssetConfigs;
using Quantum;

namespace FirstLight.Editor.SheetImporters
{
	/// <inheritdoc />
	public class ChestConfigsImporter : GoogleSheetQuantumConfigsImporter<QuantumChestConfig, ChestConfigs>
	{
		public override string GoogleSheetUrl => "***REMOVED***/edit#gid=1440451349";
	}
}