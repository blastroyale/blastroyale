using FirstLight.Game.Configs;
using FirstLightEditor.GoogleSheetImporter;

namespace FirstLight.Editor.SheetImporters
{
	/// <inheritdoc />
	public class PlayerLevelConfigsImporter : GoogleSheetConfigsImporter<PlayerLevelConfig, PlayerLevelConfigs>
	{
		/// <inheritdoc />
		public override string GoogleSheetUrl => "***REMOVED***/edit#gid=15200435";
	}
}