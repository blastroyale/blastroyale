/*
* Copyright 2020, Helpshift, Inc.
* All rights reserved
*/

#if UNITY_ANDROID
using UnityEngine;
using System;
using System.Collections.Generic;
using HSMiniJSON;

namespace Helpshift
{
    public class HelpshiftXAndroid : HelpshiftSdk
    {

        private AndroidJavaClass jc;
        private AndroidJavaObject currentActivity, application;
        private AndroidJavaClass hsUnityApiClass;

        public HelpshiftXAndroid()
        {
            this.jc = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            this.currentActivity = jc.GetStatic<AndroidJavaObject>("currentActivity");
            this.application = currentActivity.Call<AndroidJavaObject>("getApplication");
            this.hsUnityApiClass = new AndroidJavaClass("com.helpshift.unityproxy.HelpshiftUnityAPI");
        }

        public void Install(string platformId, string domain, Dictionary<string, object> configMap)
        {
            string jsonSerializedConfig = SerializeMap(configMap);
            hsUnityApiClass.CallStatic("install", new object[] { this.application, platformId, domain, jsonSerializedConfig });

            HelpshiftInternalLogger.d("Install called : Domain : " + domain + "Platform ID : " + platformId + ", Config : " + jsonSerializedConfig);
        }

        public void RegisterPushToken(string deviceToken)
        {
            HelpshiftInternalLogger.d("Register device token :" + deviceToken);
            hsUnityApiClass.CallStatic("registerPushToken", new object[] { deviceToken });
        }

        public void Login(Dictionary<string, string> userData)
        {

            if (userData == null)
            {
                userData = new Dictionary<string, string>();
            }
            HelpshiftInternalLogger.d("Login called : " + userData.ToString());
            hsUnityApiClass.CallStatic("login", new object[] { Json.Serialize(userData) });
        }

        public void ClearAnonymousUserOnLogin()
        {
            HelpshiftInternalLogger.d("ClearAnonymouseUserOnLogin api called");
            hsUnityApiClass.CallStatic("clearAnonymousUserOnLogin");
        }

        public void Logout()
        {
            HelpshiftInternalLogger.d("logout api called");
            hsUnityApiClass.CallStatic("logout");
        }

        private string SerializeMap(Dictionary<string, object> configMap)
        {
            if (configMap != null)
            {
                return Json.Serialize(configMap);
            }
            return "";
        }

        public void ShowConversation(Dictionary<string, object> configMap)
        {
            string config = SerializeMap(configMap);
            HelpshiftInternalLogger.d("show conversation api called with config" + config);
            hsUnityApiClass.CallStatic("showConversationUnity", new object[] { this.currentActivity, config });
        }

        public void ShowFAQs(Dictionary<string, object> configMap)
        {
            string config = SerializeMap(configMap);
            HelpshiftInternalLogger.d("show FAQs api called with config" + configMap);
            hsUnityApiClass.CallStatic("showFAQsUnity", new object[] { this.currentActivity, config });
        }

        public void ShowSingleFAQ(string faqId, Dictionary<string, object> configMap)
        {
            string config = SerializeMap(configMap);
            HelpshiftInternalLogger.d("show single FAQ api called with config" + config);
            hsUnityApiClass.CallStatic("showSingleFAQUnity", new object[] { this.currentActivity, faqId, config });
        }

        public void ShowFAQSection(string sectionId, Dictionary<string, object> configMap)
        {
            string config = SerializeMap(configMap);
            HelpshiftInternalLogger.d("show section api called with config" + config);
            hsUnityApiClass.CallStatic("showFAQSectionUnity", new object[] { this.currentActivity, sectionId, config });
        }

        public void RequestUnreadMessageCount(Boolean shouldFetchFromServer)
        {
            HelpshiftInternalLogger.d("request unread message count api called : shouldFetchFromServer" + shouldFetchFromServer);
            hsUnityApiClass.CallStatic("requestUnreadMessageCountUnity", new object[] { shouldFetchFromServer });
        }

        public void HandlePushNotification(Dictionary<string, object> pushNotificationData)
        {
            string pushData = SerializeMap(pushNotificationData);
            HelpshiftInternalLogger.d("Handle push notification : data :" + pushData);
            hsUnityApiClass.CallStatic("handlePush", new object[] { pushData });
        }


        public void SetHelpshiftEventsListener(IHelpshiftEventsListener eventsListener)
        {
            HelpshiftInternalLogger.d("Event listener is set");
            HelpshiftAndroidEventsListener internalEventListener = new HelpshiftAndroidEventsListener(eventsListener);
            hsUnityApiClass.CallStatic("setHelpshiftEventsListener", new object[] { internalEventListener });
        }


        public void SetSDKLanguage(string locale)
        {
            HelpshiftInternalLogger.d("setLanguage api called for language " + locale);
            hsUnityApiClass.CallStatic("setLanguage", new object[] { locale });
        }

        public void LeaveBreadcrumb(string breadcrumb)
        {
            HelpshiftInternalLogger.d("leaveBreadcrumb api called for message " + breadcrumb);
            hsUnityApiClass.CallStatic("leaveBreadCrumb", breadcrumb);
        }

        public void ClearBreadcrumbs()
        {
            HelpshiftInternalLogger.d("clearBreadCrumbs api called");
            hsUnityApiClass.CallStatic("clearBreadCrumbs");
        }
    }
    public class HelpshiftAndroidLog
    {
		private static AndroidJavaClass logger = new AndroidJavaClass("com.helpshift.HSDebugLog");

        private HelpshiftAndroidLog()
        {
        }

        public static int v(String tag, String log)
        {
			return logger.CallStatic<int>("v", new object[] {tag, log});
        }

        public static int d(String tag, String log)
        {
            return logger.CallStatic<int>("d", new object[] {tag, log});
        }

        public static int i(String tag, String log)
        {
            return logger.CallStatic<int>("i", new object[] {tag, log});
        }

        public static int w(String tag, String log)
        {
            return logger.CallStatic<int>("w", new object[] {tag, log});
        }

        public static int e(String tag, String log)
        {
            return logger.CallStatic<int>("e", new object[] {tag, log});
        }
    }
}
#endif
