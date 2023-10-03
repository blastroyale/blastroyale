using System;

namespace Helpshift
{
    /// <summary>
    /// Constants class for event names and their data keys
    /// </summary>
    public class HelpshiftEvent
    {
        public const string WIDGET_TOGGLE = "widgetToggle";
        public const string DATA_SDK_VISIBLE = "visible";

        public const string CONVERSATION_START = "conversationStart";
        public const string DATA_MESSAGE = "message";

        public const string MESSAGE_ADD = "messageAdd";
        public const string DATA_MESSAGE_TYPE = "type";
        public const string DATA_MESSAGE_BODY = "body";
        public const string DATA_MESSAGE_TYPE_ATTACHMENT = "attachment";
        public const string DATA_MESSAGE_TYPE_TEXT = "text";

        public const string CSAT_SUBMIT = "csatSubmit";
        public const string DATA_CSAT_RATING = "rating";
        public const string DATA_ADDITIONAL_FEEDBACK = "additionalFeedback";

        public const string CONVERSATION_STATUS = "conversationStatus";
        public const string DATA_LATEST_ISSUE_ID = "latestIssueId";
        public const string DATA_LATEST_ISSUE_PUBLISH_ID = "latestIssuePublishId";
        public const string DATA_IS_ISSUE_OPEN = "open";

        public const string CONVERSATION_END = "conversationEnd";

        public const string CONVERSATION_REJECTED = "conversationRejected";

        public const string CONVERSATION_RESOLVED = "conversationResolved";

        public const string CONVERSATION_REOPENED = "conversationReopened";


        public const string SDK_SESSION_STARTED = "helpshiftSessionStarted";

        public const string SDK_SESSION_ENDED = "helpshiftSessionEnded";

        public const string RECEIVED_UNREAD_MESSAGE_COUNT = "receivedUnreadMessageCount";
        public const string DATA_MESSAGE_COUNT = "count";
        public const string DATA_MESSAGE_COUNT_FROM_CACHE = "fromCache";
    }
}
