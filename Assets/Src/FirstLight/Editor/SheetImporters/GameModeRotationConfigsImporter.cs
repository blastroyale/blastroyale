using System;
using FirstLight.Editor.EditorTools;
using FirstLight.Game.Configs;
using FirstLightEditor.GoogleSheetImporter;

namespace FirstLight.Editor.SheetImporters
{
	public class GameModeRotationConfigsImporter : GoogleSheetSingleConfigSubListImporter<GameModeRotationConfig,
		GameModeRotationConfigs>
	{
		public override string GoogleSheetUrl =>
			"***REMOVED***/edit#gid=405313914";

		protected override Func<string, Type, object>[] GetDeserializers()
		{
			return new Func<string, Type, object>[]
			{
				QuantumDeserializer.FpDeserializer,
				QuantumDeserializer.QuantumGameModePairDeserializer
			};
		}
	}
}