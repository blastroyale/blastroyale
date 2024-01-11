using System;
using System.Collections.Generic;

namespace Quantum
{

	/// <summary>
	/// This class has a list of useful extensions to be used in the quantum project
	/// </summary>
	public static class QuantumExtensions
	{

		/// <summary>
		/// Sends a command that will be executed in client logic.
		/// The command will be routed via quantum server if its a quantum command to
		/// ensure its not cheatable.
		/// </summary>
		public static void ServerCommand(this Frame f, PlayerRef sender, QuantumServerCommand command)
		{
			f.Events.FireQuantumServerCommand(sender, command); // server-only
			f.Events.FireLocalQuantumServerCommand(sender, command); // prediction
		}
		
		/// <summary>
		/// Sorts a list of player by their <see cref="QuantumPlayerMatchData.PlayerRank"/>
		/// </summary>
		public static void SortByPlayerRank(this List<QuantumPlayerMatchData> players, bool isReverse)
		{
			players.Sort((a, b) =>
			{
				var rank = a.PlayerRank.CompareTo(b.PlayerRank);

				// If players have the same rank, sort them by their PlayerRef index
				if (rank == 0)
				{
					rank = a.Data.Player._index.CompareTo(b.Data.Player._index);
				}

				return isReverse ? rank * -1 : rank;
			});
		}

		/// <summary>
		/// Sorts a list of player by their <see cref="QuantumPlayerMatchData.PlayerRank"/>
		/// </summary>
		public static void SortByPlayerRef(this List<QuantumPlayerMatchData> players, bool isReverse)
		{
			players.Sort((a, b) =>
			{
				var rank = a.Data.Player._index.CompareTo(b.Data.Player._index);

				return isReverse ? rank * -1 : rank;
			});
		}

		public static void CopyFixedArray<T>(this FixedArray<T> array, FixedArray<T> otherArray) where T : unmanaged
		{
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = otherArray[i];
			}
		}

		public static List<T> ToList<T>(this FixedArray<T> array) where T : unmanaged
		{
			var list = new List<T>();

			for (int i = 0; i < array.Length; i++)
			{
				list.Add(array[i]);
			}

			return list;
		}

		public static GameId GameId(this ChestType type)
		{
			return type switch
			{
				ChestType.Equipment    => Quantum.GameId.ChestEquipment,
				ChestType.Consumable  => Quantum.GameId.ChestConsumable,
				ChestType.Legendary => Quantum.GameId.ChestLegendary,
				_                   => throw new ArgumentOutOfRangeException(nameof(type), type, null)
			};
		}
	}
}