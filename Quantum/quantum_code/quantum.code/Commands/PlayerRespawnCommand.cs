using System;
using Photon.Deterministic;

namespace Quantum.Commands
{
	/// <summary>
	/// This command makes the current player respawn again in the game
	/// </summary>
	public unsafe class PlayerRespawnCommand : CommandBase
	{
		/// <inheritdoc />
		public override void Serialize(BitStream stream)
		{
		}

		/// <inheritdoc />
		internal override void Execute(Frame f, PlayerRef playerRef)
		{
			var characterEntity = f.GetSingleton<GameContainer>().PlayersData[playerRef].Entity;

			if (!f.TryGet<DeadPlayerCharacter>(characterEntity, out var deadPlayer) || f.Time < deadPlayer.TimeOfDeath + f.GameConfig.PlayerRespawnTime)
			{
				throw new InvalidOperationException($"The player {playerRef} is not ready to be respawn yet");
			}

			var spawnPoint = QuantumHelpers.GetPlayerSpawnTransform(f);
			
			f.Unsafe.GetPointer<PlayerCharacter>(characterEntity)->Spawn(f, characterEntity, spawnPoint.Component, true);
		}
	}
}