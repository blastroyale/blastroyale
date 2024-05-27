using System.Collections.Generic;
using System.Collections.ObjectModel;
using FirstLight.AssetImporter;
using FirstLight.Game.Ids;
using Quantum;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace FirstLight.Game.Configs.AssetConfigs
{
	/// <summary>
	/// Scriptable object containing all the special move large and small icon sprite ids.
	/// </summary>
	[CreateAssetMenu(fileName = "SpecialMoveAssetConfigs", menuName = "ScriptableObjects/AssetConfigs/SpecialMoveAssetConfigs")]
	public class SpecialMoveAssetConfigs : AssetConfigsScriptableObject<SpecialType, Sprite>
	{
	}
}