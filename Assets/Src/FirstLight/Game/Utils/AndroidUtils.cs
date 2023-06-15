using System;
using UnityEngine;

namespace FirstLight.Game.Utils
{
	/// <summary>
	/// Internal FLG android utilities
	/// </summary>
	public class AndroidUtils
	{

		/// <summary>
		/// Check if android is an old device by checking if its API level is <= 24
		/// </summary>
		public static bool IsOldAndroid()
		{
			try
			{
				return GetSDKVersion() < 24;
			}
			catch (Exception e)
			{
				return false;
			}
		}
		
		static int GetSDKVersion() {
			using (var version = new AndroidJavaClass("android.os.Build$VERSION")) {
				return version.GetStatic<int>("SDK_INT");
			}
		}
	}
}