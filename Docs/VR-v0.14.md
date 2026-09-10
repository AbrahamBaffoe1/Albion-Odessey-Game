# Albion Odyssey VR (OpenXR)

The 0.14 build adds a single OpenXR path for desktop headsets and Quest. The same campus scene, collision volumes, history panels, classes, snow and wildlife run in both modes.

## Supported setup

- Unity 6000.6.0f1 with OpenXR 1.18, XR Management 4.7, XR Interaction Toolkit 3.6 and XR Hands 1.9.
- OpenXR is assigned automatically for Standalone and Android. Controller profiles and hand-joint features are enabled in the generated XR settings.
- SteamVR works through its OpenXR runtime when SteamVR is installed and selected as the active runtime. Quest uses the Android build and its OpenXR loader.

## In-world controls

- Left thumbstick: room-scale assisted locomotion.
- Right thumbstick: 30° snap turn with a short cooldown.
- Right trigger or primary button: interact with doors, history, classes and campus objects.
- Right grip: point at a memory marker and hold to grab it; release to let go.
- Head movement: physical room-scale movement is applied to the player body and remains collision-aware.
- F8: open the existing desktop/XR settings menu. Comfort vignette and snap-turn defaults are enabled.
- Motion-controller menu button: open the XR settings panel. Thumbstick moves the focused action, trigger selects it, and menu backs out. The same input path is available in the shared-campus panel once it is opened with F5.

The XR layer hides desktop mouse movement while a headset is active, then restores keyboard/mouse controls when it is removed. It also reacquires devices after a headset pause/resume and shows a comfort warning if the frame time stays below 55 FPS.

## Build and test

```text
bash Tools/unity_mac.sh build
ODYSSEY_QUEST_BUILD_PATH="Builds/Albion Odyssey-Quest.apk" bash Tools/unity_mac.sh quest
```

The Quest command requires Android Build Support, SDK, NDK and OpenJDK in Unity Hub. Sideload the APK to a Quest, enable developer mode, and launch it from the headset. For SteamVR, start SteamVR with an OpenXR-capable headset, then launch the macOS/desktop player on a supported host.

The project can be validated without a headset. A physical Quest or SteamVR headset is still required for final tracking, comfort and recovery sign-off; no headset is connected to the development machine used for this build.
