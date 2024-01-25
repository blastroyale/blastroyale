using Photon.Deterministic;

namespace Quantum.Commands
{
	/// <summary>
	/// This command kills the player executing the command
	/// </summary>
	public unsafe class CheatKillAllTutorialBots : CommandBase
	{
		public BotBehaviourType BehaviourType;

		/// <inheritdoc />
		public override void Serialize(BitStream stream)
		{
			var type = (int)BehaviourType;
			stream.Serialize(ref type);
			BehaviourType = (BotBehaviourType)type;
		}

		/// <inheritdoc />
		internal override void Execute(Frame f, PlayerRef playerRef)
		{
			if (f.Context.GameModeConfig.Id != "Tutorial")
			{
				return;
			}

			var playerEntity = f.GetSingleton<GameContainer>().PlayersData[playerRef].Entity;


			var game = f.GetSingleton<GameContainer>();
			for (var i = 0; i < game.PlayersData.Length; i++)
			{
				var entity = game.PlayersData[i].Entity;
				if (!f.Has<BotCharacter>(entity) || !f.Unsafe.TryGetPointer<Stats>(entity, out var stats)) continue;
				var bot = f.Unsafe.GetPointer<BotCharacter>(entity);
				if (bot->BehaviourType != BehaviourType)
				{
					continue;
				}

				stats->Kill(f, entity, playerEntity);
			}
		}
	}
}