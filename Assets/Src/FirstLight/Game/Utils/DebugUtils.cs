using System;
using System.IO;
using System.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Services;
using FirstLight.Server.SDK.Modules;
using FirstLight.Services;
using UnityEngine;

namespace FirstLight.Game.Utils
{
	/// <summary>
	/// Class to put any tools regarding debugging
	/// </summary>
	public static class DebugUtils
	{
		/// <summary>
		/// Class to put any bool flag that helps with testing the app
		/// </summary>
		public static class DebugFlags
		{
			public static bool OverrideCurrencyChangedIsCollecting;
		}

		public static void SaveState(IPlayfabService playfab, IDataProvider dataProvider, Action doneCallback)
		{
#if DEVELOPMENT_BUILD || UNITY_EDITOR
			var date = DateTime.Now.ToString("dd-MM-yyyy-HH-mm-ss");
			var states = $"{Application.persistentDataPath}/crash-logs/{date}/states";

			Directory.CreateDirectory(states);


			playfab.FetchServerState(state =>
			{
				foreach (var type in dataProvider.GetKeys())
				{
					string client = Path.Combine(states, $"{type.Name}_client.json");
					string server = Path.Combine(states, $"{type.Name}_server.json");
					string serverValue = string.Empty;
					if (type.FullName != null) state.TryGetValue(type.FullName, out serverValue);

					string clientValue = ModelSerializer.Serialize(dataProvider.GetData((type))).Value;

					File.AppendAllText(server, serverValue + Environment.NewLine);

					File.AppendAllText(client, clientValue + Environment.NewLine);
				}

				FLog.Info($"Writing states to {states}");
				doneCallback();
			});
#else
			doneCallback();
#endif
		}
	}
}