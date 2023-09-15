using System;
using FirstLight.Game.Logic.RPC;
using FirstLight.Game.Services.AnalyticsHelpers;
using FirstLight.Server.SDK.Models;
using FirstLight.Server.SDK.Modules;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.SharedModels;

// ReSharper disable once CheckNamespace

namespace FirstLight.Game.Services
{
	/// <summary>
	/// This service provides....
	/// </summary>
	public interface IPlayerProfileService
	{
		/// <summary>
		/// 
		/// </summary>
	}
	
	/// <inheritdoc cref="IPlayerProfileService"/>
	public class PlayerProfileService : IPlayerProfileService
	{
		private IGameBackendService _backend;
		
		public PlayerProfileService(IGameBackendService backend)
		{
			_backend = backend;
		}
		
		public void TestServerQuery()
		{
			GetPlayerPublicProfile(PlayFabSettings.staticPlayer.PlayFabId, (result) =>
			{
				foreach (var s in result.Statistics)
				{
					Debug.Log($"{s.Name} = {s.Value}");
				}
			});
		}

		public void GetPlayerPublicProfile(string playerId, Action<PublicPlayerProfile> onSuccess)
		{
			_backend.CallFunction("GetPublicProfile", r =>
			{
				var serverResult = ModelSerializer.Deserialize<PlayFabResult<LogicResult>>(r.FunctionResult.ToString());
				onSuccess(ModelSerializer.DeserializeFromData<PublicPlayerProfile>(serverResult.Result.Data));
			}, e => { _backend.HandleError(e, null, AnalyticsCallsErrors.ErrorType.Recoverable); }, new LogicRequest()
			{
				Command = playerId
			});
		}
	}
}