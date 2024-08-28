using System.Collections.Generic;
using Photon.Deterministic;

namespace Quantum.Commands
{
	/// <summary>
	/// Spawns an airdrop with no delays and an optional <see cref="Position"/> and <see cref="Chest"/>.
	///
	/// If position isn't set it will spawn on top of the current player.
	/// </summary>
	public class CheatSpawnAllSpecialsCommand : CommandBase
	{
		public override void Serialize(BitStream stream)
		{
		}

		internal override void Execute(Frame f, PlayerRef playerRef)
		{
#if DEBUG
			var consumables = new List<GameId>();
			foreach (var consumableConfig in f.ConsumableConfigs.QuantumConfigs)
			{
				if (consumableConfig.ConsumableType == ConsumableType.Special)
				{
					consumables.Add(consumableConfig.Id);
				}
			}

			var characterEntity = f.GetSingleton<GameContainer>().PlayersData[playerRef].Entity;
			var position = f.Get<Transform2D>(characterEntity).Position;

			var i = 0;
			foreach (var gameId in consumables)
			{
				Collectable.DropConsumable(f, gameId, position, i, true, consumables.Count, FP._2);
				i++;
			}
#else
		Log.Error($"Trying to use Cheat command {this.GetType().Name} in Release build of Quantum Code");
#endif
		}
	}
}