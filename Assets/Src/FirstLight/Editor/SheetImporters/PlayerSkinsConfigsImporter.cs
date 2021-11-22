using FirstLight.Game.Configs;
using FirstLightEditor.GoogleSheetImporter;

namespace FirstLight.Editor.SheetImporters
{
	/// <inheritdoc />
	public class PlayerSkinConfigsImporter : GoogleSheetQuantumConfigsImporter<PlayerSkinConfig, PlayerSkinConfigs>
	{
		/// <inheritdoc />
		public override string GoogleSheetUrl => "https://docs.google.com/spreadsheets/d/1TZuc8gOMgrN6nJWRFJymxmf2SR2QNyQfx0x-STtIN-M/edit#gid=1363678024";
	}
}