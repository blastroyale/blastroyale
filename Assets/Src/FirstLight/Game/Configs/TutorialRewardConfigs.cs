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
	
	/// <summary>
	/// Scriptable Object tool to import the <seealso cref="TutorialRewardConfigs"/> sheet data
	/// </summary>
	[CreateAssetMenu(fileName = "BattlePassRewardConfigs",
	                 menuName = "ScriptableObjects/Configs/TutorialRewardConfigs")]
	public class TutorialRewardConfigs : ScriptableObject, IConfigsContainer<TutorialRewardConfig>
	{
		[SerializeField] private List<TutorialRewardConfig> _configs;

		public List<TutorialRewardConfig> Configs
		{
			get => _configs;
			set => _configs = value;
		}
	}
}