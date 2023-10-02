#import "UnityAppController.h"
#import "SingularStateWrapper.h"
#import "Singular.h"
#import <objc/runtime.h>

@implementation UnityAppController (SingularSwizzledAppController)

/// Uncomment the code below in case the SingularAppDelegate is clashing with another plugin
/*
static IMP originalContinueUserActivity __unused;
static IMP originalDidFinishLaunchingWithOptions __unused;

+ (void)load {
    static dispatch_once_t onceToken;
    dispatch_once(&onceToken, ^{
        [self swizzleDidFinishLaunchingWithOptions:[self class]];
        [self swizzleContinueUserActivity:[self class]];
    });
}

BOOL didFinishLaunchingWithOptions(id self, SEL _cmd, UIApplication* application, NSDictionary* launchOptions) {
    [SingularStateWrapper setLaunchOptions:launchOptions];
    
    if(originalDidFinishLaunchingWithOptions) {
        return ((BOOL(*)(id, SEL, UIApplication*, NSDictionary*))originalDidFinishLaunchingWithOptions)(self, _cmd, application, launchOptions);
    }
    
    return YES;
}

BOOL continueUserActivity(id self, SEL _cmd, UIApplication* application, NSUserActivity* userActivity, void (^restorationHandler)(NSArray*)) {
    if([SingularStateWrapper isSingularLinksEnabled]){
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
    }
    
    if(originalContinueUserActivity){
        return ((BOOL(*)(id, SEL, UIApplication*, NSUserActivity*))originalContinueUserActivity)(self, _cmd, application, userActivity);
    }
    
    return YES;
}

+(void)swizzleDidFinishLaunchingWithOptions:(Class)klass {
    
    SEL originalSelector = @selector(application:didFinishLaunchingWithOptions:);
    
    Method defaultMethod = class_getInstanceMethod(klass, originalSelector);
    Method swizzledMethod = class_getInstanceMethod(klass, @selector(didFinishLaunchingWithOptions));
    
    BOOL isMethodExists = !class_addMethod(klass, originalSelector, method_getImplementation(swizzledMethod), method_getTypeEncoding(swizzledMethod));
    
    if (isMethodExists) {
        originalDidFinishLaunchingWithOptions = method_setImplementation(defaultMethod, (IMP)didFinishLaunchingWithOptions);
    } else {
        class_replaceMethod(klass, originalSelector, (IMP)didFinishLaunchingWithOptions, method_getTypeEncoding(swizzledMethod));
    }
}

+(void)swizzleContinueUserActivity:(Class)klass {
    
    SEL originalSelector = @selector(application:continueUserActivity:restorationHandler:);
    
    Method defaultMethod = class_getInstanceMethod(klass, originalSelector);
    Method swizzledMethod = class_getInstanceMethod(klass, @selector(__swizzled_continueUserActivity));
    
    BOOL isMethodExists = !class_addMethod(klass, originalSelector, method_getImplementation(swizzledMethod), method_getTypeEncoding(swizzledMethod));
    
    if (isMethodExists) {
        originalContinueUserActivity = method_setImplementation(defaultMethod, (IMP)continueUserActivity);
    } else {
        class_replaceMethod(klass, originalSelector, (IMP)continueUserActivity, method_getTypeEncoding(swizzledMethod));
    }
}
 */

@end
