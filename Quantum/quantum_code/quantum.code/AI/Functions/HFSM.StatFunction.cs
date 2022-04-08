using Photon.Deterministic;
using System;

namespace Quantum
{
	/// <summary>
	/// Returns a value of a specified <see cref="StatType"/> of the current entity.
	/// </summary>
	[Serializable]
	[AssetObjectConfig(GenerateLinkingScripts = true, GenerateAssetCreateMenu = false,
					   GenerateAssetResetMethod = false)]
	public class StatFunction : AIFunction<FP>
	{
		public StatType Stat;

		/// <inheritdoc />
		public override FP Execute(Frame f, EntityRef e)
		{
			return f.Get<Stats>(e).GetStatData(Stat).StatValue;
		}
	}
}
