using System;
using System.Collections.Generic;
using FirstLight.Game.Configs;

namespace FirstLight.Editor.SheetImporters
{
	/// <inheritdoc />
	public class TrophyRewardConfigImporter : GoogleSheetQuantumConfigsImporter<TrophyRewardConfig, TrophyRewardConfigs>
	{
		/// <inheritdoc />
		public override string GoogleSheetUrl => "***REMOVED***/edit#gid=1565053521";

		protected override TrophyRewardConfig Deserialize(Dictionary<string, string> data)
		{
			var config = base.Deserialize(data);

			var rewards = new int[data.Count - 1];
			for(int i = 1; i < data.Count; i++)
			{
				rewards[i-1] = int.Parse(data[i.ToString()]);
			}
			config.Rewards = rewards;
			return config;
		}
	}
}