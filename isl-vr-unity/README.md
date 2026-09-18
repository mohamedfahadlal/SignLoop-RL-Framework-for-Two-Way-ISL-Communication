# isl-vr-unity: SignLoop Unity 6.3 LTS VR Project

This directory contains the Unity 6.3 LTS Virtual Reality project for **SignLoop**, targeted for the **Meta Quest 3 (Horizon OS)**.

## Project Structure
```text
isl-vr-unity/
├── Assets/
│   ├── Animations/
│   │   └── ISLClips/         # Authentic 30-frame ISL gesture clips (.json)
│   ├── Resources/
│   │   └── ISLClips/         # Dynamic runtime clip library (.json)
│   ├── Materials/            # Avatar, environment, and UI PBR materials
│   ├── Models/               # Rigged 3D avatars (52 ARKit blendshapes, 21-DOF hand armatures - LFS tracked)
│   │   └── AvatarPrototype/  # 54k prototype humanoid with 52 ARKit shapes & 21-joint hands
│   ├── Prefabs/              # Avatar rigs, XR Origin rigs, UI components
│   ├── Scenes/               # VR interaction scenes (MainCommunicationScene, CalibrationScene)
│   └── Scripts/
│       ├── Avatar/
│       │   ├── ISLSignPlayer.cs       # 3-phase sign player (Active -> Hold -> Return)
│       │   ├── ARKitFaceController.cs # 52 ARKit blendshapes (0 GC allocations)
│       │   └── ARKitBlendShape.cs     # Canonical blendshape enum
│       ├── Rigging/
│       │   ├── DesktopGestureTester.cs# Interactive desktop test harness (Speed, Curls, Orbit)
│       │   ├── HandPoseController.cs  # 21-joint finger pose interpolation (mirrored axes)
│       │   ├── ArmIKController.cs     # Analytical TwoBoneIK solver & forearm alignment
│       │   ├── AvatarRigWrapper.cs    # Prefab wrapper decoupling models from rigs
│       │   └── AvatarBoneMapping.cs   # Skeleton auto-population & bone binding
│       ├── Audio/            # whisper.cpp (STT) hooks & Piper TTS playback
│       ├── Bridge/           # IPC / Socket bridge communicating with Python ML policy
│       └── VR/               # XR input, Quest 3 hand tracking / controller interactions
├── Packages/
│   └── manifest.json         # Package dependencies (Animation Rigging, OpenXR, Input System)
└── ProjectSettings/          # Serialized settings (Force Text, Visible Meta Files)
```

## Setup Instructions
1. Open Unity Hub.
2. Click **Add** > **Add project from disk** and select this directory (`isl-vr-unity`).
3. Ensure Unity Editor version **6000.x (Unity 6 / 6.3 LTS)** is selected.
4. Set Build Target to **Android** (Meta Quest 3).
5. In **Project Settings > XR Plugin Management**, ensure **OpenXR** is enabled with the **Meta Quest Support** feature group active.
6. Verify under **Edit > Project Settings > Editor**:
   - Asset Serialization: **Force Text**
   - Version Control: **Visible Meta Files**

## Desktop Testing in Unity Editor (No VR Headset Required)
When testing in the Editor on desktop:
1. Press the **Play** button in the Unity Editor.
2. In the **Game** tab:
   - Ensure the toolbar **Scale** is set to **1x** (not zoomed).
   - Uncheck **Low Resolution Aspect Ratios** to guarantee full HD rendering.
3. The **SignLoop ISL Motion Studio** GUI provides:
   - **Authentic ISL Sign Playback:** Click any sign button (`Hello`, `ThankYou`, `House`, `Doctor`, etc.) to trigger 3-phase playback (Active Trajectory $\to$ Posture Hold $\to$ Smooth Return).
   - **Speed Pacing Controls:** Adjust the slider from `0.25x` to `2.0x` or use one-click presets (`0.5x Slow`, `0.75x`, `1.0x`, `1.5x`).
   - **Static Handshapes (Keys 1–8):** Test canonical postures (`Neutral`, `OpenPalm`, `Fist`, `PointIndex`, `ThumbUp`, `Victory`, `CHand`, `OHand`).
   - **Thumb Up Orientation Toggle:** Click **"Thumb Up: Flip 180°"** to verify skyward vs inverted handshake orientation.
   - **Finger Curl Multiplier:** Scale finger curl angles (`0.5x` to `2.5x`) to fine-tune posture expressiveness.
   - **Mouse Orbit & Zoom:** Hold **Right-Click** and drag to orbit camera around the avatar; use the **Scroll Wheel** to zoom directly into hands and face.
