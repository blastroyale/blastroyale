namespace FirstLight.UIService
{
	/// <summary>
	/// The layers that the UI can be on. These are not serialized, so they can be changed at any time.
	/// </summary>
	public enum UILayer
	{
		Background = -1,
		Default = 0,
		Popup = 1,

		TutorialOverlay = 2,

		Loading = 5,
		Foreground = 6,
		
		Notifications = 7,

		LegacyVFXHack = 10,
		
		Debug = 100,
	}
}