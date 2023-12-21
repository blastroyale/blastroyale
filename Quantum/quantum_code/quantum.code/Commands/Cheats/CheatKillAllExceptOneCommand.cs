using Photon.Deterministic;

namespace Quantum.Commands
{
	/// <summary>
	/// This command kills the player executing the command
	/// </summary>
	public unsafe class CheatKillAllExceptOneCommand : CommandBase
	{
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
			var spared = EntityRef.None;
			for (var i = 0; i < game.PlayersData.Length; i++)
			{
				var entity = game.PlayersData[i].Entity;
				if (!f.Has<BotCharacter>(entity) || !f.Has<Stats>(entity)) continue;
				if (spared ==  EntityRef.None)
				{
					spared = entity;
					continue;
				}
				var spell = Spell.CreateInstant(f, entity, playerEntity, playerEntity, 100000, 0, entity.GetPosition(f));
				QuantumHelpers.ProcessHit(f, &spell);
			}

			if (spared == EntityRef.None)
			{
				return;
			}

			var playerTransform = f.Unsafe.GetPointer<Transform3D>(playerEntity);
			var botTransform = f.Unsafe.GetPointer<Transform3D>(spared);
			botTransform->Position = playerTransform->Position + FPVector3.Up;
#else
		Log.Error($"Trying to use Cheat command {this.GetType().Name} in Release build of Quantum Code");
#endif
		}
	}
}