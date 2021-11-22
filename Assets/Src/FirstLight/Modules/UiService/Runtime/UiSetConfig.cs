using System;
using System.Collections.Generic;

// ReSharper disable CheckNamespace

namespace FirstLight.UiService
{
	/// <summary>
	/// Represents a configuration set of UIs that can be managed together in the <seealso cref="UiService"/>
	/// This can be helpful for a UI combo set that are always visible together (ex: player Hud with currency & settings)
	/// </summary>
	[Serializable]
	public struct UiSetConfig
	{
		public int SetId;
		public IReadOnlyList<Type> UiConfigsType;
	}
}