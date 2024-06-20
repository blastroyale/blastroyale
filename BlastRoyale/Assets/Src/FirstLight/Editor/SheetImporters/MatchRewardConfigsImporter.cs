using System;
using System.Collections.Generic;
using FirstLight.Game.Configs;
using FirstLightServerSDK.Modules;

namespace FirstLight.Editor.SheetImporters
{
	/// <inheritdoc />
	public class MatchRewardConfigsImporter : GoogleSheetQuantumConfigsImporter<MatchRewardConfig, MatchRewardConfigs>
	{
		/// <inheritdoc />
		public override string GoogleSheetUrl => "***REMOVED***/edit#gid=462188583";

		protected override MatchRewardConfig Deserialize(Dictionary<string, string> data)
		{
			var config = base.Deserialize(data);
			var hashCode = String.Concat(config.Placement, config.TeamSize).GetDeterministicHashCode();
			
			config.MatchRewardId = hashCode;
			
			return config;
		}
	}
}