using System;
using System.Collections.Generic;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using UnityEngine;

namespace FirstLight.Game.Configs
{
	[CreateAssetMenu(fileName = "GraphicsConfig", menuName = "ScriptableObjects/Configs/GraphicsConfig")]
	[IgnoreServerSerialization]
	public class GraphicsConfig : ScriptableObject
	{
		[Serializable]
		public struct DetailLevelConf
		{
			public DetailLevel Name;
			public int DetailLevelIndex;
			public int Fps;
		}
		public enum DetailLevel
		{
			High, Medium, Low
		}

		public List<DetailLevelConf> DetailLevels;
	}
	
	
}