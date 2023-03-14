using System;
using System.Collections.Generic;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using UnityEngine;

namespace FirstLight.Game.Configs
{
	public enum LiveopsAction
	{
		ButtonDialog
	}
	
	[Serializable]
	[IgnoreServerSerialization]
	public struct LiveopsSegmentActionConfig
	{
		public int ActionIdentifier;
		public string PlayerSegment;
		public LiveopsAction Action;
		public List<string> ActionParameter;
	}
	
	/// <summary>
	/// Scriptable Object tool to import the <seealso cref="LiveopsSegmentActionConfig"/> sheet data
	/// </summary>
	
	[CreateAssetMenu(fileName = "LiveopsSegmentActionConfigs", menuName = "ScriptableObjects/Configs/LiveopsSegmentActionConfigs")]
	[IgnoreServerSerialization]
	public class LiveopsSegmentActionConfigs : ScriptableObject, IConfigsContainer<LiveopsSegmentActionConfig>
	{
		[SerializeField] private List<LiveopsSegmentActionConfig> _configs = new ();
		
		/// <inheritdoc />
		public List<LiveopsSegmentActionConfig> Configs
		{
			get => _configs;
			set => _configs = value;
		}
	}
}