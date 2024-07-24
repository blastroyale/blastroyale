using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using Unity.Services.CloudSave;
using Unity.Services.CloudSave.Models.Data.Player;
using SaveOptions = Unity.Services.CloudSave.Models.Data.Player.SaveOptions;

namespace FirstLight.Game.Utils.UCSExtensions
{
	/// <summary>
	/// Helpers for the UCS CloudSave service.
	/// </summary>
	public static class CloudSaveServiceExtensions
	{
		private const string KEY_PLAYFAB_ID = "playfab_id";

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
		/// Loads the playfab master id for the given UCS player id. Returns null if it can't find it.
		/// </summary>
		public static async UniTask<string> LoadPlayfabID(this ICloudSaveService cloudSave, string playerID)
		{
			var loadOptions = new LoadOptions(new PublicReadAccessClassOptions(playerID));
			var keys = new HashSet<string>
			{
				KEY_PLAYFAB_ID
			};

			var result = await cloudSave.Data.Player.LoadAsync(keys, loadOptions);
			if (result.TryGetValue(KEY_PLAYFAB_ID, out var playfabID))
			{
				return playfabID.Value.GetAsString();
			}
			FLog.Warn($"Could not find playfab id in cloud save for user: {playerID}");
			return null;
		}
	}
}