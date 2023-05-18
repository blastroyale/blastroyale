using UnityEngine.UIElements;

namespace FirstLight.UiService
{
	public abstract class UIView
	{
		/// <summary>
		/// The element this view is attached to.
		/// </summary>
		public VisualElement Element { get; set; }

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