using Sirenix.OdinInspector;
using UnityEngine;

namespace FirstLight.Game.Utils
{
	/// <summary>
	/// This stores the current environment used by the game. This asset will have its value set at build time.
	/// </summary>
	public class FLEnvironmentAsset : ScriptableObject
	{
		[InfoBox("Do not edit this manually. This value is set at build time.")] [DisableIf("@true")]
		// ReSharper disable once InconsistentNaming
		public string EnvironmentName = "development";
	}
}