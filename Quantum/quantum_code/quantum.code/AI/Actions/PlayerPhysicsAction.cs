using System;
using Photon.Deterministic;

namespace Quantum
{
	/// <summary>
	/// Calls Move on the <see cref="CharacterController3D"/>, to force
	/// physics calculations. THis has to be running for physics (e.g. gravity) to work.
	/// </summary>
	[Serializable]
	[AssetObjectConfig(GenerateLinkingScripts = true, GenerateAssetCreateMenu = false,
	                   GenerateAssetResetMethod = false)]
	public unsafe class PlayerPhysicsAction : AIAction
	{
		/// <inheritdoc />
		public override void Update(Frame f, EntityRef e, ref AIContext aiContext)
		{
		}
	}
}