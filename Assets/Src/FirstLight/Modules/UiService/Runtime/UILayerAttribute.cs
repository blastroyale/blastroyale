using System;
using FirstLight.UIService;

namespace FirstLight.Modules.UIService.Runtime
{
	[AttributeUsage(AttributeTargets.Class)]
	public class UILayerAttribute : Attribute
	{
		public FirstLight.UIService.UIService.UILayer Layer { get; private set; }

		public UILayerAttribute(FirstLight.UIService.UIService.UILayer layer)
		{
			Layer = layer;
		}
	}
}