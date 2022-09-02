using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using FirstLight.FLogger;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.ClientModels;
using UnityEditor;
using UnityEngine;

namespace FirstLight.Editor.EditorTools
{
	/// <summary>
	/// This class handles PlayFab Admin communication with the back end
	/// </summary>
	public static class PlayFabShortcuts
	{
#if UNITY_EDITOR && ENABLE_PLAYFABADMIN_API
		[MenuItem("Tools/PlayFab/Delete Player Accounts")]
		private static void DeleteAllPlayers()
		{
			Debug.Log($"# Deleting All Players");
			
			PlayerPrefs.DeleteAll();
			GetAllPlayFabPlayers(OnPlayersSuccess);
		
			void OnPlayersSuccess(PlayFab.AdminModels.GetPlayersInSegmentResult result)
			{
				foreach (var playerProfile in result.PlayerProfiles)
				{
					PlayFabAdminAPI.DeleteMasterPlayerAccount(new PlayFab.AdminModels.DeleteMasterPlayerAccountRequest
					{
						PlayFabId = playerProfile.PlayerId
					}, null, OnPlayFabError);
				}
				
				FLog.Info($"# Deleting {result.PlayerProfiles.Count.ToString()} Players");

				var task = new HttpClient().DeleteAsync("https://devmarketplaceapi.azure-api.net/accounts/admin/unlinkall?key=devkey");
				task.Wait();

				FLog.Info("Accounts wallets unlinked");
			}
		}

		[MenuItem("Tools/PlayFab/Unlink All Accounts")]
		private static void UnlinkAllPlayers()
		{
			Debug.Log($"# Unlinking All Players");
			
			PlayerPrefs.DeleteAll();
			GetAllPlayFabPlayers(OnPlayersSuccess);

			void OnPlayersSuccess(PlayFab.AdminModels.GetPlayersInSegmentResult result)
			{
				foreach (var playerProfile in result.PlayerProfiles)
				{
					var request = new PlayFab.AdminModels.GetPlayerProfileRequest
					{
						PlayFabId = playerProfile.PlayerId,
						ProfileConstraints = new PlayFab.AdminModels.PlayerProfileViewConstraints { ShowLinkedAccounts = true }
					};
					PlayFabAdminAPI.GetPlayerProfile(request, OnPlayerProfileSuccess, OnPlayFabError);
				}

				Debug.Log($"# Unlinking {result.PlayerProfiles.Count.ToString()} Players");
			}
			
			void OnPlayerProfileSuccess(PlayFab.AdminModels.GetPlayerProfileResult result)
			{
				if (result.PlayerProfile.LinkedAccounts == null || result.PlayerProfile.LinkedAccounts.Count == 0)
				{
					return;
				}

				foreach (var account in result.PlayerProfile.LinkedAccounts)
				{
					switch (account.Platform)
					{
						case PlayFab.AdminModels.LoginIdentityProvider.Custom:
							PlayFabClientAPI.LoginWithCustomID(new LoginWithCustomIDRequest { CustomId = account.PlatformUserId },
							                                   resultLogin =>
							                                   {
								                                   PlayFabClientAPI.UnlinkCustomID(new UnlinkCustomIDRequest
								                                   {
									                                   CustomId = account.PlatformUserId,
									                                   AuthenticationContext = resultLogin.AuthenticationContext
								                                   }, null, OnPlayFabError);
							                                   }, OnPlayFabError);
							break;
						case PlayFab.AdminModels.LoginIdentityProvider.IOSDevice:
							PlayFabClientAPI.LoginWithIOSDeviceID(new LoginWithIOSDeviceIDRequest { DeviceId = account.PlatformUserId },
							                                   resultLogin =>
							                                   {
								                                   PlayFabClientAPI.UnlinkIOSDeviceID(new UnlinkIOSDeviceIDRequest
								                                   {
									                                   DeviceId = account.PlatformUserId,
									                                   AuthenticationContext = resultLogin.AuthenticationContext
								                                   }, null, OnPlayFabError);
							                                   }, OnPlayFabError);
							break;
						case PlayFab.AdminModels.LoginIdentityProvider.AndroidDevice:
							PlayFabClientAPI.LoginWithAndroidDeviceID(new LoginWithAndroidDeviceIDRequest { AndroidDeviceId = account.PlatformUserId },
							                                      resultLogin =>
							                                      {
								                                      PlayFabClientAPI.UnlinkAndroidDeviceID(new UnlinkAndroidDeviceIDRequest
								                                      {
									                                      AndroidDeviceId = account.PlatformUserId,
									                                      AuthenticationContext = resultLogin.AuthenticationContext
								                                      }, null, OnPlayFabError);
							                                      }, OnPlayFabError);
							break;
						default:
							throw new ArgumentOutOfRangeException($"The platform '{account.Platform}' is yet not configured to unlink");
					}
				}
			}
		}

		/// <summary>
		/// Sets internal title data key value pair.
		/// </summary>
		public static void SetTitleInternalData(string key, string data)
		{
			PlayFabAdminAPI.SetTitleInternalData(new PlayFab.AdminModels.SetTitleDataRequest()
				{
					Key = key,
					Value = data,
					TitleId = PlayFabSettings.TitleId
				},
				res =>
				{
					Debug.Log($"Internal Title Data {key} Set");
				},
				OnPlayFabError
			);
		}

		/// <summary>
		/// Gets an specific internal title key data
		/// </summary>
		public static void GetTitleInternalData(string key, Action<string> callback)
		{
			PlayFabAdminAPI.GetTitleInternalData(
				new PlayFab.AdminModels.GetTitleDataRequest()
				{ Keys = new List<string>() { key }},
				res =>
				{
					if (!res.Data.TryGetValue(key, out var data))
					{
						data = null;
					}
					callback(data);
				}, OnPlayFabError
			);
		}

		private static void GetAllPlayFabPlayers(Action<PlayFab.AdminModels.GetPlayersInSegmentResult> onSuccess)
		{
			PlayFabAdminAPI.GetAllSegments(new PlayFab.AdminModels.GetAllSegmentsRequest(), OnSegmentsSuccess, OnPlayFabError);
		
			void OnSegmentsSuccess(PlayFab.AdminModels.GetAllSegmentsResult result)
			{
				foreach (var segment in result.Segments)
				{
					if (segment.Name == "All Players")
					{
						PlayFabAdminAPI.GetPlayersInSegment(new PlayFab.AdminModels.GetPlayersInSegmentRequest
						{
							SegmentId = segment.Id
						}, onSuccess, OnPlayFabError);
						break;
					}
				}
				
				FLog.Info($"# Obtained {result.Segments.Count.ToString()} Segments");
			}
		}
#endif

		private static void OnPlayFabError(PlayFabError result)
		{
			FLog.Error($"PlayFab Error {result.Error} - {result.ErrorMessage}");
			if (result.ErrorDetails != null)
			{
				FLog.Error(JsonConvert.SerializeObject(result.ErrorDetails));
			}
		}
	}
}