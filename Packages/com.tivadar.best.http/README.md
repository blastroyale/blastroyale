Best HTTP is a comprehensive networking library for Unity that empowers developers to make HTTP and HTTPS requests with ease. 
Whether you're building web applications, multiplayer games, or real-time communication solutions, Best HTTP has got you covered.

## Overview
In today's digital era, efficient and reliable web communication forms the backbone of many applications. 
Whether you're fetching data from remote servers, sending game scores, or updating user profiles, HTTP requests are indispensable. 
Recognizing the multifaceted needs of Unity developers, Best HTTP is designed to simplify these interactions, providing a streamlined and efficient means to handle web-based communication.

## Key Features
- **Supported Unity Versions**: Best HTTP is compatible with Unity versions starting from :fontawesome-brands-unity: **2021.1 onwards**.
- **Cross-Platform:** Best HTTP is designed to work seamlessly across a diverse range of Unity platforms, ensuring versatility for all your development needs. Specifically, it supports:
    - :fontawesome-solid-desktop: **Desktop**: Windows, Linux, MacOS
    - :fontawesome-solid-mobile: **Mobile**: iOS, Android
    - :material-microsoft-windows: **Universal Windows Platform** (UWP)
    - :material-web: **Web Browsers**: WebGL

	This broad platform support means you can confidently use Best HTTP, regardless of your target audience or deployment strategy.

- **Versatile Request Outcome Handling**: Best HTTP ensures flexibility in managing network request outcomes to seamlessly fit within your development style and the varied structures of different applications:
    - **Traditional Callbacks:** Adopt the classic approach with regular C# callbacks. Ideal for those who prefer traditional programming patterns, allowing for simple and straightforward handling of responses.
    - **Unity Coroutines:** For those who are deeply integrated with Unity's workflow, Best HTTP provides native support for Unity's coroutines. This facilitates non-blocking operations while keeping the code structure clean and readable, particularly when sequencing multiple network requests.
    - **Async-Await Pattern:** Embrace the modern C# asynchronous programming paradigm with the async-await pattern. Best HTTP's support for this ensures that developers can write non-blocking code in a linear fashion, greatly simplifying error handling and state management for asynchronous operations.
    
    With these diverse options for request outcome handling, developers can choose the best approach that aligns with their project requirements and personal coding preferences.

- **HTTP/HTTPS Support**: Best HTTP supports both HTTP and HTTPS protocols, ensuring secure communication for your applications.
- **HTTP/2 Support**: Benefit from the advantages of HTTP/2, including faster loading times, reduced latency and **trailing headers** for advanced scenarios like **GRPC**.
- **Caching**: Implement efficient caching mechanisms to reduce redundant network requests, optimizing your application's performance and data usage.
- **Authentication**: Easily handle various authentication methods, such as Basic, Digest, and Bearer token authentication.
- **Cookie Management**: Manage cookies effortlessly, ensuring smooth user experiences in web applications.
- **Compression**: Compress and decompress data using gzip and deflate algorithms to save bandwidth and improve loading times.
- **Streaming**: Best HTTP supports streaming for both downloads and uploads. This enables you to stream large files and responses directly to storage when downloading, and stream data from storage when uploading, effectively avoiding memory bottlenecks.
- **Customization**: Tailor your HTTP requests with customizable headers, timeouts, and other parameters to meet your specific needs.
- **Built-In Profiler Support**: Best HTTP now comes with a built-in profiler, ensuring developers have direct access to critical insights without the need for external tools. 
This enhancement is instrumental in understanding the performance and network behavior of your application, thereby facilitating optimization and debugging. Key features of the built-in profiler include:
	- **Memory Profiler:** Dive into the library's internal memory usage. This tool is invaluable for ensuring optimal memory management and for identifying potential bottlenecks or leaks.
	- **Network Profiler:** This profiler allows for a granular analysis of your network operations. Notable features include:
		- **Byte Tracking:** Monitor the bytes sent and received between two frames, providing a clear overview of data transfers and insight into traffic patterns.
		- **Connection Analysis:** Stay informed on the total number of open and closed connections. This data gives a transparent view of your app's network activity.
		- **DNS Cache Profiling:** With this feature, you can track DNS cache hits and misses, aiding in the fine-tuning of DNS cache strategies and understanding potential network resolution delays.

		With the integration of this built-in profiler support, developers can not only ensure that their application's network activities are optimized but also make data-driven decisions that enhance both performance and user experience.

## Installation Guide

Getting started with Best HTTP is straightforward. 
Depending on your preference, you can either install the package via the Unity Asset Store or use a .unitypackage. 
Below are step-by-step guides for both methods:

### Installing from the Unity Asset Store using the Package Manager Window

1. **Purchase:** If you haven't previously purchased the package, proceed to do so. Once purchased, Unity will recognize your purchase, and you can install the package directly from within the Unity Editor. If you already own the package, you can skip these steps.
    1. **Visit the Unity Asset Store:** Using your preferred web browser, navigate to the [Unity Asset Store](https://assetstore.unity.com/publishers/4137?aid=1101lfX8E).
    2. **Search for Best HTTP:** Locate and choose the official Best HTTP package.
	3. **Buy Best HTTP:** By clicking on the `Buy Now` button go though the purchase process.
2. **Open Unity & Access the Package Manager:** Launch Unity and open your desired project. Navigate to [Window > Package Manager](https://docs.unity3d.com/Manual/upm-ui.html).
3. **Select 'My Assets':** In the Package Manager window, switch to the [My Assets](https://docs.unity3d.com/Manual/upm-ui-import.html) tab. This will show all assets you have access to, including ones you've purchased or added to your Asset Store account.
4. **Find Best HTTP and Download:** Locate "Best HTTP" in the list. Once found, click on it to view the details. If the package isn't already downloaded, you'll see a Download button. Click this button and wait for the package to download. Once downloaded, the button will change to Import.
5. **Import the Package:** After downloading, click the Import button. Unity will then show you a list of all the assets associated with the Best HTTP package. Ensure all assets are checked and click Import.
6. **Confirmation:** Upon importing, Best HTTP will be added to your project, indicating a successful installation.

### Installing from a .unitypackage file

In some cases you might have a .unitypackage file containing the plugin. 

1. **Download the .unitypackage:** Ensure you have the Best HTTP.unitypackage file saved on your computer. This might be from a direct download link or an email attachment, depending on how it was distributed.
2. **Import into Unity:** Launch Unity and open your project. Go to Assets > Import Package > Custom Package.
3. **Locate and Select the .unitypackage:** Navigate to the location where you saved the Best HTTP.unitypackage file. Select it and click Open.
4. **Review and Import:** Unity will show you a list of all the assets contained in the package. Make sure all assets are selected and click Import.
5. **Confirmation:** After importing, you should see all the Best HTTP assets added to your project's Asset folder, confirming a successful installation.

!!! Note
    Best HTTP also supports other installation methods as documented in Unity's manual for packages. 
    For more advanced installation options, refer to the Unity Manual on [Sharing Packages](https://docs.unity3d.com/Manual/cus-share.html).

### Assembly Definitions and Runtime References
For developers familiar with Unity's development patterns, it's essential to understand how Best HTTP incorporates Unity's systems:

- **Assembly Definition Files:** Best HTTP incorporates [Unity's Assembly Definition files](https://docs.unity3d.com/Manual/ScriptCompilationAssemblyDefinitionFiles.html). It aids in organizing and managing the codebase efficiently.
- **Auto-Referencing of Runtime DLLs:** The runtime DLLs produced by Best HTTP are [Auto Referenced](https://docs.unity3d.com/Manual/class-AssemblyDefinitionImporter.html), allowing Unity to automatically recognize and utilize them without manual intervention.
- **Manual Package Referencing:** Should you need to reference Best HTTP manually in your project (for advanced setups or specific use cases), you can do so. Simply [reference the package](https://docs.unity3d.com/Manual/ScriptCompilationAssemblyDefinitionFiles.html#reference-another-assembly) by searching for `com.Tivadar.Best.HTTP`.

That's it! You've now successfully installed Best HTTP in your Unity project. Dive into the [Getting Started guide](getting-started/index.md) to begin your journey with Best HTTP.

If you encounter any issues or need further assistance, please visit our [Community and Support page](../Shared/support.md).