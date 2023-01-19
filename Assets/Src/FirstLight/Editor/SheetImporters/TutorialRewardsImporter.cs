using FirstLight.Game.Configs;
using FirstLightEditor.GoogleSheetImporter;

namespace FirstLight.Editor.SheetImporters
{
	/// <inheritdoc />
	public class TutorialRewardsImporter : GoogleSheetConfigsImporter<TutorialRewardConfig, TutorialRewardConfigs>
	{
		/// <inheritdoc />
		public override string GoogleSheetUrl => "***REMOVED***/edit#gid=1345410922";
	}
}