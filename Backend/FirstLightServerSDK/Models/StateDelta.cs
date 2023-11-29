using System;
using System.Collections.Generic;

namespace FirstLight.Server.SDK.Models
{
	/// <summary>
	/// Our approach to a minimal delta compression code to detect
	/// changes on server & client to ensure they are still in sync
	/// </summary>
	[Serializable]
	public class StateDelta
	{
		/// <summary>
		/// Map to the modified types and the resulting hash code after the modification
		/// </summary>
		public Dictionary<Type, int> ModifiedTypes = new Dictionary<Type, int>();

		public void TrackModification(object o)
		{
			ModifiedTypes[o.GetType()] = o.GetHashCode();
		}

		public void Merge(StateDelta delta)
		{
			foreach (var deltaModifiedType in delta.ModifiedTypes)
			{
				ModifiedTypes[deltaModifiedType.Key] = deltaModifiedType.Value;
			}
		}

		public IEnumerable<Type> GetModifiedTypes()
		{
			return ModifiedTypes.Keys;
		}

		public void Clear()
		{
			ModifiedTypes.Clear();
		}
	}
}