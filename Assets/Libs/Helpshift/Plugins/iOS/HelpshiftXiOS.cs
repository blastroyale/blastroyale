
/*
 * Copyright 2020, Helpshift, Inc.
 * All rights reserved
 */

#if UNITY_IPHONE

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using HSMiniJSON;

namespace Helpshift
{
    /// <summary>
    /// This class contains all the iOS APIs corresponding to the public APIs
    /// </summary>
    public class HelpshiftXiOS
    {
        // Runtime linked C methods. These methods should be named exactlly as they are declared in the native code.

        [DllImport("__Internal")]
        private static extern void HsInstallForPlatformIdWithConfig(string platformId, string domainName, string jsonOptionsDict);

        [DllImport("__Internal")]
        private static extern void HsShowConversationWithConfig(string jsonConfigDictionary);

        [DllImport("__Internal")]
        private static extern void HsSetLanguage(string languageCode);

        [DllImport("__Internal")]
        private static extern void HsLogin(string jsonUserDetailsDict);

        [DllImport("__Internal")]
        private static extern void HsLogout();

        [DllImport("__Internal")]
        private static extern void HsClearAnonymousUserOnLogin();

        [DllImport("__Internal")]
        private static extern void HsRegisterDeviceToken(string deviceToken);

        [DllImport("__Internal")]
        private static extern void HsPauseDisplayOfInAppNotification(bool pauseInApp);

        [DllImport("__Internal")]
        private static extern void HsHandleNotificationWithUserInfoDictionary(string jsonNotificationDataDict, bool isAppLaunch);

        [DllImport("__Internal")]
        private static extern void HsShowFaqsWithConfig(string configDictionaryString);

        [DllImport("__Internal")]
        private static extern void HsShowFaqSectionWithConfig(string faqSectionPublishID, string configDictionaryString);

        [DllImport("__Internal")]
        private static extern void HsShowSingleFaqWithConfig(string faqPublishID, string configDictionaryString);

        [DllImport("__Internal")]
        private static extern void HsRequestUnreadMessageCount(bool shouldFetchFromServer);

        [DllImport("__Internal")]
        private static extern void HsLeaveBreadcrumb(string breadcrumb);

        [DllImport("__Internal")]
        private static extern void HsClearBreadcrumbs();

        public HelpshiftXiOS()
        {

        }

        // Public APIs

        public void Install(string platformId, string domainName, Dictionary<string, object> installConfig)
        {
            HelpshiftInternalLogger.d("Install called : Domain : " + domainName + "Platform ID : " + platformId + ", Config : " + SerializeDictionary(installConfig));
            HsInstallForPlatformIdWithConfig(platformId, domainName, SerializeDictionary(installConfig));
        }

        public void ShowConversation(Dictionary<string, object> config)
        {
            HelpshiftInternalLogger.d("show conversation api called with config" + SerializeDictionary(config));
            HsShowConversationWithConfig(SerializeDictionary(config));
        }

        public void ShowFAQs(Dictionary<string, object> configMap)
        {
            HelpshiftInternalLogger.d("show FAQs api called with config" + SerializeDictionary(configMap));
            HsShowFaqsWithConfig(SerializeDictionary(configMap));
        }

        public void ShowSingleFAQ(string faqId, Dictionary<string, object> configMap)
        {
            HelpshiftInternalLogger.d("show single FAQ api called with faqId" + faqId + " config" + SerializeDictionary(configMap));
            HsShowSingleFaqWithConfig(faqId, SerializeDictionary(configMap));
        }

        public void ShowFAQSection(string sectionId, Dictionary<string, object> configMap)
        {
            HelpshiftInternalLogger.d("show FAQ section api called with sectionId" + sectionId+ " config" + SerializeDictionary(configMap));
            HsShowFaqSectionWithConfig(sectionId, SerializeDictionary(configMap));
        }

        public void RequestUnreadMessageCount(Boolean shouldFetchFromServer)
        {
            HelpshiftInternalLogger.d("request unread message count api called with remote fetch : " + shouldFetchFromServer); 
            HsRequestUnreadMessageCount(shouldFetchFromServer);
        }

        public void SetSDKLanguage(string languageCode)
        {
            HelpshiftInternalLogger.d("setLanguage api called for language " + languageCode);
            HsSetLanguage(languageCode);
        }

        public void Login(Dictionary<string, string> userDetails)
        {
            if(userDetails == null)
            {
                HelpshiftInternalLogger.e("userDetails are null in Login API!");
                userDetails = new Dictionary<string, string>();
            }
            HelpshiftInternalLogger.d("Login called : " + userDetails);
            HsLogin(Json.Serialize(userDetails));
        }

        public void Logout()
        {
            HelpshiftInternalLogger.d("logout api called");
            HsLogout();
        }

        public void ClearAnonymousUserOnLogin()
        {
            HelpshiftInternalLogger.d("ClearAnonymouseUserOnLogin api called");
            HsClearAnonymousUserOnLogin();
        }

        public void RegisterPushToken(string deviceToken)
        {
            HelpshiftInternalLogger.d("Register device token :" + deviceToken);
            HsRegisterDeviceToken(deviceToken);
        }

        public void PauseDisplayOfInAppNotification(bool pauseInAppNotifications)
        {
            HelpshiftInternalLogger.d("Pause in-app notification called with shouldPause :" + pauseInAppNotifications);
            HsPauseDisplayOfInAppNotification(pauseInAppNotifications);
        }

        public void HandlePushNotification(Dictionary<string, object> notificationDataDict)
        {
            HelpshiftInternalLogger.d("Handle push notification data :" + SerializeDictionary(notificationDataDict));
            HsHandleNotificationWithUserInfoDictionary(SerializeDictionary(notificationDataDict), false);
        }

        public void SetHelpshiftEventsListener(IHelpshiftEventsListener listener)
        {
            HelpshiftInternalLogger.d("Event listener is set");
            HelpshiftXiOSDelegate.SetExternalDelegate(listener);
        }

        public void LeaveBreadcrumb(string breadcrumb)
        {
            HsLeaveBreadcrumb(breadcrumb);
        }

        public void ClearBreadcrumbs()
        {
            HsClearBreadcrumbs();
        }


        // Private Helpers

        private string SerializeDictionary(Dictionary<string, object> configMap)
        {
            if (configMap == null)
            {
                configMap = new Dictionary<string, object>();
            }
            return Json.Serialize(configMap);
        }
    }

    /// <summary>
    /// Class for adding debug logs for iOS
    /// </summary>
    public class HelpshiftiOSLog
    {

        [DllImport("__Internal")]
        private static extern void HsAddDebugLog(string log);

        [DllImport("__Internal")]
        private static extern void HsLog(string log);

        private HelpshiftiOSLog()
        {
        }

        public static int v(String tag, String log)
        {
            HsAddDebugLog("HelpshiftLog:Verbose::" + tag + "::" + log);
            return 0;
        }

        public static int d(String tag, String log)
        {
            HsAddDebugLog("HelpshiftLog:Debug::" + tag + "::" + log);
            return 0;
        }

        public static int i(String tag, String log)
        {
            HsAddDebugLog("HelpshiftLog:Info::" + tag + "::" + log);
            return 0;
        }

        public static int w(String tag, String log)
        {
            HsAddDebugLog("HelpshiftLog:Warn::" + tag + "::" + log);
            return 0;
        }

        public static int e(String tag, String log)
        {
            HsAddDebugLog("HelpshiftLog:Error::" + tag + "::" + log);
            return 0;
        }

        public static int log(String log)
        {
            HsLog(log);
            return 0;
        }

    }
}

#endif