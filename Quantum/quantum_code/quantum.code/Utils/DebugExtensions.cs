using System.Text;

namespace Quantum
{
	/// <summary>
	/// Extensions to help debug simulation data
	/// </summary>
	public static class DebugExtensions
	{
		/// <summary>
		/// Get a debug string containing all player slots in the simulation.
		/// </summary>
		public static string PlayersDebugString(this Frame f)
		{
			var str = new StringBuilder();
			for (var x = 0; x < f.PlayerCount; x++)
			{
				str.AppendLine("==== Player " + x + " ====");
				var flags = f.GetPlayerInputFlags(x);

				str.AppendLine("Flags: " + flags);


				var data = f.GetPlayerData(x);
				if (data != null)
				{
					str.Append($"PlayerID: {data.PlayerId}, ");
					str.Append($"PlayerName: {data.PlayerName}, ");
					str.Append($"PartyID: {data.PartyId}, ");
					str.AppendLine();
				}
				else
				{
					str.AppendLine("WITHOUT DATA!");
				}
				
				str.AppendLine("==== End Player " + x + " ====");
				str.AppendLine();
			}

			return str.ToString();
		}
	}
}