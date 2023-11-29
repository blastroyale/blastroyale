# Best SignalR

Welcome to the Best SignalR Documentation! Best SignalR is a leading Unity networking library, engineered for seamless integration of the [SignalR technology](https://learn.microsoft.com/en-us/aspnet/core/signalr/introduction). 
Perfect for dynamic real-time experiences like online multiplayer games, chat systems, and live interactive dashboards.

!!! Warning "**Dependency Alert**"
    Best SignalR depends on both the **Best HTTP** and **Best WebSockets** packages! 
    Ensure you've installed and configured both these packages in your Unity project before diving into Best SignalR. 
    Explore more about the [installation of Best HTTP](../HTTP/installation.md) and [Best WebSockets](../WebSockets/installation.md).

!!! Warning
	Please be aware that Best SignalR provided here is a **client-side implementation**. If you're aiming to set up the server-side or need server-specific details, please refer to the Official SignalR Server Installation Guide.

	[Official SignalR Server Installation Guide](https://learn.microsoft.com/en-us/aspnet/core/signalr/introduction)
	
	Make sure you're implementing the appropriate component for your requirements!

## Overview
In the modern digital realm, real-time interactions have become a cornerstone for a multitude of applications.
From real-time game updates to live chat systems, the importance of instantaneous communication cannot be overstated. 

**SignalR** is a groundbreaking technology developed by Microsoft. It facilitates adding real-time web functionality to applications.
Unlike traditional request-response models, SignalR allows the server to push content to connected clients instantly as it becomes available.
This ensures that the application is always updated in real time. 
SignalR provides a simple API for creating server-to-client remote procedure calls (RPC) that calls functions in client applications from server-side .NET core code. 
Moreover, SignalR takes care of all the complexities involved in real-time communications, such as connection management, data serialization, and many more.

Best SignalR simplifies the process of integrating this innovative technology into your Unity projects.
It's tailored to ensure robust and efficient bi-directional communication, providing your users with a dynamic and responsive application experience.

## Key Features
- **Supported Unity Versions:** Best SignalR is compatible with Unity versions starting from :fontawesome-brands-unity: **2021.1 onwards**.
- **Compatibility with SignalR:** Best SignalR is fully aligned with the latest version of SignalR, equipping you with the forefront of real-time communication tools.
- **Cross-Platform Mastery:** Best SignalR seamlessly operates across a wide range of Unity-supported platforms, ensuring its applicability for diverse development projects. Specifically, it supports:

    - :fontawesome-solid-desktop: **Desktop:** Windows, Linux, MacOS
    - :fontawesome-solid-mobile:  **Mobile:** iOS, Android
    - :material-microsoft-windows: **Universal Windows Platform (UWP)**
    - :material-web: **Web Browsers:** WebGL

    With such expansive platform coverage, Best SignalR emerges as the top choice for your diverse platform needs and audience.

- **Seamless Integration:** With user-friendly APIs and detailed documentation, weaving Best SignalR into any Unity project is a cinch.
- **Performance Optimized:** Best SignalR is crafted for peak performance, assuring minimal latency and effective data exchanges for real-time engagements.
- **WebSocket Integration:** One of SignalR's paramount features is its adept use of [WebSockets](../WebSockets/index.md).
This communication protocol ensures full-duplex channels over a single TCP connection, promoting simultaneous data transmission without re-establishing connections.
This leads to rapid and efficient communication, pivotal for real-time applications.
If WebSockets aren't availabe due to specific constraints, SignalR seamlessly falls back to Long-Polling, keeping the protocol running.
- **Event-Driven Communication:** Harness event-based real-time communication, making your applications vibrant and user-centric.
- **Auto-Reconnection:** Best SignalR adeptly handles reconnections, guaranteeing continuous user experiences even amidst unstable network scenarios.
- **Secure Communications:** With encrypted connection support, Best SignalR ensures your application data remains confidential and shielded.
- **Profiler Integration:** Unlock the potential of the in-depth [Best HTTP profiler](../Shared/profiler/index.md) integration:
    - **Memory Profiler:** Evaluate internal memory usages, enhance performance, and spot potential memory challenges.
    - **Network Profiler:** Keep a tab on your network dynamics, studying data flow, connection statuses, and more.
- **Group and Hub Extensibility:** Classify your SignalR interactions effortlessly, curating distinct groups and hubs suited to your application's landscape.
- **Effective Data Models:** With support for both [JSON and MessagePack](intermediate-topics/encoders.md) encoding, grants you both flexibility in data management and performance in decoding.
- **Debugging and Logging:** Extensive logging capabilities empower developers to delve into the nuances of the package and simplify the debugging trajectory.

## Documentation Sections
Embark on your Best SignalR odyssey:

- [Installation Guide:](installation.md) Kickstart with Best SignalR, set up the package, and optimize your Unity environment.
- [Upgrade Guide:](upgrade-guide.md) Transitioning from a prior version? Get acquainted with the newest features and smooth out your upgrade steps.
- [Getting Started:](getting-started/index.md) Initiate your SignalR exploration, grasp the basics, and align Best SignalR to your application's essence.
- [Advanced Topics:](intermediate-topics/index.md) Plunge into deeper SignalR themes, encompassing event management, group strategies, and beyond.

This documentation is designed for developers of all backgrounds and expertise. 
Whether you're new to Unity or a seasoned professional, these guides will assist you in maximizing the capabilities of Best SignalR.

Dive in now and elevate your Unity projects with superior real-time communication features using Best SignalR!

## Installation Guide

!!! Warning "Dependency Alert"
    Before installing Best SignalR, ensure you have the [Best HTTP package](../HTTP/index.md) and the [Best WebSockets package](../WebSockets/index.md) installed and set up in your Unity project. 
    If you haven't done so yet, refer to the [Best HTTP Installation Guide](../HTTP/installation.md) and the [Best WebSockets Installation Guide](../WebSockets/installation.md).

Getting started with Best SignalR demands a prior setup of both the Best HTTP and Best WebSockets packages. After ensuring these are properly integrated, you can then effortlessly add Best SignalR to your Unity projects.

### Installing from the Unity Asset Store using the Package Manager Window

1. **Purchase:** If you haven't previously purchased the package, proceed to do so. 
    Once purchased, Unity will recognize your purchase, and you can install the package directly from within the Unity Editor. If you already own the package, you can skip these steps.
    1. **Visit the Unity Asset Store:** Navigate to the [Unity Asset Store](https://assetstore.unity.com/publishers/4137?aid=1101lfX8E) using your web browser.
    2. **Search for Best SignalR:** Locate and choose the official Best SignalR package.
    3. **Buy Best SignalR:** By clicking on the `Buy Now` button go though the purchase process.
2. **Open Unity & Access the Package Manager:** Start Unity and select your project. Head to [Window > Package Manager](https://docs.unity3d.com/Manual/upm-ui.html).
3. **Select 'My Assets':** In the Package Manager, switch to the [My Assets](https://docs.unity3d.com/Manual/upm-ui-import.html) tab to view all accessible assets.
4. **Find Best SignalR and Download:** Scroll to find "Best SignalR". Click to view its details. If it isn't downloaded, you'll notice a Download button. Click and wait. After downloading, this button will change to Import.
5. **Import the Package:** Once downloaded, click the Import button. Unity will display all Best SignalR' assets. Ensure all are selected and click Import.
6. **Confirmation:** After the import, Best SignalR will integrate into your project, signaling a successful installation.

### Installing from a .unitypackage file

If you have a .unitypackage file for Best SignalR, follow these steps:

1. **Download the .unitypackage:** Make sure the Best SignalR.unitypackage file is saved on your device. 
2. **Import into Unity:** Open Unity and your project. Go to Assets > Import Package > Custom Package.
3. **Locate and Select the .unitypackage:** Find where you saved the Best SignalR.unitypackage file, select it, and click Open.
4. **Review and Import:** Unity will show a list of all the package's assets. Ensure all assets are selected and click Import.
5. **Confirmation:** Post import, you'll see all the Best SignalR assets in your project's Asset folder, indicating a successful setup.

!!! Note
    Best SignalR also supports other installation techniques as documented in Unity's manual for packages. 
    For more advanced installation methods, please see the Unity Manual on [Sharing Packages](https://docs.unity3d.com/Manual/cus-share.html).

### Assembly Definitions and Runtime References
For developers familiar with Unity's development patterns, it's essential to understand how Best SignalR incorporates Unity's systems:

- **Assembly Definition Files:** Best SignalR incorporates [Unity's Assembly Definition files](https://docs.unity3d.com/Manual/ScriptCompilationAssemblyDefinitionFiles.html). It aids in organizing and managing the codebase efficiently.
- **Auto-Referencing of Runtime DLLs:** The runtime DLLs produced by Best SignalR are [Auto Referenced](https://docs.unity3d.com/Manual/class-AssemblyDefinitionImporter.html), allowing Unity to automatically recognize and utilize them without manual intervention.
- **Manual Package Referencing:** Should you need to reference Best SignalR manually in your project (for advanced setups or specific use cases), you can do so. 
Simply [reference the package](https://docs.unity3d.com/Manual/ScriptCompilationAssemblyDefinitionFiles.html#reference-another-assembly) by searching for `com.Tivadar.Best.SignalR`.

Congratulations! You've successfully integrated Best SignalR into your Unity project. Begin your SignalR adventure with the [Getting Started guide](getting-started/index.md).

For any issues or additional assistance, please consult the [Community and Support page](../Shared/support.md).