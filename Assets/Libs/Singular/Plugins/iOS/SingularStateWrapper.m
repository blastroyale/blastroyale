//
//  SingularStateWrapper.m
//  Unity-iPhone
//
//  Created by Eyal Rabinovich on 16/04/2019.
//

#import "SingularStateWrapper.h"
#import "SingularLinkParams.h"

@implementation SingularStateWrapper

static NSDictionary* launchOptions;
static bool isSingularLinksEnabled = NO;
static NSString* apiKey;
static NSString* apiSecret;
static void(^singularLinkHandler)(SingularLinkParams*);
static int shortlinkResolveTimeout;
static NSArray* supportedDomains;

+(NSString*)getApiKey{
    return apiKey;
}

+(NSString*)getApiSecret{
    return apiKey;
}

+(void (^)(SingularLinkParams*))getSingularLinkHandler{
    return singularLinkHandler;
}

+(int)getShortlinkResolveTimeout{
    return shortlinkResolveTimeout;
}

+(void)setLaunchOptions:(NSDictionary*) options{
    if (!options){
        return;
    }
    
    launchOptions = options;
}

+(NSDictionary*)getLaunchOptions{
    return launchOptions;
}

+(void)clearLaunchOptions {
    launchOptions = nil;
}

+(NSArray*)getSupportedDomains{
    return supportedDomains;
}

+(BOOL)enableSingularLinks:(NSString*)key withSecret:(NSString*)secret andHandler:(void (^)(SingularLinkParams*))handler withTimeout:(int)timeoutSec withSupportedDomains:(NSArray*) domains{
    if(key && secret && handler){
        apiKey = key;
        apiSecret = secret;
        singularLinkHandler = handler;
        shortlinkResolveTimeout = timeoutSec;
        supportedDomains = domains;
        
        isSingularLinksEnabled = YES;
    }
    
    return isSingularLinksEnabled;
}

+(BOOL)isSingularLinksEnabled{
    return isSingularLinksEnabled;
}

@end
