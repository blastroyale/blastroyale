/*
 * Copyright 2020, Helpshift, Inc.
 * All rights reserved
 */

#if UNITY_IOS
using System.Runtime.InteropServices;
using System.Collections.Generic;
using HSMiniJSON;

namespace Helpshift
{
    /// <summary>
    /// Contains the event names that can be received from HelpshiftDelegate
    /// </summary>
    static class HelpshiftEventConstants
    {
        public const string HandleHelpshiftEvent = "handleHelpshiftEvent";
        public const string AuthenticationFailed = "authenticationFailedForUserWithReason";
    }

    public class HelpshiftXiOSDelegate
    {
        /// <summary>
        /// The signature of the delegate that is invoked when we receive message on unity side from native
        /// </summary>
        /// <param name="methodName">The name of the method that was invoked</param>
        /// <param name="parametersJson">The JSON representation of the parameters of the method</param>
        private delegate void UnitySupportMessageCallback(string methodName, string parametersJson);
/*
        /// <summary>
        /// Runtimed linked C method to register callback with the declared signature
        /// </summary>
        /// <param name="callback">The callback object</param>
        [DllImport("__Internal")]
        private static extern void HsRegisterHelpshiftDelegateCallback(UnitySupportMessageCallback callback);
*/
        /// <summary>
        /// The shared instance of this class.
        /// </summary>
        private static HelpshiftXiOSDelegate sharedDelegate;

        /// <summary>
        /// The external delegate object that has been passed by the developer
        /// </summary>
        internal IHelpshiftEventsListener externalDelegate;

        private HelpshiftXiOSDelegate() { }


        public static HelpshiftXiOSDelegate GetInstance()
        {
            if (sharedDelegate == null)
            {
                sharedDelegate = new HelpshiftXiOSDelegate();
            }

            return sharedDelegate;
        }

        /// <summary>
        /// Call this method to set the external delegate to listen to Helpshift Events
        /// </summary>
        /// <param name="helpshiftEventListener"></param>
        public static void SetExternalDelegate(IHelpshiftEventsListener helpshiftEventListener)
        {
            GetInstance().externalDelegate = helpshiftEventListener;
            RegisterHelpshiftDelegateCallback();
        }

        // Private Helpers

        private static void RegisterHelpshiftDelegateCallback()
        {
            //HsRegisterHelpshiftDelegateCallback(UnityHelpshiftDelegateCallbackImpl);
        }

        [MonoPInvokeCallback(typeof(UnitySupportMessageCallback))]
        private static void UnityHelpshiftDelegateCallbackImpl(string methodName, string parametersJson)
        {
            IHelpshiftEventsListener externalDelegate = GetInstance().externalDelegate;

            if (externalDelegate == null)
            {
                return;
            }

            if (methodName == HelpshiftEventConstants.HandleHelpshiftEvent)
            {
                Dictionary<string, object> eventJson = (Dictionary<string, object>)Json.Deserialize(parametersJson);
                string eventName = "";
                if(eventJson.ContainsKey("eventName"))
                {
                    eventName = (string)eventJson["eventName"];
                }

                object eventData = null;
                if(eventJson.ContainsKey("eventData"))
                {
                    eventData = eventJson["eventData"];
                }

                Dictionary<string, object> eventDataJson = null;
                if(eventData != null)
                {
                    eventDataJson = (Dictionary<string, object>)eventData;
                }

                externalDelegate.HandleHelpshiftEvent(eventName, eventDataJson);
            }
            else if (methodName == HelpshiftEventConstants.AuthenticationFailed)
            {
                Dictionary<string, object> failureReasonJson = (Dictionary<string, object>)Json.Deserialize(parametersJson);
                object failureReasonObject = failureReasonJson["reason"];
                int failureReason = System.Convert.ToInt32(failureReasonObject);

                HelpshiftAuthenticationFailureReason authFailureReason = HelpshiftAuthenticationFailureReason.UNKNOWN;

                if(failureReason == 0) // Auth token not provided
                {
                    authFailureReason = HelpshiftAuthenticationFailureReason.AUTH_TOKEN_NOT_PROVIDED;
                }
                else if (failureReason == 1) // Invalid auth token
                {
                    authFailureReason = HelpshiftAuthenticationFailureReason.INVALID_AUTH_TOKEN;
                }


                externalDelegate.AuthenticationFailedForUser(authFailureReason);
            }
        }
    }
}
#endif
