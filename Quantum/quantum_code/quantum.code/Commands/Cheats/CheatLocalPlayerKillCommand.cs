using Photon.Deterministic;

namespace Quantum.Commands
{
	/// <summary>
	/// This command kills the player executing the command
	/// </summary>
	public unsafe class CheatLocalPlayerKillCommand : CommandBase
	{
		/// <inheritdoc />
		public override void Serialize(BitStream stream)
		{
		}

		/// <inheritdoc />
		internal override void Execute(Frame f, PlayerRef playerRef)
		{
#if DEBUG
		var characterEntity = f.GetSingleton<GameContainer>().PlayersData[playerRef].Entity;
		var stats = f.Unsafe.GetPointer<Stats>(characterEntity);
		stats->ReduceHealth(f, characterEntity, new Spell { Attacker = characterEntity, PowerAmount = uint.MaxValue });
#else
		Log.Error($"Trying to use Cheat command {this.GetType().Name} in Release build of Quantum Code");
#endif
		}
	}
}