using System;
using System.Collections.Generic;
using Quantum;

namespace FirstLight.Models
{
	/// <summary>
	/// Map of game id currencies to playfab currency names
	/// </summary>
	public static class PlayfabCurrencies
	{
		private static readonly IReadOnlyDictionary<GameId, string> CurrencyMap = new Dictionary<GameId, string>()
		{
			{ GameId.COIN, "CN" },
			{ GameId.CS, "CS" },
			{ GameId.BlastBuck, "BB" },
			{ GameId.XP, "XP" },
			{ GameId.RealMoney, "RM" },
			{ GameId.NOOB, "NB" },
			{ GameId.PartnerAPECOIN, "AP"},
			{ GameId.PartnerBEAM, "BE" },
			{ GameId.PartnerBLOCKLORDS, "BK" },
			{ GameId.PartnerBLOODLOOP, "BL" },
			{ GameId.PartnerBREED, "BD" },
			{ GameId.PartnerCROSSTHEAGES, "CA" },
			{ GameId.PartnerFARCANA, "FC" },
			{ GameId.PartnerGAM3SGG, "GG" },
			{ GameId.PartnerIMMUTABLE, "IM" },
			{ GameId.PartnerMEME, "MM" },
			{ GameId.PartnerMOCAVERSE, "MV" },
			{ GameId.PartnerNYANHEROES, "NH" },
			{ GameId.PartnerPIRATENATION, "PN" },
			{ GameId.PartnerPIXELMON, "PX" },
			{ GameId.PartnerPLANETMOJO, "PM" },
			{ GameId.PartnerSEEDIFY, "SD" },
			{ GameId.PartnerWILDERWORLD, "WW" },
			{ GameId.PartnerXBORG, "XB" },
			{ GameId.PartnerYGG, "YG"},
			{ GameId.FestiveSNOWFLAKE, "SF"},
			{ GameId.EventTicket, "ET"},
			{ GameId.FestiveLUNARCOIN, "LC"},
			{ GameId.FestiveFEATHER, "FT"},
			{ GameId.FestiveLANTERN, "LT"}
		};

		/// <summary>
		/// Gets the game id of a given setup currency in playfab
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public static GameId GetCurrency(string id)
		{
			foreach (var kp in CurrencyMap)
			{
				if (kp.Value.ToLowerInvariant() == id.ToLowerInvariant()) return kp.Key;
			}
			throw new Exception($"Currency {id} not registered in playfab currencies");
		}
		
		/// <summary>
		/// Gets how a currency is defined in Playfab
		/// </summary>
		public static string GetPlayfabCurrencyName(GameId id)
		{
			return CurrencyMap[id];
		}
	}
}