using System.Collections.Generic;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Configs
{
	[CreateAssetMenu(fileName = "BuffConfigs", menuName = "ScriptableObjects/Configs/BuffConfigs")]
	public class BuffConfigs : QuantumBuffConfigsAsset, ISingleConfigContainer<QuantumBuffConfigs>
	{
		public QuantumBuffConfigs Config
		{
			get => Settings;
			set => Settings = value;
		}
	}
}