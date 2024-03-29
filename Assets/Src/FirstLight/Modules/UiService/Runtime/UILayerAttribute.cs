using System;
using FirstLight.UIService;

namespace FirstLight.Modules.UIService.Runtime
{
	[AttributeUsage(AttributeTargets.Class)]
	public class UILayerAttribute : Attribute
	{
		public UIService2.UILayer Layer { get; private set; }

		public UILayerAttribute(UIService2.UILayer layer)
		{
			Layer = layer;
		}
	}
}