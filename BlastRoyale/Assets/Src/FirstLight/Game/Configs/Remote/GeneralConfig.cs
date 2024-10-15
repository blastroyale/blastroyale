using System;

namespace FirstLight.Game.Configs.Remote
{
	/// <summary>
	/// This is a temporary (probably permanent config), we had a config key for each  of those values, and they were accessed statically
	/// so I migrated all those values to this GeneralConfigs
	/// </summary>
	[Serializable]
	public class GeneralConfig
	{
		/// <summary>
		/// Shows or hides the BETA tag on home screen.
		/// </summary>
		public bool ShowBetaLabel; //true

		/// <summary>
		/// Enables or disables showing review prompt to a user
		/// </summary>
		public bool EnableReviewPrompt;

		/// <summary>
		/// The number of games played required to show a review prompt
		/// </summary>
		public int ReviewPromptGamesPlayedReq;

		/// <summary>
		/// If rooms should be created with a commit lock (only clients on the same commit
		/// can play together).
		/// </summary>
		public bool EnableCommitVersionLock;

		/// <summary>
		/// Shows the persistent bug report button.
		/// </summary>
		public bool ShowBugReportButton;

		/// <summary>
		/// Enables or disables deep linking feature.
		/// </summary>
		public bool EnableDeepLinking;

		/// <summary>
		/// Config cache lifespan in seconds
		/// </summary>
		public int ConfigCacheInSeconds;
	}
}