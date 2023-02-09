using System;

namespace Quantum
{
	/// <summary>
	/// Returns true if the player's current weapon is the same firing mode as the given type
	/// </summary>
	[Serializable]
	[AssetObjectConfig(GenerateLinkingScripts = true, GenerateAssetCreateMenu = false, GenerateAssetResetMethod = false)]
	public partial class CheckFiringMode : HFSMDecision
	{
		public FiringMode DesiredValue;

		public override unsafe bool Decide(Frame frame, EntityRef entity, ref AIContext aiContext)
		{
			var config = frame.WeaponConfigs.GetConfig(frame.Unsafe.GetPointer<PlayerCharacter>(entity)->CurrentWeapon.GameId);
			return config.FiringMode == DesiredValue;
		}
	}
}