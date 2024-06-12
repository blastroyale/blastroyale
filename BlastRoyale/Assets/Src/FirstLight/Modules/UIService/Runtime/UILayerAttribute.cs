using System;

namespace FirstLight.UIService
{
	[AttributeUsage(AttributeTargets.Class)]
	public class UILayerAttribute : Attribute
	{
		public UILayer Layer { get; private set; }

		public bool AutoClose { get; private set; }

		/// <summary>
		/// Attaches layer information to a UIPresenter
		/// </summary>
		/// <param name="layer">The UI layer / sorting order to use</param>
		/// <param name="autoClose">If true the screen is automatically closed when a new one on the same layer is opened.</param>
		public UILayerAttribute(UILayer layer, bool autoClose = true)
		{
			Layer = layer;
			AutoClose = autoClose;
		}
	}
}