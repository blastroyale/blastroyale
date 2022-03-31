using System;
using Photon.Deterministic;

namespace Quantum
{
	/// <summary>
	/// Calls Move on the <see cref="CharacterController3D"/>, to force
	/// physics calculations. Either this or <see cref="PlayerMoveAction"/>
	/// has to be running for physics (e.g. gravity) to work.
	/// </summary>
	[Serializable]
	[AssetObjectConfig(GenerateLinkingScripts = true, GenerateAssetCreateMenu = false,
	                   GenerateAssetResetMethod = false)]
	public unsafe class PlayerPhysicsAction : AIAction
	{
		/// <inheritdoc />
		public override void Update(Frame f, EntityRef e)
		{
			f.Unsafe.GetPointer<CharacterController3D>(e)->Move(f, e, FPVector3.Zero);
		}
	}
}