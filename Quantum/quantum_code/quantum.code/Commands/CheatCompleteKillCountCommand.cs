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
			var container = f.Unsafe.GetPointerSingleton<GameContainer>();
			var playerInt = (int) playerRef;
			var player = playerInt == 0 ? 1 : playerInt;

			container->PlayersData.GetPointer(IsLocalWinner ? playerInt : player)->PlayersKilledCount = container->TargetProgress;
			container->UpdateRanks(f);
			container->UpdateGameProgress(f, container->TargetProgress);
		}
	}
}