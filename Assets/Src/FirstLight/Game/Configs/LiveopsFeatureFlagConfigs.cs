using System;
using System.Collections.Generic;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using FirstLightServerSDK.Modules;
using UnityEngine;

namespace FirstLight.Game.Configs
{

	[Serializable]
	public struct LiveopsFeatureFlagConfig
	{
		public string PlayerSegment;
		public string FeatureFlag;
		public bool Enabled;

		public int UniqueIdentifier()
		{
			var hash = 23;
			hash += 23 * PlayerSegment.GetDeterministicHashCode();
			hash += 23 * FeatureFlag.GetDeterministicHashCode();
			hash += Enabled.GetHashCode();
			return hash;
		}
	}
	
	/// <summary>
	/// Scriptable Object tool to import the <seealso cref="LiveopsSegmentActionConfig"/> sheet data
	/// </summary>
	[IgnoreServerSerialization]
	[CreateAssetMenu(fileName = "LiveopsFeatureFlagConfigs", menuName = "ScriptableObjects/Configs/LiveopsFeatureFlagConfigs")]
	public class LiveopsFeatureFlagConfigs : ScriptableObject, IConfigsContainer<LiveopsFeatureFlagConfig>
	{
		[SerializeField] private List<LiveopsFeatureFlagConfig> _configs = new ();
		
		/// <inheritdoc />
		public List<LiveopsFeatureFlagConfig> Configs
		{
			get => _configs;
			set => _configs = value;
		}
	}
}