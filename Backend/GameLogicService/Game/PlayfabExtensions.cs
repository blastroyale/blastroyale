using System.Collections.Generic;
using FirstLight.Game.Logic;
using FirstLight.Server.SDK.Models;
using FirstLight.Server.SDK.Modules;
using PlayFab;

namespace GameLogicService.Game
{
	public class Playfab
	{
		public static PlayFabResult<BackendLogicResult> Result(string player, Dictionary<string, string>? data = null)
		{
			return new PlayFabResult<BackendLogicResult>
			{
				Result = new BackendLogicResult
				{
					PlayFabId = player,
					Data = data ?? new Dictionary<string, string>()
				}
			};
		}
		
		public static PlayFabResult<BackendLogicResult> Result(string player, ServerState state)
		{
			return new PlayFabResult<BackendLogicResult>
			{
				Result = new BackendLogicResult
				{
					PlayFabId = player,
					Data = state
				}
			};
		}
		
		public static PlayFabResult<BackendLogicResult> Result(string player, BackendLogicResult logicResult)
		{
			return new PlayFabResult<BackendLogicResult>
			{
				Result = logicResult
			};
		}
		
		public static PlayFabResult<BackendLogicResult> Result(string player, object toSerialize)
		{
			var kp = ModelSerializer.Serialize(toSerialize);
			return new PlayFabResult<BackendLogicResult>
			{
				Result = new BackendLogicResult
				{
					PlayFabId = player,
					Data = new Dictionary<string, string> { { kp.Key, kp.Value }}
				}
			};
		}
	}
}