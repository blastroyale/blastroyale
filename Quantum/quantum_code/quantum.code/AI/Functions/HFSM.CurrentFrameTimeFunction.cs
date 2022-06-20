using System;
using Photon.Deterministic;

namespace Quantum
{
	/// <summary>
	/// Fetches the current frame time
	/// </summary>
	[Serializable]
	[AssetObjectConfig(GenerateLinkingScripts = true, GenerateAssetCreateMenu = false,
	                   GenerateAssetResetMethod = false)]
	public class CurrentFrameTimeFunction : AIFunction<FP>
	{
		/// <inheritdoc />
		public override FP Execute(Frame f, EntityRef e)
		{
			return f.Time;
		}
	}
}