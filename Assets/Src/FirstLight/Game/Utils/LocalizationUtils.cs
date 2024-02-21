using FirstLight.Game.Configs;
using FirstLight.Game.Ids;
using FirstLight.Game.Infos;
using FirstLight.Game.Services.Party;
using I2.Loc;
using Quantum;

namespace FirstLight.Game.Utils
{
	/// <summary>
	/// Extensions and helpers to manage localizations.
	/// </summary>
	public static class LocalizationUtils
	{
		/// <summary>
		/// Gets the translation for the given<paramref name="strategy"/>
		/// </summary>
		public static string GetLocalization(this GameCompletionStrategy strategy)
		{
			return strategy switch
			{
				GameCompletionStrategy.EveryoneDead => ScriptLocalization.UITMatchmaking.br_mode_desc,
				GameCompletionStrategy.KillCount    => ScriptLocalization.UITMatchmaking.dm_mode_desc,
				_                                   => ""
			};
		}

		/// <summary>
		/// Requests the localized text representing the given <paramref name="gameId"/> as a string
		/// </summary>
		public static string GetTranslationGameIdString(this string gameId)
		{
			return LocalizationManager.GetTranslation($"{nameof(ScriptTerms.GameIds)}/{gameId}");
		}

		/// <summary>
		/// Requests the localized text representing the given <paramref name="stat"/>
		/// </summary>
		public static string GetLocalization(this StatType stat)
		{
			return LocalizationManager.GetTranslation($"{nameof(ScriptTerms.General)}/{stat.ToString()}");
		}

		/// <summary>
		/// Get's the translation string of the given <paramref name="id"/>
		/// </summary>
		public static string GetLocalization(this GameId id)
		{
			return LocalizationManager.TryGetTranslation(id.GetLocalizationKey(), out var translation) ? translation : id.ToString();
		}

		/// <summary>
		/// Get's the translation string of the given <paramref name="id"/>
		/// </summary>
		public static string GetCurrencyLocalization(this GameId id, uint amount)
		{
			return GetCurrencyLocalization(id, (ulong) amount);
		}

		/// <summary>
		/// Get's the translation string of the given <paramref name="id"/>
		/// </summary>
		public static string GetCurrencyLocalization(this GameId id, ulong amount)
		{
			var key = id.GetLocalizationKey();
			if (amount != 1 && LocalizationManager.TryGetTranslation(key + "_Plural", out var translation))
			{
				return translation;
			}

			return LocalizationManager.GetTranslation(key);
		}

		/// <summary>
		/// Get's the translation string of the given <paramref name="id"/>
		/// </summary>
		public static string GetGameIdGroupLocalization(this GameIdGroup id)
		{
			var key = id.GetLocalizationKey();
			return LocalizationManager.GetTranslation(key);
		}

		/// <summary>
		/// Get's the translation string of the given <paramref name="id"/> + Description;
		/// </summary>
		public static string GetDescriptionLocalization(this GameId id)
		{
			var localized = LocalizationManager.GetTranslation(id.GetLocalizationKey() + "Description");
			if (string.IsNullOrEmpty(localized)) return id.ToString();
			return localized;
		}

		/// <summary>
		/// Get's the translation string of the given <paramref name="id"/>
		/// </summary>
		public static string GetLocalization(this ConsumableType id)
		{
			return LocalizationManager.GetTranslation($"{nameof(ScriptTerms.GameIds)}/{id.ToString()}");
		}

		/// <summary>
		/// Gets the translation term of the given <paramref name="stat"/>
		/// </summary>
		public static string GetLocalizationKey(this EquipmentStatType stat)
		{
			return $"{nameof(ScriptTerms.General)}/{stat.ToString()}";
		}

		/// <summary>
		/// Requests the localized text representing the given <paramref name="stat"/>
		/// </summary>
		public static string GetLocalization(this EquipmentStatType stat)
		{
			return LocalizationManager.GetTranslation(stat.GetLocalizationKey()).ToUpperInvariant();
		}

		/// <summary>
		/// Gets the translation term of the given <paramref name="id"/>
		/// </summary>
		public static string GetLocalizationKey(this GameId id)
		{
			return $"{nameof(ScriptTerms.GameIds)}/{id.ToString()}";
		}

		/// <summary>
		/// Gets the translation term of the given <paramref name="id"/>
		/// </summary>
		public static string GetLocalizationKey(this GameIdGroup id)
		{
			return $"{nameof(ScriptTerms.GameIdGroups)}/{id.ToString()}";
		}

		/// <summary>
		/// Gets the translation term of the given <paramref name="id"/>
		/// </summary>
		public static string GetLocalizationKey(this EquipmentRarity rarity)
		{
			return $"{nameof(ScriptTerms.UITEquipment)}/rarity_{rarity.ToString().ToLowerInvariant()}";
		}

		public static string GetLocalization(this EquipmentRarity rarity)
		{
			return LocalizationManager.GetTranslation(GetLocalizationKey(rarity));
		}

		/// <summary>
		/// Gets the translation term of the given <paramref name="id"/> for a match type
		/// </summary>
		public static string GetLocalization(this MatchType id)
		{
			return LocalizationManager.GetTranslation($"{nameof(ScriptTerms.GameIds)}/{id.ToString()}");
		}

		/// <summary>
		/// Gets the translation for  of the given <paramref name="group"/>
		/// </summary>
		public static string GetMapDropPointLocalization(this string dropPointName)
		{
			return LocalizationManager.GetTranslation($"MapDropPoints/{dropPointName}");
		}

		/// <summary>
		/// Gets the localization for the given <paramref name="grade"/>.
		/// </summary>
		public static string GetLocalization(this EquipmentGrade grade)
		{
			return LocalizationManager.GetTranslation($"UITEquipment/grade_{(int) grade + 1}");
		}

		/// <summary>
		/// Gets the localization for the given <paramref name="manufacturer"/>.
		/// </summary>
		public static string GetLocalization(this EquipmentManufacturer manufacturer)
		{
			return LocalizationManager.GetTranslation(
				$"UITEquipment/manufacturer_{manufacturer.ToString().ToLowerInvariant()}");
		}

		/// <summary>
		/// Gets the localization for the given <paramref name="edition"/>.
		/// </summary>
		public static string GetLocalization(this EquipmentEdition edition)
		{
			return LocalizationManager.GetTranslation($"UITEquipment/edition_{edition.ToString().ToLowerInvariant()}");
		}

		/// <summary>
		/// Gets the localization for the given <paramref name="material"/>.
		/// </summary>
		public static string GetLocalization(this EquipmentMaterial material)
		{
			return LocalizationManager.GetTranslation($"UITEquipment/material_{material.ToString().ToLowerInvariant()}");
		}

		/// <summary>
		/// Gets the localization for the given <paramref name="faction"/>.
		/// </summary>
		public static string GetLocalization(this EquipmentFaction faction)
		{
			return LocalizationManager.GetTranslation($"UITEquipment/faction_{faction.ToString().ToLowerInvariant()}");
		}

		/// <summary>
		/// Requests the localized text representing the given <paramref name="error"/> as a string
		/// </summary>
		public static string GetTranslation(this PartyErrors error)
		{
			return LocalizationManager.GetTranslation($"{nameof(ScriptTerms.UITSquads)}/error_{error}");
		}

		/// <summary>
		/// Requests the localized text representing the given <paramref name="gameModeId"/> as a string
		/// </summary>
		public static string GetTranslationForGameModeId(string gameModeId)
		{
			var term = $"{nameof(ScriptTerms.UITHomeScreen)}/gamemode_{gameModeId}";
			if (LocalizationManager.TryGetTranslation(term, out var translation))
			{
				return translation;
			}

			return gameModeId.ToUpperInvariant();
		}

		public static string GetTranslation(this UnlockSystem unlockSystem)
		{
			var term = $"{nameof(ScriptTerms.UnlockSystems)}/" + unlockSystem.ToString();
			if (LocalizationManager.TryGetTranslation(term, out var translation))
			{
				return translation;
			}

			return unlockSystem.ToString().ToUpperInvariant();
		}
	}
}