using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Data;
using FirstLight.Game.Messages;
using FirstLight.Game.Utils;
using FirstLight.Game.Utils.UCSExtensions;
using FirstLight.SDK.Services;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;

namespace FirstLight.Game.Services.Social
{
	public interface IPlayfabUnityBridgeService
	{
		UniTask<PlayfabUnityBridgeService.CacheHackData> LoadDataForPlayer(string unityId, string playerName);
	}

	public class PlayfabUnityBridgeService : IPlayfabUnityBridgeService
	{
		public class CacheHackData
		{
			public string AvatarUrl;
			public int Trophies;
			public string PlayerName;
		}

		private IPlayerProfileService _profileService;
		private IMessageBrokerService _messageBroker;

		public PlayfabUnityBridgeService(IPlayerProfileService profileService, IMessageBrokerService messageBroker)
		{
			_profileService = profileService;
			_messageBroker = messageBroker;
			_messageBroker.Subscribe<CollectionItemEquippedMessage>(message =>
			{
				if (message.Category == CollectionCategories.PROFILE_PICTURE)
				{
					_cacheHack.Remove(AuthenticationService.Instance.PlayerId);
				}
			});
		}

		private static readonly BatchQueue<CacheHackData> _batchQueue = new (3);
		private Dictionary<string, CacheHackData> _cacheHack = new ();

		public async UniTask<CacheHackData> LoadDataForPlayer(string unityId, string playerName)
		{
			if (_cacheHack.TryGetValue(unityId, out var hackData))
				return hackData;

			try
			{
				var loadedData = await _batchQueue.AddAsync(async () =>
				{
					var data = await CloudSaveService.Instance.LoadPlayerDataAsync(unityId);
					if (data == null)
					{
						return null;
					}

					if (data.AvatarURL != null)
					{
						return new CacheHackData()
						{
							AvatarUrl = data.AvatarURL,
							PlayerName = playerName,
							Trophies = data.Trophies
						};
					}

					if (data.PlayfabID == null) return null;

					var profile = await _profileService.GetPlayerPublicProfile(data.PlayfabID);
					return new CacheHackData()
					{
						AvatarUrl = profile.AvatarUrl,
						Trophies = profile.Statistics.Where(st => st.Name == GameConstants.Stats.RANKED_LEADERBOARD_LADDER_NAME)
							.Select(s => s.Value)
							.FirstOrDefault(),
						PlayerName = playerName
					};
				});
				if (loadedData != null)
				{
					_cacheHack[unityId] = loadedData;
					return loadedData;
				}
			}
			catch (Exception e)
			{
				FLog.Warn($"Error setting friend unityid {unityId} avatar", e);
			}

			return null;
		}
	}
}