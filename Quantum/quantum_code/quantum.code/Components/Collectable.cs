using Photon.Deterministic;

namespace Quantum
{
	public unsafe partial struct Collectable
	{

		/// <summary>
		/// Drops a collectable of the given <paramref name="gameId"/> in the given <paramref name="position"/>
		/// </summary>
		public static void DropCollectable(Frame f, GameId gameId, FPVector3 position, int angleDropStep, bool isWeapon)
		{
			var angleStep = FPVector2.Rotate(FPVector2.Left, FP.PiTimes2 * angleDropStep / 5);
			var dropPosition = (angleStep * Constants.DROP_OFFSET_RADIUS).XOY + position;
			
			QuantumHelpers.TryFindPosOnNavMesh(f, EntityRef.None, dropPosition, out FPVector3 newPosition);

			if (isWeapon)
			{
				var configWeapon = f.WeaponConfigs.GetConfig(gameId);
				var entityWeapon = f.Create(f.FindAsset<EntityPrototype>(configWeapon.AssetRef.Id));
				f.Unsafe.GetPointer<WeaponCollectable>(entityWeapon)->Init(f, entityWeapon, newPosition, 
				                                                           FPQuaternion.Identity, configWeapon);
			}
			else
			{
				var configConsumable = f.ConsumableConfigs.GetConfig(gameId);
				var entityConsumable = f.Create(f.FindAsset<EntityPrototype>(configConsumable.AssetRef.Id));
				f.Unsafe.GetPointer<Consumable>(entityConsumable)->Init(f, entityConsumable, newPosition, 
				                                                        FPQuaternion.Identity, configConsumable);
			}
		}
		
		/// <summary>
		/// Checks if the given <paramref name="playerRef"/> is collecting the collectable
		/// </summary>
		public bool IsCollecting(PlayerRef playerRef)
		{
			return CollectorsEndTime[playerRef] > FP._0;
		}
	}
}