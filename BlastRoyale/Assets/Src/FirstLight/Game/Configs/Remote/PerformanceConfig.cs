using System;
using Newtonsoft.Json;

namespace FirstLight.Game.Configs.Remote
{
	[Serializable]
	public class PerformanceConfig
	{
		public int MidMinMemory = 4;
		public int HighMinMemory = 12;	
		public int MidMinCpu = 2;
		public int HighMinCpu = 3;
		public float HighMinBattery = 0.25f;
		public int HighMinGpuMemory = 2;

		public override string ToString()
		{
			return JsonConvert.SerializeObject(this);
		}
	}
}