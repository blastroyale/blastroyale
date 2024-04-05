using UnityEngine;

namespace FirstLight.Game.Services
{
	public class LocalPrefsService
	{
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
		public ObservableField<bool> IsDialogueEnabled { get; } = CreateBoolSetting(nameof(IsDialogueEnabled), true);

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
		public ObservableField<bool> InvertSpecialCanceling { get; } = CreateBoolSetting(nameof(InvertSpecialCanceling), false);

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

		private static ObservableField<bool> CreateBoolSetting(string id, bool defaultValue)
		{
			return new ObservableResolverField<bool>(() => GetBool(id, defaultValue), val => SetBool(id, val));

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