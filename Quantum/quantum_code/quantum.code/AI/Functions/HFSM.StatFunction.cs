using Photon.Deterministic;
using System;

namespace Quantum
{
	/// <summary>
	/// Fetches the current position of the <see cref="Transform3D"/> of the
	/// current entity.
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
