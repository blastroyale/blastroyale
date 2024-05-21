using UnityEngine;

namespace FirstLight.Game.Services
{
	public class LocalPrefsService
	{
		/// <summary>
		/// Stores the last selected map on gamemode selection screen
		/// </summary>
		public ObservableField<int> SelectedRankedMap { get; } = CreateIntSetting(nameof(SelectedRankedMap), 0);

		/// <summary>
		/// If the background music is enabled
		/// </summary>
		public ObservableField<bool> IsBGMEnabled { get; } = CreateBoolSetting(nameof(IsBGMEnabled), true);

		/// <summary>
		/// If the sound effects are enabled.
		/// </summary>
		public ObservableField<bool> IsSFXEnabled { get; } = CreateBoolSetting(nameof(IsSFXEnabled), true);

		/// <summary>
		/// If the dialogue is enabled.
		/// </summary>
		public ObservableField<bool> IsDialogueEnabled { get; } = CreateBoolSetting(nameof(IsDialogueEnabled), false);

		/// <summary>
		/// If the haptics (vibrations) are enabled.
		/// </summary>
		public ObservableField<bool> IsHapticsEnabled { get; } = CreateBoolSetting(nameof(IsHapticsEnabled), true);

		/// <summary>
		/// If we use overhead UI instead of the default (bottom of screen) one.
		/// </summary>
		public ObservableField<bool> UseOverheadUI { get; } = CreateBoolSetting(nameof(UseOverheadUI), false);

		/// <summary>
		/// Which specials canceling system is used.
		/// </summary>
		public ObservableField<bool> InvertSpecialCanceling { get; } = CreateBoolSetting(nameof(InvertSpecialCanceling), true);

		/// <summary>
		/// If the Aim and Move joysticks are swapped.
		/// </summary>
		public ObservableField<bool> SwapJoysticks { get; } = CreateBoolSetting(nameof(SwapJoysticks), false);

		/// <summary>
		/// If screen camera shake is enabled.
		/// </summary>
		public ObservableField<bool> IsScreenShakeEnabled { get; } = CreateBoolSetting(nameof(IsScreenShakeEnabled), true);

		/// <summary>
		/// If High FPS mode is enabled.
		/// </summary>
		public ObservableField<bool> IsFPSLimitEnabled { get; } = CreateBoolSetting(nameof(IsFPSLimitEnabled), false);

		/// <summary>
		/// The current server region.
		/// </summary>
		public ObservableField<string> ServerRegion { get; } = CreateStringSetting(nameof(ServerRegion), string.Empty);

		/// <summary>
		/// If we show the latency during the game
		/// </summary>
		public ObservableField<bool> ShowLatency { get; } = CreateBoolSetting(nameof(ShowLatency), false);

		/// <summary>
		/// If we show the rate and review prompt
		/// </summary>
		public ObservableField<bool> RateAndReviewPromptShown { get; } = CreateBoolSetting(nameof(RateAndReviewPromptShown), false);

		public ObservableField<int> GamesPlayed { get; } = CreateIntSetting(nameof(GamesPlayed), 0);
		
		private static ObservableField<bool> CreateBoolSetting(string key, bool defaultValue)
		{
			return new ObservableResolverField<bool>(() => GetBool(key, defaultValue), val => SetBool(key, val));

			static bool GetBool(string key, bool defaultValue)
			{
				return PlayerPrefs.GetInt(ConstructKey(key), defaultValue ? 1 : 0) == 1;
			}

			static void SetBool(string key, bool value)
			{
				PlayerPrefs.SetInt(ConstructKey(key), value ? 1 : 0);
				PlayerPrefs.Save();
			}
		}
		
		private static ObservableField<int> CreateIntSetting(string key, int defaultValue)
		{
			return new ObservableResolverField<int>(() => GetInt(key, defaultValue), val => SetInt(key, val));

			static int GetInt(string key, int defaultValue)
			{
				return PlayerPrefs.GetInt(ConstructKey(key), defaultValue);
			}

			static void SetInt(string key, int value)
			{
				PlayerPrefs.SetInt(ConstructKey(key), value);
				PlayerPrefs.Save();
			}
		}

		private static ObservableField<string> CreateStringSetting(string key, string defaultValue)
		{
			return new ObservableResolverField<string>(() => GetString(key, defaultValue), val => SetString(key, val));

			static string GetString(string key, string defaultValue)
			{
				return PlayerPrefs.GetString(ConstructKey(key), defaultValue);
			}

			static void SetString(string key, string value)
			{
				PlayerPrefs.SetString(ConstructKey(key), value);
				PlayerPrefs.Save();
			}
		}

		private static ObservableField<int> CreateIntSetting(string key, int defaultValue)
		{
			return new ObservableResolverField<int>(() => GetInt(key, defaultValue), val => SetInt(key, val));

			static int GetInt(string key, int defaultValue)
			{
				return PlayerPrefs.GetInt(ConstructKey(key), defaultValue);
			}

			static void SetInt(string key, int value)
			{
				PlayerPrefs.SetInt(ConstructKey(key), value);
				PlayerPrefs.Save();
			}
		}

		private static string ConstructKey(string id)
		{
			const string PREFIX = "ss_";

#if UNITY_EDITOR
			return PREFIX + id + ParrelSync.ClonesManager.GetArgument();
#else
			return PREFIX + id;
#endif
		}
	}
}