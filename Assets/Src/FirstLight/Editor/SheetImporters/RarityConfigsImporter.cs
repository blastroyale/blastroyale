using FirstLight.Game.Configs;

namespace FirstLight.Editor.SheetImporters
{
	/// <inheritdoc />
	public class RarityConfigsImporter : GoogleSheetQuantumConfigsImporter<RarityConfig, RarityConfigs>
	{
		/// <inheritdoc />
		public override string GoogleSheetUrl => "https://docs.google.com/spreadsheets/d/1TZuc8gOMgrN6nJWRFJymxmf2SR2QNyQfx0x-STtIN-M/edit#gid=1743383203";
	}
}