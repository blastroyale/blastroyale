Best WebSockets is a premier networking library for Unity, tailored specifically for seamless WebSocket integration. 
It's perfect for applications that require real-time, bi-directional communication such as chat applications, multiplayer games, and live interactive systems.

Warning! **Dependency Alert**

    Best WebSockets relies on the Best HTTP package!
    Ensure you have it installed and set up in your Unity project before diving into Best WebSockets. Learn more about the [installation of Best HTTP](https://bestdocshub.pages.dev/HTTP//HTTP/installation.md).

## Overview
In the fast-paced digital landscape, real-time communication is crucial for a multitude of applications. 
Whether it's sending instantaneous game state updates, chat messages, or receiving live feeds, WebSockets provide an edge in facilitating these real-time interactions. 
Best WebSockets is crafted to effortlessly integrate this technology into your Unity projects, making bi-directional communication straightforward and efficient.

## Key Features
- **Supported Unity Versions:** Best WebSockets is compatible with Unity versions starting from :fontawesome-brands-unity: **2021.1 onwards**.
- **Cross-Platform:** Best WebSockets seamlessly operates across a wide variety of Unity platforms, ensuring its applicability for diverse development projects. Specifically, it supports:
    
    - :fontawesome-solid-desktop: **Desktop:** Windows, Linux, MacOS
    - :fontawesome-solid-mobile:  **Mobile:** iOS, Android
    - :material-microsoft-windows: **Universal Windows Platform (UWP)**
    - :material-web: **Web Browsers:** WebGL

    This vast platform compatibility assures that Best WebSockets is an excellent choice for any project, regardless of your target platform or audience.

- **Persistent Connections:** Unlike traditional request-response communication, WebSockets offer a persistent, low-latency connection that's perfect for applications that need instant communication.
- **Binary and Text Data:** Whether you're sending textual messages or binary data like images and files, Best WebSockets is equipped to handle both with ease.
- **Secure Communication:** With support for WSS:// (WebSocket over TLS), your application's data remains secure and encrypted.
- **Built-In Profiler Support:** To ensure peak performance and help debug potential issues, Best WebSockets integrates with the base [Best HTTP profiler](https://bestdocshub.pages.dev/Shared/profiler/index.md):
    - **Memory Profiler:** Examine the library's internal memory usage, helping identify potential bottlenecks or memory leaks.
    - **Network Profiler:** Delve deep into your network operations, monitoring data transfers, open and closed connections, and much more.
- **Custom Protocols:** Easily extend and adapt your WebSocket communication with custom protocols, allowing for versatile application-specific communication styles.
- **Compression:** Data compression capabilities ensure efficient bandwidth usage, leading to faster data transfers and reduced latency.

## Documentation Sections
Delve into the details and start using Best WebSockets in your projects:

- [Installation Guide:](https://bestdocshub.pages.dev/WebSockets/installation.md) Kick off with Best WebSockets by setting up the package and configuring your Unity project.
- [Upgrade Guide:](https://bestdocshub.pages.dev/WebSockets/upgrade-guide.md) Transitioning from an earlier version? Find out the latest improvements and how to smoothly upgrade to the most recent release.
- [Getting Started:](https://bestdocshub.pages.dev/WebSockets/getting-started/index.md) Embark on your WebSocket journey, understand the basics, and configure Best WebSockets tailored to your application's needs.
- [Advanced Topics:](https://bestdocshub.pages.dev/WebSockets/intermediate-topics/index.md) Enhance your knowledge with deeper insights into WebSocket topics, like custom protocols, security, and more.

This documentation is designed for developers of all backgrounds and expertise. 
Whether you're new to Unity or a seasoned professional, these guides will assist you in maximizing the capabilities of Best WebSockets.

Dive in now and elevate your Unity projects with superior real-time communication features using Best WebSockets!

## Installation Guide

!!! Warning "Dependency Alert"
    Before installing Best WebSockets, ensure you have the [Best HTTP package](../HTTP/index.md) installed and set up in your Unity project. If you haven't done so yet, refer to the [Best HTTP Installation Guide](../HTTP/installation.md).

Getting started with Best WebSockets requires a prior installation of the Best HTTP package. Once Best HTTP is set up, integrating Best WebSockets into your Unity project is a breeze.

### Installing from the Unity Asset Store using the Package Manager Window

1. **Purchase:** If you haven't previously purchased the package, proceed to do so. Once purchased, Unity will recognize your purchase, and you can install the package directly from within the Unity Editor. If you already own the package, you can skip these steps.
    1. **Visit the Unity Asset Store:** Navigate to the [Unity Asset Store](https://assetstore.unity.com/publishers/4137?aid=1101lfX8E) using your web browser.
    2. **Search for Best WebSockets:** Locate and choose the official Best WebSockets package.
    3. **Buy Best WebSockets:** By clicking on the `Buy Now` button go though the purchase process.
2. **Open Unity & Access the Package Manager:** Start Unity and select your project. Head to [Window > Package Manager](https://docs.unity3d.com/Manual/upm-ui.html).
3. **Select 'My Assets':** In the Package Manager, switch to the [My Assets](https://docs.unity3d.com/Manual/upm-ui-import.html) tab to view all accessible assets.
4. **Find Best WebSockets and Download:** Scroll to find "Best WebSockets". Click to view its details. If it isn't downloaded, you'll notice a Download button. Click and wait. After downloading, this button will change to Import.
5. **Import the Package:** Once downloaded, click the Import button. Unity will display all Best WebSockets' assets. Ensure all are selected and click Import.
6. **Confirmation:** After the import, Best WebSockets will integrate into your project, signaling a successful installation.

### Installing from a .unitypackage file

If you have a .unitypackage file for Best WebSockets, follow these steps:

1. **Download the .unitypackage:** Make sure the Best WebSockets.unitypackage file is saved on your device. 
2. **Import into Unity:** Open Unity and your project. Go to Assets > Import Package > Custom Package.
3. **Locate and Select the .unitypackage:** Find where you saved the Best WebSockets.unitypackage file, select it, and click Open.
4. **Review and Import:** Unity will show a list of all the package's assets. Ensure all assets are selected and click Import.
5. **Confirmation:** Post import, you'll see all the Best WebSockets assets in your project's Asset folder, indicating a successful setup.

!!! Note
    Best WebSockets also supports other installation techniques as documented in Unity's manual for packages. For more advanced installation methods, please see the Unity Manual on [Sharing Packages](https://docs.unity3d.com/Manual/cus-share.html).

### Assembly Definitions and Runtime References
For developers familiar with Unity's development patterns, it's essential to understand how Best WebSockets incorporates Unity's systems:

- **Assembly Definition Files:** Best WebSockets incorporates [Unity's Assembly Definition files](https://docs.unity3d.com/Manual/ScriptCompilationAssemblyDefinitionFiles.html). It aids in organizing and managing the codebase efficiently.
- **Auto-Referencing of Runtime DLLs:** The runtime DLLs produced by Best WebSockets are [Auto Referenced](https://docs.unity3d.com/Manual/class-AssemblyDefinitionImporter.html), allowing Unity to automatically recognize and utilize them without manual intervention.
- **Manual Package Referencing:** Should you need to reference Best WebSockets manually in your project (for advanced setups or specific use cases), you can do so. Simply [reference the package](https://docs.unity3d.com/Manual/ScriptCompilationAssemblyDefinitionFiles.html#reference-another-assembly) by searching for `com.Tivadar.Best.WebSockets`.

Congratulations! You've successfully integrated Best WebSockets into your Unity project. Begin your WebSocket adventure with the [Getting Started guide](getting-started/index.md).

For any issues or additional assistance, please consult the [Community and Support page](../Shared/support.md).