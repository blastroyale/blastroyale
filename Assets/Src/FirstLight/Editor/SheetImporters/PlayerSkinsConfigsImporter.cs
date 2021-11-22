using FirstLight.Game.Configs;
using FirstLightEditor.GoogleSheetImporter;

namespace FirstLight.Editor.SheetImporters
{
	/// <inheritdoc />
	public class PlayerSkinConfigsImporter : GoogleSheetQuantumConfigsImporter<PlayerSkinConfig, PlayerSkinConfigs>
	{
		/// <inheritdoc />
		public override string GoogleSheetUrl => "***REMOVED***/edit#gid=1363678024";
	}
}