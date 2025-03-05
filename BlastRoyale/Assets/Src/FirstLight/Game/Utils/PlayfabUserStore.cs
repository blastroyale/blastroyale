using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Server.SDK.Modules;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.ClientModels;
using Quantum;

namespace FirstLight.Web3.Runtime.ChainSafe
{
	/// <summary>
	/// Saves user controlled data.
	/// Only the user can write / read
	/// </summary>
	public class PlayfabUserStore
	{
		public static async UniTask<bool> SaveUserDataObject(object data)
		{
			var serialized = JsonConvert.SerializeObject(data);
			var r = await AsyncPlayfabAPI.ClientAPI.UpdateUserData(new UpdateUserDataRequest()
			{
				Data = new Dictionary<string, string>()
				{
					{data.GetType().FullName, serialized}
				}
			});
			return r != null;
		}
		
		public static async UniTask<T> GetUserDataObject<T>()
		{
			var key = typeof(T).FullName;
			var r = await AsyncPlayfabAPI.ClientAPI.GetUserData(new GetUserDataRequest()
			{
				Keys = new List<string>() { key }
			});
			if (!r.Data.TryGetValue(key, out var value))
			{
				return default;
			}
			return JsonConvert.DeserializeObject<T>(value.Value);
		}
	}
}