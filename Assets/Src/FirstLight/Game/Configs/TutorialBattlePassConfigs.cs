using System;
using System.Collections.Generic;
using FirstLight.Game.Utils;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Configs
{
	[Serializable]
	public struct TutorialBattlePassConfig
	{
		public uint CurrentSeason;
		public uint DefaultPointsPerLevel;
		public List<BattlePassConfig.BattlePassLevel> Levels;
		

		public BattlePassConfig ToBattlePassConfig()
		{
			return new BattlePassConfig()
			{
				CurrentSeason = this.CurrentSeason,
				DefaultPointsPerLevel = DefaultPointsPerLevel,
				Levels = this.Levels,
			};
		}
	}

	/// <summary>
	/// Scriptable Object tool to import the <seealso cref="TutorialBattlePassConfigs"/> sheet data
	/// </summary>
	[CreateAssetMenu(fileName = "TutorialBattlePassConfigs", menuName = "ScriptableObjects/Configs/TutorialBattlePassConfigs")]
	public class TutorialBattlePassConfigs : ScriptableObject, ISingleConfigContainer<TutorialBattlePassConfig>
	{
		[SerializeField] private TutorialBattlePassConfig _config;

		public TutorialBattlePassConfig Config
		{
			get => _config;
			set => _config = value;
		}
	}
}