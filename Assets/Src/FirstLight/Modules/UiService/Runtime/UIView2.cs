using UnityEngine.UIElements;

namespace FirstLight.UIService
{
	public class UIView2
	{
		/// <summary>
		/// The element this view is attached to.
		/// </summary>
		public VisualElement Element { get; private set; }

		/// <summary>
		/// Called once, the first time the presenter screen is opened.
		/// </summary>
		/// <param name="element"></param>
		public virtual void Attached(VisualElement element)
		{
			Element = element;
		}

		/// <summary>
		/// Called when runtime logic should be initialized (subscribing to events etc...)
		/// </summary>
		public virtual void SubscribeToEvents()
		{
		}

		/// <summary>
		/// Called when runtime logic should be initialized (subscribing to events etc...)
		/// </summary>
		public virtual void UnsubscribeFromEvents()
		{
		}
	}
}