using System;
using System.Collections.Generic;
using UnityEngine.Serialization;

namespace FirstLight.Game.Configs.Remote
{
	[Serializable]
	public class GameMaintenanceConfig
	{
		public bool Maintenance;
		public string MaintenanceMessage;
		public string AllowedVersion;
		public string VersionBlockMessage;
	}
}