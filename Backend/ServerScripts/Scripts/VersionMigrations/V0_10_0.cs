using System;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Server.SDK.Models;
using FirstLight.Server.SDK.Modules;
using Quantum;

namespace Scripts.Scripts.VersionMigrations
{

	/// <summary>
	/// Moving LastRepairTimestamp from NFTData to Equipment
	/// </summary>
	public class V0_10_0 : VersionMigrationScript
	{
		public override Environment GetEnvironment() => Environment.DEV;

		public override Version VersionApplied() => new Version("0.10.0");

		public override async Task<bool> MigrateData(string playerId, ServerState state)
		{
			if (!state.Has<EquipmentData>()) // players never logged in
			{
				return false;
			}
				
			var equipDataJson = state.GetRawJson<EquipmentData>();
			var jsonNode = JsonNode.Parse(equipDataJson);
			var inventory = jsonNode["Inventory"];
			var nftInventory = jsonNode["NftInventory"];
			foreach (var nft in nftInventory.AsObject())
			{
				var uniqueId = nft.Key;
				var nftData = nft.Value;
				try
				{
					var repairTime = nftData["LastRepairTimestamp"].AsValue().ToString();
					inventory[uniqueId]["LastRepairTimestamp"] = Int64.Parse(repairTime);
				}
				catch (Exception e)
				{
					Console.WriteLine($"Player {playerId} didnt had NftLastRepairTime due to being super outdated");
					return false;
				}
			}
			var newModel = ModelSerializer.Deserialize<EquipmentData>(jsonNode.ToJsonString());
			state.UpdateModel(newModel);
			return true;
		}
	}
}

