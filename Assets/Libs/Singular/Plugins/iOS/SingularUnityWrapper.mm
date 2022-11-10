//
//  SingularUnityWrapper.mm
//  Singular
//

#import "Singular.h"
#import "SingularStateWrapper.h"
#import <AdSupport/ASIdentifierManager.h>

enum { __STRING__,__INT__,__LONG__,__FLOAT__,__DOUBLE__,__NULL__,__ARRAY__,__DICTIONARY__};


NSMutableDictionary *tmpDictionary;

NSMutableArray *tmpMasterArray;

/* utility function to convert hex character representation to their nibble (4 bit) values */
static uint8_t nibbleFromChar(char c)
{
    if(c >= '0' && c <= '9') return c - '0';
    if(c >= 'a' && c <= 'f') return c - 'a' + 10;
    if(c >= 'A' && c <= 'F') return c - 'A' + 10;
    return 255;
}

/* Convert a string of characters representing a hex buffer into a series of bytes of that real value */
uint8_t *hexStringToBytes(const char *inhex)
{
    uint8_t *retval;
    uint8_t *p;
    int len, i;
    
    len = (int)(strlen(inhex) / 2);
    retval = (uint8_t*)malloc(len+1);
    for(i=0, p = (uint8_t *) inhex; i<len; i++) {
        retval[i] = (nibbleFromChar(*p) << 4) | nibbleFromChar(*(p+1));
        p += 2;
    }
    retval[len] = 0;
    return retval;
}

static NSDictionary* singularLinkParamsToDictionary(SingularLinkParams* params){
    return [NSDictionary dictionaryWithObjectsAndKeys:
            [params getDeepLink],@"deeplink",
            [params getPassthrough], @"passthrough",
            [params isDeferred] ? @YES : @NO, @"is_deferred", nil];
}

static NSString* dictionaryToJson(NSDictionary* dictionary){
    NSError *writeError = nil;
    NSData *jsonData = [NSJSONSerialization dataWithJSONObject:dictionary
                                                       options:NSJSONWritingPrettyPrinted
                                                         error:&writeError];
    if (writeError){
        return nil;
    }
    
    return [[NSString alloc] initWithData:jsonData encoding:NSUTF8StringEncoding];
}

static NSString* singularLinkParamsToJson(SingularLinkParams* params){
    NSDictionary* values = singularLinkParamsToDictionary(params);
    return dictionaryToJson(values);
}

static void sendSdkMessage(const char *methodName, NSString *param) {
    const char* str = [param UTF8String];
    char* result = (char*)malloc(strlen(str)+1);
    strcpy(result,str);
    
    UnitySendMessage("SingularSDKObject", methodName, result);
}


static void handleSingularLinkParams(SingularLinkParams *params) {
    NSString *jsonString = singularLinkParamsToJson(params);
    sendSdkMessage("SingularLinkHandlerResolved", jsonString);
}



static void handleConversionValueUpdated(int value) {
    // UnitySendMessage only accepts strings
    sendSdkMessage("ConversionValueUpdated", [NSString stringWithFormat:@"%d",value]);
}

extern "C" {


    bool createReferrerShortLink_(const char * baseLink,
        const char * referrerName,
        const char * referrerId,
        const char * args){
            
        NSDictionary* argsDictionary = [NSJSONSerialization JSONObjectWithData:[[NSString stringWithUTF8String:args]
                                                                    dataUsingEncoding:NSUTF8StringEncoding] options:0 error:nil];
        [Singular createReferrerShortLink: [NSString stringWithUTF8String:baseLink]
                referrerName: [NSString stringWithUTF8String:referrerName]
                referrerId: [NSString stringWithUTF8String:referrerId]
                passthroughParams: argsDictionary
                completionHandler:^(NSString *data, NSError *error) {

                    NSString * jsonString = dictionaryToJson(@{
                                @"data": data? data: @"",
                                @"error": error ? error.description: @""
                            });
                    sendSdkMessage("ShortLinkResolved", jsonString);
        }];
    }

    
    bool StartSingularSession_(const char* configString){
        NSDictionary* config = [NSJSONSerialization JSONObjectWithData:[[NSString stringWithUTF8String:configString]
                                                                        dataUsingEncoding:NSUTF8StringEncoding] options:0 error:nil];
        
        NSString* apiKey = [config objectForKey:@"apiKey"];
        NSString* apiSecret = [config objectForKey:@"secret"];
        void (^singularLinkHandler)(SingularLinkParams*) = ^(SingularLinkParams* params) {
            handleSingularLinkParams(params);
        };
        
        NSArray* supportedDomains = [config objectForKey:@"supportedDomains"];
        int shortlinkResolveTimeout = [[config objectForKey:@"shortlinkResolveTimeout"] integerValue];
        
        [SingularStateWrapper
         enableSingularLinks:apiKey
         withSecret:apiSecret
         andHandler:singularLinkHandler
         withTimeout:shortlinkResolveTimeout
         withSupportedDomains:supportedDomains];
        
        NSDictionary* globalProperties = [config objectForKey:@"globalProperties"];

        SingularConfig* singularConfig = [[SingularConfig alloc] initWithApiKey:apiKey andSecret:apiSecret];
        singularConfig.supportedDomains = supportedDomains;
        singularConfig.shortLinkResolveTimeOut = shortlinkResolveTimeout;
        singularConfig.singularLinksHandler = singularLinkHandler;
        singularConfig.skAdNetworkEnabled = [[config objectForKey:@"skAdNetworkEnabled"] boolValue];
        singularConfig.clipboardAttribution = [[config objectForKey:@"clipboardAttribution"] boolValue];
        singularConfig.manualSkanConversionManagement = [[config objectForKey:@"manualSkanConversionManagement"] boolValue];
        singularConfig.waitForTrackingAuthorizationWithTimeoutInterval =
            [[config objectForKey:@"waitForTrackingAuthorizationWithTimeoutInterval"] intValue];
        
        singularConfig.conversionValueUpdatedCallback = ^(NSInteger conversionValue) {
            handleConversionValueUpdated(conversionValue);
        };
        
        if ([globalProperties count] > 0){
             for (NSDictionary* property in [globalProperties allValues]) {
                 NSString* propertyKey = [property objectForKey:@"Key"];
                 
                 if (propertyKey && ![propertyKey isEqualToString:@""]){
                     [singularConfig setGlobalProperty:propertyKey
                                             withValue:[property objectForKey:@"Value"]
                                      overrideExisting:[[property objectForKey:@"OverrideExisting"] boolValue]];
                 }
            }
        }
        
        NSDictionary* launchOptions = [SingularStateWrapper getLaunchOptions];
        [SingularStateWrapper clearLaunchOptions];
        singularConfig.launchOptions = launchOptions ? launchOptions : [[NSDictionary alloc] init];
        
        int sessionTimeoutSec = [[config objectForKey:@"sessionTimeoutSec"] intValue];
        if (sessionTimeoutSec > 0) {
            [Singular setSessionTimeout:sessionTimeoutSec];
        }
        [Singular start:singularConfig];
        
        return true;
    }
    
    bool StartSingularSessionWithLaunchOptions_(const char* key, const char* secret){
        [Singular startSession:[NSString stringWithUTF8String:key] withKey:[NSString stringWithUTF8String:secret] andLaunchOptions:tmpDictionary];
        return true;
    }
    
    bool StartSingularSessionWithLaunchURL_(const char* key, const char* secret, const char* url){
        [Singular startSession:[NSString stringWithUTF8String:key] withKey:[NSString stringWithUTF8String:secret] andLaunchURL:[NSURL URLWithString:[NSString stringWithUTF8String:url]]];
        return true;
    }
    
    void SendEvent_(const char * name){
        NSString *nsName = [NSString stringWithUTF8String:name];
        [Singular event:nsName];
    }
    
    void EndSingularSession_(){
        [Singular endSession];
    }
    
    void RestartSingularSession_(const char* key, const char* secret){
        [Singular reStartSession:[NSString stringWithUTF8String:key] withKey:[NSString stringWithUTF8String:secret]];
    }
    
    void SendEventWithArgs(const char* name){
        [Singular event:[NSString stringWithUTF8String:name] withArgs:tmpDictionary];
    }
    
    void SetDeviceCustomUserId_(const char* customUserId){
        [Singular setDeviceCustomUserId:[NSString stringWithUTF8String:customUserId]];
    }
    
    void Init_NSDictionary(){
        tmpDictionary = [[NSMutableDictionary alloc] init];
    }
    
    void Init_NSMasterArray(){
        tmpMasterArray = [[NSMutableArray alloc] init];
    }
    
    void Push_NSDictionary(char* key, char* value, int type){
        if(type == __STRING__){
            [tmpDictionary setObject:[NSString stringWithUTF8String:value] forKey:[NSString stringWithUTF8String:key]];
        }else if(type == __INT__){
            [tmpDictionary setObject:[NSNumber numberWithInt:[[NSString stringWithUTF8String:value] intValue]] forKey:[NSString stringWithUTF8String:key]];
        }else if(type == __LONG__){
            [tmpDictionary setObject:[NSNumber numberWithLong:[[NSString stringWithUTF8String:value] longLongValue]] forKey:[NSString stringWithUTF8String:key]];
        }else if(type == __FLOAT__){
            [tmpDictionary setObject:[NSNumber numberWithFloat:[[NSString stringWithUTF8String:value] floatValue]] forKey:[NSString stringWithUTF8String:key]];
        }else if(type == __DOUBLE__){
            [tmpDictionary setObject:[NSNumber numberWithDouble:[[NSString stringWithUTF8String:value] doubleValue]] forKey:[NSString stringWithUTF8String:key]];
        }else if(type == __NULL__){
            [tmpDictionary setObject:[NSNull null] forKey:[NSString stringWithUTF8String:key]];
        }
    }
    
    void Push_Container_NSDictionary(char* key, int containerIndex){
        [tmpDictionary setObject:[tmpMasterArray objectAtIndex:(NSUInteger)containerIndex] forKey:[NSString stringWithUTF8String:key]];
    }
    
    void Free_NSDictionary(){
        tmpDictionary = nil;
        //        [tmpDictionary release];
    }
    
    void Free_NSMasterArray(){
        tmpMasterArray = nil;
        //        [tmpMasterArray release];
    }
    
    int New_NSDictionary(){
        [tmpMasterArray addObject:[[NSMutableDictionary alloc] init]];
        return [tmpMasterArray count]-1;
    }
    
    int New_NSArray(){
        [tmpMasterArray addObject:[[NSMutableArray alloc] init]];
        return [tmpMasterArray count]-1;
    }
    
    void Push_To_Child_Dictionary(char* key, char* value, int type, int dictionaryIndex){
        NSMutableDictionary *dict = [tmpMasterArray objectAtIndex:(NSUInteger)dictionaryIndex];
        if(type == __STRING__){
            [dict setObject:[NSString stringWithUTF8String:value] forKey:[NSString stringWithUTF8String:key]];
        }else if(type == __INT__){
            [dict setObject:[NSNumber numberWithInt:[[NSString stringWithUTF8String:value] intValue]] forKey:[NSString stringWithUTF8String:key]];
        }else if(type == __LONG__){
            [dict setObject:[NSNumber numberWithLong:[[NSString stringWithUTF8String:value] longLongValue]] forKey:[NSString stringWithUTF8String:key]];
        }else if(type == __FLOAT__){
            [dict setObject:[NSNumber numberWithFloat:[[NSString stringWithUTF8String:value] floatValue]] forKey:[NSString stringWithUTF8String:key]];
        }else if(type == __DOUBLE__){
            [dict setObject:[NSNumber numberWithDouble:[[NSString stringWithUTF8String:value] doubleValue]] forKey:[NSString stringWithUTF8String:key]];
        }else if(type == __NULL__){
            [dict setObject:[NSNull null] forKey:[NSString stringWithUTF8String:key]];
        }
    }
    
    void Push_To_Child_Array(char* value,int type, int arrayIndex){
        NSMutableArray *array = [tmpMasterArray objectAtIndex:(NSUInteger)arrayIndex];
        if(type == __STRING__){
            [array addObject:[NSString stringWithUTF8String:value]];
        }else if(type == __INT__){
            [array addObject:[NSNumber numberWithInt:[[NSString stringWithUTF8String:value] intValue]]];
        }else if(type == __LONG__){
            [array addObject:[NSNumber numberWithLong:[[NSString stringWithUTF8String:value] longLongValue]]];
        }else if(type == __FLOAT__){
            [array addObject:[NSNumber numberWithFloat:[[NSString stringWithUTF8String:value] floatValue]]];
        }else if(type == __DOUBLE__){
            [array addObject:[NSNumber numberWithDouble:[[NSString stringWithUTF8String:value] doubleValue]]];
        }else if(type == __NULL__){
            [array addObject:[NSNull null]];
        }
    }
    
    void Push_Container_To_Child_Dictionary(char* key, int dictionaryIndex, int containerIndex){
        NSMutableDictionary* dict = [tmpMasterArray objectAtIndex:(NSUInteger)dictionaryIndex];
        [dict setObject:[tmpMasterArray objectAtIndex:(NSUInteger)containerIndex] forKey:[NSString stringWithUTF8String:key]];
    }
    
    void Push_Container_To_Child_Array(int arrayIndex, int containerIndex){
        NSMutableArray *array = [tmpMasterArray objectAtIndex:(NSUInteger)arrayIndex];
        [array addObject:[tmpMasterArray objectAtIndex:(NSUInteger)containerIndex]];
    }
    
    void SetAllowAutoIAPComplete_(bool allowed){
        BOOL b = allowed ? YES : NO;
        [Singular setAllowAutoIAPComplete:b];
    }
    
    void SetBatchesEvents_(bool setBatches){
        BOOL b = setBatches ? YES : NO;
        [Singular setBatchesEvents:b];
    }
    
    void SetBatchInterval_(int interval){
        [Singular setBatchInterval:interval];
    }
    
    void SendAllBatches_(){
        [Singular sendAllBatches];
    }
    
    void SetAge_(int age){
        [Singular setAge:[NSNumber numberWithInt:age]];
    }
    
    void SetGender_(const char * gender){
        [Singular setGender:[NSString stringWithUTF8String:gender]];
    }
    
    void RegisterDeviceTokenForUninstall_(const char * APNSToken){
        unsigned int token_length = (unsigned int)strlen(APNSToken);
        
        if (token_length % 2 != 0)
        {
            return; // odd length
        }
        
        unsigned char* APNSToken_decoded = hexStringToBytes(APNSToken);
        NSData *data = [NSData dataWithBytes:APNSToken_decoded length:token_length/2];
        free(APNSToken_decoded); // don't leak :)
        [Singular registerDeviceTokenForUninstall:data];
    }
    
    void RegisterDeferredDeepLinkHandler_(){
        [Singular registerDeferredDeepLinkHandler:^(NSString *deeplink) {
            if(deeplink != NULL){
                const char* str = [deeplink UTF8String];
                char* result = (char*)malloc(strlen(str)+1);
                strcpy(result,str);
                UnitySendMessage("SingularSDKObject", "DeepLinkHandler", result);
            }else{
                UnitySendMessage("SingularSDKObject", "DeepLinkHandler", "");
            }
        }];
    }
    
    int SetDeferredDeepLinkTimeout_(int duration){
        return [Singular setDeferredDeepLinkTimeout:duration];
    }
    
    const char* GetAPID_(){
        const char* str = [[Singular singularID] UTF8String];
        if(str == NULL){
            return NULL;
        }
        char* result = (char*)malloc(strlen(str)+1);
        strcpy(result,str);
        return result;
    }
    
    const char* GetIDFA_(){
        const char* str = [[[[ASIdentifierManager sharedManager] advertisingIdentifier] UUIDString] UTF8String];
        if(str == NULL){
            return NULL;
        }
        char* result = (char*)malloc(strlen(str)+1);
        strcpy(result,str);
        return result;
    }
    
    // Revenue methods
    void Revenue_(const char* currency, double amount){
        [Singular revenue:[NSString stringWithUTF8String:currency] amount:amount];
    }
    
    void CustomRevenue_(const char* eventName, const char* currency, double amount){
        [Singular customRevenue:[NSString stringWithUTF8String:eventName] currency:[NSString stringWithUTF8String:currency] amount:amount];
    }
    
    void RevenueWithAllParams_(const char* currency, double amount, const char* productSKU, const char* productName, const char* productCategory, int productQuantity, double productPrice){
        [Singular revenue:[NSString stringWithUTF8String:currency] amount:amount productSKU:[NSString stringWithUTF8String:productSKU] productName:[NSString stringWithUTF8String:productName] productCategory:[NSString stringWithUTF8String:productCategory] productQuantity:productQuantity productPrice:productPrice];
    }
    
    void CustomRevenueWithAllParams_(const char* eventName, const char* currency, double amount, const char* productSKU, const char* productName, const char* productCategory, int productQuantity, double productPrice){
        [Singular customRevenue:[NSString stringWithUTF8String:eventName] currency:[NSString stringWithUTF8String:currency] amount:amount productSKU:[NSString stringWithUTF8String:productSKU] productName:[NSString stringWithUTF8String:productName] productCategory:[NSString stringWithUTF8String:productCategory] productQuantity:productQuantity productPrice:productPrice];
    }
    
    // Custom user id
    void SetCustomUserId_(const char* customUserId){
        [Singular setCustomUserId:[NSString stringWithUTF8String:customUserId]];
    }
    
    void UnsetCustomUserId_(){
        [Singular unsetCustomUserId];
    }
    
    void SetWrapperNameAndVersion_(const char* wrapper, const char* version){
        [Singular setWrapperName:[NSString stringWithUTF8String:wrapper] andVersion:[NSString stringWithUTF8String:version]];
    }
    
    /* GDPR helpers */
    
    void TrackingOptIn_() {
        [Singular trackingOptIn];
    }
    
    void TrackingUnder13_() {
        [Singular trackingUnder13];
    }
    
    void StopAllTracking_() {
        [Singular stopAllTracking];
    }
    
    void ResumeAllTracking_() {
        [Singular resumeAllTracking];
    }
    
    bool IsAllTrackingStopped_() {
        return [Singular isAllTrackingStopped];
    }

    void LimitDataSharing_(bool limitDataSharingValue) {
        [Singular limitDataSharing:limitDataSharingValue];
    }
    
    bool GetLimitDataSharing_() {
        return [Singular getLimitDataSharing];
    }

    /* Global Properties */

    const char* GetGlobalProperties_() {
        NSDictionary* globalProperties = [Singular getGlobalProperties];
        
        NSString* propertiesJson = dictionaryToJson(globalProperties);
        
        if (!propertiesJson){
            return NULL;
        }
        
        const char* str = [propertiesJson UTF8String];
        if(str == NULL){
            return NULL;
        }
        char* result = (char*)malloc(strlen(str)+1);
        strcpy(result,str);
        return result;
    }

    bool SetGlobalProperty_(const char* key, const char* value, bool overrideExisting) {
        return [Singular setGlobalProperty:[NSString stringWithUTF8String:key]
                                  andValue:[NSString stringWithUTF8String:value]
                          overrideExisting:overrideExisting];
    }

    void UnsetGlobalProperty_(const char* key) {
        [Singular unsetGlobalProperty:[NSString stringWithUTF8String:key]];
    }

    void ClearGlobalProperties_() {
        [Singular clearGlobalProperties];
    }

    /* SKAN Methods */

    void SkanRegisterAppForAdNetworkAttribution_() {
        [Singular skanRegisterAppForAdNetworkAttribution];
    }

    bool SkanUpdateConversionValue_(int conversionValue) {
        return [Singular skanUpdateConversionValue:conversionValue];
    }

    int SkanGetConversionValue_() {
        NSNumber *value = [Singular skanGetConversionValue];
        
        if (value == nil) {
            return -1;
        }
        
        return [value intValue];
    }
}
