//
//  SingularLinkParams.h
//  Singular
//
//  Created by Eyal Rabinovich on 15/04/2019.
//

#import <Foundation/Foundation.h>

@interface SingularLinkParams: NSObject{
    NSString* deeplink;
    NSString* passthrough;
    BOOL isDeferred;
}

- (id)initWithDeepLink:(NSString *)link andPassthrough:(NSString *)sentPassthrough isDeferred:(BOOL)isLinkDeferred;

- (NSString*)getDeepLink;
- (NSString*)getPassthrough;
- (BOOL)isDeferred;

@end

