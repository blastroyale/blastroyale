using System;
using System.Collections.Generic;
using FirstLight.Game.Configs;
using FirstLightServerSDK.Modules;

namespace FirstLight.Editor.SheetImporters
{
	/// <inheritdoc />
	public class TrophyRewardConfigImporter : GoogleSheetQuantumConfigsImporter<TrophyRewardConfig, TrophyRewardConfigs>
	{
		/// <inheritdoc />
		public override string GoogleSheetUrl => "***REMOVED***/edit#gid=462188583";

		protected override TrophyRewardConfig Deserialize(Dictionary<string, string> data)
		{
			var config = base.Deserialize(data);
			var hashCode = String.Concat(config.Placement).GetDeterministicHashCode();
			
			return config;
		}
	}
}