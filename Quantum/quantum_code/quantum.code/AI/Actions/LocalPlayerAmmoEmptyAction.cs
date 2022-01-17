using System;

namespace Quantum
{
	/// <summary>
	/// This action processes when the <see cref="PlayerCharacter"/> <see cref="Weapon"/> is empty
	/// </summary>
	[Serializable]
	[AssetObjectConfig(GenerateLinkingScripts = true, GenerateAssetCreateMenu = false, GenerateAssetResetMethod = false)]
	public class LocalPlayerAmmoEmptyAction : AIAction
	{
		/// <inheritdoc />
		public override void Update(Frame f, EntityRef e)
		{
			f.Events.OnLocalPlayerAmmoEmpty(f.Get<PlayerCharacter>(e).Player, e);
		}
	}
}