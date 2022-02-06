using System;

namespace Quantum
{
	/// <summary>
	/// This action processes when the <see cref="PlayerCharacter"/> <see cref="Weapon"/> is empty
	/// </summary>
	[Serializable]
	[AssetObjectConfig(GenerateLinkingScripts = true, GenerateAssetCreateMenu = false, GenerateAssetResetMethod = false)]
	public class PlayerAmmoEmptyAction : AIAction
	{
		/// <inheritdoc />
		public override void Update(Frame f, EntityRef e)
		{
			var player = f.Get<PlayerCharacter>(e).Player;
			
			f.Events.OnLocalPlayerAmmoEmpty(player, e);
			f.Events.OnPlayerAmmoEmpty(player, e);
		}
	}
}