using Photon.Deterministic;

namespace Quantum.Commands
{
	/// <summary>
	/// This command makes the current player quit the game
	/// </summary>
	public unsafe class PlayerQuitCommand : CommandBase
	{
		/// <inheritdoc />
		public override void Serialize(BitStream stream)
		{
		}

		/// <inheritdoc />
		internal override void Execute(Frame f, PlayerRef playerRef)
		{
			var characterEntity = f.GetSingleton<GameContainer>().PlayersData[playerRef].Entity;
			
			f.Events.OnPlayerLeft(playerRef, characterEntity);
			if(f.Has<AlivePlayerCharacter>(characterEntity))
			{
				f.Events.FireQuantumServerCommand(playerRef, QuantumServerCommand.EndOfGameRewards);
			}
		}
	}
}