using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FirstLight.Game.Data;
using PlayFab.ServerModels;
using Quantum;

namespace Scripts.Scripts
{
	/// <summary>
	/// Script to replace Hammers in all players inventories to a random weapon
	/// Hammer in player inventory should be an invalid state of the data.  
	/// </summary>
	public class ConvertHammers : PlayfabScript
	{
		readonly GameId[] _equip =
		{
			GameId.ModSniper,
			GameId.ModRifle,
			GameId.ModPistol,
		};


		private readonly Random _rnd = new();

		public override Environment GetEnvironment()
		{
			return Environment.DEV;
		}

		public override void Execute(ScriptParameters args)
		{
			RunAsync().Wait();
		}

		public async Task RunAsync()
		{
			var tasks = new List<Task<int>>();
			foreach (var player in await GetAllPlayers())
			{
				tasks.Add(RemoveHammer(player));
			}

			var result = await Task.WhenAll(tasks.ToArray());

			Console.WriteLine($"Done removed {result.Sum()} hammers in total!");
		}

		private GameId GenerateNewEquipment()
		{
			return _equip[_rnd.Next(_equip.Length)];
		}


		private async Task<int> RemoveHammer(PlayerProfile profile)
		{
			var state = await ReadUserState(profile.PlayerId);
			if (state == null)
			{
				return 0;
			}

			var removed = 0;
			var equipmentData = state.DeserializeModel<EquipmentData>();
			var idData = state.DeserializeModel<IdData>();
			foreach (var (key, value) in equipmentData.Inventory)
			{
				if (value.GameId == GameId.Hammer)
				{
					var copy = value;
					var newEquipment = GenerateNewEquipment();
					copy.GameId = newEquipment;
					equipmentData.Inventory[key] = copy;
					idData.GameIds[key] = newEquipment;
					idData.NewIds.Add(key);
					removed++;
				}
			}

			if (removed <= 0) return removed;

			state.UpdateModel(equipmentData);
			state.UpdateModel(idData);
			await SetUserState(profile.PlayerId, state);
			Console.WriteLine($"Removed {removed} hammers from {profile.PlayerId}");
			return removed;
		}
	}
}
