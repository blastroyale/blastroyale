using System.Collections.Generic;
using BuffSystem;
using FirstLight.FLogger;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Logic;
using Quantum;

namespace FirstLight.Game.Services
{
	public interface IBuffService
	{
		/// <summary>
		/// Represents a virtual meta entity that is not a real simulation entity.
		/// This entity is calculated given assets the player have and contains modifiers to internal statistics
		/// </summary>
		public BuffVirtualEntity MetaEntity { get; }

		public GameId GetRelatedGameId(BuffStat stat);
		public string GetDisplayString(BuffStat stat);
	}

	public class BuffService : IBuffService
	{
		private IGameServices _services;
		private IGameLogic _data;

		public BuffService(IGameServices services, IGameLogic data)
		{
			_services = services;
			_data = data;

#if DEBUG
			_services.AuthenticationService.OnLogin += _ => FLog.Verbose("Buff Virtual Entity: " + MetaEntity);
#endif
		}

		public string GetDisplayString(BuffStat stat)
		{
			if (stat == BuffStat.PctBonusXP) return "Fame";
			if (stat == BuffStat.PctBonusCoins) return "Coins";
			if (stat == BuffStat.PctBonusBPP) return "Bpp";
			if (stat == BuffStat.PctBonusNoob) return "Noob";
			if (stat == BuffStat.PctBonusPartnerToken) return "Crypto";
			if (stat == BuffStat.PctBonusBBs) return "Blast Bucks";
			if (stat == BuffStat.PctBonusSnowflakes) return "Snowflake";
			return "Goo";
		}

		public GameId GetRelatedGameId(BuffStat stat)
		{
			if (stat == BuffStat.PctBonusXP) return GameId.XP;
			if (stat == BuffStat.PctBonusCoins) return GameId.COIN;
			if (stat == BuffStat.PctBonusBPP) return GameId.BPP;
			if (stat == BuffStat.PctBonusNoob) return GameId.NOOB;
			if (stat == BuffStat.PctBonusPartnerToken) return GameId.Any;
			if (stat == BuffStat.PctBonusBBs) return GameId.BlastBuck;
			if (stat == BuffStat.PctBonusSnowflakes) return GameId.FestiveSNOWFLAKE;
			return GameId.Any;
		}

		public BuffVirtualEntity MetaEntity => _data.BuffsLogic.CalculateMetaEntity();
	}
}