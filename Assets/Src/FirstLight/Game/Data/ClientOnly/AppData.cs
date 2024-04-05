using System;
using System.Collections.Generic;
using FirstLight.Game.Configs;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using Quantum;
using Environment = FirstLight.Game.Services.Environment;

namespace FirstLight.Game.Data
{
	/// <summary>
	/// Represents user custom game preferences, set in last custom game played.
	/// </summary>
	[Serializable]
	public class CustomGameOptions
	{
		public List<string> Mutators = new();
		public int GameModeIndex;
		public int MapIndex;
		public int BotDifficulty;
		public string WeaponLimiter;
		public int PlayersNumber;
		public int TeamSize;
	}
	
	/// <summary>
	/// Contains all the data in the scope of the Game's App
	/// </summary>
	[Serializable]
	public class AppData
	{
		public string DisplayName;
		public string PlayerId;
		public string AvatarUrl;
		public DateTime FirstLoginTime;
		public DateTime LastLoginTime;
		public DateTime LoginTime;
		public bool IsFirstSession;

		public Environment LastEnvironment;

		// Moved to AccountData, this is here for backwards compatibility
		[Obsolete]
		public string DeviceId;
		
		[Obsolete]
		public string LastLoginEmail;
		
		public DateTime GameReviewDate;
		
		public FrameSnapshot LastCapturedFrameSnapshot;
		

		public GameModeRotationConfig.GameModeEntry LastGameMode;
		public CustomGameOptions LastCustomGameOptions = new();
		
		[NonSerialized] public Dictionary<string, string> TitleData;
	}


}