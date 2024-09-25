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
	public unsafe class TransformPositionFunction : AIFunction<FPVector3>
	{
		/// <inheritdoc />
		public override FPVector3 Execute(Frame f, EntityRef e, ref AIContext aiContext)
		{
			Log.Warn("Using reposition function !!");
			return f.Unsafe.GetPointer<Transform2D>(e)->Position.XOY;
		}
	}
}