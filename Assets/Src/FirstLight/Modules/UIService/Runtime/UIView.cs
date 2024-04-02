using UnityEngine.UIElements;

namespace FirstLight.UIService
{
	public class UIView
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
		/// <param name="reload"></param>
		public virtual void OnScreenOpen(bool reload)
		{
		}

		/// <summary>
		/// Called when runtime logic should be initialized (subscribing to events etc...)
		/// </summary>
		public virtual void OnScreenClose()
		{
		}
	}
}