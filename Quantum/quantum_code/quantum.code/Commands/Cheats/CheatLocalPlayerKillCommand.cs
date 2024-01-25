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
		var container = f.Unsafe.GetPointerSingleton<GameContainer>();
		var playerInt = (int) playerRef;
		
		var characterEntity = container->PlayersData[playerInt].Entity;
			
		var stats = f.Unsafe.GetPointer<Stats>(characterEntity);
		stats->Kill(f, characterEntity, true);
#else
		Log.Error($"Trying to use Cheat command {this.GetType().Name} in Release build of Quantum Code");
#endif
		}
	}
}