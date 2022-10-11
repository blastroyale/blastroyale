namespace FirstLight.UiService
{
	/// <summary>
	/// Hooks a VisualElement into the lifecycle of the panel it's on.
	///
	/// To use this just implement it on a custom VisualElement. The methods will
	/// be called automatically IF the VisualElement is added to a panel driven
	/// by <see cref="UiToolkitPresenterData{T}"/>
	/// </summary>
	public interface IVisualElementLifecycle
	{
		/// <summary>
		/// Called when runtime logic should be initialized (subscribing to events etc...)
		/// </summary>
		void RuntimeInit();

		/// <summary>
		/// Called when runtime logic should be cleaned up (unsubscribing to events etc...)
		/// </summary>
		void RuntimeCleanup();
	}
}