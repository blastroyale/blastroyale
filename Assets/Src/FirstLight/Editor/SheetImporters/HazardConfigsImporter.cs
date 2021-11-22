using System.Collections.Generic;
using FirstLight.Editor.EditorTools;
using FirstLight.Game.Configs;
using FirstLight.Game.Ids;
using FirstLight.Game.Utils;
using FirstLight.GoogleSheetImporter;
using FirstLight.Game.Configs.AssetConfigs;
using Quantum;
using UnityEditor;
using UnityEngine;

namespace FirstLight.Editor.SheetImporters
{
	/// <inheritdoc />
	public class HazardConfigsImporter : GoogleSheetQuantumConfigsImporter<QuantumHazardConfig, HazardConfigs>
	{
		/// <inheritdoc />
		public override string GoogleSheetUrl => "***REMOVED***/edit#gid=1824310771";
	}
}