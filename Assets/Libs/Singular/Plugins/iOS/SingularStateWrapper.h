//
//  SingularStateWrapper.h
//  Unity-iPhone
//
//  Created by Eyal Rabinovich on 16/04/2019.
//

#import <Foundation/Foundation.h>
#import "SingularLinkParams.h"

NS_ASSUME_NONNULL_BEGIN

@interface SingularStateWrapper : NSObject

+(void)setLaunchOptions:(NSDictionary*) options;
+(NSDictionary*)getLaunchOptions;
+(void)clearLaunchOptions;
+(NSString*)getApiKey;
+(NSString*)getApiSecret;
+(void (^)(SingularLinkParams*))getSingularLinkHandler;
+(int)getShortlinkResolveTimeout;
+(NSArray*)getSupportedDomains;
+(BOOL)enableSingularLinks:(NSString*)key withSecret:(NSString*)secret andHandler:(void (^)(SingularLinkParams*))handler withTimeout:(int)timeoutSec withSupportedDomains:(NSArray*) domains;
+(BOOL)isSingularLinksEnabled;

@end

NS_ASSUME_NONNULL_END
