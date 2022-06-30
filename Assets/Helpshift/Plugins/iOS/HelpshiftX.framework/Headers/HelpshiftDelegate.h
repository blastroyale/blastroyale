//
//  HelpshiftDelegate.h
//  HelpshiftX
//
//  Copyright © 2019 Helpshift. All rights reserved.
//

#ifndef HelpshiftDelegate_h
#define HelpshiftDelegate_h

#pragma mark Helpshift Events Names and respective data keys

static NSString *_Nonnull const HelpshiftEventNameWidgetToggle = @"widgetToggle";
static NSString *_Nonnull const HelpshiftEventDataVisible = @"visible";

static NSString *_Nonnull const HelpshiftEventNameConversationStart = @"conversationStart";
static NSString *_Nonnull const HelpshiftEventDataMessage = @"message";

static NSString *_Nonnull const HelpshiftEventNameConversationStatus = @"conversationStatus";
static NSString *_Nonnull const HelpshiftEventDataLatestIssueId = @"latestIssueId";
static NSString *_Nonnull const HelpshiftEventDataLatestIssuePublishId = @"latestIssuePublishId";
static NSString *_Nonnull const HelpshiftEventDataIsIssueOpen = @"open";

static NSString *_Nonnull const HelpshiftEventNameMessageAdd = @"messageAdd";
static NSString *_Nonnull const HelpshiftEventDataMessageType = @"type";
static NSString *_Nonnull const HelpshiftEventDataMessageTypeText = @"text";
static NSString *_Nonnull const HelpshiftEventDataMessageTypeAttachment = @"attachment";
static NSString *_Nonnull const HelpshiftEventDataMessageBody = @"body";

static NSString *_Nonnull const HelpshiftEventNameCSATSubmit = @"csatSubmit";
static NSString *_Nonnull const HelpshiftEventDataRating = @"rating";
static NSString *_Nonnull const HelpshiftEventDataAdditionalFeedback = @"additionalFeedback";

static NSString *_Nonnull const HelpshiftEventNameConversationEnd = @"conversationEnd";

static NSString *_Nonnull const HelpshiftEventNameConversationRejected = @"conversationRejected";

static NSString *_Nonnull const HelpshiftEventNameConversationResolved = @"conversationResolved";

static NSString *_Nonnull const HelpshiftEventNameConversationReopened = @"conversationReopened";

static NSString *_Nonnull const HelpshiftEventNameReceivedUnreadMessageCount = @"receivedUnreadMessageCount";
static NSString *_Nonnull const HelpshiftEventDataUnreadMessageCount = @"count";
static NSString *_Nonnull const HelpshiftEventDataUnreadMessageCountIsFromCache = @"fromCache";

static NSString *_Nonnull const HelpshiftEventNameSessionStarted = @"helpshiftSessionStarted";

static NSString *_Nonnull const HelpshiftEventNameSessionEnded = @"helpshiftSessionEnded";

#pragma mark Authentication Failure Reasons

/**
 * Possible reasons for authentication failure received in [HelpshiftDelegate authenticationFailedForUserWithReason:] method
 */
typedef NS_ENUM (NSUInteger, HelpshiftAuthenticationFailureReason)
{
    /// Authentication failed as the auth token was not provided
    HelpshiftAuthenticationFailureReasonAuthTokenNotProvided = 0,

    /// Authentication failed as invalid auth token was provided
    HelpshiftAuthenticationFailureReasonInvalidAuthToken,
};

/// Delegate protocol to be implemented for receiving Helpshift events
@protocol HelpshiftDelegate <NSObject>

/**
 * Delegate method that gets called when Helpshift sends events. For possible events,
 * refer : https://developers.helpshift.com/sdkx_ios/delegates/
 *
 * @param eventName The name of the event
 * @param data The data received from the event if any.
 */
- (void) handleHelpshiftEvent:(nonnull NSString *)eventName withData:(nullable NSDictionary *)data;

/**
 * Delegate method that gets called when the user authentication fails.
 * Whenever you receive this call, You should be calling the [Helpshift loginUser:] API with valid authToken.
 *
 * @param reason The reason for authentication failure
 *
 */
- (void) authenticationFailedForUserWithReason:(HelpshiftAuthenticationFailureReason)reason;
@end

#endif /* HelpshiftWebchatDelegate_h */
