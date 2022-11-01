using UnityEngine;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System;
using System.Globalization;
using System.Threading;

using Newtonsoft.Json;
using System;

#if UNITY_5_3_OR_NEWER && UNITY_PURCHASING
using UnityEngine.Purchasing;
#endif


public delegate void ShortLinkCallback(string data, string error);

public class SingularSDK : MonoBehaviour {
    // public properties
    public string SingularAPIKey = "<YourAPIKey>";
    public string SingularAPISecret = "<YourAPISecret>";
    public bool InitializeOnAwake = true;

    public bool autoIAPComplete = false;
    private Dictionary<string, SingularGlobalProperty> globalProperties = new Dictionary<string, SingularGlobalProperty>();

    private static ShortLinkCallback shortLinkCallback;
    public static bool batchEvents = false;
    public static bool endSessionOnGoingToBackground = false;
    public static bool restartSessionOnReturningToForeground = false;

    public static bool enableDeferredDeepLinks = true;
    public bool enableLogging = true;
    public int logLevel = 3;
    public static string fcmDeviceToken = null;
    public static string facebookAppId;
    public bool clipboardAttribution = false;
    public static string openUri;
    public bool collectOAID = false;

#if UNITY_ANDROID
    private static string imei;
    // This are only for android because it does not support setting the custom user id before init
    private static string customUserId;
#endif

    public long ddlTimeoutSec = 0; // default - 0 - use default timeout (60s)
    public long sessionTimeoutSec = 0; // default - 0 - use default timeout (60s)
    public long shortlinkResolveTimeout = 0; // default - 0 - use default timeout (10s)

#if UNITY_IOS
    public bool SKANEnabled = false;
    public bool manualSKANConversionManagement = false;
    public int waitForTrackingAuthorizationWithTimeoutInterval = 0;
#endif

    private const long DEFAULT_SHORT_LINKS_TIMEOUT = 10;
    private const long DEFAULT_DDL_TIMEOUT = 60;

    private static List<String> supportedDomains = new List<string>();
    private SingularLinkParams resolvedSingularLinkParams = null;
    private Int32 resolvedSingularLinkTime;


    // private properties
    private static bool Initialized = false;

    private const string UNITY_WRAPPER_NAME = "Unity";
    private const string UNITY_VERSION = "4.0.11";


#if UNITY_ANDROID
    static AndroidJavaClass singular;
    static AndroidJavaClass jclass;
    static AndroidJavaObject activity;
    static AndroidJavaClass jniSingularUnityBridge;
    

    static bool status = false;
#endif

    private const string ADMON_REVENUE_EVENT_NAME = "__ADMON_USER_LEVEL_REVENUE__";

    // singleton instance kept here
    private static SingularSDK instance = null;
    public static SingularLinkHandler registeredSingularLinkHandler = null;
    public static SingularDeferredDeepLinkHandler registeredDDLHandler = null;
    public static SingularConversionValueUpdatedHandler registeredConversionValueUpdatedHandler = null;

    static System.Int32 cachedDDLMessageTime;
    static string cachedDDLMessage;


    // The Singular SDK is initialized here
    void Awake() {
        Debug.Log(string.Format("SingularSDK Awake, InitializeOnAwake={0}", InitializeOnAwake));

        if (Application.isEditor) {
            return;
        }

        if (instance)
            return;

        // Initialize singleton
        instance = this;

        // Keep this script running when another scene loads
        DontDestroyOnLoad(gameObject);

        if (InitializeOnAwake) {
            Debug.Log("Awake : calling Singular Init");
            InitializeSingularSDK();
        }
    }

    // Only call this if you have disabled InitializeOnAwake
    public static void InitializeSingularSDK() {
        if (Initialized)
            return;

        if (!instance) {
            Debug.LogError("SingularSDK InitializeSingularSDK, no instance available - cannot initialize");
            return;
        }

        Debug.Log(string.Format("SingularSDK InitializeSingularSDK, APIKey={0}", instance.SingularAPIKey));

        if (Application.isEditor) {
            return;
        }

        SingularConfig config = BuildSingularConfig();

#if UNITY_IOS
        StartSingularSession(config);
        SetAllowAutoIAPComplete_(instance.autoIAPComplete);
#elif UNITY_ANDROID
        initSDK(config);
#endif
        Initialized = true;
    }


    public static void createReferrerShortLink(string baseLink, string referrerName, string referrerId, Dictionary<string, string> passthroughParams, ShortLinkCallback completionHandler){
        shortLinkCallback = completionHandler;
#if UNITY_IOS
        createReferrerShortLink_( baseLink,  referrerName,  referrerId, JsonConvert.SerializeObject(passthroughParams));
#elif UNITY_ANDROID
        jniSingularUnityBridge.CallStatic("createReferrerShortLink", baseLink, referrerName, referrerId, JsonConvert.SerializeObject(passthroughParams));

#endif


    } 


    private static SingularConfig BuildSingularConfig() {
        SingularConfig config = new SingularConfig();
        config.SetValue("apiKey", instance.SingularAPIKey);
        config.SetValue("secret", instance.SingularAPISecret);
        config.SetValue("supportedDomains", supportedDomains);
        config.SetValue("shortlinkResolveTimeout", instance.shortlinkResolveTimeout == 0 ?
            DEFAULT_SHORT_LINKS_TIMEOUT : instance.shortlinkResolveTimeout);
        config.SetValue("globalProperties", instance.globalProperties);
        config.SetValue("sessionTimeoutSec", instance.sessionTimeoutSec);

#if UNITY_ANDROID
        config.SetValue("facebookAppId", facebookAppId);
        config.SetValue("customUserId", customUserId);
        config.SetValue("imei", imei);
        config.SetValue("openUri", openUri);
        config.SetValue("ddlTimeoutSec", instance.ddlTimeoutSec);
        config.SetValue("enableDeferredDeepLinks", enableDeferredDeepLinks);
        config.SetValue("enableLogging", instance.enableLogging);
        config.SetValue("logLevel", instance.logLevel);
        if (SingularSDK.fcmDeviceToken != null){
            config.SetValue("fcmDeviceToken", SingularSDK.fcmDeviceToken);
        }
        config.SetValue("collectOAID", instance.collectOAID);
#elif UNITY_IOS
        config.SetValue("clipboardAttribution", instance.clipboardAttribution);
        config.SetValue("skAdNetworkEnabled", instance.SKANEnabled);
        config.SetValue("manualSkanConversionManagement", instance.manualSKANConversionManagement);
        config.SetValue("waitForTrackingAuthorizationWithTimeoutInterval",
            instance.waitForTrackingAuthorizationWithTimeoutInterval);
#endif

        return config;
    }

    public void Update() { }

#if UNITY_ANDROID
    private static void initSDK(SingularConfig config) {
        Debug.Log("UNITY_ANDROID - init Is called");

        InitAndroidJavaClasses();

        activity = jclass.GetStatic<AndroidJavaObject>("currentActivity");

        jniSingularUnityBridge.CallStatic("init", config.ToJsonString());

        singular.CallStatic("setWrapperNameAndVersion", UNITY_WRAPPER_NAME, UNITY_VERSION);
    }

    private static void InitAndroidJavaClasses() {
        if (singular == null) {
            singular = new AndroidJavaClass("com.singular.sdk.Singular");
        }

        if (jclass == null) {
            jclass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        }

        if (jniSingularUnityBridge == null) {
            jniSingularUnityBridge = new AndroidJavaClass("com.singular.unitybridge.SingularUnityBridge");
        }
    }

    private static AndroidJavaObject JavaArrayFromCS(string[] values) {
        AndroidJavaClass arrayClass = new AndroidJavaClass("java.lang.reflect.Array");
        AndroidJavaObject arrayObject = arrayClass.CallStatic<AndroidJavaObject>("newInstance", new AndroidJavaClass("java.lang.String"), values.Length);
        for (int i = 0; i < values.Length; ++i) {
            arrayClass.CallStatic("set", arrayObject, i, new AndroidJavaObject("java.lang.String", values[i]));
        }

        return arrayObject;
    }

#endif

    private enum NSType {
        STRING = 0,
        INT,
        LONG,
        FLOAT,
        DOUBLE,
        NULL,
        ARRAY,
        DICTIONARY,
    }

#if UNITY_IOS

    [DllImport("__Internal")]
    private static extern bool createReferrerShortLink_(string baseLink, string referrerName, string referrerId, string passthroughParams);

    [DllImport("__Internal")]
    private static extern bool StartSingularSession_(string config);

    [DllImport("__Internal")]
    private static extern bool StartSingularSessionWithLaunchOptions_(string key, string secret);

    [DllImport("__Internal")]
    private static extern bool StartSingularSessionWithLaunchURL_(string key, string secret, string url);

    [DllImport("__Internal")]
    private static extern void SendEvent_(string name);

    [DllImport("__Internal")]
    private static extern void SendEventWithArgs(string name);

    [DllImport("__Internal")]
    private static extern void SetDeviceCustomUserId_(string customUserId);

    [DllImport("__Internal")]
    private static extern void EndSingularSession_();

    [DllImport("__Internal")]
    private static extern void RestartSingularSession_(string key, string secret);

    [DllImport("__Internal")]
    private static extern void SetAllowAutoIAPComplete_(bool allowed);

    [DllImport("__Internal")]
    private static extern void SetBatchesEvents_(bool allowed);

    [DllImport("__Internal")]
    private static extern void SetBatchInterval_(int interval);

    [DllImport("__Internal")]
    private static extern void SendAllBatches_();

    [DllImport("__Internal")]
    private static extern void SetAge_(int age);

    [DllImport("__Internal")]
    private static extern void SetGender_(string gender);

    [DllImport("__Internal")]
    private static extern string GetAPID_();

    [DllImport("__Internal")]
    private static extern string GetIDFA_();

    // Revenue functions
    [DllImport("__Internal")]
    private static extern void Revenue_(string currency, double amount);

    [DllImport("__Internal")]
    private static extern void CustomRevenue_(string eventName, string currency, double amount);

    [DllImport("__Internal")]
    private static extern void RevenueWithAllParams_(string currency, double amount, string productSKU, string productName, string productCategory, int productQuantity, double productPrice);[DllImport("__Internal")] private static extern void CustomRevenueWithAllParams_(string eventName, string currency, double amount, string productSKU, string productName, string productCategory, int productQuantity, double productPrice);

    // Auxiliary functions;
    [DllImport("__Internal")]
    private static extern void Init_NSDictionary();

    [DllImport("__Internal")]
    private static extern void Init_NSMasterArray();

    [DllImport("__Internal")]
    private static extern void Push_NSDictionary(string key, string value, int type);

    [DllImport("__Internal")]
    private static extern void Free_NSDictionary();

    [DllImport("__Internal")]
    private static extern void Free_NSMasterArray();

    [DllImport("__Internal")]
    private static extern int New_NSDictionary();

    [DllImport("__Internal")]
    private static extern int New_NSArray();

    [DllImport("__Internal")]
    private static extern void Push_Container_NSDictionary(string key, int containerIndex);

    [DllImport("__Internal")]
    private static extern void Push_To_Child_Dictionary(string key, string value, int type, int dictionaryIndex);

    [DllImport("__Internal")]
    private static extern void Push_To_Child_Array(string value, int type, int arrayIndex);

    [DllImport("__Internal")]
    private static extern void Push_Container_To_Child_Dictionary(string key, int dictionaryIndex, int containerIndex);

    [DllImport("__Internal")]
    private static extern void Push_Container_To_Child_Array(int arrayIndex, int containerIndex);

    [DllImport("__Internal")]
    private static extern void RegisterDeviceTokenForUninstall_(string APNSToken);

    [DllImport("__Internal")]
    private static extern void RegisterDeferredDeepLinkHandler_();

    [DllImport("__Internal")]
    private static extern int SetDeferredDeepLinkTimeout_(int duration);

    [DllImport("__Internal")]
    private static extern void SetCustomUserId_(string customUserId);

    [DllImport("__Internal")]
    private static extern void UnsetCustomUserId_();

    [DllImport("__Internal")]
    private static extern void SetWrapperNameAndVersion_(string wrapper, string version);

    [DllImport("__Internal")]
    private static extern string GetGlobalProperties_();
    
    [DllImport("__Internal")]
    private static extern bool SetGlobalProperty_(string key, string value, bool overrideExisting);

    [DllImport("__Internal")]
    private static extern void UnsetGlobalProperty_(string key);

    [DllImport("__Internal")]
    private static extern void ClearGlobalProperties_();

    [DllImport("__Internal")]
    private static extern void TrackingOptIn_();

    [DllImport("__Internal")]
    private static extern void TrackingUnder13_();

    [DllImport("__Internal")]
    private static extern void StopAllTracking_();

    [DllImport("__Internal")]
    private static extern void ResumeAllTracking_();

    [DllImport("__Internal")]
    private static extern bool IsAllTrackingStopped_();

    [DllImport("__Internal")]
    private static extern void LimitDataSharing_(bool limitDataSharingValue);

    [DllImport("__Internal")]
    private static extern bool GetLimitDataSharing_();

    [DllImport("__Internal")]
    private static extern void SkanRegisterAppForAdNetworkAttribution_();

    [DllImport("__Internal")]
    private static extern bool SkanUpdateConversionValue_(int conversionValue);

    [DllImport("__Internal")]
    private static extern int SkanGetConversionValue_();

    private static void CreateDictionary(int parent, NSType parentType, string key, Dictionary<string, object> source) {
        int dictionaryIndex = New_NSDictionary();

        Dictionary<string, object>.Enumerator enumerator = source.GetEnumerator();

        while (enumerator.MoveNext()) {
            //test if string,int,float,double,null;
            NSType type = NSType.STRING;
            if (enumerator.Current.Value == null) {
                type = NSType.NULL;
                Push_To_Child_Dictionary(enumerator.Current.Key, "", (int)type, dictionaryIndex);
            } else {
                System.Type valueType = enumerator.Current.Value.GetType();

                if (valueType == typeof(int)) {
                    type = NSType.INT;
                } else if (valueType == typeof(long)) {
                    type = NSType.LONG;
                } else if (valueType == typeof(float)) {
                    type = NSType.FLOAT;
                } else if (valueType == typeof(double)) {
                    type = NSType.DOUBLE;
                } else if (valueType == typeof(Dictionary<string, object>)) {
                    type = NSType.DICTIONARY;
                    CreateDictionary(dictionaryIndex, NSType.DICTIONARY, enumerator.Current.Key, (Dictionary<string, object>)enumerator.Current.Value);
                } else if (valueType == typeof(ArrayList)) {
                    type = NSType.ARRAY;
                    CreateArray(dictionaryIndex, NSType.DICTIONARY, enumerator.Current.Key, (ArrayList)enumerator.Current.Value);
                }

                if ((int)type < (int)NSType.ARRAY) {
                    Push_To_Child_Dictionary(enumerator.Current.Key, enumerator.Current.Value.ToString(), (int)type, dictionaryIndex);
                }
            }
        }

        if (parent < 0) {
            Push_Container_NSDictionary(key, dictionaryIndex);
        } else {
            if (parentType == NSType.ARRAY) {
                Push_Container_To_Child_Array(parent, dictionaryIndex);
            } else {
                Push_Container_To_Child_Dictionary(key, parent, dictionaryIndex);
            }
        }
    }

    private static void CreateArray(int parent, NSType parentType, string key, ArrayList source) {
        int arrayIndex = New_NSArray();

        foreach (object o in source) {
            //test if string,int,float,double,null;
            NSType type = NSType.STRING;

            if (o == null) {
                type = NSType.NULL;
                Push_To_Child_Array("", (int)type, arrayIndex);
            } else {
                System.Type valueType = o.GetType();

                if (valueType == typeof(int)) {
                    type = NSType.INT;
                } else if (valueType == typeof(long)) {
                    type = NSType.LONG;
                } else if (valueType == typeof(float)) {
                    type = NSType.FLOAT;
                } else if (valueType == typeof(double)) {
                    type = NSType.DOUBLE;
                } else if (valueType == typeof(Dictionary<string, object>)) {
                    type = NSType.DICTIONARY;
                    CreateDictionary(arrayIndex, NSType.ARRAY, "", (Dictionary<string, object>)o);
                } else if (valueType == typeof(ArrayList)) {
                    type = NSType.ARRAY;
                    CreateArray(arrayIndex, NSType.ARRAY, "", (ArrayList)o);
                }

                if ((int)type < (int)NSType.ARRAY) {
                    Push_To_Child_Array(o.ToString(), (int)type, arrayIndex);
                }
            }
        }

        if (parent < 0) {
            Push_Container_NSDictionary(key, arrayIndex);
        } else {
            if (parentType == NSType.ARRAY) {
                Push_Container_To_Child_Array(parent, arrayIndex);
            } else {
                Push_Container_To_Child_Dictionary(key, parent, arrayIndex);
            }
        }
    }

#endif

    private static bool StartSingularSession(SingularConfig config) {
        if (!Application.isEditor) {
#if UNITY_IOS
            RegisterDeferredDeepLinkHandler_();

            SetWrapperNameAndVersion_(UNITY_WRAPPER_NAME, UNITY_VERSION);

            return StartSingularSession_(config.ToJsonString());
#endif
        }

        return false;
    }

    public static bool StartSingularSessionWithLaunchOptions(string key, string secret, Dictionary<string, object> options) {
        if (!Application.isEditor) {
#if UNITY_IOS
            Init_NSDictionary();
            Init_NSMasterArray();

            Dictionary<string, object>.Enumerator enumerator = options.GetEnumerator();

            while (enumerator.MoveNext()) {
                NSType type = NSType.STRING;

                if (enumerator.Current.Value == null) {
                    type = NSType.NULL;
                    Push_NSDictionary(enumerator.Current.Key, "", (int)type);
                } else {
                    System.Type valueType = enumerator.Current.Value.GetType();

                    if (valueType == typeof(int)) {
                        type = NSType.INT;
                    } else if (valueType == typeof(long)) {
                        type = NSType.LONG;
                    } else if (valueType == typeof(float)) {
                        type = NSType.FLOAT;
                    } else if (valueType == typeof(double)) {
                        type = NSType.DOUBLE;
                    } else if (valueType == typeof(Dictionary<string, object>)) {
                        type = NSType.DICTIONARY;
                        CreateDictionary(-1, NSType.DICTIONARY, enumerator.Current.Key, (Dictionary<string, object>)enumerator.Current.Value);
                    } else if (valueType == typeof(ArrayList)) {
                        type = NSType.ARRAY;
                        CreateArray(-1, NSType.DICTIONARY, enumerator.Current.Key, (ArrayList)enumerator.Current.Value);
                    }

                    if ((int)type < (int)NSType.ARRAY) {
                        Push_NSDictionary(enumerator.Current.Key, enumerator.Current.Value.ToString(), (int)type);
                    }
                }
            }

            StartSingularSessionWithLaunchOptions_(key, secret);


            Free_NSDictionary();
            Free_NSMasterArray();

            return true;
#endif
        }
        return false;
    }

    public static bool StartSingularSessionWithLaunchURL(string key, string secret, string url) {
        if (!Application.isEditor) {
#if UNITY_IOS
            return StartSingularSessionWithLaunchURL_(key, secret, url);
#endif
        }
        return false;
    }


    public static void RestartSingularSession(string key, string secret) {
        if (!Application.isEditor) {
#if UNITY_IOS
#elif UNITY_ANDROID
            if (singular != null) {
                singular.CallStatic("onActivityResumed");
            }
#endif
        }
    }

    public static void EndSingularSession() {
        if (!Application.isEditor) {
#if UNITY_IOS
#elif UNITY_ANDROID
            if (singular != null) {
                singular.CallStatic("onActivityPaused");
            }
#endif
        }
    }

    public static void Event(string name) {
        if (!Initialized)
            return;

        if (!Application.isEditor) {
#if UNITY_IOS
            SendEvent_(name);
#elif UNITY_ANDROID
            if (singular != null) {
                status = singular.CallStatic<bool>("isInitialized");
                singular.CallStatic<bool>("event", name);
            }
#endif
        }
    }

    /*
	dictionary is first parameter, because the compiler must be able to see a difference between
	SendEventWithArgs(Dictionary<string,object> args,string name) 
	and
	public static void SendEventsWithArgs(string name, params object[] args)
	the elements in the ArrayList and values in the Dictionary must have one of these types:
	  string, int, long, float, double, null, ArrayList, Dictionary<String,object>
	*/
    public static void Event(Dictionary<string, object> args, string name) {
        Debug.Log(string.Format("SingularSDK Event: args JSON={0}", JsonConvert.SerializeObject(args, Formatting.None)));

        if (!Initialized)
            return;

        if (!Application.isEditor) {
#if UNITY_IOS
            Init_NSDictionary();
            Init_NSMasterArray();

            Dictionary<string, object>.Enumerator enumerator = args.GetEnumerator();

            while (enumerator.MoveNext()) {
                NSType type = NSType.STRING;

                if (enumerator.Current.Value == null) {
                    type = NSType.NULL;
                    Push_NSDictionary(enumerator.Current.Key, "", (int)type);
                } else {
                    System.Type valueType = enumerator.Current.Value.GetType();

                    if (valueType == typeof(int)) {
                        type = NSType.INT;
                    } else if (valueType == typeof(long)) {
                        type = NSType.LONG;
                    } else if (valueType == typeof(float)) {
                        type = NSType.FLOAT;
                    } else if (valueType == typeof(double) || valueType == typeof(decimal) ) {
                        type = NSType.DOUBLE;
                    } else if (valueType == typeof(Dictionary<string, object>)) {
                        type = NSType.DICTIONARY;
                        CreateDictionary(-1, NSType.DICTIONARY, enumerator.Current.Key, (Dictionary<string, object>)enumerator.Current.Value);
                    } else if (valueType == typeof(ArrayList)) {
                        type = NSType.ARRAY;
                        CreateArray(-1, NSType.DICTIONARY, enumerator.Current.Key, (ArrayList)enumerator.Current.Value);
                    }

                    String stringVal;
                    String specifier = "G";
                    CultureInfo culture = CultureInfo.CreateSpecificCulture("en-US");
                    /*
                     *the Push_NSDictionary parses the stringVal to NSNumber in case the passed type is numeric (INT, FLOAT, DOUBLE...)
                     *in case the number is floating point, we need to convert it to string with en-US locale because otherwise,
                     *it will be converted according to the hosting app's locale, and some locales use a comma instead of a decimal point, 
                     *so that Push_NSDictionary will have trouble parsing it to NSNumber (for ex. 1.235 will be sent as 1,235)
                    */
                    if (valueType == typeof(float)){
                        stringVal = ((float)enumerator.Current.Value).ToString(specifier, culture);
                    }else if (valueType == typeof(double)){
                        stringVal = ((double)enumerator.Current.Value).ToString(specifier, culture);
                    }else if (valueType == typeof(decimal)){
                        stringVal = ((decimal)enumerator.Current.Value).ToString(specifier, culture);
                    }else{
                        stringVal = enumerator.Current.Value.ToString();
                    }
                    if ((int)type < (int)NSType.ARRAY) {
                        Push_NSDictionary(enumerator.Current.Key, stringVal, (int)type);
                    }
                }
            }

            SendEventWithArgs(name);
            Free_NSDictionary();
            Free_NSMasterArray();
#elif UNITY_ANDROID
            AndroidJavaObject json = new AndroidJavaObject("org.json.JSONObject", JsonConvert.SerializeObject(args, Formatting.None));

            if (singular != null) {
                status = singular.CallStatic<bool>("eventJSON", name, json);
            }
#endif
        }
    }

    /* 
	allowed argumenst are: string, int, long, float, double, null, ArrayList, Dictionary<String,object>
	the elements in the ArrayList and values in the Dictionary must have one of these types:
	string, int, long, float, double, null, ArrayList, Dictionary<String,object>
    */

    public static void Event(string name, params object[] args) {
        if (!Initialized)
            return;

        if (!Application.isEditor) {
#if UNITY_IOS || UNITY_ANDROID
            if (args.Length % 2 != 0) {
                // Debug.LogWarning("The number of arguments is ann odd number. The arguments are key-value pairs so the number of arguments should be even.");
            } else {
                Dictionary<string, object> dict = new Dictionary<string, object>();

                for (int i = 0; i < args.Length; i += 2) {
                    dict.Add(args[i].ToString(), args[i + 1]);
                }

                Event(dict, name);
            }
#endif
        }
    }

    public static void SetDeviceCustomUserId(string customUserId) {
        if (Application.isEditor) {
            return;
        }

        if (!Initialized) {
            return;
        }

#if UNITY_IOS
        SetDeviceCustomUserId_(customUserId);
#elif UNITY_ANDROID
        if (singular != null) {
            singular.CallStatic("setDeviceCustomUserId", customUserId);
        }
#endif
    }

    public static void SetAge(int age) {
        if (!Initialized)
            return;

        if (Mathf.Clamp(age, 0, 100) != age) {
            Debug.Log("Age " + age + "is not between 0 and 100");
            return;
        }
#if UNITY_IOS
        if (!Application.isEditor) {
            SetAge_(age);
        }
#endif
    }

    public static void SetGender(string gender) {
        if (!Initialized)
            return;

        if (gender != "m" && gender != "f") {
            Debug.Log("gender " + gender + "is not m or f");
            return;
        }
#if UNITY_IOS
        if (!Application.isEditor) {
            SetGender_(gender);
        }
#endif
    }

    public static void SetAllowAutoIAPComplete(bool allowed) {
#if UNITY_IOS
        if (!Application.isEditor) {
            SetAllowAutoIAPComplete_(allowed);
        }

        if (instance != null) {
            instance.autoIAPComplete = allowed;
        }
#elif UNITY_ANDROID
        if (Application.isEditor) {
            Debug.Log("SetAllowAutoIAPComplete is not supported on Android");
        }
#endif
    }

    void OnApplicationPause(bool paused) {
        if (!Initialized || !instance)
            return;

#if UNITY_IOS || UNITY_ANDROID
        if (paused) { //Application goes to background.
            if (!Application.isEditor) {
                if (endSessionOnGoingToBackground) {
                    EndSingularSession();
                }
            }
        } else { //Application did become active again.
            if (!Application.isEditor) {
                if (restartSessionOnReturningToForeground) {
                    RestartSingularSession(instance.SingularAPIKey, instance.SingularAPISecret);
                }
            }
        }
#endif
    }

    void OnApplicationQuit() {
        if (Application.isEditor) {
            return;
        }

        if (!Initialized)
            return;

#if UNITY_IOS || UNITY_ANDROID
        EndSingularSession();
#endif
    }

    public static void SetDeferredDeepLinkHandler(SingularDeferredDeepLinkHandler ddlHandler) {
        if (!instance) {
            Debug.LogError("SingularSDK SetDeferredDeepLinkHandler, no instance available - cannot set deferred deeplink handler!");
            return;
        }

        if (Application.isEditor) {
            return;
        }

        registeredDDLHandler = ddlHandler;
        System.Int32 now = (System.Int32)(System.DateTime.UtcNow.Subtract(new System.DateTime(1970, 1, 1))).TotalSeconds;

        // call the ddl handler with the cached value if the timeout has not passed yet
        if (now - cachedDDLMessageTime < instance.ddlTimeoutSec && cachedDDLMessage != null) {
            registeredDDLHandler.OnDeferredDeepLink(cachedDDLMessage);
        }
    }

    // this is the internal handler - handling deeplinks for both iOS & Android
    public void DeepLinkHandler(string message) {
        Debug.Log(string.Format("SingularSDK DeepLinkHandler called! message='{0}'", message));

        if (Application.isEditor) {
            return;
        }

        if (message == "") {
            message = null;
        }
        if (registeredDDLHandler != null) {
            registeredDDLHandler.OnDeferredDeepLink(message);
        } else {
            cachedDDLMessage = message;
            cachedDDLMessageTime = CurrentTimeSec();
        }
    }

    private static int CurrentTimeSec() {
        return (System.Int32)(System.DateTime.UtcNow.Subtract(new System.DateTime(1970, 1, 1))).TotalSeconds;
    }

    public static void SetSingularLinkHandler(SingularLinkHandler handler) {
        if (Application.isEditor) {
            return;
        }

        registeredSingularLinkHandler = handler;

        // In case the link was resolved before the client registered
        if (instance != null) {
            instance.ResolveSingularLink();
        }
    }

    private void SingularLinkHandlerResolved(string handlerParamsJson) {
        instance.resolvedSingularLinkParams = JsonConvert.DeserializeObject<SingularLinkParams>(handlerParamsJson);
        instance.resolvedSingularLinkTime = CurrentTimeSec();

        ResolveSingularLink();
    }

    private void ShortLinkResolved(string json) {
        ShortLinkParams shortLinkParams;
        shortLinkParams = JsonConvert.DeserializeObject<ShortLinkParams>(json);
        if (shortLinkCallback != null){
            shortLinkCallback(string.IsNullOrEmpty(shortLinkParams.Data)?null:shortLinkParams.Data, string.IsNullOrEmpty(shortLinkParams.Error)?null:shortLinkParams.Error);
            shortLinkCallback = null;
        }
    }

    public static void SetConversionValueUpdatedHandler(SingularConversionValueUpdatedHandler handler)
    {
#if UNITY_IOS
        if (Application.isEditor)
        {
            return;
        }

        registeredConversionValueUpdatedHandler = handler;
#endif
    }

    private void ConversionValueUpdated(string value)
    {
#if UNITY_IOS
        if (registeredConversionValueUpdatedHandler != null)
        {
            int intValue;
            if (int.TryParse(value, out intValue)) {
                registeredConversionValueUpdatedHandler.OnConversionValueUpdated(intValue);
            }
        }
#endif
    }

    private void ResolveSingularLink() {
        if (instance.resolvedSingularLinkParams != null) {
            if (registeredSingularLinkHandler != null) {

                if (CurrentTimeSec() - resolvedSingularLinkTime <= shortlinkResolveTimeout) {
                    registeredSingularLinkHandler.OnSingularLinkResolved(instance.resolvedSingularLinkParams);
                }

                instance.resolvedSingularLinkParams = null;

            } else if (registeredDDLHandler != null) {

                if (ddlTimeoutSec <= 0) {
                    ddlTimeoutSec = DEFAULT_DDL_TIMEOUT;
                }

                if (CurrentTimeSec() - instance.resolvedSingularLinkTime <= ddlTimeoutSec) {
                    registeredDDLHandler.OnDeferredDeepLink(instance.resolvedSingularLinkParams.Deeplink);
                }

                instance.resolvedSingularLinkParams = null;
            }
        }
    }

    public static void RegisterDeviceTokenForUninstall(string APNSToken) {
#if UNITY_IOS
        if (!Application.isEditor) {
            if (APNSToken.Length % 2 != 0) {
                Debug.Log("RegisterDeviceTokenForUninstall: token must be an even-length hex string!");
                return;
            }

            RegisterDeviceTokenForUninstall_(APNSToken);
        }
#elif UNITY_ANDROID
        Debug.Log("RegisterDeviceTokenForUninstall is supported only for iOS");
#endif
    }


    public static string GetAPID() {
        //only works for iOS. Will return null until Singular is initialized.
#if UNITY_IOS
        if (!Application.isEditor) {
            return GetAPID_();
        }
#endif
        return null;
    }

    public static string GetIDFA() {
        //only works for iOS. Will return null until Singular is initialized.
#if UNITY_IOS
        if (!Application.isEditor) {
            return GetIDFA_();
        }
#endif
        return null;
    }

#if UNITY_5_3_OR_NEWER && UNITY_PURCHASING

    public static void InAppPurchase(IEnumerable<Product> products, Dictionary<string, object> attributes, bool isRestored = false) {
        InAppPurchase("__iap__", products, attributes, isRestored);
    }

    public static void InAppPurchase(string eventName, IEnumerable<Product> products, Dictionary<string, object> attributes, bool isRestored = false) {
        foreach (var item in products) {
            InAppPurchase(eventName, item, attributes, isRestored);
        }
    }

    public static void InAppPurchase(Product product, Dictionary<string, object> attributes, bool isRestored = false) {
        InAppPurchase("__iap__", product, attributes, isRestored);
    }

    public static void InAppPurchase(string eventName, Product product, Dictionary<string, object> attributes, bool isRestored = false) {
        if (Application.isEditor) {
            return;
        }

        if (product == null) {
            return;
        }

        double revenue = (double)product.metadata.localizedPrice;

        // Restored transactions are not counted as revenue. This is to be consistent with the iOS SDK
        if (isRestored) {
            revenue = 0.0;
        }

        if (!product.hasReceipt) {
            CustomRevenue(eventName, product.metadata.isoCurrencyCode, revenue);
        } else {
            Dictionary<string, object> purchaseData= null;
#if UNITY_IOS
            purchaseData = BuildIOSPurchaseAttributes(product, attributes, isRestored);
#elif UNITY_ANDROID
            purchaseData = BuildAndroidPurchaseAttributes(product, attributes, isRestored);
#endif
            Event(purchaseData, eventName);
        }
    }

#if UNITY_IOS
    private static Dictionary<string, object> BuildIOSPurchaseAttributes(Product product, Dictionary<string, object> attributes, bool isRestored) {

        var transactionData = new Dictionary<string, object>();

        if (product.definition != null) {
            transactionData["pk"] = product.definition.id;

#if UNITY_2017_2_OR_NEWER
            if (product.definition.payout != null) {
                transactionData["pq"] = product.definition.payout.quantity;
            }
#endif
        }

        if (product.metadata != null) {
            transactionData["pn"] = product.metadata.localizedTitle;
            transactionData["pcc"] = product.metadata.isoCurrencyCode;
            transactionData["pp"] = (double)product.metadata.localizedPrice;
        }

        transactionData["ps"] = @"a";
        transactionData["pt"] = @"o";
        transactionData["pc"] = @"";
        transactionData["ptc"] = isRestored;
        transactionData["pti"] = product.transactionID;
        transactionData["ptr"] = ExtractIOSTransactionReceipt(product.receipt);
        transactionData["is_revenue_event"] = true;


        // Restored transactions are not counted as revenue
        if (isRestored) {
            transactionData["original_price"] = transactionData["pp"];
            transactionData["pp"] = 0.0;
        }

        if (attributes != null) {
            foreach (var item in attributes) {
                transactionData[item.Key] = item.Value;
            }
        }

        return transactionData;
    }

    private static string ExtractIOSTransactionReceipt(string receipt) {
        if (string.IsNullOrEmpty(receipt.Trim())) {
            return null;
        }

        Dictionary<string, string> values = JsonConvert.DeserializeObject<Dictionary<string, string>>(receipt);

        if (!values.ContainsKey("Payload")) {
            return null;
        }

        return values["Payload"];
    }

#endif

#if UNITY_ANDROID
    private static Dictionary<string, object> BuildAndroidPurchaseAttributes(Product product, Dictionary<string, object> attributes, bool isRestored) {
        var transactionData = new Dictionary<string, object>();

        if (product.receipt == null) {
            return attributes;
        }

        if (attributes != null) {
            foreach (var item in attributes) {
                transactionData[item.Key] = item.Value;
            }
        }

        var values = JsonConvert.DeserializeObject<Dictionary<string, string>>(product.receipt);

        if (values.ContainsKey("signature")) {
            transactionData["receipt_signature"] = values["signature"];
        }

        if (product.definition != null) {
            transactionData["pk"] = product.definition.id;
        }

        transactionData["receipt"] = product.receipt;
        transactionData["is_revenue_event"] = true;

        if (product.metadata != null) {
            transactionData["r"] = isRestored ? 0 : (double)product.metadata.localizedPrice;
            transactionData["pcc"] = product.metadata.isoCurrencyCode;
        }

        return transactionData;
    }
#endif

#endif
    public static void Revenue(string currency, double amount) {
        if (Application.isEditor) {
            return;
        }
#if UNITY_IOS
        Revenue_(currency, amount);
#elif UNITY_ANDROID
        if (singular != null) {
            singular.CallStatic<bool>("revenue", currency, amount);
        }
#endif
    }

    public static void CustomRevenue(string eventName, string currency, double amount) {
        if (Application.isEditor) {
            return;
        }
#if UNITY_IOS
        CustomRevenue_(eventName, currency, amount);
#elif UNITY_ANDROID
        if (singular != null) {
            singular.CallStatic<bool>("customRevenue", eventName, currency, amount);
        }
#endif
    }

    public static void Revenue(string currency, double amount, string receipt, string signature) {
        if (Application.isEditor) {
            return;
        }
#if UNITY_ANDROID
        if (singular != null) {
            singular.CallStatic<bool>("revenue", currency, amount, receipt, signature);
        }
#endif
    }

    public static void CustomRevenue(string eventName, string currency, double amount, string receipt, string signature) {
        if (Application.isEditor) {
            return;
        }
#if UNITY_ANDROID
        if (singular != null) {
            singular.CallStatic<bool>("customRevenue", eventName, currency, amount, receipt, signature);
        }
#endif
    }
    public static void Revenue(string currency, double amount, string productSKU, string productName, string productCategory, int productQuantity, double productPrice) {
        if (Application.isEditor) {
            return;
        }

#if UNITY_IOS
        RevenueWithAllParams_(currency, amount, productSKU, productName, productCategory, productQuantity, productPrice);

#elif UNITY_ANDROID
        if (singular != null) {
            singular.CallStatic<bool>("revenue", currency, amount, productSKU, productName, productCategory, productQuantity, productPrice);
        }
#endif
    }

    public static void CustomRevenue(string eventName, string currency, double amount, string productSKU, string productName, string productCategory, int productQuantity, double productPrice) {
        if (Application.isEditor) {
            return;
        }
#if UNITY_IOS
        CustomRevenueWithAllParams_(eventName, currency, amount, productSKU, productName, productCategory, productQuantity, productPrice);

#elif UNITY_ANDROID
        if (singular != null) {
            singular.CallStatic<bool>("customRevenue", eventName, currency, amount, productSKU, productName, productCategory, productQuantity, productPrice);
        }
#endif
    }

    public static void RegisterTokenForUninstall(String token){
#if UNITY_IOS
        RegisterDeviceTokenForUninstall(token);
#elif UNITY_ANDROID
        SetFCMDeviceToken(token);
#endif
        
    }

    public static void SetFCMDeviceToken(string fcmDeviceToken) {
        if (Application.isEditor) {
            return;
        }
#if UNITY_IOS

#elif UNITY_ANDROID
        if (singular != null) {
            singular.CallStatic("setFCMDeviceToken", fcmDeviceToken);
        }else{
            SingularSDK.fcmDeviceToken = fcmDeviceToken;
        }
#endif
    }

    public static void SetGCMDeviceToken(string gcmDeviceToken) {
        if (Application.isEditor) {
            return;
        }
#if UNITY_IOS

#elif UNITY_ANDROID
        if (singular != null) {
            singular.CallStatic("setGCMDeviceToken", gcmDeviceToken);
        }
#endif
    }

    public static void SetCustomUserId(string userId) {
        if (Application.isEditor) {
            return;
        }
#if UNITY_IOS
        SetCustomUserId_(userId);
#elif UNITY_ANDROID
        if (singular != null) {
            customUserId = userId;
            singular.CallStatic("setCustomUserId", userId);
        } else {
            customUserId = userId;
        }
#endif
    }

    public static void UnsetCustomUserId() {
        if (Application.isEditor) {
            return;
        }
#if UNITY_IOS
        UnsetCustomUserId_();
#elif UNITY_ANDROID
        if (singular != null) {
            singular.CallStatic("unsetCustomUserId");
        } else {
            customUserId = null;
        }
#endif
    }

    public static void TrackingOptIn() {
        if (Application.isEditor) {
            return;
        }
#if UNITY_IOS
        TrackingOptIn_();
#elif UNITY_ANDROID
        if (singular != null) {
            singular.CallStatic("trackingOptIn");
        }
#endif
    }

    public static void TrackingUnder13() {
        if (Application.isEditor) {
            return;
        }
#if UNITY_IOS
        TrackingUnder13_();
#elif UNITY_ANDROID
        if (singular != null) {
            singular.CallStatic("trackingUnder13");
        }
#endif
    }

    public static void StopAllTracking() {
        if (Application.isEditor) {
            return;
        }
#if UNITY_IOS
        StopAllTracking_();
#elif UNITY_ANDROID
        if (singular != null) {
            singular.CallStatic("stopAllTracking");
        }
#endif
    }

    public static void ResumeAllTracking() {
        if (Application.isEditor) {
            return;
        }
#if UNITY_IOS
        ResumeAllTracking_();
#elif UNITY_ANDROID
        if (singular != null) {
            singular.CallStatic("resumeAllTracking");
        }
#endif
    }

    public static bool IsAllTrackingStopped() {
        if (Application.isEditor) {
            return false;
        }

        if (Application.platform == RuntimePlatform.IPhonePlayer) {
#if UNITY_IOS
            return IsAllTrackingStopped_();
#endif
        } else if (Application.platform == RuntimePlatform.Android) {
#if UNITY_ANDROID
            if (singular != null) {
                return singular.CallStatic<bool>("isAllTrackingStopped");
            }
#endif
        }

        return false;
    }

    public static void LimitDataSharing(bool limitDataSharingValue)
    {
        if (Application.isEditor)
        {
            return;
        }
#if UNITY_IOS
        LimitDataSharing_(limitDataSharingValue);
#elif UNITY_ANDROID
        if (singular != null)
        {
            singular.CallStatic("limitDataSharing", limitDataSharingValue);
        }
#endif
    }

    public static bool GetLimitDataSharing()
    {
        if (Application.isEditor)
        {
            return false;
        }

#if UNITY_IOS
        return GetLimitDataSharing_();
#endif
#if UNITY_ANDROID
        if (singular != null)
        {
            return singular.CallStatic<bool>("getLimitDataSharing");
        }
#endif

        return false;
    }

      public static void AdRevenue(SingularAdData adData) {
        try {
            if(!Initialized || adData == null || !adData.HasRequiredParams()) {
                return;
            }

            Event(adData, ADMON_REVENUE_EVENT_NAME);
        } catch (Exception) { }
    }

#if UNITY_ANDROID
    public static void SetIMEI(string imeiData) {
        if (Application.isEditor) {
            return;
        }

        if (singular != null) {
            singular.CallStatic("setIMEI", imeiData);
        } else {
            imei = imeiData;
        }
    }
#endif

    #region Global Properties

    public static Dictionary<string, string> GetGlobalProperties() {
        if (Application.isEditor) {
            return null;
        }

        string propertiesJsonString = null;

#if UNITY_IOS
        propertiesJsonString = GetGlobalProperties_();
#elif UNITY_ANDROID
        if (singular != null) {
            AndroidJavaObject javaMap = singular.CallStatic<AndroidJavaObject>("getGlobalProperties");

            AndroidJavaObject propertiesJson = new AndroidJavaObject("org.json.JSONObject", javaMap);
            propertiesJsonString = propertiesJson.Call<string>("toString");
        }
#endif

        if (propertiesJsonString == null || propertiesJsonString.Trim() == "") {
            return null;
        }

        return JsonConvert.DeserializeObject<Dictionary<string, string>>(propertiesJsonString);
    }

    public static bool SetGlobalProperty(string key, string value, bool overrideExisting) {
        if (Application.isEditor) {
            return false;
        }

        if (key == null || key.Trim() == string.Empty) {
            return false;
        }

        if (!Initialized) {
            instance.globalProperties[key] = new SingularGlobalProperty(key, value, overrideExisting);
            return true;
        } 

#if UNITY_IOS
        return SetGlobalProperty_(key, value, overrideExisting);
#elif UNITY_ANDROID
        if (singular != null) {
            return singular.CallStatic<bool>("setGlobalProperty", key, value, overrideExisting);
        }
#endif
        return false;
    }

    public static void UnsetGlobalProperty(string key) {
        if (Application.isEditor || !Initialized) {
            return;
        }

#if UNITY_IOS
        UnsetGlobalProperty_(key);
#elif UNITY_ANDROID
        if (singular != null) {
            singular.CallStatic("unsetGlobalProperty", key);
        }
#endif
    }

    public static void ClearGlobalProperties() {
        if (Application.isEditor) {
            return;
        }

#if UNITY_IOS
        ClearGlobalProperties_();
#elif UNITY_ANDROID
        if (singular != null) {
            singular.CallStatic("clearGlobalProperties");
        }
#endif
    }

    public static void SkanRegisterAppForAdNetworkAttribution() {
#if UNITY_IOS
        SkanRegisterAppForAdNetworkAttribution_();
#endif
    }

    public static bool SkanUpdateConversionValue(int conversionValue) {
#if UNITY_IOS
        return SkanUpdateConversionValue_(conversionValue);
#else
        return false;
#endif
    }

    public static int? SkanGetConversionValue() {
#if UNITY_IOS
        int value = SkanGetConversionValue_();
        if (value == -1) {
            return null;
        }

        return value;
#else
        return null;
#endif
    }

    #endregion

    private class SingularConfig {
        private Dictionary<string, object> _configValues;

        public SingularConfig() {
            _configValues = new Dictionary<string, object>();
        }

        public void SetValue(string key, object value) {
            if (key == null || key.Trim() == "" || value == null) {
                return;
            }

            _configValues[key] = value;
        }

        public string ToJsonString() {
            return JsonConvert.SerializeObject(_configValues);
        }
    }

    private class SingularGlobalProperty {
        public string Key { get; set; }
        public string Value { get; set; }
        public bool OverrideExisting { get; set; }

        public SingularGlobalProperty(string key, string value, bool overrideExisting) {
            Key = key;
            Value = value;
            OverrideExisting = overrideExisting;
        }
    }
}

