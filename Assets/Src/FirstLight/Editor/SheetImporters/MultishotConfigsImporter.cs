using System.Collections.Generic;
using FirstLight.Editor.EditorTools;
using FirstLight.Game.Configs;
using FirstLight.Game.Ids;
using FirstLight.Game.Utils;
using FirstLight.GoogleSheetImporter;
using FirstLightEditor.GoogleSheetImporter;
using Photon.Deterministic;
using Quantum;

namespace FirstLight.Editor.SheetImporters
{
	/// <inheritdoc />
	public class MultishotConfigsImporter : GoogleSheetQuantumConfigsImporter<QuantumMultishotConfig, MultishotConfigs>
	{
		/// <inheritdoc />
		public override string GoogleSheetUrl => "***REMOVED***/edit#gid=2094850344";
	}
}