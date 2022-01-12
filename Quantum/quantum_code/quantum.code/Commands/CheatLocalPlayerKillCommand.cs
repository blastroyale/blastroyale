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
			var characterEntity = f.GetSingleton<GameContainer>().PlayersData[playerRef + 1].Entity;

			f.Destroy(characterEntity);
			f.Events.OnPlayerDead(playerRef + 1, characterEntity);
			//f.Signals.HealthIsZero(characterEntity, characterEntity);
		}
	}
}