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
			var characterEntity = f.GetSingleton<GameContainer>().PlayersData[playerRef].Entity;
			var stats = f.Unsafe.GetPointer<Stats>(characterEntity);

			stats->AttackerSetCurrentHealth(f, characterEntity, characterEntity, 0);
		}
	}
}