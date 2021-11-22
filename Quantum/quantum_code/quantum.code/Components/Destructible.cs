using System.Collections.Generic;
using Photon.Deterministic;

namespace Quantum
{
	public unsafe partial struct Destructible
	{
		/// <summary>
		/// Initializes this <see cref="Destructible"/> with all the necessary data
		/// </summary>
		internal void Init(Frame f, EntityRef e, Transform3D spawnPosition, QuantumDestructibleConfig destructibleConfig)
		{
			var targetable = new Targetable();
			var transform = f.Unsafe.GetPointer<Transform3D>(e);
			var baseHealth = (int) destructibleConfig.Health;
			var power = (int) destructibleConfig.PowerAmount;

			transform->Position = spawnPosition.Position;
			transform->Rotation = spawnPosition.Rotation;
			targetable.Team = (int) TeamType.Neutral;
			targetable.IsUntargetable = true;
			ProjectileAssetRef = destructibleConfig.ProjectileAssetRef;
			SplashRadius = destructibleConfig.SplashRadius;
			IsDestructing = false;
			DestructionLengthTime = destructibleConfig.DestructionLengthTime;
			TimeToDestroy = FP.MaxValue;
			GameId = destructibleConfig.Id;
			
			f.Add(e, targetable);
			f.Add(e, new Stats(baseHealth, power, 0, 0, 0));
		}
	}
}