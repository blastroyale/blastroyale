using Photon.Deterministic;
using Quantum.Systems;

namespace Quantum.Commands
{
	/// <summary>
	/// This command kills the player executing the command
	/// </summary>
	public unsafe class CheatKillTeamMatesCommand : CommandBase
	{
		/// <inheritdoc />
		public override void Serialize(BitStream stream)
		{
		}

		/// <inheritdoc />
		internal override void Execute(Frame f, PlayerRef playerRef)
		{
#if DEBUG
			var game = f.GetSingleton<GameContainer>();
			var playerEntity = f.GetSingleton<GameContainer>().PlayersData[playerRef].Entity;

			for (var i = 0; i < game.PlayersData.Length; i++)
			{
				var entity = game.PlayersData[i].Entity;
				if (!f.Has<BotCharacter>(entity) || !f.Unsafe.TryGetPointer<Stats>(entity, out var stats)) continue;
				if (TeamSystem.HasSameTeam(f, entity, playerEntity))
				{
					stats->Kill(f, entity, canKnockOut: true);
				}
			}

#else
		Log.Error($"Trying to use Cheat command {this.GetType().Name} in Release build of Quantum Code");
#endif
		}
	}
}