#if UNITY_ANDROID
using System;
using System.Collections.Generic;
using UnityEngine;
using HSMiniJSON;

namespace Helpshift
{
    /// <summary>
    /// Internal event listener to delegate the events
    /// </summary>
    public class HelpshiftAndroidEventsListener : AndroidJavaProxy
    {
        private IHelpshiftEventsListener externalHelpshiftEventsListener;

        public HelpshiftAndroidEventsListener(IHelpshiftEventsListener externalEventsListener) :
            base("com.helpshift.unityproxy.HelpshiftEventDelegate")
        {
            this.externalHelpshiftEventsListener = externalEventsListener;
        }

        void onEventOccurred(string eventName, string eventDataJson)
        {
            Dictionary<string, object> eventData = new Dictionary<string, object>();
            if (!String.IsNullOrEmpty(eventDataJson))
            {
                eventData = (Dictionary<string, object>)Json.Deserialize(eventDataJson);
            }
            externalHelpshiftEventsListener.HandleHelpshiftEvent(eventName, eventData);
        }

        void onUserAuthenticationFailure(int reason)
        {
            HelpshiftAuthenticationFailureReason reasonValue = HelpshiftAuthenticationFailureReason.UNKNOWN;

            if (reason == 0)
            {
                reasonValue = HelpshiftAuthenticationFailureReason.AUTH_TOKEN_NOT_PROVIDED;
            }
            else if (reason == 1)
            {
                reasonValue = HelpshiftAuthenticationFailureReason.INVALID_AUTH_TOKEN;
            }

            externalHelpshiftEventsListener.AuthenticationFailedForUser(reasonValue);
        }
    }
}
#endif
