using FirstLight.Game.Configs;
using FirstLightEditor.GoogleSheetImporter;

namespace FirstLight.Editor.SheetImporters
{
	/// <inheritdoc />
	public class PlayerLevelConfigsImporter : GoogleSheetConfigsImporter<PlayerLevelConfig, PlayerLevelConfigs>
	{
		/// <inheritdoc />
		public override string GoogleSheetUrl => "https://docs.google.com/spreadsheets/d/1TZuc8gOMgrN6nJWRFJymxmf2SR2QNyQfx0x-STtIN-M/edit#gid=15200435";
	}
}