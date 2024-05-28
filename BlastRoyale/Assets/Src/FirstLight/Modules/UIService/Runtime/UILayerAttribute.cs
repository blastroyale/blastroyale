using System;

namespace FirstLight.UIService
{
	[AttributeUsage(AttributeTargets.Class)]
	public class UILayerAttribute : Attribute
	{
		public UILayer Layer { get; private set; }

		public UILayerAttribute(UILayer layer)
		{
			Layer = layer;
		}
	}
}