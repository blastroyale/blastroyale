//
//  SingularConfig.h
//  Singular
//
//  Created by Eyal Rabinovich on 03/06/2020.
//

#import <Foundation/Foundation.h>

// avoid circular include of SingularLinkParams
#ifndef SingularLinkParams
#import "SingularLinkParams.h"
#endif

@interface SingularConfig : NSObject

// General Fields
@property NSString* apikey;
@property NSString* secret;
@property BOOL clipboardAttribution;

// Singular Links fields
@property NSDictionary *launchOptions;
@property NSUserActivity* userActivity;
@property NSURL* openUrl;
@property void(^singularLinksHandler)(SingularLinkParams*);
@property long shortLinkResolveTimeOut;
@property NSArray* supportedDomains;
@property NSArray* espDomains;

// Global Properties fields
@property (readonly) NSMutableDictionary* globalProperties;

// SKAN fields
@property BOOL skAdNetworkEnabled;
@property BOOL manualSkanConversionManagement;
@property void(^conversionValueUpdatedCallback)(NSInteger);
@property NSInteger waitForTrackingAuthorizationWithTimeoutInterval;

-(id)initWithApiKey:(NSString*)apikey andSecret:(NSString*)secret;

-(void)setGlobalProperty:(NSString*)key withValue:(NSString*)value overrideExisting:(BOOL)overrideExisiting;
-(NSString*)toJsonString;

@end
