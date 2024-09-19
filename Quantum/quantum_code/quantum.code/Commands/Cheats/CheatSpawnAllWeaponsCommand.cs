using System.Collections.Generic;
using Photon.Deterministic;

namespace Quantum.Commands
{
	/// <summary>
	/// Spawns an airdrop with no delays and an optional <see cref="Position"/> and <see cref="Chest"/>.
	///
	/// If position isn't set it will spawn on top of the current player.
	/// </summary>
	public class CheatSpawnAllWeaponsCommand : CommandBase
	{
		public bool Golden;

		public override void Serialize(BitStream stream)
		{
			stream.Serialize(ref Golden);
		}

		internal override void Execute(Frame f, PlayerRef playerRef)
		{
#if DEBUG


			var characterEntity = f.GetSingleton<GameContainer>().PlayersData[playerRef].Entity;
			var position = f.Get<Transform2D>(characterEntity).Position;

			var i = 0;
			var weaponsDrop = new List<GameId>();
			foreach (var weaponConfig in f.WeaponConfigs.QuantumConfigs)
			{
				if (weaponConfig.Id == GameId.Hammer) continue;
				var eq = new Equipment()
				{
					GameId = weaponConfig.Id,
					Level = 1,
					Material = Golden ? EquipmentMaterial.Golden : EquipmentMaterial.Bronze
				};
				Collectable.DropEquipment(f, eq, position, i, false, f.WeaponConfigs.QuantumConfigs.Count - 1, dropRadius: FP._2);
				i++;
			}
#else
		Log.Error($"Trying to use Cheat command {this.GetType().Name} in Release build of Quantum Code");
#endif
		}
	}
}