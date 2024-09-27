using System;
using System.Collections.Generic;
using FirstLight.GoogleSheetImporter;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Configs
{
	[Serializable]
	[CreateAssetMenu(fileName = "BuffConfigs", menuName = "ScriptableObjects/Configs/BuffConfigs")]
	public class BuffConfigs : QuantumBuffConfigsAsset, IConfigsContainer<BuffConfig>
	{
		public List<BuffConfig> Configs
		{
			get => Settings.Buffs;
			set => Settings.Buffs = value;
		}
	}
}