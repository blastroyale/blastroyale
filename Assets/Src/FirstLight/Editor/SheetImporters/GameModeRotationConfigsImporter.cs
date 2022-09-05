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
			"***REMOVED***/edit#gid=405313914";

		protected override GameModeRotationConfig Deserialize(List<Dictionary<string, string>> data)
		{
			var config = new GameModeRotationConfig() as object;
			var type = typeof(GameModeRotationConfig);

			for (var i = 0; i < data.Count; i++)
			{
				var row = data[i];
				var fieldName = row["Key"];
				var isSubList = fieldName.EndsWith(CsvParser.SUB_LIST_SUFFIX);
				if (isSubList)
				{
					fieldName = fieldName.Replace(CsvParser.SUB_LIST_SUFFIX, "");
				}

				var field = type.GetField(fieldName);

				if (field == null)
				{
					continue;
				}

				object value;

				if (isSubList)
				{
					value = CsvParser.DeserializeSubList(data, i, field.FieldType, field.Name,
					                                     QuantumDeserializer.FpDeserializer,
					                                     QuantumDeserializer.QuantumGameModePairDeserializer);
				}
				else
				{
					value = CsvParser.DeserializeObject(row["Value"], field.FieldType,
					                                    QuantumDeserializer.FpDeserializer,
					                                    QuantumDeserializer.QuantumGameModePairDeserializer);
				}


				field.SetValue(config, value);
			}

			return (GameModeRotationConfig) config;
		}
	}
}