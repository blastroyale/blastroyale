using UnityEngine;
namespace Helpshift
{

    /// <summary>
    /// Internal logger class to print the logs in the Android Studio / Xcode log console.
    /// As the unity Logs gets printed in the file and not on console
    /// </summary>
    public class HelpshiftInternalLogger
    {
#if UNITY_IOS || UNITY_ANDROID
        private static readonly string TAG = "HelpshiftUnityPlugin";
#endif

#if UNITY_ANDROID
        private static AndroidJavaClass hsInternalLogger = new AndroidJavaClass("com.helpshift.log.HSLogger");
#endif
        /*
	* Debug level logging
	*/
        public static void d(string message)
        {
#if UNITY_ANDROID
            hsInternalLogger.CallStatic("d", new object[] { TAG, message });
#elif UNITY_IOS
            HelpshiftiOSLog.log("DEBUG: " + TAG + ": " + message);
#endif
        }

        /*
         * Error level logging
        */
        public static void e(string message)
        {
#if UNITY_ANDROID
            hsInternalLogger.CallStatic("e", new object[] { TAG, message });
#elif UNITY_IOS
            HelpshiftiOSLog.log("ERROR: " + TAG + ": " + message);
#endif
        }

        /*
	 * Warn level logging
	*/
        public static void w(string message)
        {
#if UNITY_ANDROID
            hsInternalLogger.CallStatic("w", new object[] { TAG, message });
#elif UNITY_IOS
            HelpshiftiOSLog.log("WARN: " + TAG + ": " + message);
#endif
        }
    }
}
