using System;
using FirstLightServerSDK.Modules;
using UnityEngine.Serialization;

namespace FirstLight.Game.Data
{
	/// <summary>
	/// Represents the tutorial data state of this player
	/// </summary>
	[Serializable]
	public class ContentCreatorData
	{
		public string SupportingCreatorCode = string.Empty;

		public override int GetHashCode()
		{
			int hash = 17;
			hash = hash * 23 + SupportingCreatorCode.GetDeterministicHashCode();
			return hash;
		}
	}
}