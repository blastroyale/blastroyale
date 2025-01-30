using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine.Serialization;

namespace FirstLight.Game.Configs.Remote
{
	public class PlayfabMatchmakingConfig
	{
		public string QueueName;
		public int QueueTimeoutTimeInSeconds;
		public bool FailsOnTimeout;
	}

	[Serializable]
	public class TeamSizeConfig : PlayfabMatchmakingConfig
	{
		public string IconSpriteClass;
		public string EventImageModifierByTeam;
	}

	public class MatchmakingQueuesConfig : Dictionary<string, TeamSizeConfig>
	{
	}
}