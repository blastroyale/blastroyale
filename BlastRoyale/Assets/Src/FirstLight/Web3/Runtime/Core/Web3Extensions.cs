using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Utils;
using FirstLight.Server.SDK.Modules;
using PlayFab.ClientModels;

namespace FirstLight.Web3.Runtime
{
	public static class PlayfabUserExtensions
	{
		public static async UniTask<T> ReadFromUserData<T>()
		{
			var r = await AsyncPlayfabAPI.ClientAPI.GetUserData(new GetUserDataRequest()
			{
				Keys = new List<string>() {typeof(T).FullName}
			});
			r.Data.TryGetValue(typeof(T).FullName!, out var dataStrign);
			if (dataStrign == null)
			{
				return default;
			}
			return ModelSerializer.Deserialize<T>(dataStrign.Value);
		}
		
		public static async UniTask<T> ReadFromUserReadonlyData<T>()
		{
			var r = await AsyncPlayfabAPI.ClientAPI.GetUserReadOnlyData(new GetUserDataRequest()
			{
				Keys = new List<string>() {typeof(T).FullName}
			});
			r.Data.TryGetValue(typeof(T).FullName!, out var dataStrign);
			if (dataStrign == null)
			{
				return default;
			}
			return ModelSerializer.Deserialize<T>(dataStrign.Value);
		}
		
		public static async UniTask SaveInUserData<T>(T model)
		{
			var r = await AsyncPlayfabAPI.ClientAPI.UpdateUserData(new UpdateUserDataRequest()
			{
				Data = new Dictionary<string, string>() {{ typeof(T).FullName!, ModelSerializer.Serialize(model).Value } }
			});
		}
		
		 
	}
}