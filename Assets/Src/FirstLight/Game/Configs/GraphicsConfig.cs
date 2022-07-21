using System;
using System.Collections.Generic;
using UnityEngine;

namespace FirstLight.Game.Configs
{
	[CreateAssetMenu(fileName = "GraphicsConfig", menuName = "ScriptableObjects/Configs/GraphicsConfig")]
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