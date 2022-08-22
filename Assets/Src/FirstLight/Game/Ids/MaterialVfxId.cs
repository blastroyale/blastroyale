using System.Collections.Generic;
using Quantum;

namespace FirstLight.Game.Ids
{
	public enum MaterialVfxId
	{
		Invisibility,
		Dissolve,
		Invincible,
		TOTAL
	}
	
	/// <summary>
	/// Avoids boxing for Dictionary
	/// </summary>
	public class MaterialVfxIdComparer : IEqualityComparer<MaterialVfxId>
	{
		public bool Equals(MaterialVfxId x, MaterialVfxId y)
		{
			return x == y;
		}

		public int GetHashCode(MaterialVfxId obj)
		{
			return (int)obj;
		}
	}

	public static class MaterialVfxIdLookup
	{
		public static bool TryGetVfx(this StatusModifierType modifier, out MaterialVfxId vfx)
		{
			return _modifiers.TryGetValue(modifier, out vfx);
		}
		
		private static readonly Dictionary<StatusModifierType, MaterialVfxId> _modifiers =
			new Dictionary<StatusModifierType, MaterialVfxId>
			{
				{StatusModifierType.Invisibility, MaterialVfxId.Invisibility},
				{StatusModifierType.Immunity, MaterialVfxId.Invincible},
			};
	}
}