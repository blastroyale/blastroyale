using FirstLight.GoogleSheetImporter;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Configs
{
	/// <summary>
	/// Scriptable Object tool to import the <seealso cref="QuantumGameConfig"/> sheet data
	/// </summary>
	[CreateAssetMenu(fileName = "GameConfigs", menuName = "ScriptableObjects/Configs/GameConfigs")]
	public class GameConfigs : QuantumGameConfigsAsset, ISingleConfigContainer<QuantumGameConfig>
	{
		public QuantumGameConfig Config
		{
			get => Settings.QuantumConfig;
			set => Settings.QuantumConfig = value;
		}
	}
}