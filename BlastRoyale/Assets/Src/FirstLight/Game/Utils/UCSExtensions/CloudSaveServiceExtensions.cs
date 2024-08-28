using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using Unity.Services.CloudSave;
using Unity.Services.CloudSave.Models.Data.Player;
using SaveOptions = Unity.Services.CloudSave.Models.Data.Player.SaveOptions;

namespace FirstLight.Game.Utils.UCSExtensions
{
	public class UnityPlayerData
	{
		public string PlayfabID;
		public string AvatarURL;
		public int Trophies;
	}

	/// <summary>
	/// Helpers for the UCS CloudSave service.
	/// </summary>
	public static class CloudSaveServiceExtensions
	{
		private const string KEY_PLAYFAB_ID = "playfab_id";
		private const string KEY_AVATAR_URL = "avatar_url";
		private const string KEY_TROPHIES = "trophies";

		/// <summary>
		/// Saves the playfab id for the current user.
		/// TODO: This should be done on server.
		/// </summary>
		public static async UniTask SavePlayfabIDAsync(this ICloudSaveService cloudSave, string playfabID)
		{
			await cloudSave.Data.Player.SaveAsync(new Dictionary<string, object>
			{
				{KEY_PLAYFAB_ID, playfabID}
			}, new SaveOptions(new PublicWriteAccessClassOptions()));
		}

		/// <summary>
		/// Saves the playfab id for the current user.
		/// TODO: This should be done on server.
		/// </summary>
		public static async UniTask SaveAvatarURLAsync(this ICloudSaveService cloudSave, string avatarUrl)
		{
			await cloudSave.Data.Player.SaveAsync(new Dictionary<string, object>
			{
				{KEY_AVATAR_URL, avatarUrl}
			}, new SaveOptions(new PublicWriteAccessClassOptions()));
		}

		/// <summary>
		/// Loads the playfab master id for the given UCS player id. Returns null if it can't find it.
		/// </summary>
		public static async UniTask<UnityPlayerData> LoadPlayerDataAsync(this ICloudSaveService cloudSave, string playerID)
		{
			var data = new UnityPlayerData();
			var loadOptions = new LoadOptions(new PublicReadAccessClassOptions(playerID));
			var keys = new HashSet<string>
			{
				KEY_PLAYFAB_ID, KEY_AVATAR_URL, KEY_TROPHIES
			};

			var result = await cloudSave.Data.Player.LoadAsync(keys, loadOptions);
			if (result.TryGetValue(KEY_PLAYFAB_ID, out var playfabID))
			{
				data.PlayfabID = playfabID.Value.GetAsString();
			}

			if (result.TryGetValue(KEY_AVATAR_URL, out var avatarUrl))
			{
				data.AvatarURL = avatarUrl.Value.GetAsString();
			}

			if (result.TryGetValue(KEY_TROPHIES, out var trophiesString))
			{
				if (int.TryParse(trophiesString.Value.GetAsString(), out var trophies))
				{
					data.Trophies = trophies;
				}

				data.AvatarURL = avatarUrl.Value.GetAsString();
			}

			return data;
		}
	}
}