//
//  HsUnityAppController.m
//

#import <Foundation/Foundation.h>
#import <UserNotifications/UserNotifications.h>
#import <HelpshiftX/HelpshiftX.h>
#import "HelpshiftX-Unity.h"
#import "UnityAppController.h"
#import "AppDelegateListener.h"

@interface HsUnityAppController: NSObject<AppDelegateListener, UNUserNotificationCenterDelegate>

@end

@implementation HsUnityAppController

static HsUnityAppController *shared = [[HsUnityAppController alloc] init];

- (instancetype) init {
    self = [super init];
    if(self) {
        UnityRegisterAppDelegateListener(self);
    }
    return self;
}

- (void) didFinishLaunching:(NSNotification *)notification {

    // Uncomment the following code block to register for push notifications using UNUserNotification framework.

    /*
    UNUserNotificationCenter *center = [UNUserNotificationCenter currentNotificationCenter];
    center.delegate = self;
    [center requestAuthorizationWithOptions:(UNAuthorizationOptionBadge | UNAuthorizationOptionSound | UNAuthorizationOptionAlert)
                          completionHandler:^(BOOL granted, NSError *_Nullable error) {
                              if(error) {
                                  NSLog(@"Error while requesting Notification permissions.");
                              }
                          }];
    [[UIApplication sharedApplication] registerForRemoteNotifications];
    */

    /* Uncomment the following code block to register for push notification using UILocalNotification framework.
     * Please make sure that you are registering only from one framwork, Either from UNUserNotification or from UILocalNotification framework.
     */

    /*
     UIUserNotificationType notificationType = UIUserNotificationTypeBadge | UIUserNotificationTypeAlert;
     UIUserNotificationSettings *notificationSettings = [UIUserNotificationSettings settingsForTypes:notificationType categories:nil];
     [[UIApplication sharedApplication] registerUserNotificationSettings:notificationSettings];
     [[UIApplication sharedApplication] registerForRemoteNotifications];
     */

    if(notification.userInfo[UIApplicationLaunchOptionsRemoteNotificationKey]) {
        NSDictionary *payload = notification.userInfo[UIApplicationLaunchOptionsRemoteNotificationKey];
        if ([[payload objectForKey:@"origin"] isEqualToString:@"helpshift"]) {
            hsInstallFromCache();
            dispatch_after(dispatch_time(DISPATCH_TIME_NOW, (int64_t)(2 * NSEC_PER_SEC)), dispatch_get_main_queue(), ^{
                [Helpshift handleNotificationWithUserInfoDictionary:payload
                                                        isAppLaunch:YES
                                                     withController:UIApplication.sharedApplication.keyWindow.rootViewController];
            });
        }
    }
}

- (void) didRegisterForRemoteNotificationsWithDeviceToken:(NSNotification *)notification {
    NSData *deviceToken = (NSData *) notification.userInfo;
    hsInstallFromCache();
    HsRegisterDeviceTokenData(deviceToken);
}

- (void) didReceiveRemoteNotification:(NSNotification *)notification {
    hsInstallFromCache();
    [Helpshift handleNotificationWithUserInfoDictionary:notification.userInfo
                                            isAppLaunch:false
                                         withController:UIApplication.sharedApplication.keyWindow.rootViewController];
}

/* UNUserNotification delegates implementation starts here.
 * If your app already implements these delegates than copy this code snippet to your implementation and remove this implementation.
 */
- (void) userNotificationCenter:(UNUserNotificationCenter *)center
        willPresentNotification:(UNNotification *)notification
          withCompletionHandler:(void (^)(UNNotificationPresentationOptions options))completionHandler {
    if([notification.request.content.userInfo[@"origin"] isEqualToString:@"helpshift"]) {
        hsInstallFromCache();
        [Helpshift handleNotificationWithUserInfoDictionary:notification.request.content.userInfo
                                                isAppLaunch:false
                                             withController:UIApplication.sharedApplication.keyWindow.rootViewController];
    }
    completionHandler(UNNotificationPresentationOptionNone);
}

- (void) userNotificationCenter:(UNUserNotificationCenter *)center
 didReceiveNotificationResponse:(UNNotificationResponse *)response
          withCompletionHandler:(void (^)(void))completionHandler {
    UNNotification *notification = response.notification;
    if([notification.request.content.userInfo[@"origin"] isEqualToString:@"helpshift"]) {
        hsInstallFromCache();
        [Helpshift handleNotificationWithUserInfoDictionary:notification.request.content.userInfo 
                                                isAppLaunch:false
                                             withController:UIApplication.sharedApplication.keyWindow.rootViewController];
    }
    completionHandler();
}

// UNUserNotification delegates implemenation ends here.

@end
