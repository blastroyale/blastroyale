using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
#if UNITY_IOS || UNITY_ANDROID
using Helpshift;
#endif

namespace HelpshiftExample
{
    public class HelpshiftExampleScript : MonoBehaviour
    {
#if UNITY_IOS || UNITY_ANDROID

        private HelpshiftSdk _helpshiftX;

        void Awake()
        {
            _helpshiftX = HelpshiftSdk.GetInstance();
            string domainName = "<your-domain>.helpshift.com";
            string platformId;
#if UNITY_ANDROID
            platformId = "<your-app-android-platform-id>";
#elif UNITY_IOS
            platformId = "<your-app-ios-platform-id>";
#endif
            _helpshiftX.Install(platformId, domainName, GetInstallConfig());
            Debug.Log("Unity - Awake called");
            _helpshiftX.SetHelpshiftEventsListener(new HSEventsListener());
        }

        public void ShowConversation()
        {
#if UNITY_ANDROID || UNITY_IOS
            _helpshiftX.ShowConversation(GetConversationConfig());
            Debug.Log("Helpshift - ShowConversation called");
#endif
        }

        public void ShowFAQs()
        {
#if UNITY_ANDROID || UNITY_IOS
            _helpshiftX.ShowFAQs(GetConversationConfig());
            Debug.Log("Helpshift - ShowFAQs called");
#endif
        }

        public void ShowFAQSection()
        {
#if UNITY_ANDROID || UNITY_IOS
            GameObject inputFieldGo = GameObject.FindGameObjectWithTag("sectionid");
            InputField inputFieldCo = inputFieldGo.GetComponent<InputField>();
            try
            {
                Convert.ToInt16(inputFieldCo.text);
                _helpshiftX.ShowFAQSection(inputFieldCo.text, GetConversationConfig());
                Debug.Log("Helpshift - ShowFAQSection called");
            }
            catch (FormatException e)
            {
                Debug.Log("Input string is not a sequence of digits : " + e);
            }
#endif
        }

        public void ShowSingleFAQ()
        {
#if UNITY_ANDROID || UNITY_IOS
            GameObject inputFieldGo = GameObject.FindGameObjectWithTag("faqidd");
            InputField inputFieldCo = inputFieldGo.GetComponent<InputField>();
            try
            {
                Convert.ToInt16(inputFieldCo.text);
                _helpshiftX.ShowSingleFAQ(inputFieldCo.text, GetConversationConfig());
                Debug.Log("Helpshift - ShowSingleFAQ called");
            }
            catch (FormatException e)
            {
                Debug.Log("Input string is not a sequence of digits : " + e);
            }
#endif
        }


        public void RequestUnreadCount()
        {
#if UNITY_ANDROID || UNITY_IOS
            _helpshiftX.RequestUnreadMessageCount(true);
            Debug.Log("Helpshift - RequestUnreadCount called");
#endif
        }


        public void Login()
        {
#if UNITY_ANDROID || UNITY_IOS
            _helpshiftX.Login(GetUserDetails());
            Debug.Log("Helpshift - Login called");
#endif

        }

        public void Logout()
        {
#if UNITY_ANDROID || UNITY_IOS
            _helpshiftX.Logout();
            Debug.Log("Helpshift - Logout called");
#endif
        }

        public void CustomizedApi()
        {
#if UNITY_ANDROID || UNITY_IOS
            // Call any custom api you want from here.
            //_helpshiftX.ClearAnonymousUserOnLogin();
            //_helpshiftX.SetSDKLanguage("Fr");
            Debug.Log("Helpshift - CustomizedApi called");
#endif
        }

        private Dictionary<string, string> GetUserDetails()
        {
            Dictionary<string, string> userDetails = new Dictionary<string, string>
            {
                { "userId", "<your-user-id>" },
                { "userEmail", "<your-user-email>" },
                { "userName", "<your-username>" }
            };
            // uncomment and pass the valid token here to check the user authentication feature
            // userDetails.Add("userAuthToken", "<some_random_token_to_check>");
            return userDetails;
        }

        private Dictionary<string, object> GetConversationConfig()
        {
            Dictionary<string, object> config = new Dictionary<string, object>
            {
                { "tags", new String[] { "foo", "bar" } },
                { "customIssueFields", GetCifs() }
            };
            return config;
        }


        public static Dictionary<string, object> GetCifs()
        {
            Dictionary<string, string> joiningDate = new Dictionary<string, string>();
            joiningDate.Add("type", "date");
            joiningDate.Add("value", "1505927361535");

            Dictionary<string, string> stockLevel = new Dictionary<string, string>();
            stockLevel.Add("type", "n");
            stockLevel.Add("value", "1505");

            Dictionary<string, string> employeeName = new Dictionary<string, string>();
            employeeName.Add("type", "sl");
            employeeName.Add("value", "Bugs helpshift");

            Dictionary<string, string> isPro = new Dictionary<string, string>();
            isPro.Add("type", "b");
            isPro.Add("value", "true");

            Dictionary<string, object> cifDictionary = new Dictionary<string, object>();
            cifDictionary.Add("joining_date", joiningDate);
            cifDictionary.Add("stock_level", stockLevel);
            cifDictionary.Add("employee_name", employeeName);
            cifDictionary.Add("is_pro", isPro);
            return cifDictionary;
        }

        private Dictionary<string, object> GetInstallConfig()
        {

            Dictionary<string, object> installDictionary = new Dictionary<string, object>();
            installDictionary.Add(HelpshiftSdk.ENABLE_LOGGING, true);
#if UNITY_ANDROID
            //installDictionary.Add(HelpshiftSdk.NOTIFICATION_SOUND_ID, R.raw.custom_notification);
            //installDictionary.Add(HelpshiftSdk.NOTIFICATION_ICON, R.drawable.hs__conversation_image);
            //installDictionary.Add(HelpshiftSdk.NOTIFICATION_CHANNEL_ID, SampleAppConstants.CHANNEL_ID);
            //installDictionary.Add(HelpshiftSdk.NOTIFICATION_LARGE_ICON, R.drawable.airplane);
#endif
            return installDictionary;
        }
#endif
    }
}