using System;
using System.Collections.Generic;
using FirstLight.Game.Configs;
using FirstLightEditor.GoogleSheetImporter;
using FirstLightServerSDK.Modules;

namespace FirstLight.Editor.SheetImporters
{
	/// <inheritdoc />
	public class TrophyRewardConfigImporter : GoogleSheetQuantumConfigsImporter<TrophyRewardConfig, TrophyRwardConfigs>
	{
		/// <inheritdoc />
		public override string GoogleSheetUrl => "***REMOVED***/edit#gid=1565053521";

	}
}