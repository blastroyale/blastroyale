using Photon.Deterministic;

namespace Quantum.Commands
{
	/// <summary>
	/// This cheat command completes the current game's quest for the current player
	/// </summary>
	public unsafe class CheatCompleteKillCountCommand : CommandBase
	{
		public bool IsLocalWinner;
		
		/// <inheritdoc />
		public override void Serialize(BitStream stream)
		{
			stream.Serialize(ref IsLocalWinner);
		}

		/// <inheritdoc />
		internal override void Execute(Frame f, PlayerRef playerRef)
		{
#if DEBUG
			var container = f.Unsafe.GetPointerSingleton<GameContainer>();
			var playerInt = (int) playerRef;
			var player = playerInt == 0 ? 1 : playerInt;

			container->PlayersData.GetPointer(IsLocalWinner ? playerInt : player)->PlayersKilledCount = (ushort)container->TargetProgress;
			container->UpdateGameProgress(f, container->TargetProgress);
#else
			Log.Error($"Trying to use Cheat command {this.GetType().Name} in Release build of Quantum Code");
#endif
		}
	}
}