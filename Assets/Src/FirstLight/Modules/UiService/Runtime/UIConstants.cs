namespace FirstLight.Game.UIElements
{
	/// <summary>
	/// Constants related to UI Toolkit (classes / identifiers...).
	/// </summary>
	public static class UIConstants
	{
		/// <summary>
		/// The name that should be set to the root VisualElement in a screen.
		/// </summary>
		public const string ID_ROOT = "root";

		/// <summary>
		/// This class is toggled on the root VisualElement when opening / closing screens.
		/// </summary>
		public const string CLASS_HIDDEN = "hidden";

		/// <summary>
		/// Adds the forward click SFX to the element (on pointer down).
		/// </summary>
		public const string SFX_CLICK_FORWARDS = "sfx-click-forwards";
		
		/// <summary>
		/// Adds the backwards click SFX to the element (on pointer down).
		/// </summary>
		public const string SFX_CLICK_BACKWARDS = "sfx-click-backwards";

		/// <summary>
		/// Class to use labels to display the player name, this is usually used bellow characters
		/// </summary>
		public const string USS_PLAYER_LABEL = "player-name";
	}
}