using System;
using Photon.Deterministic;

namespace Quantum.Systems
{
	/// <summary>
	/// This system is responsible for handling all the stages of an AirDrop.
	/// </summary>
	public unsafe class AirDropSystem : SystemMainThreadFilter<AirDropSystem.AirDropFilter>
	{
		public struct AirDropFilter
		{
			public EntityRef Entity;
			public AirDrop* AirDrop;
		}

		public override void Update(Frame f, ref AirDropFilter filter)
		{
			var drop = filter.AirDrop;

			switch (drop->Stage)
			{
				case AirDropStage.Waiting:
					if (f.Time >= drop->StartTime + drop->Delay)
					{
						if (f.Context.GameModeConfig.AirdropNearPlayer)
						{
							CenterAirdropOnLocalPlayer(f, ref filter);
						}
						drop->Stage = AirDropStage.Announcing;
						f.Events.OnAirDropDropped(filter.Entity, f.Get<AirDrop>(filter.Entity));
						f.Unsafe.GetPointer<PhysicsCollider2D>(filter.Entity)->Enabled = false;
					}

					break;
				case AirDropStage.Announcing:
					var transform = f.Unsafe.GetPointer<Transform2D>(filter.Entity);

					var modifier = FP._1 - (f.Time - drop->StartTime - drop->Delay) / (drop->Duration);
					transform->Position = drop->Position + FPVector2.Up * f.GameConfig.AirdropHeight * modifier;

					var airdrop = f.Unsafe.GetPointer<AirDrop>(filter.Entity);
					if (f.Time >= drop->StartTime + drop->Delay + drop->Duration)
					{
						drop->Stage = AirDropStage.Dropped;
						var chestEntity = ChestSystem.SpawnChest(f, drop->Chest, drop->Position);
						f.Add(chestEntity, *drop);
						f.Events.OnAirDropLanded(filter.Entity, chestEntity, *airdrop);
						f.Destroy(filter.Entity);
					}

					break;
				case AirDropStage.Dropped:
					// Do nothing
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private void CenterAirdropOnLocalPlayer(Frame f, ref AirDropFilter filter)
		{
			var characterEntity = f.Unsafe.GetPointerSingleton<GameContainer>()->PlayersData[0].Entity;
			if (QuantumHelpers.IsDestroyed(f, characterEntity))
			{
				return;
			}

			if (f.TryGet<Transform2D>(characterEntity, out var trans))
			{
				filter.AirDrop->Position = trans.Position;
			}
		}
	}
}