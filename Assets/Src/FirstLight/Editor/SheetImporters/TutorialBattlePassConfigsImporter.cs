using System;
using FirstLight.Editor.EditorTools;
using FirstLight.Game.Configs;
using FirstLightEditor.GoogleSheetImporter;

namespace FirstLight.Editor.SheetImporters
{
	public class TutorialBattlePassConfigsImporter : GoogleSheetSingleConfigSubListImporter<TutorialBattlePassConfig, TutorialBattlePassConfigs>
	{
		public override string GoogleSheetUrl =>
			"***REMOVED***/edit#gid=1636886981";

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