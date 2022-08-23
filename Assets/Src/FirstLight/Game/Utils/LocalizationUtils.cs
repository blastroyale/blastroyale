using System;
using System.Collections;
using System.Collections.Generic;
using FirstLight.Game.Ids;
using I2.Loc;
using Quantum;
using UnityEngine;

/// <summary>
/// Helper utility methods to manage localization
/// </summary>
public static class LocalizationUtils
{
	/// <summary>
	/// Returns localized region name for the provided region key from Photon master servers
	/// </summary>
	public static string GetRegionName(string regionKey)
	{
		switch (regionKey)
		{
			case "eu":
				return ScriptLocalization.MainMenu.ServerEuropeName;

			case "us":
				return ScriptLocalization.MainMenu.ServerAmericaName;

			case "hk":
				return ScriptLocalization.MainMenu.ServerAsiaName;
			
			default:
				return regionKey;
		}
	}

	/// <summary>
	/// Returns localized game mode name for the given Quantum game mode
	/// </summary>
	public static string GetGameModeName(GameMode mode)
	{
		switch (mode)
		{
			case GameMode.Tutorial:
				return ScriptLocalization.MainMenu.GameModeTuName;
			
			case GameMode.Deathmatch:
				return ScriptLocalization.MainMenu.GameModeDmName;
			
			case GameMode.BattleRoyale:
				return ScriptLocalization.MainMenu.GameModeBrName;
			
			default:
				return "Invalid";
		}
	}
	
	/// <summary>
	/// Returns localized match type name for the given Quantum game mode
	/// </summary>
	public static string GetMatchTypeName(MatchType mode)
	{
		switch (mode)
		{
			case MatchType.Casual:
				return ScriptLocalization.MainMenu.MatchTypeCasualName;
			
			case MatchType.Ranked:
				return ScriptLocalization.MainMenu.MatchTypeRankedName;
			
			default:
				return "Invalid";
		}
	}
}