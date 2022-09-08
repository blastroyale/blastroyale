using System;
using System.Threading.Tasks;
using FirstLight.Game.Data;
using FirstLight.Server.SDK.Models;
using Quantum;

namespace Scripts.Scripts.VersionMigrations
{
	public class V0_6_0 : VersionMigrationScript
	{
		public override PlayfabEnvironment GetEnvironment() => PlayfabEnvironment.STAGING;

		public override Version VersionApplied() => new Version("0.6.0");

		public override async Task<bool> MigrateData(string playerId, ServerState state)
		{
			if (state.ContainsKey("FirstLight.Game.Data.DataTypes.NftEquipmentData"))
			{
				await DeleteStateKey(playerId, "FirstLight.Game.Data.DataTypes.NftEquipmentData");
			}
			var playerData = state.DeserializeModel<PlayerData>();
			return playerData.ResourcePools.Remove(GameId.EquipmentXP);
		}
	}
}

