using Quantum.Systems.Collectables;

namespace Quantum.Systems
{
	/// <inheritdoc />
	/// <remarks>
	/// Implementation for the <see cref="WeaponCollectable"/> to be collected in the game.
	/// </remarks>
	public unsafe class WeaponCollectableSystem : CollectableSystemBase
	{
		protected override void OnCollectablePicked(Frame f, EntityRef e, EntityRef playerEntity, PlayerRef player, Collectable collectable)
		{
			var playerCharacter = f.Unsafe.GetPointer<PlayerCharacter>(playerEntity);
			playerCharacter->SetWeapon(f, playerEntity, collectable.GameId, ItemRarity.Common, 1);
			
			f.Add<EntityDestroyer>(e);
		}

		protected override bool IsCorrectSystem(Frame f, EntityRef e, Collectable collectable)
		{
			return f.Has<WeaponCollectable>(e);
		}
	}
}