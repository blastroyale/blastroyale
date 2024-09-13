using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine.Serialization;

namespace FirstLight.Game.Configs.Remote
{
	[Serializable]
	public class TeamSizeConfig
	{
		public string QueueName;
		public int QueueTimeoutTimeInSeconds;
		public string IconSpriteClass;
		public string EventImageModifierByTeam;
	}

	public class MatchmakingQueuesConfig : Dictionary<string, TeamSizeConfig>
	{
	}
}