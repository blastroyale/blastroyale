using FirstLight.Game.Configs;
using Quantum;


namespace FirstLight.Editor.SheetImporters
{
	/// <inheritdoc />
	public class BotConfigsImporter : GoogleSheetQuantumConfigsImporter<QuantumBotConfig, BotConfigs>
	{
		public override string GoogleSheetUrl => "https://docs.google.com/spreadsheets/d/1TZuc8gOMgrN6nJWRFJymxmf2SR2QNyQfx0x-STtIN-M/edit#gid=374573795";
	}
}