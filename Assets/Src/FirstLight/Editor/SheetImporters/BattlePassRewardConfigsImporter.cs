using System.Collections.Generic;
using FirstLight.Game.Configs;
using FirstLight.GoogleSheetImporter;
using FirstLightEditor.GoogleSheetImporter;
using Quantum;

namespace FirstLight.Editor.SheetImporters
{
	public class BattlePassRewardConfigsImporter : GoogleSheetScriptableObjectImportContainer<BattlePassRewardConfigs>
	{
		public override string GoogleSheetUrl =>
			"***REMOVED***/edit#gid=1611215020";

		protected override void OnImport(BattlePassRewardConfigs scriptableObject,
		                                 List<Dictionary<string, string>> data)
		{
			var configs = new List<BattlePassRewardConfig>();

			foreach (var row in data)
			{
				configs.Add(new BattlePassRewardConfig(int.Parse(row["Id"]), Deserialize(row)));
			}

			scriptableObject.Configs = configs;
		}

		protected virtual Equipment Deserialize(Dictionary<string, string> data)
		{
			return CsvParser.DeserializeTo<Equipment>(data);
		}
	}
}