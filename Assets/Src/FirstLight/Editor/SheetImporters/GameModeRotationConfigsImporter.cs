using System.Collections.Generic;
using FirstLight.Editor.EditorTools;
using FirstLight.Game.Configs;
using FirstLight.GoogleSheetImporter;
using FirstLightEditor.GoogleSheetImporter;

namespace FirstLight.Editor.SheetImporters
{
	public class GameModeRotationConfigsImporter : GoogleSheetSingleConfigImporter<GameModeRotationConfig,
		GameModeRotationConfigs>
	{
		public override string GoogleSheetUrl =>
			"***REMOVED***/edit#gid=2128837022";

		protected override GameModeRotationConfig Deserialize(List<Dictionary<string, string>> data)
		{
			var config = new GameModeRotationConfig();
			var type = typeof(GameModeRotationConfig);

			config.RotationEntries = new List<GameModeRotationConfig.RotationEntry>();

			foreach (var row in data)
			{
				var keyValue = row["Key"];

				if (!string.IsNullOrEmpty(keyValue) && keyValue != "x")
				{
					// Values section
					var field = type.GetField(row["Key"]);

					if (field == null)
					{
						continue;
					}

					var value = CsvParser.DeserializeObject(row["Value"], field.FieldType);

					var configObj = config as object;
					field.SetValue(configObj, value);
					config = (GameModeRotationConfig) configObj;
				}
				else
				{
					// GameModes section
					var entry = QuantumDeserializer.DeserializeTo<GameModeRotationConfig.RotationEntry>(row);
					config.RotationEntries.Add(entry);
				}
			}

			return config;
		}
	}
}