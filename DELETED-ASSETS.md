# Deleted assets 
## Photon Quantum
Removing all dlls and code from Photon Quantum to avoid infringing their license.
You can download their 2.1.9 SDK from [Here](https://doc.photonengine.com/quantum/v2/getting-started/initial-setup) and following the procedures to install in the project, we use a splitted project setup where the simulation is inside Quantum/quantum_code, and the compiled dll is imported inside the game. 
The deleted files are:
- Quantum/quantum_code/quantum.console.runner/ 
- Quantum/quantum_code/quantum.console.spectator/
- BlastRoyale/Assets/Libs/Photon/ 
- Quantum/assemblies/
- Quantum/tools/
You can find these files in the downloaded SDK zip.

## Quantum Server
These files are not publically available, to get access to those files you need an enterprise contract with Photon, but they are not needed to run the game, only for server side rewards.
- Quantum/quantum_server/assemblies/
- Quantum/quantum_server/PhotonServer/

## Other Libraries
- Deleted `Packages/Bakery` [Found here](https://assetstore.unity.com/packages/tools/level-design/bakery-gpu-lightmapper-122218?srsltid=AfmBOoo9yjg3KLJ_5y1Wk1FEzcDQbVZz9RqV0ytUqFpTB-XKpDHR3-fa)

- Deleted `Packages/PlayFabSDK` [Found here](https://github.com/PlayFab/UnitySDK?tab=readme-ov-file)
- Deleted `Packages/Singular` [Found here](https://support.singular.net/hc/en-us/articles/360037635452-Unity-Package-Manager-SDK-Integration-Guide)
- Deleted `Packages/Animancer` [Found here](https://assetstore.unity.com/packages/tools/animation/animancer-lite-v8-293524?srsltid=AfmBOora_hyqDuz-QbZMr2Y-IU2BZsJ_DrHGQ7Sb-gSPFEKaNtS3t_40)
- Deleted `Packages/Demigiant` this is dotween pro [found here](https://assetstore.unity.com/packages/tools/visual-scripting/dotween-pro-32416?srsltid=AfmBOor0iugY0Lu9K_kygDnVYCosuSeU9-iG4zEbYHzZvZFt9CK-rBNd) 
- Deleted `Packages/I2` [found here](https://assetstore.unity.com/packages/tools/localization/i2-localization-14884) 
- Deleted `Packages/ParrelSync` [found here](https://github.com/VeriorPies/ParrelSync) 
- Deleted `Packages/PlayerPrefsEditor` [found here](https://assetstore.unity.com/packages/tools/utilities/playerprefs-editor-167903?srsltid=AfmBOopTFSrfYJhwN2uNTPfeujje_U1_jXgmrQ9u-j_rCO88RUxz8BqN) 
- Deleted `Packages/StompyRobot` this is SRDebugger [found here](https://assetstore.unity.com/packages/tools/gui/srdebugger-console-tools-on-device-27688?srsltid=AfmBOopnrU9rk0RX-tRqfz1to-E1PMbJ8lbj2enTlv6gL6R9HEN_D5Oq) 
- Deleted `Packages/UITKTimeline` this is SRDebugger [found here](https://github.com/mihakrajnc/UITTimeline) 
- Deleted `Assets/Plugins/Sirenix/` this is OdinInspector [found here](https://odininspector.com/) 
- Deleted `Assets/Libs/NiceVibrations/` [found here](https://github.com/Lofelt/NiceVibrations) 
- Deleted `Assets/FacebookSDK/` [found here](https://developers.facebook.com/docs/unity/) 
- Deleted `Assets/Plugins/VoxelBusters/` this is EssentialKit [found here](https://assetstore.unity.com/packages/tools/integration/essential-kit-v3-iap-leaderboards-cloud-save-notifications-galle-301752?aid=1100lK2e) 

- Deleted `Packages/com.google.external-dependency-manager-1.2.179.tgz` [Found here](https://developers.google.com/unity/archive)
- Deleted `Packages/com.google.firebase.analytics-11.9.0.tgz`[Found here](https://developers.google.com/unity/archive)
- Deleted `Packages/com.google.firebase.app-11.9.0.tgz` [Found here](https://developers.google.com/unity/archive)

## Asset Packs
- Deleted `Assets/Art/AssetPacks/PolygonApocalypse`
- Deleted `Assets/Art/AssetPacks/PolygonBattleRoyale`
- Deleted `Assets/Art/AssetPacks/PolygonConstruction`
- Deleted `Assets/Art/AssetPacks/PolygonNature`
- Deleted `Assets/Art/AssetPacks/PolygonSciFiCity`
- Deleted `Assets/Art/AssetPacks/SimpleFarm`
- Deleted `Assets/Art/AssetPacks/SimpleMilitary`
- Deleted `Assets/Art/AssetPacks/SimplePort`
- Deleted `Assets/Art/AssetPacks/SimpleProps`
- Deleted `Assets/Art/AssetPacks/SimpleTemple`
- Deleted `Assets/Art/AssetPacks/SimpleTown`
- Deleted `Assets/Art/AssetPacks/SimpleTownLite`
- Deleted `Assets/Art/AssetPacks/SimpleTrains`

# Secrets

The client ids and secrets were removed from the code history. To make the game run you need to configure the values in `BlastRoyale\Assets\Src\FirstLight\Game\Utils\FLEnvironment.cs`.
