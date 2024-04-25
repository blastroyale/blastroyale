using System.Collections.Generic;
using Photon.Deterministic;

namespace Quantum.Commands
{
	/// <summary>
	/// This command kills the player executing the command
	/// </summary>
	public unsafe class CheatKillAllExceptCommand : CommandBase
	{
		public int Amount = 2;

		/// <inheritdoc />
		public override void Serialize(BitStream stream)
		{
		}

		/// <inheritdoc />
		internal override void Execute(Frame f, PlayerRef playerRef)
		{
#if DEBUG
			var container = f.Unsafe.GetPointerSingleton<GameContainer>();
			var playerInt = (int)playerRef;

			var playerEntity = f.GetSingleton<GameContainer>().PlayersData[playerRef].Entity;

			var game = f.GetSingleton<GameContainer>();
			var spared = new List<EntityRef>();
			for (var i = 0; i < game.PlayersData.Length; i++)
			{
				var entity = game.PlayersData[i].Entity;
				if (!f.Has<BotCharacter>(entity) || !f.Unsafe.TryGetPointer<Stats>(entity, out var stats)) continue;
				if (spared.Count < Amount)
				{
					spared.Add(entity);
					continue;
				}

				stats->Kill(f, entity, playerEntity);
			}


			FP offset = FP._1;
			foreach (var entityRef in spared)
			{
				
				var playerTransform = f.Unsafe.GetPointer<Transform3D>(playerEntity);
				var botTransform = f.Unsafe.GetPointer<Transform3D>(entityRef);
				var bot = f.Unsafe.GetPointer<BotCharacter>(entityRef);
				botTransform->Position = playerTransform->Position + FPVector3.Forward * offset;
				offset += FP._1;
				// Reset the current action so it does't goes running
				bot->NextDecisionTime = f.Time;
				bot->ResetTargetWaypoint(f);
				f.Unsafe.GetPointer<NavMeshPathfinder>(entityRef)->Stop(f, entityRef);
			}
#else
		Log.Error($"Trying to use Cheat command {this.GetType().Name} in Release build of Quantum Code");
#endif
		}
	}
}