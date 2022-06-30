using System.Collections.Generic;
using Helpshift;
using UnityEngine;

namespace HelpshiftExample
{
    public class HSEventsListener: IHelpshiftEventsListener
    {

        public void AuthenticationFailedForUser(HelpshiftAuthenticationFailureReason reason)
        {
            Debug.Log("Helpshift - Authentication Failed for reason " + reason.ToString());
        }

        public void HandleHelpshiftEvent(string eventName, Dictionary<string, object> eventData)
        {
            string eventDataString = "";
            if (eventData != null)
            {
                foreach (KeyValuePair<string, object> kvp in eventData)
                {
                    eventDataString += kvp.Key + ":" + kvp.Value.ToString();
                }
            }
            Debug.Log("Helpshift - event_" + eventName + " " + eventDataString);
        }
    }
}
