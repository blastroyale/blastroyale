using System;

namespace FirstLight.UiService
{
	/// <summary>
	/// Presenters marked with this will be loaded synchronously by <see cref="UiService"/>
	/// </summary>
	public class LoadSynchronouslyAttribute : Attribute
	{
	}
}