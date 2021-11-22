using FirstLight.AssetImporter;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Configs.AssetConfigs
{
	/// <summary>
	/// Scriptable object containing all the quantum's assets configurations
	/// </summary>
	[CreateAssetMenu(fileName = "QuantumPrototypeAssetConfigs", menuName = "ScriptableObjects/AssetConfigs/QuantumPrototypeAssetConfigs")]
	public class QuantumPrototypeAssetConfigs : AssetConfigsScriptableObject<GameId, GameObject>
	{
	}
}