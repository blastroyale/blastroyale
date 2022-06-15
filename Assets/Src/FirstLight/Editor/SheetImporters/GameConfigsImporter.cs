using System.Collections.Generic;
using FirstLight.Editor.EditorTools;
using FirstLight.GoogleSheetImporter;
using FirstLightEditor.GoogleSheetImporter;
using Quantum;
using GameConfigs = FirstLight.Game.Configs.GameConfigs;

namespace FirstLight.Editor.SheetImporters
{
	/// <inheritdoc />
	public class GameConfigsImporter : GoogleSheetSingleConfigImporter<QuantumGameConfig, GameConfigs>
	{
		/// <inheritdoc />
		public override string GoogleSheetUrl => "***REMOVED***/edit#gid=1302509779";

		/// <inheritdoc />
		protected override QuantumGameConfig Deserialize(List<Dictionary<string, string>> data)
		{
			var config = new QuantumGameConfig() as object;
			var type = typeof(QuantumGameConfig);

			foreach (var row in data)
			{
				var field = type.GetField(row["Key"]);

				if (field == null)
				{
					continue;
				}
				
				var value = CsvParser.DeserializeObject(row["Value"], field.FieldType, 
				                                        QuantumDeserializer.FpDeserializer, 
				                                        QuantumDeserializer.QuantumGameModePairDeserializer);
				
				field.SetValue(config, value);
			}
			
			return (QuantumGameConfig) config;
		}
	}
}