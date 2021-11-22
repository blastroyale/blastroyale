using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Backend.Data;
using Backend.Data.DataTypes;
using Backend.Models;
using Backend.Util;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.Json;
using PlayFab.ServerModels;

namespace Backend.Functions
{
	/// <summary>
	/// This command is executed by by the client when a player is created in the game.
	/// It setups the basic game backend data for the player.
	/// </summary>
	public static class SetupPlayerCommand
	{
		private static readonly List<string> _initialSkins = new List<string> {"Male01Avatar", "Male02Avatar", "Female01Avatar", "Female02Avatar"};
		private static readonly List<string> _initialWeapons = new List<string> {"Hammer"};

		/// <summary>
		/// Command Execution
		/// </summary>
		[FunctionName("SetupPlayerCommand")]
		public static async Task<dynamic> RunAsync([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]
		                                                                    HttpRequestMessage req, ILogger log)
		{
			var context = await ContextProcessor.ProcessContext<LogicRequest>(req);
			var server = new PlayFabServerInstanceAPI(context.ApiSettings, context.AuthenticationContext);
			var request = GetInitialDataRequest(context.AuthenticationContext.PlayFabId);
			
			log.Log(LogLevel.Information, $"{request.PlayFabId} is executing - SetupPlayerCommand");
			
			var result = await server.UpdateUserReadOnlyDataAsync(request);
			
			return new PlayFabResult<LogicResult>
			{
				CustomData = result.CustomData,
				Error = result.Error,
				Result = new LogicResult
				{
					PlayFabId = context.AuthenticationContext.PlayFabId,
					Data = request.Data
				}
			};
		}

		/// <summary>
		/// Requests the <see cref="UpdateUserDataRequest"/> to set the initial data for the player with the given <paramref name="playFabId"/>
		/// </summary>
		public static UpdateUserDataRequest GetInitialDataRequest(string playFabId)
		{
			var rngData = SetupInitialRngData(playFabId.GetHashCode());
			var idData = new IdData();
			var playerData = SetupInitialPlayerData(idData, rngData);
			
			return new UpdateUserDataRequest
			{
				PlayFabId = playFabId,
				Data = new Dictionary<string, string>
				{
					{ nameof(IdData), JsonConvert.SerializeObject(idData) },
					{ nameof(RngData), JsonConvert.SerializeObject(rngData) },
					{ nameof(PlayerData), JsonConvert.SerializeObject(playerData) },
				} 
			};
		}

		private static RngData SetupInitialRngData(int seed)
		{
			return new RngData
			{
				Count = 0,
				Seed = seed,
				State = RngUtils.GenerateRngState(seed)
			};
		}

		private static PlayerData SetupInitialPlayerData(IdData idData, RngData rngData)
		{
			var rngSkin = Rng.Range(0, _initialSkins.Count, rngData.State, false);
			var rngWeapon = Rng.Range(0, _initialWeapons.Count, rngData.State, false);
			var playerData = new PlayerData { Level = 1, PlayerSkinId = _initialSkins[rngSkin] };

			rngData.Count += 2;

			playerData.Currencies.Add("HC", 50);
			playerData.Currencies.Add("SC", 100);
			
			playerData.EquippedItems.Add("Weapon", idData.UniqueIdCounter + 1);
			idData.GameIds.Add(++idData.UniqueIdCounter, _initialWeapons[rngWeapon]);

			foreach (var id in idData.GameIds)
			{
				playerData.Inventory.Add(new EquipmentData { Id = id.Key, Rarity = "Common", Level = 1 });
			}

			playerData.Emoji.Add("EmojiAngry");
			playerData.Emoji.Add("EmojiLove");
			playerData.Emoji.Add("EmojiAngel");
			playerData.Emoji.Add("EmojiCool");
			playerData.Emoji.Add("EmojiSick");

			return playerData;
		}
	}
}