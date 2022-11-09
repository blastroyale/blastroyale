//
//  SingularAdData.h
//  Singular
//
//  Created by Eyal Rabinovich on 13/06/2019.
//

#import <Foundation/Foundation.h>

NS_ASSUME_NONNULL_BEGIN

@interface SingularAdData : NSMutableDictionary

- (id)initWithAdPlatfrom:(NSString*)adPlatform withCurrency:(NSString*)currency withRevenue:(NSNumber*)revenue;

- (void)setNetworkName:(NSString*)networkName;
- (void)setAdType:(NSString*)adType;
- (void)setGroupType:(NSString*)adGroupType;
- (void)setImpressionId:(NSString*)impressionId;
- (void)setAdPlacementName:(NSString*)adPlacementName;
- (void)setAdUnitId:(NSString*)adUnitId;
- (void)setAdGroupId:(NSString*)adGroupId;
- (void)setAdGroupName:(NSString*)adGroupName;
- (void)setAdGroupPriority:(NSString*)adGroupPriority;
- (void)setPrecision:(NSString*)precision;
- (void)setPlacementId:(NSString*)placementId;
- (void)setAdUnitName:(NSString*)adUnitName;

- (BOOL)hasRequiredParams;

@end

NS_ASSUME_NONNULL_END
