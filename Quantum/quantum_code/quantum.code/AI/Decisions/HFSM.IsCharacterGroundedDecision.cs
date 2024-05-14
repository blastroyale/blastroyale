using System;

namespace Quantum
{
	/// <summary>
	///  Checks if the player is standing on the ground.
	/// </summary>
	[Serializable]
	[AssetObjectConfig(GenerateLinkingScripts = true, GenerateAssetCreateMenu = false,
	                   GenerateAssetResetMethod = false)]
	public unsafe partial class IsCharacterGroundedDecision : HFSMDecision
	{
		/// <inheritdoc />
		public override bool Decide(Frame f, EntityRef e, ref AIContext aiContext)
		{
			// We have to use Grounded check here because it is necessary in the step where character drops to the ground after flying in
			// In ALL other cases we should check IsSkydiving blackboard variable instead of Grounded
			return f.Unsafe.GetPointer<CharacterController3D>(e)->Grounded;
		}
	}
}