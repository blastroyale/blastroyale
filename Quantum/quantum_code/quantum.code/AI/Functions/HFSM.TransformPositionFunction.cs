using System;
using Photon.Deterministic;

namespace Quantum
{
	/// <summary>
	/// Fetches the current position of the <see cref="Transform3D"/> of the
	/// current entity.
	/// </summary>
	[Serializable]
	[AssetObjectConfig(GenerateLinkingScripts = true, GenerateAssetCreateMenu = false,
	                   GenerateAssetResetMethod = false)]
	public class TransformPositionFunction : AIFunction<FPVector3>
	{
		public override FPVector3 Execute(Frame f, EntityRef e)
		{
			return f.Get<Transform3D>(e).Position;
		}
	}
}