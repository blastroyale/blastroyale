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
			var entity = f.GetSingleton<GameContainer>().PlayersData[playerRef].Entity;

			if (!f.TryGet<DeadPlayerCharacter>(entity, out var deadPlayer) || 
			    f.Time < deadPlayer.TimeOfDeath + f.GameConfig.PlayerRespawnTime)
			{
				throw new InvalidOperationException($"The player {playerRef} is not ready to be respawn yet");
			}

			var agent = f.Unsafe.GetPointer<HFSMAgent>(entity);
			HFSMManager.TriggerEvent(f, &agent->Data, entity, Constants.RespawnEvent);
		}
	}
}