using System.Collections.Generic;
using FirstLight.Editor.EditorTools;
using FirstLight.Game.Configs;
using FirstLight.Game.Configs.AssetConfigs;
using FirstLight.Game.Ids;
using FirstLight.Game.Utils;
using FirstLight.GoogleSheetImporter;
using Quantum;
using UnityEditor;

namespace FirstLight.Editor.SheetImporters
{
	/// <inheritdoc />
	public class GearConfigsImporter : GoogleSheetQuantumConfigsImporter<QuantumGearConfig, GearConfigs>
	{
		/// <inheritdoc />
		public override string GoogleSheetUrl => "***REMOVED***/edit#gid=79837249";
	}
}