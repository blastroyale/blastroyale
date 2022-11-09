#import "UnityAppController.h"
#import "SingularStateWrapper.h"
#import "Singular.h"


@interface SingularAppDelegate : UnityAppController
@end


IMPL_APP_CONTROLLER_SUBCLASS(SingularAppDelegate)


@implementation SingularAppDelegate

-(BOOL)application:(UIApplication*) application didFinishLaunchingWithOptions:(NSDictionary*) options
{
    [SingularStateWrapper setLaunchOptions:options];
    
    return [super application:application didFinishLaunchingWithOptions:options];
}

- (BOOL)application:(UIApplication *)application
continueUserActivity:(NSUserActivity *)userActivity
 restorationHandler:(void (^)(NSArray<id<UIUserActivityRestoring>> *restorableObjects))restorationHandler {
    [super application:application
  continueUserActivity:userActivity
    restorationHandler:restorationHandler];
    
    if(![SingularStateWrapper isSingularLinksEnabled]){
        return NO;
    }
    
    NSString* apiKey = [SingularStateWrapper getApiKey];
    NSString* apiSecret = [SingularStateWrapper getApiSecret];
    void (^singularLinkHandler)(SingularLinkParams*) = [SingularStateWrapper getSingularLinkHandler];
    int shortlinkResolveTimeout = [SingularStateWrapper getShortlinkResolveTimeout];
    NSArray* domains = [SingularStateWrapper getSupportedDomains];
    
    if(shortlinkResolveTimeout <= 0){
        [Singular startSession:apiKey
                       withKey:apiSecret
               andUserActivity:userActivity
       withSingularLinkHandler:singularLinkHandler
           andSupportedDomains:domains];
    } else{
        [Singular startSession:apiKey
                       withKey:apiSecret
               andUserActivity:userActivity
       withSingularLinkHandler:singularLinkHandler
    andShortLinkResolveTimeout:shortlinkResolveTimeout
           andSupportedDomains:domains];
    }
    
    return YES;
}


@end
