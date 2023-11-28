using System;
using System.Collections.Generic;

namespace Helpshift
{
    /// <summary>
    /// The interface that needs to be implemented for listening to Helpshift Events
    /// </summary>
    public interface IHelpshiftEventsListener
    {

        void HandleHelpshiftEvent(string eventName, Dictionary<string, object> eventData);
        void AuthenticationFailedForUser(HelpshiftAuthenticationFailureReason reason);
    }
}