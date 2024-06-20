using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using FirstLight.Game.Configs;
using FirstLightServerSDK.Modules;
using FirstLight.Game.Utils;
using UnityEngine;

namespace FirstLight.Editor.SheetImporters
{
	/// <inheritdoc />
	public class TrophyRewardConfigImporter : GoogleSheetQuantumConfigsImporter<TrophyRewardConfig, TrophyRewardConfigs>
	{
		/// <inheritdoc />
		public override string GoogleSheetUrl => "***REMOVED***/edit#gid=190590325";

		protected override TrophyRewardConfig Deserialize(Dictionary<string, string> data)
		{
			var config = base.Deserialize(data);
			var hashCode = string.Concat(config.Placement, config.TeamSize).GetDeterministicHashCode();
			var rewards = new SerializedDictionary<int, int>();

			foreach (var key in data.Keys)
			{
				if(int.TryParse(key, out _))
				{
					rewards.Add(int.Parse(key), int.Parse(data[key]));
				}
			}

			config.BracketReward = rewards;
			config.MatchRewardId = hashCode;

			return config;
		}
	}
}