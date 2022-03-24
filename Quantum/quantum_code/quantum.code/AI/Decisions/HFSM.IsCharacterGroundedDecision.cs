using System;

namespace Quantum
{
	/// <summary>
	///  Checks if the player is standing on the ground.
	/// </summary>
	[Serializable]
	[AssetObjectConfig(GenerateLinkingScripts = true, GenerateAssetCreateMenu = false,
	                   GenerateAssetResetMethod = false)]
	public partial class IsCharacterGroundedDecision : HFSMDecision
	{
		/// <inheritdoc />
		public override bool Decide(Frame f, EntityRef e)
		{
			return f.Get<CharacterController3D>(e).Grounded;
		}
	}
}