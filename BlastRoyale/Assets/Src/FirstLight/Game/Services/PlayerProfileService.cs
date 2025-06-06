using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Logic.RPC;
using FirstLight.Game.Utils;
using FirstLight.Server.SDK.Models;
using FirstLight.Server.SDK.Modules;
using FirstLight.Server.SDK.Modules.Commands;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.SharedModels;

// ReSharper disable once CheckNamespace

namespace FirstLight.Game.Services
{
	/// <summary>
	/// This service provides access to player profile
	/// </summary>
	public interface IPlayerProfileService
	{
		/// <summary>
		/// Query read-only public player profile data
		/// </summary>
		UniTask<PublicPlayerProfile> GetPlayerPublicProfile(string playerId);
	}

	/// <inheritdoc cref="IPlayerProfileService"/>
	public class PlayerProfileService : IPlayerProfileService
	{
		private IGameBackendService _backend;

		public PlayerProfileService(IGameBackendService backend)
		{
			_backend = backend;
		}

		public async UniTask<PublicPlayerProfile> GetPlayerPublicProfile(string playerId)
		{
			var r = await _backend.CallGenericFunction(CommandNames.GET_PLAYER_PROFILE, new Dictionary<string, string>()
			{
				{CommandFields.PlayerId, playerId},
			});
			var serverResult = ModelSerializer.Deserialize<PlayFabResult<LogicResult>>(r.FunctionResult.ToString());
			return ModelSerializer.DeserializeFromData<PublicPlayerProfile>(serverResult.Result.Data);
		}
	}
}