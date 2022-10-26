using UnityEngine.UIElements;

namespace FirstLight.UiService
{
	public interface IUIView
	{
		/// <summary>
		/// Called once, the first time the presenter screen is opened.
		/// </summary>
		/// <param name="element"></param>
		void Attached(VisualElement element);

		/// <summary>
		/// Called when runtime logic should be initialized (subscribing to events etc...)
		/// </summary>
		void SubscribeToEvents();

		/// <summary>
		/// Called when runtime logic should be initialized (subscribing to events etc...)
		/// </summary>
		void UnsubscribeFromEvents();
	}
}