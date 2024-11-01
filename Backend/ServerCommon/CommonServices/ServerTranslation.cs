using FirstLight.Game.Infos;
using I2.Loc;
using Quantum;

namespace ServerCommon.CommonServices
{
	public static class ServerTranslation
	{
		/// <summary>
		/// Gets the translation term of the given <paramref name="gameid"/>
		/// </summary>
		public static string GetTranslationTerm(this GameId e)
		{
			return $"{nameof(ScriptTerms.GameIds)}/{e.ToString()}";
		}
		
		/// <summary>
		/// Gets the translation term of the given <paramref name="stat"/>
		/// </summary>
		public static string GetTranslationTerm(this EquipmentStatType stat)
		{
			return $"{nameof(ScriptTerms.General)}/{stat.ToString()}";
		}
	}
}

