using System;
using System.Collections.Generic;
using FirstLight.Game.Data;
using FirstLight.Game.Utils;
using Photon.Deterministic;
using Quantum;
using UnityEngine;
using UnityEngine.Serialization;

namespace FirstLight.Game.Configs
{
	/// <summary>
	/// Flags which rewards shall be given at which tutorial step
	/// </summary>
	[Serializable]
	public struct TutorialRewardConfig
	{
		public TutorialSection Section;
		public List<uint> RewardIds;
	}

	[Serializable]
	public struct TutorialConfig
	{
		[SerializeField] public List<TutorialRewardConfig> Rewards;
		[SerializeField] public SimulationMatchConfig SecondMatch;
	}

	/// <summary>
	/// Scriptable Object tool to import the <seealso cref="TutorialRewardConfigs"/> sheet data
	/// </summary>
	[CreateAssetMenu(fileName = "TutorialConfigs",
		menuName = "ScriptableObjects/Configs/TutorialConfigs")]
	public class TutorialConfigs : ScriptableObject, ISingleConfigContainer<TutorialConfig>
	{
		[SerializeField] TutorialConfig _config;

		public TutorialConfig Config
		{
			get => _config;
			set => _config = value;
		}

		private void OnValidate()
		{
			_config.SecondMatch.ConfigId ??= "second-match-id";
		}
	}
}