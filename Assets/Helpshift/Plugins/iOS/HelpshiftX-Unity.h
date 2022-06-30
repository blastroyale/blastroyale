//
// HelpshiftX-Unity.h
// Created for HelpshiftX in 2020
// Copyright © 2020 Helpshift. All rights reserved.
//

#import <Foundation/Foundation.h>
#import <HelpshiftX/HelpshiftX.h>

#ifdef __cplusplus
extern "C" {
#endif

/**
 * Makes an install call by using the credentials stored in the cache if any.
 */
void hsInstallFromCache();

/** Register the deviceToken to enable push notifications
 * To enable push notifications in the Helpshift iOS SDK, set the Push Notifications’ deviceToken using this method.
 *  @param deviceTokenData The data representing the deviceToken received from the push notification servers.
 */
void HsRegisterDeviceTokenData(NSData *deviceTokenData);

/** Register the deviceToken to enable push notifications
 * To enable push notifications in the Helpshift iOS SDK, set the Push Notifications’ deviceToken using this method.
 *  @param deviceToken The string representing the deviceToken received from the push notification servers.
 */
void HsRegisterDeviceToken(const char *deviceToken);

#ifdef __cplusplus
}
#endif
