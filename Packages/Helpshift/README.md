# Getting Started
===============

Guide to integrate the Unity plugin for Helpshift SDK X, which you can call from your C# and JavaScript game scripts.

## Requirements

*   Unity 5.5.6 and above
    
*   Supported Android OS version: 16 and above
    
*   Xcode 12.0 and above
    
*   Supported iOS versions: 14, 12, 11, and 10
    

## Integrate Helpshift Unity SDK X
-------------------------------

Helpshift SDK _.zip_ folder includes:

helpshiftX-plugin-unity.unitypackage: Unity package of Helpshift SDK

unity-jar-resolver (v1.2.160.0): Resolves Android Helpshift package support lib dependencies.

### Add Helpshift to your Unity project

*   Unzip the Helpshift Unity SDK package.
*   Helpshift Unity SDK appears as a standard `.unitypackage` which you can import through the Unity package import procedure.
*   Following are the steps to import the `helpshiftx-plugin-unity-version.unitypackage` into your Unity game:
    
    1.  In Open Unity project, navigate to **Assets** drop-down menu and select the **Import Package** > **Custom Package**
        
    2.  From the unzipped SDK, select `helpshiftx-plugin-unity-version.unitypackage` file to import the Helpshift SDK.
        
    3.  In the _Import Unity Package_ window, click **Import**
        
    4.  If you are also integrating for iOS, select `iOS/Helpshift.framework` folder while importing the unitypackage.
        

### Integrating Android

#### Resolve Android Appcompat library requirements

Helpshift SDK depends on Android appcompat libraries. You can get these libraries in one of the following ways, depending on the build process that you use.

##### Resolve dependency when using Unity's Internal or Unity's internal Gradle build system

Use **Unity Jar Resolver** plugin to download and integrate Android library dependencies.

Note: If your project already uses the _Unity Jar Resolver_, you can skip the _Unity Jar Resolver_ importing step.

*   Import the **Unity Jar Resolver** plugin into your Unity project.
    
    1.  In Open Unity project, navigate to **Assets** drop-down menu and select the **Import Package** > **Custom Package.**
        
    2.  From the unzipped Helpshift SDK, select `unity-jar-resolver/external-dependency-manager-1.2.160.unitypackage` file to import the Unity Jar resolver.
        
    3.  In the _Import Unity Package_ window, click **Import**
        
*   If you cannot import _Unity Jar Resolver_ packaged with Helpshift plugin due to any reason, you can use any version of _Unity Jar Resolver_ as per your needs.  
    Refer here: [Unity Jar Resolver](https://github.com/googlesamples/unity-jar-resolver/releases)
    
*   If _Unity Jar Resolver_ wants to enable Android Auto-resolution, click the _Enable_ button to resolve all the dependencies automatically on changing any dependency file. You can enable or disable the settings using _Assets_ > _Play Services Resolver_ > _Android Resolver_ > _Settings._
    
*   By default, _Unity Jar Resolver_ auto-resolves all the required dependencies. Following are the steps to resolve dependencies manually:
    
    1.  In the Open Unity project, navigate to **Assets** dropdown menu and choose **Play services resolver** > **Android Resolve.**
        
    2.  Select the **Resolve** or **Force Resolve.**
        
*   To know more about the Unity Jar Resolver, refer to: [Unity Jar Resolver](https://github.com/googlesamples/unity-jar-resolver)
    

##### Resolve dependency when using custom Gradle template or Export Project

_Unity's in-built Gradle build support_ and _exporting to Android Studio_ does not support per plugin Gradle script. Therefore, by default, Helpshift SDK cannot add the dependencies by itself.

The `mainTemplate.gradle` file is generated when you enable the **Custom Gradle Template** property on the Player window.

The `build.gradle` file exists in the generated Gradle project when you enable the **Export Project** property on the Player window and build the project.

Update **dependencies** section of the `mainTemplate.gradle` or `build.gradle` file as:
~~~code
dependencies {
    implementation fileTree(dir: 'libs', include: \['\*.jar'\])
    implementation(name: 'Helpshift', ext:'aar')
    implementation 'com.android.support:appcompat-v7:28.0.0'
    //...
}
~~~
### Integrating iOS

#### Info.plist changes - Addition of usage description strings

When the “User Attachments” feature is enabled on the in-app configuration, the SDK allows the user to upload the attachments. The attachments can be picked from the Photo Library or can be captured directly using the camera.

To use the camera and photo library, Apple requires the app to have Usage Description strings in the Info.plist file. Failing to add these strings might result in app rejection. The following strings are needed for the attachment feature:

Key: _NSPhotoLibraryUsageDescription_  
Suggested string: “We need to access photos library to allow users to manually pick images meant to be sent as an attachment for help and support reasons.”  
Note: This is not needed if your app is on iOS 11 or above. Below iOS 11, this key is compulsory, else the app may crash when user tries to open the photo library for attaching photos.

Key: _NSCameraUsageDescription_  
Suggested string: “We need to access the camera to allow users to click images meant to be sent as an attachment for help and support reasons.”  
Note: This key is needed for capturing a photo using the camera and attaching it.

Key: _NSMicrophoneUsageDescription_  
Suggested string: “We need to access the microphone to allow users to record videos using the camera meant to be sent as an attachment for help and support reasons.”  
Note: This key is needed for capturing a video using the camera and attaching it.

The above strings are just suggestions. If you need localizations for these, please contact us.

Note:  
End-users can attach files such as PDF, video, etc. to their issues. For iOS 10 and below, to access files in the “Files” app of iOS, you need to add iCloud capability with iCloud Documents services enabled. For more information, please refer to the [Prerequisites section here.](https://developer.apple.com/library/archive/documentation/FileManagement/Conceptual/DocumentPickerProgrammingGuide/Introduction/Introduction.html#//apple_ref/doc/uid/TP40014451)

### Initializing Helpshift in your app

To use Helpshift's APIs, please import the Helpshift's namespace like below

    using Helpshift;

1.  First, create an app on the Helpshift Dashboard
    
2.  Create an app with **Android** / **iOS** as selected `Platform`
    

Helpshift uniquely identifies each registered App with a combination of 2 tokens:

`Domain Name`: Your Helpshift domain. e.g. [_happyapps.helpshift.com_](http://happyapps.helpshift.com)

`Platform ID`: Your App's unique platform id (App's `App ID` on dashboard is your Platform ID)

You can find these by navigating to `Settings`\>`SDK (for Developers)` on the Helpshift Dashboard.

Select your App and _check Android as a platform_ from the dropdowns, and copy the 2 tokens to be passed when initializing Helpshift.

Initialize Helpshift by calling the `install(platformId, domain)` API

~~~code
using Helpshift;
.
.
.
public class MyGameControl : MonoBehaviour
{
    private HelpshiftSdk help;
    ...

    void Awake() {
        help = HelpshiftSdk.GetInstance();
        var configMap = new Dictionary<string, object>();
        help.Install(platformId, domainName, configMap);
    }
    ...
}
~~~

#### Install Helpshift via ObjC for iOS

If you intend to initialize the SDK from Xcode using Objective-C, you can specify Domain Name, Platform ID, and other install configurations in the Objective-C code. To achieve this, you need to override `application:didFinishLaunchingWithOptions` in `HsUnityAppcontroller` as shown below.

~~~code
#import <HelpshiftX/HelpshiftX.h>
#import "HelpshiftX-Unity.h"

- (BOOL) application:(UIApplication \*)application didFinishLaunchingWithOptions:(NSDictionary \*)launchOptions {
    // Set install configurations
    NSDictionary \*installConfig = @{@"enableInAppNotification":@YES};

    // Make install call
    \[Helpshift installWithPlatformId:@"<your\_platform\_id>" domain:@"<your\_domain\_name>" config:installConfig\];
    return \[super application:application didFinishLaunchingWithOptions:launchOptions\];
}
~~~

**Note:**  
**Placing the install call**

You should place install call inside the `Awake()` method. Placing it elsewhere might cause unexpected runtime problems.

**HelpshiftInitializationException**

Calling any API before the install call would throw an unchecked HelpshiftInitializationException in debug mode.

**Android OS version Support**

Calling `install()` below android SDK version 21 will not work. All the APIs will be non-operable.

### Start using Helpshift
---------------------

Helpshift is now integrated in your app. You should now use the support APIs for conversation screen inside your app.  
[https://developers.helpshift.com/sdkx-unity/support-tools-ios/#conversation-view](https://developers.helpshift.com/sdkx-unity/support-tools-ios/#conversation-view)  
[https://developers.helpshift.com/sdkx-unity/support-tools-android/#conversation-view](https://developers.helpshift.com/sdkx-unity/support-tools-android/#conversation-view)

Run your app, and try starting a test conversation using the _ShowConversation_ API call.  
After starting a conversation, go to your Helpshift Dashboard and reply to experience in-app messaging.

Sample usage for FAQs and conversation APIs:

~~~code
public class MyGameControl : MonoBehaviour
{
    private HelpshiftSdk help;

    void Awake(){
        help = HelpshiftSdk.GetInstance();
        var configMap = new Dictionary<string, object>();
        help.Install(platformId, domainName, configMap);
    }

    void OnGUI () {
        ...
        var configMap = new Dictionary<string, object>();

        // Starting a conversation with your customers
        if (MenuButton (contactButton))
        {
            help.ShowConversation(configMap);
        }
        else if (MenuButton (helpButton))
        {
            // Show an FAQ screen
            help.ShowFAQs(configMap);
        }
    }
}
~~~ 
**Note:**

Since the Helpshift plugin is meant for mobile devices only, you should put all Helpshift calls inside checks to make sure they are only called when running on a device.

For detailed configurations and more exciting features, please refer to these documents:  
[https://developers.helpshift.com/sdkx-unity/getting-started-ios/](https://developers.helpshift.com/sdkx-unity/getting-started-ios/)  
[https://developers.helpshift.com/sdkx-unity/getting-started-android/](https://developers.helpshift.com/sdkx-unity/getting-started-android/)